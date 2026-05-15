# Local k8s stack: kind cluster + SecurityRule app
#
# Использует LOCAL backend (файл на диске) — не требует облака.
# Запускать из этой директории:
#
#   terragrunt init
#   terragrunt apply
#
# Перед запуском выполните:
#   docker build -t security-rule:local .             # сборка образа
#   kind load docker-image security-rule:local \      # загрузка в kind
#     --name security-rule-local
#
# После apply: http://localhost:8080

include "root" {
  # Переопределяем root конфиг, чтобы использовать local backend вместо Azure Blob
  path = find_in_parent_folders()
}

# Переопределяем backend на локальный файл
remote_state {
  backend = "local"

  generate = {
    path      = "backend.tf"
    if_exists = "overwrite_terragrunt"
  }

  config = {
    path = "${get_repo_root()}/.terraform-local-state/local/terraform.tfstate"
  }
}

terraform {
  source = "${get_repo_root()}//terraform/modules/k8s-local"
}

# Переопределяем провайдер для локального режима
generate "provider_local" {
  path      = "provider.tf"
  if_exists = "overwrite_terragrunt"
  contents  = <<-EOF
    terraform {
      required_version = ">= 1.6"
      required_providers {
        kind = {
          source  = "tehcyx/kind"
          version = "~> 0.4"
        }
        kubernetes = {
          source  = "hashicorp/kubernetes"
          version = "~> 2.0"
        }
        null = {
          source  = "hashicorp/null"
          version = "~> 3.0"
        }
      }
    }

    # Kubernetes провайдер читает kubeconfig автоматически после создания кластера
    provider "kubernetes" {
      config_path    = "~/.kube/config"
      config_context = "kind-security-rule-local"
    }
  EOF
}

inputs = {
  cluster_name          = "security-rule-local"
  namespace             = "security-rule"
  app_image             = "security-rule:local"
  app_image_pull_policy = "Never"
  development_user      = "local-dev-user"
  host_port_app         = 8080
  host_port_mssql       = 1433

  # Пароль SA для SQL Server.
  # Для локальной разработки можно оставить дефолт из переменной модуля,
  # или переопределить через env-переменную: TF_VAR_mssql_sa_password=...
}
