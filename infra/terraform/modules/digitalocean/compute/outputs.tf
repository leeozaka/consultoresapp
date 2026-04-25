output "control_plane_ips" {
  description = "Public IPv4 addresses of control plane nodes"
  value       = [for d in digitalocean_droplet.control_plane : d.ipv4_address]
}

output "worker_ips" {
  description = "Public IPv4 addresses of worker nodes"
  value       = [for d in digitalocean_droplet.worker : d.ipv4_address]
}

output "control_plane_ids" {
  description = "Droplet IDs of control plane nodes"
  value       = [for d in digitalocean_droplet.control_plane : d.id]
}

output "worker_ids" {
  description = "Droplet IDs of worker nodes"
  value       = [for d in digitalocean_droplet.worker : d.id]
}

output "frontend_worker_ips" {
  description = "Public IPv4 addresses of dedicated frontend worker nodes"
  value       = [for d in digitalocean_droplet.frontend_worker : d.ipv4_address]
}

output "frontend_worker_ids" {
  description = "Droplet IDs of dedicated frontend worker nodes"
  value       = [for d in digitalocean_droplet.frontend_worker : d.id]
}
