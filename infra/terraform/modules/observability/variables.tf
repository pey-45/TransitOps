variable "project_slug" {
  description = "Lowercase project identifier."
  type        = string
}

variable "environment" {
  description = "Environment name."
  type        = string
}

variable "service_slug" {
  description = "Lowercase service identifier."
  type        = string
}

variable "log_retention_days" {
  description = "CloudWatch log retention in days."
  type        = number
  default     = 30
}

variable "tags" {
  description = "Tags applied to observability resources."
  type        = map(string)
  default     = {}
}
