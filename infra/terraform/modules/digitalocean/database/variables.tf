variable "project_name" {
  description = "Short name used in resource naming"
  type        = string
}

variable "region" {
  description = "DigitalOcean region slug"
  type        = string
}

variable "vpc_id" {
  description = "VPC UUID to place the database cluster in"
  type        = string
}

variable "engine" {
  description = "Database engine (pg, mysql, mongodb)"
  type        = string
  default     = "pg"
}

variable "engine_version" {
  description = "Database engine version"
  type        = string
  default     = "16"
}

variable "size" {
  description = "Database cluster node size slug"
  type        = string
  default     = "db-s-1vcpu-1gb"
}

variable "node_count" {
  description = "Number of database nodes (1 = standalone, 2+ = HA)"
  type        = number
  default     = 1
}

variable "db_name" {
  description = "Name of the application database to create"
  type        = string
  default     = "homeless"
}

variable "db_user" {
  description = "Name of the application database user to create"
  type        = string
  default     = "consultores_app"
}

variable "node_tag" {
  description = "DO tag used to restrict DB firewall access to tagged droplets"
  type        = string
}
