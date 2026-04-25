variable "domain_name" {
  description = "Root domain (e.g. itcorretor.com)"
  type        = string
}

variable "ingress_ip" {
  description = "Public IP of the ingress load balancer — A records point here"
  type        = string
}
