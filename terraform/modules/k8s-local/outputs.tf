output "cluster_name" {
  description = "Name of the kind cluster"
  value       = kind_cluster.this.name
}

output "kubeconfig_path" {
  description = "Path to the kubeconfig file written by the kind provider"
  value       = kind_cluster.this.kubeconfig_path
}

output "app_url" {
  description = "URL to access the SecurityRule application locally"
  value       = "http://localhost:${var.host_port_app}"
}

output "mssql_connection" {
  description = "SQL Server connection string for connecting from your local machine (e.g. from SSMS or Azure Data Studio)"
  value       = "Server=localhost,${var.host_port_mssql};Database=SecurityRuleDb;User Id=sa;Password=<mssql_sa_password>;TrustServerCertificate=True;"
  sensitive   = false
}
