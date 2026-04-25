# ── Cluster access ─────────────────────────────────────────────────────────────

output "kubeconfig" {
  description = "Raw Kubernetes admin kubeconfig. Retrieve with: terraform output -raw kubeconfig"
  value       = talos_cluster_kubeconfig.this.kubeconfig_raw
  sensitive   = true
}

output "kubeconfig_path" {
  description = "Filesystem path where the kubeconfig file is written"
  value       = local.kubeconfig_path
}

output "talosconfig" {
  description = "talosctl client config. Retrieve with: terraform output -raw talosconfig"
  value       = data.talos_client_configuration.this.talos_config
  sensitive   = true
}

output "talosconfig_path" {
  description = "Filesystem path where the talosconfig file is written"
  value       = local.talosconfig_path
}

output "k8s_api_endpoint" {
  description = "Kubernetes API server endpoint"
  value       = "https://${local.k8s_api_host}:6443"
}

# ── Kubernetes client configuration (consumed by 02-platform) ─────────────────
# These outputs allow 02-platform to configure the kubernetes/helm providers
# without needing a kubeconfig file on disk.

output "k8s_client_host" {
  description = "Kubernetes API host for provider configuration"
  value       = talos_cluster_kubeconfig.this.kubernetes_client_configuration.host
  sensitive   = true
}

output "k8s_client_ca_certificate" {
  description = "Base64-encoded cluster CA certificate"
  value       = talos_cluster_kubeconfig.this.kubernetes_client_configuration.ca_certificate
  sensitive   = true
}

output "k8s_client_certificate" {
  description = "Base64-encoded client certificate"
  value       = talos_cluster_kubeconfig.this.kubernetes_client_configuration.client_certificate
  sensitive   = true
}

output "k8s_client_key" {
  description = "Base64-encoded client key"
  value       = talos_cluster_kubeconfig.this.kubernetes_client_configuration.client_key
  sensitive   = true
}

# ── Ingress ────────────────────────────────────────────────────────────────────

output "ingress_ip" {
  description = "Public IP for DNS A records"
  value       = local.ingress_public_ip
}

# ── Registry ───────────────────────────────────────────────────────────────────

output "registry_endpoint" {
  description = "DOCR endpoint (e.g. registry.digitalocean.com/consultores)"
  value       = local.cfg.enable_container_registry ? module.registry[0].endpoint : null
}

output "registry_server" {
  description = "DOCR server hostname for docker login"
  value       = local.cfg.enable_container_registry ? module.registry[0].server_url : null
}

output "docr_docker_credentials" {
  description = "Docker credentials JSON for Kubernetes imagePullSecret"
  value       = local.cfg.enable_container_registry ? module.registry[0].docker_credentials : null
  sensitive   = true
}

# ── Database ───────────────────────────────────────────────────────────────────

output "db_private_host" {
  description = "Private hostname of the managed database (VPC-only)"
  value       = local.cfg.enable_managed_database ? module.database[0].private_host : null
  sensitive   = true
}

output "db_port" {
  description = "Database port"
  value       = local.cfg.enable_managed_database ? module.database[0].port : null
}

output "db_user" {
  description = "Application database username"
  value       = local.cfg.enable_managed_database ? module.database[0].user : null
}

output "db_password" {
  description = "Application database password"
  value       = local.cfg.enable_managed_database ? module.database[0].password : null
  sensitive   = true
}

output "db_write_connection_string" {
  description = "Npgsql write connection string for ConnectionStrings__DefaultConnection"
  value       = local.cfg.enable_managed_database ? module.database[0].write_connection_string : null
  sensitive   = true
}

output "db_read_connection_string" {
  description = "Npgsql read connection string for ConnectionStrings__ReadConnection"
  value       = local.cfg.enable_managed_database ? module.database[0].read_connection_string : null
  sensitive   = true
}

# ── Nodes ──────────────────────────────────────────────────────────────────────

output "control_plane_ips" {
  description = "Public IPs of control plane nodes"
  value       = module.compute.control_plane_ips
}

output "worker_ips" {
  description = "Public IPs of worker nodes"
  value       = module.compute.worker_ips
}

output "frontend_worker_ips" {
  description = "Public IPs of dedicated frontend worker nodes (tainted; production profile default)"
  value       = module.compute.frontend_worker_ips
}
