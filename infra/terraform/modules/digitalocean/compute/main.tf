# ── Talos image registration ───────────────────────────────────────────────────
# Registers the Talos raw disk image as a DigitalOcean custom image.
# The image URL is built by the caller (from the talos_image_factory_schematic).

resource "digitalocean_custom_image" "talos" {
  name         = "talos-${replace(var.talos_version, ".", "-")}"
  url          = var.talos_image_url
  regions      = [var.region]
  description  = "Talos Linux ${var.talos_version} — managed by Terraform"
  distribution = "Unknown OS"
  tags         = ["talos"]
}

# ── Control plane Droplets ────────────────────────────────────────────────────

resource "digitalocean_droplet" "control_plane" {
  count  = var.control_plane_count
  name   = "${var.project_name}-cp-${count.index + 1}"
  image  = digitalocean_custom_image.talos.id
  size   = var.control_plane_size
  region = var.region

  vpc_uuid          = var.vpc_id
  ipv6              = false
  backups           = var.control_plane_backups_enabled
  monitoring        = false
  graceful_shutdown = false

  # Talos does not use SSH; the key is required by the DO API for custom images.
  ssh_keys = var.ssh_key_ids

  # Machine configuration applied on first boot — Talos ignores it on subsequent boots.
  user_data = var.control_plane_user_data

  tags = [var.project_name, "control-plane", "talos"]

  lifecycle {
    # Upgrades happen via talosctl, not by replacing droplets.
    ignore_changes = [user_data, image]
  }
}

# ── Worker Droplets ───────────────────────────────────────────────────────────

resource "digitalocean_droplet" "worker" {
  count  = var.worker_count
  name   = "${var.project_name}-worker-${count.index + 1}"
  image  = digitalocean_custom_image.talos.id
  size   = var.worker_size
  region = var.region

  vpc_uuid          = var.vpc_id
  ipv6              = false
  backups           = var.worker_backups_enabled
  monitoring        = false
  graceful_shutdown = false

  ssh_keys  = var.ssh_key_ids
  user_data = var.worker_user_data

  tags = [var.project_name, "worker", "talos"]

  lifecycle {
    ignore_changes = [user_data, image]
  }
}

# ── Frontend worker Droplets ─────────────────────────────────────────────────
# Tagged worker-frontend (not "worker") so the DO ingress load balancer only
# targets core workers where NGINX Ingress is expected to listen on :80/:443.

resource "digitalocean_droplet" "frontend_worker" {
  count  = var.frontend_worker_count
  name   = "${var.project_name}-frontend-${count.index + 1}"
  image  = digitalocean_custom_image.talos.id
  size   = var.frontend_worker_size
  region = var.region

  vpc_uuid          = var.vpc_id
  ipv6              = false
  backups           = var.frontend_worker_backups_enabled
  monitoring        = false
  graceful_shutdown = false

  ssh_keys  = var.ssh_key_ids
  user_data = var.frontend_worker_user_data

  tags = [var.project_name, "worker-frontend", "talos"]

  lifecycle {
    ignore_changes = [user_data, image]
  }
}
