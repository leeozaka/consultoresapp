locals {
  create_vpc   = var.existing_vpc_id == ""
  vpc_id       = local.create_vpc ? digitalocean_vpc.main[0].id : data.digitalocean_vpc.existing[0].id
  vpc_ip_range = local.create_vpc ? digitalocean_vpc.main[0].ip_range : data.digitalocean_vpc.existing[0].ip_range

  # When trusted_cidrs is empty, fall back to open access (backward-compatible).
  # Set trusted_cidrs in tfvars to lock down Talos/K8s API ports in production.
  admin_source_cidrs = length(var.trusted_cidrs) > 0 ? var.trusted_cidrs : ["0.0.0.0/0", "::/0"]
}

# ── VPC ────────────────────────────────────────────────────────────────────────

resource "digitalocean_vpc" "main" {
  count = local.create_vpc ? 1 : 0

  name     = "${var.project_name}-vpc"
  region   = var.region
  ip_range = var.vpc_ip_range
}

data "digitalocean_vpc" "existing" {
  count = local.create_vpc ? 0 : 1
  id    = var.existing_vpc_id
}

# ── Tags ───────────────────────────────────────────────────────────────────────

resource "digitalocean_tag" "project" {
  name = var.project_name
}

resource "digitalocean_tag" "control_plane" {
  name = "control-plane"
}

resource "digitalocean_tag" "worker" {
  name = "worker"
}

resource "digitalocean_tag" "worker_frontend" {
  name = "worker-frontend"
}

resource "digitalocean_tag" "talos" {
  name = "talos"
}

# ── Firewall ───────────────────────────────────────────────────────────────────

resource "digitalocean_firewall" "k8s_nodes" {
  name = "${var.project_name}-k8s-nodes"
  tags = [digitalocean_tag.project.name]

  depends_on = [
    digitalocean_tag.project,
    digitalocean_tag.control_plane,
    digitalocean_tag.worker,
    digitalocean_tag.worker_frontend,
    digitalocean_tag.talos,
  ]

  # All traffic within the VPC (pod-to-pod, node-to-node)
  inbound_rule {
    protocol         = "tcp"
    port_range       = "1-65535"
    source_addresses = [local.vpc_ip_range]
  }

  inbound_rule {
    protocol         = "udp"
    port_range       = "1-65535"
    source_addresses = [local.vpc_ip_range]
  }

  # Talos API — locked to trusted_cidrs (or open if not set)
  inbound_rule {
    protocol         = "tcp"
    port_range       = "50000"
    source_addresses = local.admin_source_cidrs
  }

  # Kubernetes API — locked to trusted_cidrs (or open if not set)
  inbound_rule {
    protocol         = "tcp"
    port_range       = "6443"
    source_addresses = local.admin_source_cidrs
  }

  # HTTP/HTTPS for hostPort ingress (used when no LB is configured)
  inbound_rule {
    protocol         = "tcp"
    port_range       = "80-443"
    source_addresses = ["0.0.0.0/0", "::/0"]
  }

  # NGINX Ingress NodePorts
  inbound_rule {
    protocol         = "tcp"
    port_range       = "30080-30443"
    source_addresses = ["0.0.0.0/0", "::/0"]
  }

  outbound_rule {
    protocol              = "tcp"
    port_range            = "1-65535"
    destination_addresses = ["0.0.0.0/0", "::/0"]
  }

  outbound_rule {
    protocol              = "udp"
    port_range            = "1-65535"
    destination_addresses = ["0.0.0.0/0", "::/0"]
  }

  outbound_rule {
    protocol              = "icmp"
    destination_addresses = ["0.0.0.0/0", "::/0"]
  }
}

# ── Load Balancer — Kubernetes API ────────────────────────────────────────────

resource "digitalocean_loadbalancer" "k8s_api" {
  count    = var.enable_api_loadbalancer ? 1 : 0
  name     = "${var.project_name}-k8s-api"
  region   = var.region
  vpc_uuid = local.vpc_id

  forwarding_rule {
    entry_port      = 6443
    entry_protocol  = "tcp"
    target_port     = 6443
    target_protocol = "tcp"
  }

  healthcheck {
    port     = 6443
    protocol = "tcp"
  }

  droplet_tag                      = "control-plane"
  disable_lets_encrypt_dns_records = true
}

# ── Load Balancer — NGINX Ingress ─────────────────────────────────────────────
# Pre-created so its IP is known before cert-manager issues TLS certificates
# and before DNS records are configured.

resource "digitalocean_loadbalancer" "ingress" {
  count    = var.enable_ingress_loadbalancer ? 1 : 0
  name     = "${var.project_name}-ingress"
  region   = var.region
  vpc_uuid = local.vpc_id

  forwarding_rule {
    entry_port      = 80
    entry_protocol  = "tcp"
    target_port     = 80
    target_protocol = "tcp"
  }

  forwarding_rule {
    entry_port      = 443
    entry_protocol  = "tcp"
    target_port     = 443
    target_protocol = "tcp"
  }

  healthcheck {
    port     = 80
    protocol = "tcp"
  }

  droplet_tag                      = var.ingress_target_tag
  disable_lets_encrypt_dns_records = true
}
