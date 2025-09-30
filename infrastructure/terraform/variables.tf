# Riverty BNPL Platform - Terraform Variables
# Define all input variables for infrastructure deployment

# Environment Configuration
variable "environment" {
  description = "Environment name (development, staging, production)"
  type        = string
  default     = "development"
  
  validation {
    condition     = contains(["development", "staging", "production"], var.environment)
    error_message = "Environment must be development, staging, or production."
  }
}

# Kubernetes Configuration
variable "kubernetes_version" {
  description = "Kubernetes version for AKS cluster"
  type        = string
  default     = "1.28.3"
}

variable "aks_admin_group_object_ids" {
  description = "Azure AD group object IDs for AKS cluster administrators"
  type        = list(string)
  default     = []
}

# SQL Server Configuration
variable "sql_admin_username" {
  description = "Administrator username for SQL Server"
  type        = string
  default     = "sqladmin"
  sensitive   = true
}

variable "sql_admin_password" {
  description = "Administrator password for SQL Server"
  type        = string
  sensitive   = true
}

variable "sql_azuread_admin_login" {
  description = "Azure AD admin login for SQL Server"
  type        = string
}

variable "sql_azuread_admin_object_id" {
  description = "Azure AD admin object ID for SQL Server"
  type        = string
}

# Security Configuration
variable "security_alert_emails" {
  description = "Email addresses for security alerts"
  type        = list(string)
  default     = ["security@riverty.com"]
}

# SSL Certificate Configuration (for Application Gateway)
variable "ssl_certificate_data" {
  description = "Base64-encoded SSL certificate data"
  type        = string
  default     = ""
  sensitive   = true
}

variable "ssl_certificate_password" {
  description = "Password for SSL certificate"
  type        = string
  default     = ""
  sensitive   = true
}

# Tags
variable "additional_tags" {
  description = "Additional tags to apply to all resources"
  type        = map(string)
  default     = {}
}

# Resource Configuration
variable "enable_advanced_threat_protection" {
  description = "Enable Advanced Threat Protection for databases"
  type        = bool
  default     = true
}

variable "enable_private_endpoints" {
  description = "Enable private endpoints for all services"
  type        = bool
  default     = true
}

# Backup Configuration
variable "backup_retention_days" {
  description = "Number of days to retain backups (7 years for Norwegian compliance)"
  type        = number
  default     = 2555
}

# Monitoring Configuration
variable "log_retention_days" {
  description = "Number of days to retain logs (7 years for audit compliance)"
  type        = number
  default     = 2555
}

variable "enable_application_insights" {
  description = "Enable Application Insights monitoring"
  type        = bool
  default     = true
}

# Network Configuration
variable "allowed_ip_ranges" {
  description = "IP ranges allowed to access the platform"
  type        = list(string)
  default     = []
}

# Norwegian Compliance
variable "data_residency_required" {
  description = "Enforce data residency in Norway"
  type        = bool
  default     = true
}

# Scaling Configuration
variable "min_node_count" {
  description = "Minimum number of nodes in AKS cluster"
  type        = number
  default     = 2
}

variable "max_node_count" {
  description = "Maximum number of nodes in AKS cluster"
  type        = number
  default     = 10
}

# Database Configuration
variable "database_sku" {
  description = "SKU for SQL databases"
  type        = string
  default     = "S1"
  
  validation {
    condition     = contains(["S0", "S1", "S2", "S3", "P1", "P2", "P4"], var.database_sku)
    error_message = "Database SKU must be a valid SQL Database tier."
  }
}

# Redis Configuration
variable "redis_sku" {
  description = "SKU for Redis cache"
  type        = string
  default     = "Standard"
  
  validation {
    condition     = contains(["Basic", "Standard", "Premium"], var.redis_sku)
    error_message = "Redis SKU must be Basic, Standard, or Premium."
  }
}

variable "redis_capacity" {
  description = "Capacity for Redis cache"
  type        = number
  default     = 1
}

# Service Bus Configuration
variable "service_bus_sku" {
  description = "SKU for Service Bus"
  type        = string
  default     = "Standard"
  
  validation {
    condition     = contains(["Basic", "Standard", "Premium"], var.service_bus_sku)
    error_message = "Service Bus SKU must be Basic, Standard, or Premium."
  }
}

# Application Gateway Configuration (Production only)
variable "enable_application_gateway" {
  description = "Enable Application Gateway for ingress"
  type        = bool
  default     = false
}

variable "application_gateway_capacity" {
  description = "Capacity for Application Gateway"
  type        = number
  default     = 2
}

# Cost Optimization
variable "enable_cost_optimization" {
  description = "Enable cost optimization features (auto-shutdown, reserved instances)"
  type        = bool
  default     = false
}

# Feature Flags
variable "enable_geo_replication" {
  description = "Enable geo-replication for Container Registry"
  type        = bool
  default     = false
}

variable "enable_zone_redundancy" {
  description = "Enable zone redundancy for services"
  type        = bool
  default     = false
}

# Norwegian Banking Integration
variable "dnb_api_base_url" {
  description = "DNB Open Banking API base URL"
  type        = string
  default     = "https://api.dnb.no"
}

variable "enable_vipps_integration" {
  description = "Enable Vipps payment integration"
  type        = bool
  default     = true
}

variable "enable_bankid_integration" {
  description = "Enable BankID authentication integration"
  type        = bool
  default     = true
}