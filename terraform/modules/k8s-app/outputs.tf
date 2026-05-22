output "namespace" {
  description = "Kubernetes namespace where the app is deployed"
  value       = kubernetes_namespace.this.metadata[0].name
}

output "app_service_name" {
  description = "Name of the Kubernetes Service for the application"
  value       = kubernetes_service.app.metadata[0].name
}
