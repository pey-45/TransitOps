variable "project_slug" {
  description = "Lowercase project identifier used in AWS resource names."
  type        = string
}

variable "service_slug" {
  description = "Lowercase primary service identifier."
  type        = string
}

variable "environment" {
  description = "Long-lived environment name."
  type        = string

  validation {
    condition     = contains(["dev", "prod"], var.environment)
    error_message = "Environment must be one of: dev, prod."
  }
}

variable "aws_region" {
  description = "AWS region used by the environment."
  type        = string
}

variable "owner" {
  description = "Human owner tag value."
  type        = string
}

variable "repository" {
  description = "Source repository tag value."
  type        = string
}

variable "root_domain" {
  description = "Optional DNS root domain. Leave empty until a real domain exists."
  type        = string
  default     = ""
}

variable "availability_zones" {
  description = "Target availability zones for the environment foundation."
  type        = list(string)
}

variable "tags_override" {
  description = "Optional extra tags merged on top of the mandatory baseline."
  type        = map(string)
  default     = {}
}
