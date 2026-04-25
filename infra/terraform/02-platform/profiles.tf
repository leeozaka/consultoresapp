locals {
  platform_profiles = {
    dev = {
      enable_ingress_loadbalancer = false
      enable_cert_manager         = true
      enable_letsencrypt_issuer   = false
      enable_headlamp             = false
      enable_argocd               = false
    }
    staging = {
      enable_ingress_loadbalancer = true
      enable_cert_manager         = true
      enable_letsencrypt_issuer   = true
      enable_headlamp             = false
      enable_argocd               = false
    }
    production = {
      enable_ingress_loadbalancer = true
      enable_cert_manager         = true
      enable_letsencrypt_issuer   = true
      enable_headlamp             = true
      enable_argocd               = true
    }
  }

  pcfg = {
    enable_ingress_loadbalancer = coalesce(var.enable_ingress_loadbalancer, local.platform_profiles[var.profile].enable_ingress_loadbalancer)
    enable_cert_manager         = coalesce(var.enable_cert_manager, local.platform_profiles[var.profile].enable_cert_manager)
    enable_letsencrypt_issuer   = coalesce(var.enable_letsencrypt_issuer, local.platform_profiles[var.profile].enable_letsencrypt_issuer)
    enable_headlamp             = coalesce(var.enable_headlamp, local.platform_profiles[var.profile].enable_headlamp)
    enable_argocd               = coalesce(var.enable_argocd, local.platform_profiles[var.profile].enable_argocd)
  }

  enable_cloudflare_dns01 = (
    var.cloudflare_api_token != "" &&
    local.pcfg.enable_cert_manager &&
    local.pcfg.enable_letsencrypt_issuer
  )
}
