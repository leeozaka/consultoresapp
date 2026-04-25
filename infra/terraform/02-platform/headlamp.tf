resource "kubernetes_namespace" "headlamp" {
  count = local.pcfg.enable_headlamp ? 1 : 0

  metadata {
    name = "headlamp"
    labels = {
      "app.kubernetes.io/name"       = "headlamp"
      "app.kubernetes.io/managed-by" = "terraform"
    }
  }
}

resource "helm_release" "headlamp" {
  count            = local.pcfg.enable_headlamp ? 1 : 0
  name             = "headlamp"
  chart            = "headlamp"
  repository       = "https://kubernetes-sigs.github.io/headlamp/"
  namespace        = kubernetes_namespace.headlamp[0].metadata[0].name
  create_namespace = false

  depends_on = [helm_release.ingress_nginx, kubernetes_namespace.headlamp]
}

resource "kubernetes_service_account" "headlamp_viewer" {
  count = local.pcfg.enable_headlamp ? 1 : 0

  metadata {
    name      = "headlamp-viewer"
    namespace = kubernetes_namespace.headlamp[0].metadata[0].name
    labels = {
      "app.kubernetes.io/managed-by" = "terraform"
    }
  }
}

resource "kubernetes_cluster_role_binding" "headlamp_viewer" {
  count = local.pcfg.enable_headlamp ? 1 : 0

  metadata {
    name = "headlamp-viewer"
    labels = {
      "app.kubernetes.io/managed-by" = "terraform"
    }
  }

  role_ref {
    api_group = "rbac.authorization.k8s.io"
    kind      = "ClusterRole"
    name      = "view"
  }

  subject {
    kind      = "ServiceAccount"
    name      = kubernetes_service_account.headlamp_viewer[0].metadata[0].name
    namespace = kubernetes_service_account.headlamp_viewer[0].metadata[0].namespace
  }
}
