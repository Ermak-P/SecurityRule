# ------------------------------------------------------------------------------
# Module: k8s-local
#
# Поднимает локальный Kubernetes кластер с помощью kind (Kubernetes IN Docker)
# и деплоит приложение SecurityRule.
#
# Требования:
#   - Docker (запущен локально)
#   - kind   (https://kind.sigs.k8s.io/docs/user/quick-start/#installation)
#   - kubectl (для ручных проверок после apply)
#
# Этот модуль НЕ использует облачный провайдер.
# Backend — локальный файл (см. terragrunt/environments/local/).
# ------------------------------------------------------------------------------

terraform {
  required_version = ">= 1.6"

  required_providers {
    # kind-провайдер управляет lifecycle кластера
    kind = {
      source  = "tehcyx/kind"
      version = "~> 0.4"
    }
    # kubernetes-провайдер деплоит ресурсы внутрь кластера
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = "~> 2.0"
    }
    # null-провайдер для ожидания готовности
    null = {
      source  = "hashicorp/null"
      version = "~> 3.0"
    }
  }
}

# ---------------------------------------------------------------------------
# 1. Kind кластер
# ---------------------------------------------------------------------------
resource "kind_cluster" "this" {
  name            = var.cluster_name
  wait_for_ready  = true

  kind_config {
    kind        = "Cluster"
    api_version = "kind.x-k8s.io/v1alpha4"

    # Один control-plane нода (достаточно для локальной разработки)
    node {
      role = "control-plane"

      # Пробрасываем порты с хоста в кластер:
      # 8080 → приложение SecurityRule
      # 1433 → SQL Server (для прямого подключения из IDE)
      extra_port_mappings {
        container_port = 80
        host_port      = var.host_port_app
        protocol       = "TCP"
      }
      extra_port_mappings {
        container_port = 1433
        host_port      = var.host_port_mssql
        protocol       = "TCP"
      }
    }
  }
}

# ---------------------------------------------------------------------------
# 2. Namespace
# ---------------------------------------------------------------------------
resource "kubernetes_namespace" "this" {
  depends_on = [kind_cluster.this]

  metadata {
    name = var.namespace
  }
}

# ---------------------------------------------------------------------------
# 3. SQL Server (SQL Server 2022 Developer Edition — бесплатна локально)
# ---------------------------------------------------------------------------
resource "kubernetes_secret" "mssql" {
  metadata {
    name      = "mssql-secret"
    namespace = kubernetes_namespace.this.metadata[0].name
  }

  data = {
    "sa-password" = var.mssql_sa_password
  }
}

resource "kubernetes_stateful_set" "mssql" {
  metadata {
    name      = "mssql"
    namespace = kubernetes_namespace.this.metadata[0].name
    labels    = { app = "mssql" }
  }

  spec {
    service_name = "mssql"
    replicas     = 1

    selector {
      match_labels = { app = "mssql" }
    }

    template {
      metadata { labels = { app = "mssql" } }

      spec {
        container {
          name  = "mssql"
          image = "mcr.microsoft.com/mssql/server:2022-latest"

          port { container_port = 1433 }

          env {
            name  = "ACCEPT_EULA"
            value = "Y"
          }
          env {
            name  = "MSSQL_PID"
            value = "Developer"
          }
          env {
            name = "SA_PASSWORD"
            value_from {
              secret_key_ref {
                name = kubernetes_secret.mssql.metadata[0].name
                key  = "sa-password"
              }
            }
          }

          resources {
            requests = { memory = "512Mi"; cpu = "250m" }
            limits   = { memory = "2Gi";   cpu = "1000m" }
          }

          volume_mount {
            name       = "mssql-data"
            mount_path = "/var/opt/mssql"
          }
        }
      }
    }

    volume_claim_template {
      metadata { name = "mssql-data" }
      spec {
        access_modes = ["ReadWriteOnce"]
        resources {
          requests = { storage = "2Gi" }
        }
      }
    }
  }
}

resource "kubernetes_service" "mssql" {
  metadata {
    name      = "mssql"
    namespace = kubernetes_namespace.this.metadata[0].name
  }

  spec {
    selector = { app = "mssql" }
    port {
      name        = "mssql"
      port        = 1433
      target_port = 1433
      node_port   = 31433
    }
    type = "NodePort"
  }
}

# ---------------------------------------------------------------------------
# 4. Приложение SecurityRule
# ---------------------------------------------------------------------------
locals {
  db_conn    = "Server=mssql.${var.namespace}.svc.cluster.local,1433;Database=SecurityRuleDb;User Id=sa;Password=${var.mssql_sa_password};TrustServerCertificate=True;MultipleActiveResultSets=True;"
  fakead_conn = "Server=mssql.${var.namespace}.svc.cluster.local,1433;Database=FakeAdDb;User Id=sa;Password=${var.mssql_sa_password};TrustServerCertificate=True;MultipleActiveResultSets=True;"
}

resource "kubernetes_config_map" "app" {
  metadata {
    name      = "app-config"
    namespace = kubernetes_namespace.this.metadata[0].name
  }

  data = {
    ASPNETCORE_ENVIRONMENT                    = "Development"
    ASPNETCORE_URLS                           = "http://+:8080"
    "Authentication__UseActiveDirectory"      = "false"
    "Authentication__DevelopmentUser"         = var.development_user
    "Logging__LogLevel__Default"              = "Information"
    "Logging__LogLevel__Microsoft.AspNetCore" = "Warning"
  }
}

resource "kubernetes_secret" "app" {
  metadata {
    name      = "app-secret"
    namespace = kubernetes_namespace.this.metadata[0].name
  }

  data = {
    "ConnectionStrings__DefaultConnection" = local.db_conn
    "ConnectionStrings__FakeAdConnection"  = local.fakead_conn
  }
}

resource "kubernetes_deployment" "app" {
  metadata {
    name      = "security-rule"
    namespace = kubernetes_namespace.this.metadata[0].name
    labels    = { app = "security-rule" }
  }

  spec {
    replicas = 1

    selector { match_labels = { app = "security-rule" } }

    template {
      metadata { labels = { app = "security-rule" } }

      spec {
        # Ждём готовности SQL Server перед стартом приложения
        init_container {
          name    = "wait-for-mssql"
          image   = "busybox:1.36"
          command = [
            "sh", "-c",
            "until nc -z mssql 1433; do echo 'Waiting for SQL Server...'; sleep 3; done; echo 'SQL Server is ready'"
          ]
        }

        container {
          name              = "security-rule"
          image             = var.app_image
          image_pull_policy = var.app_image_pull_policy

          port { container_port = 8080 }

          env_from {
            config_map_ref { name = kubernetes_config_map.app.metadata[0].name }
          }
          env_from {
            secret_ref { name = kubernetes_secret.app.metadata[0].name }
          }

          liveness_probe {
            http_get { path = "/"; port = 8080 }
            initial_delay_seconds = 60
            period_seconds        = 15
            failure_threshold     = 5
          }

          readiness_probe {
            http_get { path = "/"; port = 8080 }
            initial_delay_seconds = 30
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

# ---------------------------------------------------------------------------
# 5. Service типа NodePort — доступен снаружи кластера через kind port mapping
# ---------------------------------------------------------------------------
resource "kubernetes_service" "app" {
  metadata {
    name      = "security-rule"
    namespace = kubernetes_namespace.this.metadata[0].name
  }

  spec {
    selector = { app = "security-rule" }
    port {
      name        = "http"
      port        = 80
      target_port = 8080
      node_port   = 30080
    }
    type = "NodePort"
  }
}
