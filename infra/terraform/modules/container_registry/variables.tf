variable "project_slug" {
  description = "Lowercase project identifier used in repository names."
  type        = string
}

variable "service_slug" {
  description = "Lowercase service identifier used in repository names."
  type        = string
}

variable "image_scan_on_push" {
  description = "Whether ECR scans images when they are pushed."
  type        = bool
  default     = true
}

variable "max_image_count" {
  description = "Maximum number of images retained in the repository."
  type        = number
  default     = 20
}

variable "tags" {
  description = "Tags applied to ECR resources."
  type        = map(string)
  default     = {}
}
