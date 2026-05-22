variable "cluster_name" {
  description = "Name of the local kind cluster"
  type        = string
  default     = "security-rule-local"
}

variable "namespace" {
  description = "Kubernetes namespace for the application"
  type        = string
  default     = "security-rule"
}

variable "app_image" {
  description = "Docker image for the SecurityRule application. Use 'security-rule:local' when built locally via 'docker build'."
  type        = string
  default     = "security-rule:local"
}

variable "app_image_pull_policy" {
  description = "Image pull policy. Use 'Never' for locally built images (already loaded into kind), 'IfNotPresent' for images from a registry."
  type        = string
  default     = "Never"
}

variable "mssql_sa_password" {
  description = "SA password for the SQL Server container. Must meet SQL Server complexity requirements (upper, lower, digit, special char, min 8 chars)."
  type        = string
  sensitive   = true
  default     = "LocalDev!Passw0rd"
}

variable "development_user" {
  description = "Username passed to DevelopmentAuthenticationHandler (used when UseActiveDirectory=false)"
  type        = string
  default     = "local-dev-user"
}

variable "host_port_app" {
  description = "Host port mapped to the application's NodePort (30080). Access via http://localhost:<host_port_app>"
  type        = number
  default     = 8080
}

variable "host_port_mssql" {
  description = "Host port mapped to SQL Server (1433) for direct IDE access"
  type        = number
  default     = 1433
}
