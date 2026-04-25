locals {
  # Derived from profile/overrides
  worker_count                = local.cfg.worker_count
  frontend_worker_count       = local.cfg.frontend_worker_count
  control_plane_count         = local.cfg.control_plane_count
  workloads_on_control_planes = local.worker_count == 0 ? true : false

  # Ingress LB targets core workers (tag "worker"). Frontend-only nodes use
  # "worker-frontend" so they are not LB backends (no ingress listener there).
  ingress_target_tag = (
    local.worker_count > 0 ? "worker" : (
      local.frontend_worker_count > 0 ? "worker-frontend" : "control-plane"
    )
  )

  # Kubernetes API host: LB IP if enabled, otherwise use the override
  k8s_api_host = local.cfg.enable_api_loadbalancer ? module.networking.k8s_api_lb_ip : var.cluster_api_host_override

  # Public ingress IP: LB IP if enabled, otherwise first core worker, else frontend worker, else control plane
  ingress_public_ip = (
    local.cfg.enable_ingress_loadbalancer
    ? module.networking.ingress_lb_ip
    : (
      local.worker_count > 0
      ? module.compute.worker_ips[0]
      : (
        local.frontend_worker_count > 0
        ? module.compute.frontend_worker_ips[0]
        : module.compute.control_plane_ips[0]
      )
    )
  )

  kubeconfig_path  = abspath(var.kubeconfig_path)
  talosconfig_path = abspath(var.talosconfig_path)
}

check "api_host_required" {
  assert {
    condition     = local.cfg.enable_api_loadbalancer || trimspace(var.cluster_api_host_override) != ""
    error_message = "cluster_api_host_override must be set when enable_api_loadbalancer is false."
  }
}

check "frontend_workers_require_core_workers" {
  assert {
    condition     = local.frontend_worker_count == 0 || local.worker_count > 0
    error_message = "Dedicated frontend workers require at least one core worker (worker_count > 0) so API, ingress, and system workloads stay off the tainted frontend pool."
  }
}

# ── Modules ────────────────────────────────────────────────────────────────────

module "networking" {
  source = "../modules/digitalocean/networking"

  project_name                = "consultores"
  region                      = var.region
  vpc_ip_range                = var.vpc_ip_range
  existing_vpc_id             = var.existing_vpc_id
  enable_api_loadbalancer     = local.cfg.enable_api_loadbalancer
  enable_ingress_loadbalancer = local.cfg.enable_ingress_loadbalancer
  ingress_target_tag          = local.ingress_target_tag
  trusted_cidrs               = var.trusted_cidrs
}

module "compute" {
  source = "../modules/digitalocean/compute"

  project_name                    = "consultores"
  region                          = var.region
  vpc_id                          = module.networking.vpc_id
  talos_version                   = var.talos_version
  talos_image_url                 = "https://factory.talos.dev/image/${talos_image_factory_schematic.this.id}/${var.talos_version}/digital-ocean-amd64.raw.gz"
  control_plane_count             = local.control_plane_count
  control_plane_size              = local.cfg.control_plane_size
  control_plane_user_data         = data.talos_machine_configuration.control_plane.machine_configuration
  control_plane_backups_enabled   = local.cfg.control_plane_backups_enabled
  worker_count                    = local.worker_count
  worker_size                     = local.cfg.worker_size
  worker_user_data                = local.worker_count > 0 ? data.talos_machine_configuration.worker.machine_configuration : ""
  worker_backups_enabled          = local.cfg.worker_backups_enabled
  frontend_worker_count           = local.frontend_worker_count
  frontend_worker_size            = local.cfg.frontend_worker_size
  frontend_worker_user_data       = local.frontend_worker_count > 0 ? data.talos_machine_configuration.frontend_worker.machine_configuration : ""
  frontend_worker_backups_enabled = local.cfg.frontend_worker_backups_enabled
  ssh_key_ids                     = var.ssh_key_ids
}

module "database" {
  count  = local.cfg.enable_managed_database ? 1 : 0
  source = "../modules/digitalocean/database"

  project_name   = "consultores"
  region         = var.region
  vpc_id         = module.networking.vpc_id
  engine         = var.db_engine
  engine_version = var.db_version
  size           = var.db_size
  node_count     = var.db_node_count
  node_tag       = module.networking.project_tag
}

module "registry" {
  count  = local.cfg.enable_container_registry ? 1 : 0
  source = "../modules/digitalocean/registry"

  name   = var.registry_name
  tier   = var.registry_tier
  region = var.registry_region
}

module "dns" {
  count  = local.cfg.manage_dns ? 1 : 0
  source = "../modules/digitalocean/dns"

  domain_name = var.domain
  ingress_ip  = local.ingress_public_ip
}
