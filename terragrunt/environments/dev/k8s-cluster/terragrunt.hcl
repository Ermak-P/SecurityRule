# Dev k8s-cluster: small single-node cluster
include "root" {
  path = find_in_parent_folders()
}

include "envcommon" {
  path   = "${get_repo_root()}/terragrunt/_envcommon/k8s-cluster.hcl"
  expose = true
}

inputs = {
  node_count = 1
  vm_size    = "Standard_B2s"
}
