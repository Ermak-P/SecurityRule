variable "namespace" {
  description = "Kubernetes namespace for the application"
  type        = string
  default     = "security-rule"
}

variable "environment" {
  description = "ASP.NET Core environment name (e.g. Production, Development)"
  type        = string
  default     = "Production"
}

variable "app_image" {
  description = "Full Docker image reference including tag (e.g. ghcr.io/ermak-p/securityrule:1.2.3)"
  type        = string
}

variable "app_replicas" {
  description = "Number of application pod replicas"
  type        = number
  default     = 1
}

variable "use_active_directory" {
  description = "Whether to use Active Directory (Negotiate) authentication"
  type        = bool
  default     = false
}

variable "development_user" {
  description = "Username used by DevelopmentAuthenticationHandler when AD is disabled"
  type        = string
  default     = "k8s-user"
}

variable "db_connection_string" {
  description = "SQL Server connection string for the main database"
  type        = string
  sensitive   = true
}

variable "fakead_connection_string" {
  description = "SQL Server connection string for the FakeAD database"
  type        = string
  sensitive   = true
}

variable "mssql_sa_password" {
  description = "SA password for the SQL Server StatefulSet"
  type        = string
  sensitive   = true
}
