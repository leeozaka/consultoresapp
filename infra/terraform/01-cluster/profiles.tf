# ── Deployment profiles ────────────────────────────────────────────────────────
# Each profile bundles sensible defaults for a given environment tier.
# Variables set explicitly in terraform.tfvars override the profile value
# (any non-null variable wins over the profile default).

locals {
  profiles = {
    # Lowest-cost single-node setup for local dev / prototyping.
    # One control plane, no workers, no LB, no managed DB, no registry, no DNS.
    dev = {
      control_plane_count             = 1
      worker_count                    = 0
      frontend_worker_count           = 0
      control_plane_size              = "s-1vcpu-2gb"
      worker_size                     = "s-1vcpu-2gb"
      frontend_worker_size            = "s-1vcpu-2gb"
      enable_api_loadbalancer         = true
      enable_ingress_loadbalancer     = false
      enable_managed_database         = false
      enable_container_registry       = false
      manage_dns                      = false
      control_plane_backups_enabled   = false
      worker_backups_enabled          = false
      frontend_worker_backups_enabled = false
    }

    # Single control plane + one worker. Managed DB, registry, and DNS enabled.
    staging = {
      control_plane_count             = 1
      worker_count                    = 1
      frontend_worker_count           = 0
      control_plane_size              = "s-2vcpu-4gb"
      worker_size                     = "s-2vcpu-4gb"
      frontend_worker_size            = "s-2vcpu-4gb"
      enable_api_loadbalancer         = true
      enable_ingress_loadbalancer     = true
      enable_managed_database         = true
      enable_container_registry       = true
      manage_dns                      = true
      control_plane_backups_enabled   = false
      worker_backups_enabled          = false
      frontend_worker_backups_enabled = false
    }

    # HA: 3 control planes + 2 core workers + 2 dedicated frontend workers (tainted).
    production = {
      control_plane_count             = 3
      worker_count                    = 2
      frontend_worker_count           = 2
      control_plane_size              = "s-2vcpu-4gb"
      worker_size                     = "s-2vcpu-4gb"
      frontend_worker_size            = "s-2vcpu-4gb"
      enable_api_loadbalancer         = true
      enable_ingress_loadbalancer     = true
      enable_managed_database         = true
      enable_container_registry       = true
      manage_dns                      = true
      control_plane_backups_enabled   = true
      worker_backups_enabled          = true
      frontend_worker_backups_enabled = true
    }
  }

  # Merge: explicit variable wins over profile default (coalesce picks first non-null).
  cfg = {
    control_plane_count             = coalesce(var.control_plane_count, local.profiles[var.profile].control_plane_count)
    worker_count                    = coalesce(var.worker_count, local.profiles[var.profile].worker_count)
    frontend_worker_count           = coalesce(var.frontend_worker_count, local.profiles[var.profile].frontend_worker_count)
    control_plane_size              = coalesce(var.control_plane_size, local.profiles[var.profile].control_plane_size)
    worker_size                     = coalesce(var.worker_size, local.profiles[var.profile].worker_size)
    frontend_worker_size            = coalesce(var.frontend_worker_size, local.profiles[var.profile].frontend_worker_size)
    enable_api_loadbalancer         = coalesce(var.enable_api_loadbalancer, local.profiles[var.profile].enable_api_loadbalancer)
    enable_ingress_loadbalancer     = coalesce(var.enable_ingress_loadbalancer, local.profiles[var.profile].enable_ingress_loadbalancer)
    enable_managed_database         = coalesce(var.enable_managed_database, local.profiles[var.profile].enable_managed_database)
    enable_container_registry       = coalesce(var.enable_container_registry, local.profiles[var.profile].enable_container_registry)
    manage_dns                      = coalesce(var.manage_dns, local.profiles[var.profile].manage_dns)
    control_plane_backups_enabled   = coalesce(var.control_plane_backups_enabled, local.profiles[var.profile].control_plane_backups_enabled)
    worker_backups_enabled          = coalesce(var.worker_backups_enabled, local.profiles[var.profile].worker_backups_enabled)
    frontend_worker_backups_enabled = coalesce(var.frontend_worker_backups_enabled, local.profiles[var.profile].frontend_worker_backups_enabled)
  }
}
