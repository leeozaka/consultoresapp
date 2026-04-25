variable "name" {
  description = "Container registry name (globally unique within DigitalOcean)"
  type        = string
}

variable "tier" {
  description = "DOCR subscription tier: starter (free/500MB), basic ($5/5GB), professional ($20/unlimited)"
  type        = string
  default     = "basic"

  validation {
    condition     = contains(["starter", "basic", "professional"], var.tier)
    error_message = "Tier must be starter, basic, or professional."
  }
}

variable "region" {
  description = "DOCR region (e.g. nyc3, sfo3, fra1)"
  type        = string
  default     = "nyc3"
}
