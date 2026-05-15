# =============================================================================
# _envcommon/k8s-app.hcl
# Shared inputs for the k8s-app module, inherited by all environments.
# Secrets (db_connection_string, mssql_sa_password) MUST be overridden
# per-environment via environment variables or a secrets manager.
# =============================================================================

locals {
  env_vars    = read_terragrunt_config(find_in_parent_folders("env.hcl"))
  environment = local.env_vars.locals.environment
}

terraform {
  source = "${get_repo_root()}//terraform/modules/k8s-app"
}

inputs = {
  namespace   = "security-rule"
  environment = local.environment == "prod" ? "Production" : "Development"

  # Override app_image per environment with the correct image tag.
  app_image = "ghcr.io/ermak-p/securityrule:latest"

  use_active_directory = false
  development_user     = "k8s-user"
}
