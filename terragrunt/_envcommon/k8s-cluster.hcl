# =============================================================================
# _envcommon/k8s-cluster.hcl
# Shared inputs for the k8s-cluster module, inherited by all environments.
# Override any value in the environment-specific terragrunt.hcl.
# =============================================================================

locals {
  env_vars    = read_terragrunt_config(find_in_parent_folders("env.hcl"))
  environment = local.env_vars.locals.environment
  location    = local.env_vars.locals.location
}

terraform {
  source = "${get_repo_root()}//terraform/modules/k8s-cluster"
}

inputs = {
  location             = local.location
  resource_group_name  = "rg-securityrule-${local.environment}"
  cluster_name         = "aks-securityrule-${local.environment}"

  tags = {
    environment = local.environment
    project     = "security-rule"
    managed_by  = "terragrunt"
  }
}
