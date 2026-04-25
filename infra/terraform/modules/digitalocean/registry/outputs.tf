output "endpoint" {
  description = "Registry endpoint (e.g. registry.digitalocean.com/consultores)"
  value       = digitalocean_container_registry.this.endpoint
}

output "server_url" {
  description = "Registry server hostname for docker login"
  value       = digitalocean_container_registry.this.server_url
}

output "docker_credentials" {
  description = "Docker credentials JSON for use as a Kubernetes imagePullSecret"
  value       = digitalocean_container_registry_docker_credentials.this.docker_credentials
  sensitive   = true
}
