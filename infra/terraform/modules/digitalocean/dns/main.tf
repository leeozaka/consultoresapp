resource "digitalocean_domain" "main" {
  name = var.domain_name
}

# Root domain → ingress
resource "digitalocean_record" "root" {
  domain = digitalocean_domain.main.id
  type   = "A"
  name   = "@"
  value  = var.ingress_ip
  ttl    = 300
}

# Wildcard → ingress (covers all tenant subdomains)
resource "digitalocean_record" "wildcard" {
  domain = digitalocean_domain.main.id
  type   = "A"
  name   = "*"
  value  = var.ingress_ip
  ttl    = 300
}

# api subdomain → ingress (routed to the API by NGINX Ingress)
resource "digitalocean_record" "api" {
  domain = digitalocean_domain.main.id
  type   = "A"
  name   = "api"
  value  = var.ingress_ip
  ttl    = 300
}
