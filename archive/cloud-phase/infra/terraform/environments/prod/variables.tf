variable "aws_region" {
  description = "AWS region for the prod environment."
  type        = string
  default     = "eu-west-1"
}

variable "aws_profile" {
  description = "Optional local AWS CLI profile for manual Terraform usage."
  type        = string
  default     = ""
}

variable "project_slug" {
  description = "Lowercase project slug."
  type        = string
  default     = "transitops"
}

variable "service_slug" {
  description = "Primary application service slug."
  type        = string
  default     = "api"
}

variable "owner" {
  description = "Human owner tag value."
  type        = string
  default     = "pey"
}

variable "repository" {
  description = "Repository tag value."
  type        = string
  default     = "pey-45/TransitOps"
}

variable "root_domain" {
  description = "Optional DNS root domain for the prod environment."
  type        = string
  default     = ""
}

variable "availability_zones" {
  description = "Target availability zones for the prod environment."
  type        = list(string)
  default     = ["eu-west-1a", "eu-west-1b"]
}

variable "tags_override" {
  description = "Optional environment-specific tag overrides."
  type        = map(string)
  default     = {}
}

variable "api_container_port" {
  description = "Port the API container listens on inside ECS tasks."
  type        = number
  default     = 8080
}
