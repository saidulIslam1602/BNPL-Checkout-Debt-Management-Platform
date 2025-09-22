# Riverty BNPL Platform - Azure Infrastructure
# Production-ready Terraform configuration for Norwegian BNPL platform

terraform {
  required_version = ">= 1.0"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.80"
    }
    azuread = {
      source  = "hashicorp/azuread"
      version = "~> 2.45"
    }
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = "~> 2.23"
    }
    helm = {
      source  = "hashicorp/helm"
      version = "~> 2.11"
    }
  }

  # Backend configuration for state management
  backend "azurerm" {
    resource_group_name  = "riverty-terraform-state"
    storage_account_name = "rivertyterraformstate"
    container_name       = "tfstate"
    key                  = "riverty-bnpl.tfstate"
  }
}

# Configure the Azure Provider
provider "azurerm" {
  features {
    key_vault {
      purge_soft_delete_on_destroy    = true
      recover_soft_deleted_key_vaults = true
    }
    resource_group {
      prevent_deletion_if_contains_resources = false
    }
  }
}

provider "azuread" {}

# Data sources
data "azurerm_client_config" "current" {}

data "azuread_client_config" "current" {}

# Local variables
locals {
  # Norwegian-specific configuration
  location            = "Norway East"
  location_short      = "noe"
  environment         = var.environment
  project_name        = "riverty-bnpl"
  
  # Common tags for all resources
  common_tags = {
    Project             = "Riverty BNPL"
    Environment         = var.environment
    Owner               = "Riverty Platform Team"
    Country             = "Norway"
    Compliance          = "PSD2,GDPR"
    CostCenter          = "Engineering"
    DataClassification  = "Confidential"
    BackupRequired      = "Yes"
    MonitoringRequired  = "Yes"
    CreatedBy           = "Terraform"
    CreatedDate         = formatdate("YYYY-MM-DD", timestamp())
  }

  # Resource naming convention
  resource_prefix = "${local.project_name}-${local.environment}-${local.location_short}"
  
  # Norwegian compliance requirements
  data_residency_locations = ["Norway East", "Norway West"]
  
  # Network configuration
  vnet_address_space     = ["10.0.0.0/16"]
  aks_subnet_cidr        = "10.0.1.0/24"
  app_gateway_subnet_cidr = "10.0.2.0/24"
  private_endpoint_subnet_cidr = "10.0.3.0/24"
  
  # AKS configuration
  aks_node_count         = var.environment == "production" ? 3 : 2
  aks_node_vm_size       = var.environment == "production" ? "Standard_D4s_v3" : "Standard_D2s_v3"
  aks_max_node_count     = var.environment == "production" ? 10 : 5
  
  # Database configuration
  sql_server_version     = "12.0"
  sql_database_sku       = var.environment == "production" ? "S2" : "S1"
  
  # Norwegian regulatory compliance
  backup_retention_days  = 2555 # 7 years as per Norwegian financial regulations
  log_retention_days     = 2555 # 7 years for audit compliance
}

# Resource Group
resource "azurerm_resource_group" "main" {
  name     = "${local.resource_prefix}-rg"
  location = local.location
  tags     = local.common_tags
}

# Virtual Network
resource "azurerm_virtual_network" "main" {
  name                = "${local.resource_prefix}-vnet"
  address_space       = local.vnet_address_space
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  tags                = local.common_tags
}

# Subnets
resource "azurerm_subnet" "aks" {
  name                 = "${local.resource_prefix}-aks-subnet"
  resource_group_name  = azurerm_resource_group.main.name
  virtual_network_name = azurerm_virtual_network.main.name
  address_prefixes     = [local.aks_subnet_cidr]
}

resource "azurerm_subnet" "app_gateway" {
  name                 = "${local.resource_prefix}-appgw-subnet"
  resource_group_name  = azurerm_resource_group.main.name
  virtual_network_name = azurerm_virtual_network.main.name
  address_prefixes     = [local.app_gateway_subnet_cidr]
}

