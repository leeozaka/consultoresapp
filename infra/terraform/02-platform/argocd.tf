locals {
  argocd_ingress_annotations = merge(
    {
      "nginx.ingress.kubernetes.io/backend-protocol" = "HTTP"
      "nginx.ingress.kubernetes.io/ssl-redirect"     = "true"
    },
    local.enable_cloudflare_dns01 ? { "cert-manager.io/cluster-issuer" = "letsencrypt-prod" } : {}
  )
}

resource "kubernetes_namespace" "argocd" {
  count = local.pcfg.enable_argocd ? 1 : 0

  metadata {
    name = "argocd"
  }
}

resource "helm_release" "argocd" {
  count      = local.pcfg.enable_argocd ? 1 : 0
  name       = "argocd"
  repository = "https://argoproj.github.io/argo-helm"
  chart      = "argo-cd"
  namespace  = "argocd"
  version    = "7.8.2"

  # Removed force_update and recreate_pods — they caused unnecessary pod
  # restarts on every terraform apply, causing brief ArgoCD downtime.

  set {
    name  = "server.ingress.enabled"
    value = "false"
  }

  values = [
    yamlencode({
      configs = {
        params = {
          # TLS is terminated by NGINX Ingress; ArgoCD runs insecure internally.
          "server.insecure" = true
        }
      }
    })
  ]

  depends_on = [
    helm_release.ingress_nginx,
    kubernetes_namespace.argocd
  ]
}

resource "kubernetes_ingress_v1" "argocd_server" {
  count = local.pcfg.enable_argocd ? 1 : 0

  metadata {
    name        = "argocd-server"
    namespace   = "argocd"
    annotations = local.argocd_ingress_annotations
  }

  spec {
    ingress_class_name = "nginx"

    rule {
      host = var.argocd_hostname
      http {
        path {
          path      = "/"
          path_type = "Prefix"
          backend {
            service {
              name = "argocd-server"
              port { number = 80 }
            }
          }
        }
      }
    }

    tls {
      hosts       = [var.argocd_hostname]
      secret_name = "argocd-server-tls"
    }
  }

  depends_on = [helm_release.argocd]
}

resource "kubectl_manifest" "argocd_application" {
  count = local.pcfg.enable_argocd ? 1 : 0

  yaml_body = yamlencode({
    apiVersion = "argoproj.io/v1alpha1"
    kind       = "Application"
    metadata = {
      name      = var.argocd_app_name
      namespace = "argocd"
    }
    spec = {
      project = "default"
      source = {
        repoURL        = var.argocd_repo_url
        targetRevision = var.argocd_target_revision
        path           = var.argocd_app_path
      }
      destination = {
        server    = "https://kubernetes.default.svc"
        namespace = var.argocd_destination_namespace
      }
      syncPolicy = {
        automated = {
          prune    = true
          selfHeal = true
        }
        syncOptions = ["CreateNamespace=true"]
      }
    }
  })

  depends_on = [helm_release.argocd]
}
