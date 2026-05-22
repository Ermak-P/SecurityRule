# Prod k8s-cluster: 3-node cluster with production-grade VMs
include "root" {
  path = find_in_parent_folders()
}

include "envcommon" {
  path   = "${get_repo_root()}/terragrunt/_envcommon/k8s-cluster.hcl"
  expose = true
}

inputs = {
  node_count = 3
  vm_size    = "Standard_D4s_v3"
}
