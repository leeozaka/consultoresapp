resource "helm_release" "cert_manager" {
  count = local.pcfg.enable_cert_manager ? 1 : 0

  name             = "cert-manager"
  chart            = "cert-manager"
  repository       = "https://charts.jetstack.io"
  namespace        = "cert-manager"
  version          = "v1.17.1"
  create_namespace = true

  values = [yamlencode({
    crds = { enabled = true }
  })]

  depends_on = [helm_release.ingress_nginx]
}

resource "kubernetes_secret" "cloudflare_api_token" {
  count = local.enable_cloudflare_dns01 ? 1 : 0

  metadata {
    name      = "cloudflare-api-token-secret"
    namespace = "cert-manager"
  }

  data = {
    "api-token" = var.cloudflare_api_token
  }

  depends_on = [helm_release.cert_manager]
}

resource "kubectl_manifest" "letsencrypt_prod" {
  count = local.enable_cloudflare_dns01 ? 1 : 0

  yaml_body = yamlencode({
    apiVersion = "cert-manager.io/v1"
    kind       = "ClusterIssuer"
    metadata   = { name = "letsencrypt-prod" }
    spec = {
      acme = {
        server              = "https://acme-v02.api.letsencrypt.org/directory"
        email               = var.letsencrypt_email
        privateKeySecretRef = { name = "letsencrypt-prod" }
        solvers = [
          {
            dns01 = {
              cloudflare = {
                apiTokenSecretRef = {
                  name = "cloudflare-api-token-secret"
                  key  = "api-token"
                }
              }
            }
          }
        ]
      }
    }
  })

  depends_on = [kubernetes_secret.cloudflare_api_token]
}

# HTTP-01 ClusterIssuer for non-wildcard domains (when Cloudflare token is absent)
resource "kubectl_manifest" "letsencrypt_prod_http01" {
  count = (local.pcfg.enable_cert_manager && local.pcfg.enable_letsencrypt_issuer && !local.enable_cloudflare_dns01) ? 1 : 0

  yaml_body = yamlencode({
    apiVersion = "cert-manager.io/v1"
    kind       = "ClusterIssuer"
    metadata   = { name = "letsencrypt-prod" }
    spec = {
      acme = {
        server              = "https://acme-v02.api.letsencrypt.org/directory"
        email               = var.letsencrypt_email
        privateKeySecretRef = { name = "letsencrypt-prod" }
        solvers             = [{ http01 = { ingress = { ingressClassName = "nginx" } } }]
      }
    }
  })

  depends_on = [helm_release.cert_manager]
}