resource "azurerm_subnet" "private_endpoints" {
  name                 = "${local.resource_prefix}-pe-subnet"
  resource_group_name  = azurerm_resource_group.main.name
  virtual_network_name = azurerm_virtual_network.main.name
  address_prefixes     = [local.private_endpoint_subnet_cidr]
}

# Network Security Groups
resource "azurerm_network_security_group" "aks" {
  name                = "${local.resource_prefix}-aks-nsg"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  tags                = local.common_tags

  # Allow HTTPS traffic
  security_rule {
    name                       = "AllowHTTPS"
    priority                   = 1001
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_range     = "443"
    source_address_prefix      = "*"
    destination_address_prefix = "*"
  }

  # Allow HTTP traffic (for health checks)
  security_rule {
    name                       = "AllowHTTP"
    priority                   = 1002
    direction                  = "Inbound"
    access                     = "Allow"
    protocol                   = "Tcp"
    source_port_range          = "*"
    destination_port_range     = "80"
    source_address_prefix      = "*"
    destination_address_prefix = "*"
  }

  # Deny all other inbound traffic
  security_rule {
    name                       = "DenyAllInbound"
    priority                   = 4096
    direction                  = "Inbound"
    access                     = "Deny"
    protocol                   = "*"
    source_port_range          = "*"
    destination_port_range     = "*"
    source_address_prefix      = "*"
    destination_address_prefix = "*"
  }
}

# Associate NSG with AKS subnet
resource "azurerm_subnet_network_security_group_association" "aks" {
  subnet_id                 = azurerm_subnet.aks.id
  network_security_group_id = azurerm_network_security_group.aks.id
}

# Log Analytics Workspace for monitoring
resource "azurerm_log_analytics_workspace" "main" {
  name                = "${local.resource_prefix}-law"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  sku                 = "PerGB2018"
  retention_in_days   = local.log_retention_days
  tags                = local.common_tags

  # Norwegian data residency compliance
  daily_quota_gb = var.environment == "production" ? 50 : 10
}

# Application Insights
resource "azurerm_application_insights" "main" {
  name                = "${local.resource_prefix}-ai"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  workspace_id        = azurerm_log_analytics_workspace.main.id
  application_type    = "web"
  retention_in_days   = local.log_retention_days
  tags                = local.common_tags

  # Norwegian compliance settings
  daily_data_cap_in_gb                  = var.environment == "production" ? 100 : 20
  daily_data_cap_notifications_disabled = false
  sampling_percentage                   = var.environment == "production" ? 100 : 50
}

# Azure Key Vault for secrets management
resource "azurerm_key_vault" "main" {
  name                       = "${local.resource_prefix}-kv"
  location                   = azurerm_resource_group.main.location
  resource_group_name        = azurerm_resource_group.main.name
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "premium" # Premium for HSM support
  soft_delete_retention_days = 90
  purge_protection_enabled   = var.environment == "production" ? true : false
  tags                       = local.common_tags

  # Norwegian compliance access policies
  access_policy {
    tenant_id = data.azurerm_client_config.current.tenant_id
    object_id = data.azurerm_client_config.current.object_id

    key_permissions = [
      "Backup", "Create", "Decrypt", "Delete", "Encrypt", "Get", "Import",
      "List", "Purge", "Recover", "Restore", "Sign", "UnwrapKey", "Update",
      "Verify", "WrapKey", "Release", "Rotate", "GetRotationPolicy", "SetRotationPolicy"
    ]

    secret_permissions = [
      "Backup", "Delete", "Get", "List", "Purge", "Recover", "Restore", "Set"
    ]

    certificate_permissions = [
      "Backup", "Create", "Delete", "DeleteIssuers", "Get", "GetIssuers",
      "Import", "List", "ListIssuers", "ManageContacts", "ManageIssuers",
      "Purge", "Recover", "Restore", "SetIssuers", "Update"
    ]
  }

  # Network access restrictions for Norwegian compliance
  network_acls {
    default_action = "Deny"
    bypass         = "AzureServices"
    
    # Allow access from AKS subnet
    virtual_network_subnet_ids = [
      azurerm_subnet.aks.id,
      azurerm_subnet.private_endpoints.id
    ]
  }
}

