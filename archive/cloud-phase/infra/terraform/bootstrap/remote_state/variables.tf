variable "aws_region" {
  description = "AWS region where the remote Terraform state resources will live."
  type        = string
  default     = "eu-west-1"
}

variable "aws_profile" {
  description = "Optional local AWS CLI profile used for manual bootstrap execution."
  type        = string
  default     = ""
}

variable "project_slug" {
  description = "Lowercase project slug used in global bootstrap resource names."
  type        = string
  default     = "transitops"
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

variable "tags_override" {
  description = "Optional extra tags merged on top of the baseline tags."
  type        = map(string)
  default     = {}
}
