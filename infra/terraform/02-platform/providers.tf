# Read outputs from 01-cluster state to configure the kubernetes/helm providers.
# This eliminates the two-phase apply: run 01-cluster first, then this config.
data "terraform_remote_state" "cluster" {
  backend = "local"
  config = {
    path = "${path.module}/../01-cluster/terraform.tfstate"
  }
}

provider "kubernetes" {
  host = data.terraform_remote_state.cluster.outputs.k8s_client_host
  # Talos outputs base64-encoded PEM; the kubernetes provider wants decoded PEM.
  cluster_ca_certificate = base64decode(data.terraform_remote_state.cluster.outputs.k8s_client_ca_certificate)
  client_certificate     = base64decode(data.terraform_remote_state.cluster.outputs.k8s_client_certificate)
  client_key             = base64decode(data.terraform_remote_state.cluster.outputs.k8s_client_key)
}

# kubectl provider defers CRD schema validation to apply-time.
# Use this for cert-manager ClusterIssuers and ArgoCD Application (CRDs).
provider "kubectl" {
  host                   = data.terraform_remote_state.cluster.outputs.k8s_client_host
  cluster_ca_certificate = base64decode(data.terraform_remote_state.cluster.outputs.k8s_client_ca_certificate)
  client_certificate     = base64decode(data.terraform_remote_state.cluster.outputs.k8s_client_certificate)
  client_key             = base64decode(data.terraform_remote_state.cluster.outputs.k8s_client_key)
  load_config_file       = false
}

provider "helm" {
  kubernetes {
    host                   = data.terraform_remote_state.cluster.outputs.k8s_client_host
    cluster_ca_certificate = base64decode(data.terraform_remote_state.cluster.outputs.k8s_client_ca_certificate)
    client_certificate     = base64decode(data.terraform_remote_state.cluster.outputs.k8s_client_certificate)
    client_key             = base64decode(data.terraform_remote_state.cluster.outputs.k8s_client_key)
  }
}
