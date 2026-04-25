variable "project_name" {
  description = "Short name used in resource naming (e.g. 'consultores')"
  type        = string
}

variable "region" {
  description = "DigitalOcean region slug"
  type        = string
}

variable "vpc_ip_range" {
  description = "CIDR for the new VPC"
  type        = string
  default     = "10.10.0.0/16"
}

variable "existing_vpc_id" {
  description = "ID of an existing VPC to use instead of creating a new one. Empty = create new."
  type        = string
  default     = ""
}

variable "enable_api_loadbalancer" {
  description = "Create a public load balancer for the Kubernetes API (port 6443)"
  type        = bool
  default     = true
}

variable "enable_ingress_loadbalancer" {
  description = "Create a public load balancer for NGINX Ingress (ports 80/443)"
  type        = bool
  default     = true
}

variable "ingress_target_tag" {
  description = "DO tag used to select nodes that receive ingress LB traffic: core workers ('worker'), frontend-only workers ('worker-frontend'), or control planes ('control-plane'). Use 'worker' when core workers run ingress; frontend-tainted nodes must not use the 'worker' tag."
  type        = string
  default     = "control-plane"
}

variable "trusted_cidrs" {
  description = "CIDRs allowed to reach the Talos API (50000) and Kubernetes API (6443). Empty list = open to 0.0.0.0/0 (not recommended for production)."
  type        = list(string)
  default     = []
}
