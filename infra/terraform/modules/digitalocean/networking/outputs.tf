output "vpc_id" {
  description = "VPC UUID"
  value       = local.vpc_id
}

output "vpc_ip_range" {
  description = "CIDR of the VPC"
  value       = local.vpc_ip_range
}

output "k8s_api_lb_ip" {
  description = "Public IP of the Kubernetes API load balancer (null if disabled)"
  value       = var.enable_api_loadbalancer ? digitalocean_loadbalancer.k8s_api[0].ip : null
}

output "ingress_lb_ip" {
  description = "Public IP of the NGINX Ingress load balancer (null if disabled)"
  value       = var.enable_ingress_loadbalancer ? digitalocean_loadbalancer.ingress[0].ip : null
}

output "project_tag" {
  description = "Project tag name for use in other resource tag references"
  value       = digitalocean_tag.project.name
}
