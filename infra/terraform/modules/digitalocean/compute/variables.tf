variable "project_name" {
  description = "Short name used in resource naming (e.g. 'consultores')"
  type        = string
}

variable "region" {
  description = "DigitalOcean region slug"
  type        = string
}

variable "vpc_id" {
  description = "VPC UUID for the droplets"
  type        = string
}

variable "talos_version" {
  description = "Talos OS version (e.g. v1.9.4)"
  type        = string
}

variable "talos_image_url" {
  description = "URL of the Talos disk image to register as a DigitalOcean custom image"
  type        = string
}

variable "control_plane_count" {
  description = "Number of control plane nodes (must be odd for etcd quorum)"
  type        = number

  validation {
    condition     = var.control_plane_count % 2 != 0
    error_message = "control_plane_count must be an odd number (1, 3, 5, …)."
  }
}

variable "control_plane_size" {
  description = "DigitalOcean Droplet size slug for control plane nodes"
  type        = string
}

variable "control_plane_user_data" {
  description = "Talos machine configuration for control plane nodes"
  type        = string
  sensitive   = true
}

variable "control_plane_backups_enabled" {
  description = "Enable DigitalOcean backups for control plane droplets"
  type        = bool
  default     = false
}

variable "worker_count" {
  description = "Number of worker nodes (0 = single-node, workloads on control plane)"
  type        = number
  default     = 0
}

variable "worker_size" {
  description = "DigitalOcean Droplet size slug for worker nodes"
  type        = string
  default     = "s-2vcpu-4gb"
}

variable "worker_user_data" {
  description = "Talos machine configuration for worker nodes"
  type        = string
  sensitive   = true
  default     = ""
}

variable "worker_backups_enabled" {
  description = "Enable DigitalOcean backups for worker droplets"
  type        = bool
  default     = false
}

variable "frontend_worker_count" {
  description = "Number of dedicated frontend worker nodes (tainted; not tagged 'worker' for DO ingress LB)"
  type        = number
  default     = 0
}

variable "frontend_worker_size" {
  description = "DigitalOcean Droplet size slug for frontend worker nodes"
  type        = string
  default     = "s-2vcpu-4gb"
}

variable "frontend_worker_user_data" {
  description = "Talos machine configuration for frontend worker nodes"
  type        = string
  sensitive   = true
  default     = ""
}

variable "frontend_worker_backups_enabled" {
  description = "Enable DigitalOcean backups for frontend worker droplets"
  type        = bool
  default     = false
}

variable "ssh_key_ids" {
  description = "SSH key IDs to attach (Talos ignores SSH, but DO API requires keys for custom images)"
  type        = list(string)
  default     = []
}
