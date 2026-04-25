output "domain_id" {
  description = "DigitalOcean domain resource ID"
  value       = digitalocean_domain.main.id
}

output "domain_name" {
  description = "The registered domain name"
  value       = digitalocean_domain.main.name
}
