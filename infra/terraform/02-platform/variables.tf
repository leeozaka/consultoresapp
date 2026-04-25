variable "profile" {
  description = "Platform profile: dev, staging, production"
  type        = string
  default     = "production"

  validation {
    condition     = contains(["dev", "staging", "production"], var.profile)
    error_message = "Profile must be one of: dev, staging, production."
  }
}

# Individual overrides — set to override the profile default.

variable "enable_ingress_loadbalancer" {
  description = "Whether an external LB is used (affects NGINX Ingress controller kind: Deployment vs DaemonSet)"
  type        = bool
  default     = null
}

variable "enable_cert_manager" {
  type    = bool
  default = null
}

variable "enable_letsencrypt_issuer" {
  type    = bool
  default = null
}

variable "enable_headlamp" {
  type    = bool
  default = null
}

variable "enable_argocd" {
  type    = bool
  default = null
}

# ── TLS / cert-manager ────────────────────────────────────────────────────────

variable "letsencrypt_email" {
  description = "Email for Let's Encrypt certificate registration"
  type        = string
  default     = "devops@itcorretor.com"
}

variable "cloudflare_api_token" {
  description = "Cloudflare API token for DNS-01 wildcard TLS (leave empty to use HTTP-01)"
  type        = string
  sensitive   = true
  default     = ""
}

# ── ArgoCD ────────────────────────────────────────────────────────────────────

variable "argocd_hostname" {
  description = "ArgoCD ingress hostname. Defaults to argocd.<domain from cluster state>."
  type        = string
  default     = ""
}

variable "argocd_repo_url" {
  description = "Git repository URL for ArgoCD to sync"
  type        = string
  default     = "https://github.com/consultores-app/ConsultoresAPP.git"
}

variable "argocd_app_path" {
  description = "Path inside the repo for ArgoCD to sync"
  type        = string
  default     = "infra/k8s/overlays/production"
}

variable "argocd_app_name" {
  description = "ArgoCD Application name"
  type        = string
  default     = "consultores-prod"
}

variable "argocd_target_revision" {
  description = "Git revision for ArgoCD to track"
  type        = string
  default     = "HEAD"
}

variable "argocd_destination_namespace" {
  description = "Kubernetes namespace where ArgoCD deploys the app"
  type        = string
  default     = "consultores"
}

# ── Registry pull secret ──────────────────────────────────────────────────────

variable "registry_namespace" {
  description = "Namespace where the DOCR imagePullSecret is created"
  type        = string
  default     = "consultores"
}