# Azure Container Registry
resource "azurerm_container_registry" "main" {
  name                = "${replace(local.resource_prefix, "-", "")}acr"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  sku                 = "Premium" # Premium for geo-replication and security
  admin_enabled       = false
  tags                = local.common_tags

  # Norwegian data residency
  georeplications {
    location                = "Norway West"
    zone_redundancy_enabled = true
    tags                    = local.common_tags
  }

  # Security scanning
  trust_policy {
    enabled = true
  }

  retention_policy {
    enabled = true
    days    = 30
  }

  # Network access restrictions
  network_rule_set {
    default_action = "Deny"
    
    virtual_network {
      action    = "Allow"
      subnet_id = azurerm_subnet.aks.id
    }
  }
}

# Azure Kubernetes Service (AKS)
resource "azurerm_kubernetes_cluster" "main" {
  name                = "${local.resource_prefix}-aks"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  dns_prefix          = "${local.resource_prefix}-aks"
  kubernetes_version  = var.kubernetes_version
  tags                = local.common_tags

  # Norwegian compliance - private cluster
  private_cluster_enabled             = var.environment == "production" ? true : false
  private_dns_zone_id                = var.environment == "production" ? "System" : null
  private_cluster_public_fqdn_enabled = false

  default_node_pool {
    name                = "system"
    node_count          = local.aks_node_count
    vm_size             = local.aks_node_vm_size
    vnet_subnet_id      = azurerm_subnet.aks.id
    type                = "VirtualMachineScaleSets"
    availability_zones  = ["1", "2", "3"]
    max_pods            = 30
    os_disk_size_gb     = 128
    os_disk_type        = "Premium_LRS"
    
    # Auto-scaling
    enable_auto_scaling = true
    min_count          = local.aks_node_count
    max_count          = local.aks_max_node_count

    # Norwegian compliance
    only_critical_addons_enabled = true
    
    upgrade_settings {
      max_surge = "10%"
    }

    tags = local.common_tags
  }

  # Identity configuration
  identity {
    type = "SystemAssigned"
  }

  # Network configuration
  network_profile {
    network_plugin    = "azure"
    network_policy    = "azure"
    dns_service_ip    = "10.0.0.10"
    service_cidr      = "10.0.0.0/16"
    load_balancer_sku = "standard"
  }

  # Monitoring and logging
  oms_agent {
    log_analytics_workspace_id = azurerm_log_analytics_workspace.main.id
  }

  # Azure AD integration
  azure_active_directory_role_based_access_control {
    managed                = true
    admin_group_object_ids = var.aks_admin_group_object_ids
    azure_rbac_enabled     = true
  }

  # Security features
  auto_scaler_profile {
    balance_similar_node_groups      = false
    expander                        = "random"
    max_graceful_termination_sec    = "600"
    max_node_provisioning_time      = "15m"
    max_unready_nodes              = 3
    max_unready_percentage         = 45
    new_pod_scale_up_delay         = "10s"
    scale_down_delay_after_add     = "10m"
    scale_down_delay_after_delete  = "10s"
    scale_down_delay_after_failure = "3m"
    scan_interval                  = "10s"
    scale_down_threshold           = "0.5"
    scale_down_unneeded_time       = "10m"
    scale_down_utilization_threshold = "0.5"
    empty_bulk_delete_max          = "10"
    skip_nodes_with_local_storage  = true
    skip_nodes_with_system_pods    = true
  }

  # Norwegian regulatory compliance
  key_vault_secrets_provider {
    secret_rotation_enabled  = true
    secret_rotation_interval = "2m"
  }

  # Workload identity for secure access to Azure services
  workload_identity_enabled = true
  oidc_issuer_enabled      = true

  lifecycle {
    ignore_changes = [
      default_node_pool[0].node_count
    ]
  }
}

