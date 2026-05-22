# ------------------------------------------------------------------------------
# Module: k8s-app
#
# Deploys the SecurityRule application into an existing Kubernetes cluster.
# Uses the kubernetes provider with manifests built from the k8s/ Kustomize tree.
# ------------------------------------------------------------------------------

terraform {
  required_providers {
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = "~> 2.0"
    }
  }
}

resource "kubernetes_namespace" "this" {
  metadata {
    name = var.namespace
  }
}

resource "kubernetes_config_map" "app" {
  metadata {
    name      = "app-config"
    namespace = kubernetes_namespace.this.metadata[0].name
  }

  data = {
    ASPNETCORE_ENVIRONMENT                  = var.environment
    ASPNETCORE_URLS                         = "http://+:8080"
    "Authentication__UseActiveDirectory"    = tostring(var.use_active_directory)
    "Authentication__DevelopmentUser"       = var.development_user
    "Logging__LogLevel__Default"            = "Information"
    "Logging__LogLevel__Microsoft.AspNetCore" = "Warning"
  }
}

resource "kubernetes_secret" "app" {
  metadata {
    name      = "app-secret"
    namespace = kubernetes_namespace.this.metadata[0].name
  }

  data = {
    "ConnectionStrings__DefaultConnection" = var.db_connection_string
    "ConnectionStrings__FakeAdConnection"  = var.fakead_connection_string
  }
}

resource "kubernetes_secret" "mssql" {
  metadata {
    name      = "mssql-secret"
    namespace = kubernetes_namespace.this.metadata[0].name
  }

  data = {
    "sa-password" = var.mssql_sa_password
  }
}

resource "kubernetes_deployment" "app" {
  metadata {
    name      = "security-rule"
    namespace = kubernetes_namespace.this.metadata[0].name
    labels    = { app = "security-rule" }
  }

  spec {
    replicas = var.app_replicas

    selector {
      match_labels = { app = "security-rule" }
    }

    template {
      metadata {
        labels = { app = "security-rule" }
      }

      spec {
        container {
          name              = "security-rule"
          image             = var.app_image
          image_pull_policy = "Always"

          port { container_port = 8080 }

          env_from {
            config_map_ref { name = kubernetes_config_map.app.metadata[0].name }
          }
          env_from {
            secret_ref { name = kubernetes_secret.app.metadata[0].name }
          }

          liveness_probe {
            http_get { path = "/"; port = 8080 }
            initial_delay_seconds = 30
            period_seconds        = 15
          }

          readiness_probe {
            http_get { path = "/"; port = 8080 }
            initial_delay_seconds = 15
            period_seconds        = 10
          }

          resources {
            requests = { memory = "256Mi"; cpu = "100m" }
            limits   = { memory = "512Mi"; cpu = "500m" }
          }
        }
      }
    }
  }
}

resource "kubernetes_service" "app" {
  metadata {
    name      = "security-rule"
    namespace = kubernetes_namespace.this.metadata[0].name
  }

  spec {
    selector = { app = "security-rule" }
    port {
      port        = 80
      target_port = 8080
    }
  }
}
