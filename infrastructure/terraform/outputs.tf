# Riverty BNPL Platform - Terraform Outputs
# Output values for use in other configurations and CI/CD pipelines

# Resource Group
output "resource_group_name" {
  description = "Name of the resource group"
  value       = azurerm_resource_group.main.name
}

output "resource_group_location" {
  description = "Location of the resource group"
  value       = azurerm_resource_group.main.location
}

# Networking
output "vnet_id" {
  description = "ID of the virtual network"
  value       = azurerm_virtual_network.main.id
}

output "vnet_name" {
  description = "Name of the virtual network"
  value       = azurerm_virtual_network.main.name
}

# AKS Cluster
output "aks_cluster_name" {
  description = "Name of the AKS cluster"
  value       = azurerm_kubernetes_cluster.main.name
}

output "aks_cluster_id" {
  description = "ID of the AKS cluster"
  value       = azurerm_kubernetes_cluster.main.id
}

output "aks_cluster_fqdn" {
  description = "FQDN of the AKS cluster"
  value       = azurerm_kubernetes_cluster.main.fqdn
}

output "aks_kube_config" {
  description = "Kubernetes configuration for the AKS cluster"
  value       = azurerm_kubernetes_cluster.main.kube_config_raw
  sensitive   = true
}

# Container Registry
output "container_registry_name" {
  description = "Name of the Container Registry"
  value       = azurerm_container_registry.main.name
}

output "container_registry_login_server" {
  description = "Login server of the Container Registry"
  value       = azurerm_container_registry.main.login_server
}

output "container_registry_id" {
  description = "ID of the Container Registry"
  value       = azurerm_container_registry.main.id
}

# Key Vault
output "key_vault_name" {
  description = "Name of the Key Vault"
  value       = azurerm_key_vault.main.name
}

output "key_vault_uri" {
  description = "URI of the Key Vault"
  value       = azurerm_key_vault.main.vault_uri
}

output "key_vault_id" {
  description = "ID of the Key Vault"
  value       = azurerm_key_vault.main.id
}

# SQL Server
output "sql_server_name" {
  description = "Name of the SQL Server"
  value       = azurerm_mssql_server.main.name
}

output "sql_server_fqdn" {
  description = "FQDN of the SQL Server"
  value       = azurerm_mssql_server.main.fully_qualified_domain_name
}

output "sql_server_id" {
  description = "ID of the SQL Server"
  value       = azurerm_mssql_server.main.id
}

# Databases
output "payment_database_name" {
  description = "Name of the Payment database"
  value       = azurerm_mssql_database.payment.name
}

output "risk_database_name" {
  description = "Name of the Risk database"
  value       = azurerm_mssql_database.risk.name
}

output "settlement_database_name" {
  description = "Name of the Settlement database"
  value       = azurerm_mssql_database.settlement.name
}

# Redis Cache
output "redis_hostname" {
  description = "Hostname of the Redis cache"
  value       = azurerm_redis_cache.main.hostname
}

output "redis_port" {
  description = "Port of the Redis cache"
  value       = azurerm_redis_cache.main.ssl_port
}

output "redis_primary_access_key" {
  description = "Primary access key for Redis cache"
  value       = azurerm_redis_cache.main.primary_access_key
  sensitive   = true
}

# Service Bus
output "servicebus_namespace_name" {
  description = "Name of the Service Bus namespace"
  value       = azurerm_servicebus_namespace.main.name
}

output "servicebus_namespace_id" {
  description = "ID of the Service Bus namespace"
  value       = azurerm_servicebus_namespace.main.id
}

# Application Insights
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

output "application_insights_app_id" {
  description = "Application ID for Application Insights"
  value       = azurerm_application_insights.main.app_id
}

# Log Analytics
output "log_analytics_workspace_id" {
  description = "ID of the Log Analytics workspace"
  value       = azurerm_log_analytics_workspace.main.id
}

output "log_analytics_workspace_name" {
  description = "Name of the Log Analytics workspace"
  value       = azurerm_log_analytics_workspace.main.name
}

# Function App
output "function_app_name" {
  description = "Name of the Function App"
  value       = azurerm_linux_function_app.payment_processor.name
}

output "function_app_default_hostname" {
  description = "Default hostname of the Function App"
  value       = azurerm_linux_function_app.payment_processor.default_hostname
}

# Application Gateway (Production only)
output "application_gateway_public_ip" {
  description = "Public IP address of the Application Gateway"
  value       = var.environment == "production" ? azurerm_public_ip.app_gateway[0].ip_address : null
}

output "application_gateway_id" {
  description = "ID of the Application Gateway"
  value       = var.environment == "production" ? azurerm_application_gateway.main[0].id : null
}

# Connection Strings (for application configuration)
output "payment_database_connection_string" {
  description = "Connection string for Payment database"
  value       = "Server=${azurerm_mssql_server.main.fully_qualified_domain_name};Database=${azurerm_mssql_database.payment.name};Authentication=Active Directory Default;"
  sensitive   = true
}

output "redis_connection_string" {
  description = "Connection string for Redis"
  value       = "${azurerm_redis_cache.main.hostname}:${azurerm_redis_cache.main.ssl_port},password=${azurerm_redis_cache.main.primary_access_key},ssl=True,abortConnect=False"
  sensitive   = true
}

# Deployment Information
output "deployment_timestamp" {
  description = "Timestamp of the deployment"
  value       = timestamp()
}

output "terraform_version" {
  description = "Terraform version used for deployment"
  value       = "~> 1.0"
}