# Additional node pool for application workloads
resource "azurerm_kubernetes_cluster_node_pool" "apps" {
  name                  = "apps"
  kubernetes_cluster_id = azurerm_kubernetes_cluster.main.id
  vm_size              = var.environment == "production" ? "Standard_D8s_v3" : "Standard_D4s_v3"
  node_count           = var.environment == "production" ? 2 : 1
  availability_zones   = ["1", "2", "3"]
  max_pods             = 50
  os_disk_size_gb      = 128
  os_disk_type         = "Premium_LRS"
  vnet_subnet_id       = azurerm_subnet.aks.id

  # Auto-scaling
  enable_auto_scaling = true
  min_count          = var.environment == "production" ? 2 : 1
  max_count          = var.environment == "production" ? 20 : 10

  # Node taints for application workloads
  node_taints = ["workload=apps:NoSchedule"]

  upgrade_settings {
    max_surge = "33%"
  }

  tags = merge(local.common_tags, {
    NodePool = "applications"
  })
}

# Grant AKS access to ACR
resource "azurerm_role_assignment" "aks_acr" {
  principal_id                     = azurerm_kubernetes_cluster.main.kubelet_identity[0].object_id
  role_definition_name             = "AcrPull"
  scope                           = azurerm_container_registry.main.id
  skip_service_principal_aad_check = true
}

# Grant AKS access to Key Vault
resource "azurerm_key_vault_access_policy" "aks" {
  key_vault_id = azurerm_key_vault.main.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = azurerm_kubernetes_cluster.main.key_vault_secrets_provider[0].secret_identity[0].object_id

  secret_permissions = [
    "Get", "List"
  ]
}

# SQL Server for Norwegian BNPL data
resource "azurerm_mssql_server" "main" {
  name                         = "${local.resource_prefix}-sql"
  resource_group_name          = azurerm_resource_group.main.name
  location                     = azurerm_resource_group.main.location
  version                      = local.sql_server_version
  administrator_login          = var.sql_admin_username
  administrator_login_password = var.sql_admin_password
  tags                         = local.common_tags

  # Norwegian compliance and security
  minimum_tls_version               = "1.2"
  public_network_access_enabled     = false
  outbound_network_access_restricted = true

  # Azure AD authentication
  azuread_administrator {
    login_username = var.sql_azuread_admin_login
    object_id      = var.sql_azuread_admin_object_id
  }

  # Identity for managed services
  identity {
    type = "SystemAssigned"
  }
}

# SQL Database for Payment data
resource "azurerm_mssql_database" "payment" {
  name           = "${local.resource_prefix}-payment-db"
  server_id      = azurerm_mssql_server.main.id
  collation      = "SQL_Latin1_General_CP1_CI_AS"
  license_type   = "LicenseIncluded"
  sku_name       = local.sql_database_sku
  zone_redundant = var.environment == "production" ? true : false
  tags           = local.common_tags

  # Norwegian regulatory compliance - 7 years retention
  short_term_retention_policy {
    retention_days           = 35
    backup_interval_in_hours = 12
  }

  long_term_retention_policy {
    weekly_retention  = "P12W"   # 12 weeks
    monthly_retention = "P12M"   # 12 months
    yearly_retention  = "P7Y"    # 7 years (Norwegian requirement)
    week_of_year      = 1
  }

  # Threat detection
  threat_detection_policy {
    state                      = "Enabled"
    email_account_admins       = "Enabled"
    email_addresses           = var.security_alert_emails
    retention_days            = 30
    storage_account_access_key = azurerm_storage_account.security.primary_access_key
    storage_endpoint          = azurerm_storage_account.security.primary_blob_endpoint
  }
}

# Additional databases for other services
resource "azurerm_mssql_database" "risk" {
  name           = "${local.resource_prefix}-risk-db"
  server_id      = azurerm_mssql_server.main.id
  collation      = "SQL_Latin1_General_CP1_CI_AS"
  license_type   = "LicenseIncluded"
  sku_name       = local.sql_database_sku
  zone_redundant = var.environment == "production" ? true : false
  tags           = local.common_tags

  short_term_retention_policy {
    retention_days           = 35
    backup_interval_in_hours = 12
  }

  long_term_retention_policy {
    weekly_retention  = "P12W"
    monthly_retention = "P12M"
    yearly_retention  = "P7Y"
    week_of_year      = 1
  }
}

