# =============================================================================
# Root Terragrunt configuration
# All child terragrunt.hcl files inherit this via find_in_parent_folders().
# =============================================================================

locals {
  # Read the environment-level variables (env.hcl) bubbling up the tree.
  env_vars    = read_terragrunt_config(find_in_parent_folders("env.hcl"))
  environment = local.env_vars.locals.environment
  location    = local.env_vars.locals.location
}

# ---------------------------------------------------------------------------
# Remote state: Azure Blob Storage (swap for S3/GCS as needed)
# ---------------------------------------------------------------------------
remote_state {
  backend = "azurerm"

  generate = {
    path      = "backend.tf"
    if_exists = "overwrite_terragrunt"
  }

  config = {
    resource_group_name  = "rg-securityrule-tfstate"
    storage_account_name = "stsecurityruletf"
    container_name       = "tfstate"
    key                  = "${local.environment}/${path_relative_to_include()}/terraform.tfstate"
  }
}

# ---------------------------------------------------------------------------
# Common provider inputs injected into every child module
# ---------------------------------------------------------------------------
generate "provider_azurerm" {
  path      = "provider.tf"
  if_exists = "overwrite_terragrunt"
  contents  = <<-EOF
    terraform {
      required_version = ">= 1.6"
      required_providers {
        azurerm = {
          source  = "hashicorp/azurerm"
          version = "~> 3.0"
        }
        kubernetes = {
          source  = "hashicorp/kubernetes"
          version = "~> 2.0"
        }
      }
    }

    provider "azurerm" {
      features {}
    }
  EOF
}
