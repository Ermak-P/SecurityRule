# Dev k8s-app: 1 replica, dev user
include "root" {
  path = find_in_parent_folders()
}

include "envcommon" {
  path   = "${get_repo_root()}/terragrunt/_envcommon/k8s-app.hcl"
  expose = true
}

dependency "cluster" {
  config_path = "../k8s-cluster"
  mock_outputs = {
    host                   = "https://mock-host"
    client_certificate     = "bW9jaw=="
    client_key             = "bW9jaw=="
    cluster_ca_certificate = "bW9jaw=="
  }
  mock_outputs_allowed_terraform_commands = ["validate", "plan"]
}

generate "provider_kubernetes" {
  path      = "provider_kubernetes.tf"
  if_exists = "overwrite_terragrunt"
  contents  = <<-EOF
    provider "kubernetes" {
      host                   = "${dependency.cluster.outputs.host}"
      client_certificate     = base64decode("${dependency.cluster.outputs.client_certificate}")
      client_key             = base64decode("${dependency.cluster.outputs.client_key}")
      cluster_ca_certificate = base64decode("${dependency.cluster.outputs.cluster_ca_certificate}")
    }
  EOF
}

inputs = {
  app_replicas = 1

  # Secrets: set via environment variables or pass through CI/CD secret store.
  # TF_VAR_db_connection_string
  # TF_VAR_fakead_connection_string
  # TF_VAR_mssql_sa_password
}