resource "azurerm_mssql_database" "settlement" {
  name           = "${local.resource_prefix}-settlement-db"
  server_id      = azurerm_mssql_server.main.id
  collation      = "SQL_Latin1_General_CP1_CI_AS"
  license_type   = "LicenseIncluded"
  sku_name       = local.sql_database_sku
  zone_redundant = var.environment == "production" ? true : false
  tags           = local.common_tags

  short_term_retention_policy {
    retention_days           = 35
    backup_interval_in_hours = 12
  }

  long_term_retention_policy {
    weekly_retention  = "P12W"
    monthly_retention = "P12M"
    yearly_retention  = "P7Y"
    week_of_year      = 1
  }
}

# Private endpoint for SQL Server
resource "azurerm_private_endpoint" "sql" {
  name                = "${local.resource_prefix}-sql-pe"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  subnet_id           = azurerm_subnet.private_endpoints.id
  tags                = local.common_tags

  private_service_connection {
    name                           = "${local.resource_prefix}-sql-psc"
    private_connection_resource_id = azurerm_mssql_server.main.id
    subresource_names             = ["sqlServer"]
    is_manual_connection          = false
  }

  private_dns_zone_group {
    name                 = "sql-dns-zone-group"
    private_dns_zone_ids = [azurerm_private_dns_zone.sql.id]
  }
}

