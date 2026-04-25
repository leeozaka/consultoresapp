# ── Required ───────────────────────────────────────────────────────────────────

variable "do_token" {
  description = "DigitalOcean Personal Access Token"
  type        = string
  sensitive   = true
}

# ── Profile ────────────────────────────────────────────────────────────────────

variable "profile" {
  description = "Deployment profile: dev (single-node), staging (1CP+1W), production (3CP+2 core workers + dedicated frontend pool)"
  type        = string
  default     = "dev"

  validation {
    condition     = contains(["dev", "staging", "production"], var.profile)
    error_message = "Profile must be one of: dev, staging, production."
  }
}

# Individual overrides — set these to override a profile value without switching profiles.
# Profile values are used when the variable is null (the default).

variable "control_plane_count" {
  type    = number
  default = null
}

variable "worker_count" {
  type    = number
  default = null
}

variable "frontend_worker_count" {
  type    = number
  default = null
}

variable "control_plane_size" {
  type    = string
  default = null
}

variable "worker_size" {
  type    = string
  default = null
}

variable "frontend_worker_size" {
  type    = string
  default = null
}

variable "enable_api_loadbalancer" {
  type    = bool
  default = null
}

variable "enable_ingress_loadbalancer" {
  type    = bool
  default = null
}

variable "enable_managed_database" {
  type    = bool
  default = null
}

variable "enable_container_registry" {
  type    = bool
  default = null
}

variable "manage_dns" {
  type    = bool
  default = null
}

variable "control_plane_backups_enabled" {
  type    = bool
  default = null
}

variable "worker_backups_enabled" {
  type    = bool
  default = null
}

variable "frontend_worker_backups_enabled" {
  type    = bool
  default = null
}

# ── Region & versions ──────────────────────────────────────────────────────────

variable "region" {
  description = "DigitalOcean region slug"
  type        = string
  default     = "nyc1"
}

variable "talos_version" {
  description = "Talos OS version (e.g. v1.9.4)"
  type        = string
  default     = "v1.9.4"
}

variable "kubernetes_version" {
  description = "Kubernetes version to install inside Talos"
  type        = string
  default     = "1.32.0"
}

# ── VPC ────────────────────────────────────────────────────────────────────────

variable "vpc_ip_range" {
  description = "CIDR for the VPC when a new one is created"
  type        = string
  default     = "10.116.0.0/20"
}

variable "existing_vpc_id" {
  description = "Use an existing VPC instead of creating one. Empty = create new."
  type        = string
  default     = ""
}

# ── Compute ────────────────────────────────────────────────────────────────────

variable "ssh_key_ids" {
  description = "SSH key IDs or fingerprints (required by DO API for custom images; Talos ignores SSH)"
  type        = list(string)
  default     = []
}

# ── Networking ─────────────────────────────────────────────────────────────────

variable "trusted_cidrs" {
  description = "CIDRs allowed to reach Talos API (50000) and Kubernetes API (6443). Empty = open to all (not recommended for production)."
  type        = list(string)
  default     = []
}

variable "cluster_api_host_override" {
  description = "Pre-existing DNS name or IP for the Kubernetes API when enable_api_loadbalancer is false"
  type        = string
  default     = ""
}

# ── Database ───────────────────────────────────────────────────────────────────

variable "db_engine" {
  description = "Database engine (pg, mysql, mongodb)"
  type        = string
  default     = "pg"
}

variable "db_version" {
  description = "Database engine version"
  type        = string
  default     = "16"
}

variable "db_size" {
  description = "Database cluster node size slug"
  type        = string
  default     = "db-s-1vcpu-1gb"
}

variable "db_node_count" {
  description = "Number of database nodes (1 = standalone, 2+ = HA)"
  type        = number
  default     = 1
}

# ── Registry ───────────────────────────────────────────────────────────────────

variable "registry_name" {
  description = "DigitalOcean Container Registry name (globally unique)"
  type        = string
  default     = "consultores"
}

variable "registry_tier" {
  description = "DOCR subscription tier: starter, basic, professional"
  type        = string
  default     = "basic"
}

variable "registry_region" {
  description = "DOCR region"
  type        = string
  default     = "nyc3"
}

# ── DNS & TLS ──────────────────────────────────────────────────────────────────

variable "domain" {
  description = "Root domain (e.g. itcorretor.com)"
  type        = string
  default     = "itcorretor.com"
}

variable "letsencrypt_email" {
  description = "Email for Let's Encrypt certificate registration"
  type        = string
  default     = "devops@itcorretor.com"
}

# ── Local file paths ───────────────────────────────────────────────────────────

variable "kubeconfig_path" {
  description = "Path where the generated kubeconfig file is written"
  type        = string
  default     = "kubeconfig"
}

variable "talosconfig_path" {
  description = "Path where the generated talosconfig file is written"
  type        = string
  default     = "talosconfig"
}
