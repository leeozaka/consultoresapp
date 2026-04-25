resource "digitalocean_container_registry" "this" {
  name                   = var.name
  subscription_tier_slug = var.tier
  region                 = var.region
}

# Credentials used by Kubernetes as an imagePullSecret
resource "digitalocean_container_registry_docker_credentials" "this" {
  registry_name = digitalocean_container_registry.this.name
}
