resource "kubernetes_namespace" "ingress_nginx" {
  metadata {
    name = "ingress-nginx"
    labels = {
      "pod-security.kubernetes.io/enforce" = "privileged"
      "pod-security.kubernetes.io/audit"   = "privileged"
      "pod-security.kubernetes.io/warn"    = "privileged"
    }
  }
}

resource "helm_release" "ingress_nginx" {
  name             = "ingress-nginx"
  chart            = "ingress-nginx"
  repository       = "https://kubernetes.github.io/ingress-nginx"
  namespace        = "ingress-nginx"
  version          = "4.12.0"
  create_namespace = false
  timeout          = 900

  values = [yamlencode({
    controller = {
      # Use Deployment with a DO LB, DaemonSet for hostPort-only setups
      kind = local.pcfg.enable_ingress_loadbalancer ? "Deployment" : "DaemonSet"
      tolerations = [
        {
          key      = "node-role.kubernetes.io/control-plane"
          operator = "Exists"
          effect   = "NoSchedule"
        }
      ]
      hostPort = {
        enabled = true
      }
      config = {
        use-forwarded-headers = "true"
        proxy-real-ip-cidr    = "0.0.0.0/0"
        proxy-body-size       = "25m"
      }
      service = {
        type = "ClusterIP"
      }
    }
  })]

  depends_on = [kubernetes_namespace.ingress_nginx]
}