# Private DNS Zone for SQL Server
resource "azurerm_private_dns_zone" "sql" {
  name                = "privatelink.database.windows.net"
  resource_group_name = azurerm_resource_group.main.name
  tags                = local.common_tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "sql" {
  name                  = "${local.resource_prefix}-sql-dns-link"
  resource_group_name   = azurerm_resource_group.main.name
  private_dns_zone_name = azurerm_private_dns_zone.sql.name
  virtual_network_id    = azurerm_virtual_network.main.id
  tags                  = local.common_tags
}

# Redis Cache for session management and caching
resource "azurerm_redis_cache" "main" {
  name                = "${local.resource_prefix}-redis"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  capacity            = var.environment == "production" ? 2 : 1
  family              = var.environment == "production" ? "P" : "C"
  sku_name            = var.environment == "production" ? "Premium" : "Standard"
  enable_non_ssl_port = false
  minimum_tls_version = "1.2"
  tags                = local.common_tags

  # Norwegian compliance - data persistence
  redis_configuration {
    enable_authentication           = true
    maxmemory_reserved             = var.environment == "production" ? 200 : 50
    maxmemory_delta                = var.environment == "production" ? 200 : 50
    maxmemory_policy               = "allkeys-lru"
    rdb_backup_enabled             = var.environment == "production" ? true : false
    rdb_backup_frequency           = var.environment == "production" ? 60 : null
    rdb_backup_max_snapshot_count  = var.environment == "production" ? 5 : null
    rdb_storage_connection_string  = var.environment == "production" ? azurerm_storage_account.backup[0].primary_connection_string : null
  }

  # Network security
  public_network_access_enabled = false

  # Patch schedule for Norwegian business hours
  patch_schedule {
    day_of_week    = "Sunday"
    start_hour_utc = 2 # 3 AM Norwegian time
  }
}

# Private endpoint for Redis
resource "azurerm_private_endpoint" "redis" {
  name                = "${local.resource_prefix}-redis-pe"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  subnet_id           = azurerm_subnet.private_endpoints.id
  tags                = local.common_tags

  private_service_connection {
    name                           = "${local.resource_prefix}-redis-psc"
    private_connection_resource_id = azurerm_redis_cache.main.id
    subresource_names             = ["redisCache"]
    is_manual_connection          = false
  }

  private_dns_zone_group {
    name                 = "redis-dns-zone-group"
    private_dns_zone_ids = [azurerm_private_dns_zone.redis.id]
  }
}

# Private DNS Zone for Redis
resource "azurerm_private_dns_zone" "redis" {
  name                = "privatelink.redis.cache.windows.net"
  resource_group_name = azurerm_resource_group.main.name
  tags                = local.common_tags
}

resource "azurerm_private_dns_zone_virtual_network_link" "redis" {
  name                  = "${local.resource_prefix}-redis-dns-link"
  resource_group_name   = azurerm_resource_group.main.name
  private_dns_zone_name = azurerm_private_dns_zone.redis.name
  virtual_network_id    = azurerm_virtual_network.main.id
  tags                  = local.common_tags
}

# Service Bus for event-driven architecture
resource "azurerm_servicebus_namespace" "main" {
  name                = "${local.resource_prefix}-sb"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  sku                 = var.environment == "production" ? "Premium" : "Standard"
  capacity            = var.environment == "production" ? 1 : 0
  tags                = local.common_tags

  # Norwegian compliance
  zone_redundant                   = var.environment == "production" ? true : false
  public_network_access_enabled    = false
  minimum_tls_version              = "1.2"
  local_auth_enabled               = false # Use Azure AD only
  
  # Premium features
  dynamic "premium_messaging_partitions" {
    for_each = var.environment == "production" ? [1] : []
    content {
      count = 1
    }
  }
}

# Service Bus Topics for Norwegian BNPL events
locals {
  servicebus_topics = [
    "payment-confirmations",
    "customer-welcome", 
    "payment-plan-updates",
    "instant-settlements",
    "settlement-failures",
    "installment-paid",
    "installment-retry",
    "collections-escalation",
    "fraud-alerts",
    "risk-assessments",
    "norwegian-compliance-events"
  ]
}

resource "azurerm_servicebus_topic" "topics" {
  for_each = toset(local.servicebus_topics)
  
  name         = each.value
  namespace_id = azurerm_servicebus_namespace.main.id
  
  # Norwegian regulatory compliance - 7 years retention
  default_message_ttl               = "P7Y"
  duplicate_detection_history_time_window = "PT10M"
  enable_batched_operations         = true
  enable_express                    = false
  enable_partitioning              = var.environment == "production" ? true : false
  max_message_size_in_kilobytes    = var.environment == "production" ? 1024 : 256
  max_size_in_megabytes            = var.environment == "production" ? 5120 : 1024
  requires_duplicate_detection      = true
  support_ordering                 = true
}

# Storage Account for security logs and backups
resource "azurerm_storage_account" "security" {
  name                     = "${replace(local.resource_prefix, "-", "")}sec"
  resource_group_name      = azurerm_resource_group.main.name
  location                 = azurerm_resource_group.main.location
  account_tier             = "Standard"
  account_replication_type = var.environment == "production" ? "GRS" : "LRS"
  account_kind             = "StorageV2"
  access_tier              = "Hot"
  tags                     = local.common_tags

  # Norwegian compliance and security
  min_tls_version                 = "TLS1_2"
  allow_nested_items_to_be_public = false
  public_network_access_enabled   = false
  https_traffic_only_enabled      = true
  
  # Blob properties for compliance
  blob_properties {
    versioning_enabled       = true
    change_feed_enabled      = true
    change_feed_retention_in_days = local.log_retention_days
    last_access_time_enabled = true
    
    delete_retention_policy {
      days = local.backup_retention_days
    }
    
    container_delete_retention_policy {
      days = local.backup_retention_days
    }
  }

  # Network rules
  network_rules {
    default_action             = "Deny"
    bypass                     = ["AzureServices"]
    virtual_network_subnet_ids = [azurerm_subnet.aks.id]
  }
}

# Backup storage account (production only)
resource "azurerm_storage_account" "backup" {
  count = var.environment == "production" ? 1 : 0
  
  name                     = "${replace(local.resource_prefix, "-", "")}bak"
  resource_group_name      = azurerm_resource_group.main.name
  location                 = "Norway West" # Different region for DR
  account_tier             = "Standard"
  account_replication_type = "GRS"
  account_kind             = "StorageV2"
  access_tier              = "Cool" # Cost optimization for backups
  tags                     = local.common_tags

  min_tls_version                 = "TLS1_2"
  allow_nested_items_to_be_public = false
  public_network_access_enabled   = false
  https_traffic_only_enabled      = true

  blob_properties {
    versioning_enabled = true
    
    delete_retention_policy {
      days = local.backup_retention_days
    }
  }
}

# Function App for scheduled tasks
resource "azurerm_service_plan" "functions" {
  name                = "${local.resource_prefix}-func-plan"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  os_type             = "Linux"
  sku_name            = var.environment == "production" ? "EP1" : "Y1"
  tags                = local.common_tags
}

resource "azurerm_linux_function_app" "payment_processor" {
  name                = "${local.resource_prefix}-func"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  service_plan_id     = azurerm_service_plan.functions.id
  storage_account_name       = azurerm_storage_account.security.name
  storage_account_access_key = azurerm_storage_account.security.primary_access_key
  tags                = local.common_tags

  # Norwegian compliance
  https_only                    = true
  public_network_access_enabled = false
  
  site_config {
    minimum_tls_version = "1.2"
    application_stack {
      dotnet_version              = "8.0"
      use_dotnet_isolated_runtime = true
    }
    
    # CORS for Norwegian domains
    cors {
      allowed_origins = [
        "https://*.riverty.no",
        "https://*.riverty.com"
      ]
      support_credentials = true
    }
  }

  app_settings = {
    "FUNCTIONS_WORKER_RUNTIME"                = "dotnet-isolated"
    "AZURE_FUNCTIONS_ENVIRONMENT"            = var.environment
    "APPLICATIONINSIGHTS_CONNECTION_STRING"  = azurerm_application_insights.main.connection_string
    "ServiceBusConnection__fullyQualifiedNamespace" = "${azurerm_servicebus_namespace.main.name}.servicebus.windows.net"
    "KeyVaultUri"                            = azurerm_key_vault.main.vault_uri
    "Norwegian__TimeZone"                    = "W. Europe Standard Time"
    "Norwegian__Currency"                    = "NOK"
    "Norwegian__Locale"                      = "nb-NO"
  }

  identity {
    type = "SystemAssigned"
  }
}

# Grant Function App access to Key Vault
resource "azurerm_key_vault_access_policy" "function_app" {
  key_vault_id = azurerm_key_vault.main.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = azurerm_linux_function_app.payment_processor.identity[0].principal_id

  secret_permissions = [
    "Get", "List"
  ]
}

# Application Gateway for ingress (production only)
resource "azurerm_public_ip" "app_gateway" {
  count = var.environment == "production" ? 1 : 0
  
  name                = "${local.resource_prefix}-appgw-pip"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  allocation_method   = "Static"
  sku                 = "Standard"
  zones               = ["1", "2", "3"]
  tags                = local.common_tags
}

resource "azurerm_application_gateway" "main" {
  count = var.environment == "production" ? 1 : 0
  
  name                = "${local.resource_prefix}-appgw"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  tags                = local.common_tags

  sku {
    name     = "WAF_v2"
    tier     = "WAF_v2"
    capacity = 2
  }

  zones = ["1", "2", "3"]

  gateway_ip_configuration {
    name      = "gateway-ip-config"
    subnet_id = azurerm_subnet.app_gateway.id
  }

  frontend_port {
    name = "https-port"
    port = 443
  }

  frontend_port {
    name = "http-port"
    port = 80
  }

  frontend_ip_configuration {
    name                 = "public-frontend-ip"
    public_ip_address_id = azurerm_public_ip.app_gateway[0].id
  }

  backend_address_pool {
    name = "aks-backend-pool"
  }

  backend_http_settings {
    name                  = "https-backend-settings"
    cookie_based_affinity = "Disabled"
    path                  = "/"
    port                  = 443
    protocol              = "Https"
    request_timeout       = 60
    
    # Health probe
    probe_name = "health-probe"
  }

  http_listener {
    name                           = "https-listener"
    frontend_ip_configuration_name = "public-frontend-ip"
    frontend_port_name             = "https-port"
    protocol                       = "Https"
    ssl_certificate_name           = "riverty-ssl-cert"
  }

  # HTTP to HTTPS redirect
  http_listener {
    name                           = "http-listener"
    frontend_ip_configuration_name = "public-frontend-ip"
    frontend_port_name             = "http-port"
    protocol                       = "Http"
  }

  request_routing_rule {
    name                       = "https-routing-rule"
    rule_type                  = "Basic"
    http_listener_name         = "https-listener"
    backend_address_pool_name  = "aks-backend-pool"
    backend_http_settings_name = "https-backend-settings"
    priority                   = 100
  }

  # HTTP redirect rule
  redirect_configuration {
    name                 = "http-to-https-redirect"
    redirect_type        = "Permanent"
    target_listener_name = "https-listener"
    include_path         = true
    include_query_string = true
  }

  request_routing_rule {
    name                        = "http-redirect-rule"
    rule_type                   = "Basic"
    http_listener_name          = "http-listener"
    redirect_configuration_name = "http-to-https-redirect"
    priority                    = 200
  }

  probe {
    name                = "health-probe"
    protocol            = "Https"
    path                = "/health"
    host                = "api.riverty.no"
    interval            = 30
    timeout             = 30
    unhealthy_threshold = 3
  }

  # SSL certificate (would be managed externally)
  ssl_certificate {
    name     = "riverty-ssl-cert"
    data     = var.ssl_certificate_data
    password = var.ssl_certificate_password
  }

  # WAF configuration for Norwegian compliance
  waf_configuration {
    enabled          = true
    firewall_mode    = "Prevention"
    rule_set_type    = "OWASP"
    rule_set_version = "3.2"
    
    # Norwegian-specific exclusions if needed
    exclusion {
      match_variable          = "RequestHeaderNames"
      selector_match_operator = "Equals"
      selector                = "X-Norwegian-SSN"
    }
  }

  # Autoscale configuration
  autoscale_configuration {
    min_capacity = 2
    max_capacity = 10
  }
}

# Outputs for other configurations
output "resource_group_name" {
  description = "Name of the resource group"
  value       = azurerm_resource_group.main.name
}

output "aks_cluster_name" {
  description = "Name of the AKS cluster"
  value       = azurerm_kubernetes_cluster.main.name
}

output "aks_cluster_id" {
  description = "ID of the AKS cluster"
  value       = azurerm_kubernetes_cluster.main.id
}

output "key_vault_uri" {
  description = "URI of the Key Vault"
  value       = azurerm_key_vault.main.vault_uri
}

output "container_registry_login_server" {
  description = "Login server of the Container Registry"
  value       = azurerm_container_registry.main.login_server
}

output "sql_server_fqdn" {
  description = "FQDN of the SQL Server"
  value       = azurerm_mssql_server.main.fully_qualified_domain_name
}

output "application_insights_instrumentation_key" {
  description = "Instrumentation key for Application Insights"
  value       = azurerm_application_insights.main.instrumentation_key
  sensitive   = true
}

output "application_insights_connection_string" {
  description = "Connection string for Application Insights"
  value       = azurerm_application_insights.main.connection_string
  sensitive   = true
}

output "servicebus_namespace_name" {
  description = "Name of the Service Bus namespace"
  value       = azurerm_servicebus_namespace.main.name
}

output "redis_hostname" {
  description = "Hostname of the Redis cache"
  value       = azurerm_redis_cache.main.hostname
}

output "function_app_name" {
  description = "Name of the Function App"
  value       = azurerm_linux_function_app.payment_processor.name
}

output "application_gateway_public_ip" {
  description = "Public IP address of the Application Gateway"
  value       = var.environment == "production" ? azurerm_public_ip.app_gateway[0].ip_address : null
}