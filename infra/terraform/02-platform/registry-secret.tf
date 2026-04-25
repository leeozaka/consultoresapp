locals {
  # try() handles two cases:
  # 1. 01-cluster was applied before this output was added (stale state)
  # 2. enable_container_registry = false in 01-cluster (output value is null)
  docr_credentials = try(data.terraform_remote_state.cluster.outputs.docr_docker_credentials, null)
}

resource "kubernetes_secret" "docr_credentials" {
  count = local.docr_credentials != null ? 1 : 0

  metadata {
    name      = "docr-credentials"
    namespace = var.registry_namespace
  }

  type = "kubernetes.io/dockerconfigjson"

  data = {
    ".dockerconfigjson" = local.docr_credentials
  }
}
