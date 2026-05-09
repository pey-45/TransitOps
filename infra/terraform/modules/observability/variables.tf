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

variable "aws_region" {
  description = "AWS region used by CloudWatch dashboard widgets."
  type        = string
}

variable "alb_arn_suffix" {
  description = "ALB ARN suffix used by AWS/ApplicationELB metrics."
  type        = string
  default     = ""
}

variable "target_group_arn_suffix" {
  description = "Target group ARN suffix used by AWS/ApplicationELB metrics."
  type        = string
  default     = ""
}

variable "ecs_cluster_name" {
  description = "ECS cluster name used by AWS/ECS metrics."
  type        = string
  default     = ""
}

variable "ecs_service_name" {
  description = "ECS service name used by AWS/ECS metrics."
  type        = string
  default     = ""
}

variable "db_instance_identifier" {
  description = "RDS DB instance identifier used by AWS/RDS metrics."
  type        = string
  default     = ""
}

variable "alarm_email" {
  description = "Optional email address subscribed to runtime alarm notifications."
  type        = string
  default     = ""
}

variable "enable_alarm_actions" {
  description = "Whether CloudWatch alarms should notify the configured SNS topic."
  type        = bool
  default     = true
}

variable "alb_5xx_threshold" {
  description = "ALB target 5XX count threshold over the evaluation window."
  type        = number
  default     = 5
}

variable "alb_response_time_threshold_seconds" {
  description = "ALB target response-time threshold in seconds."
  type        = number
  default     = 2
}

variable "ecs_cpu_threshold_percent" {
  description = "ECS service average CPU utilization threshold."
  type        = number
  default     = 80
}

variable "ecs_memory_threshold_percent" {
  description = "ECS service average memory utilization threshold."
  type        = number
  default     = 80
}

variable "rds_cpu_threshold_percent" {
  description = "RDS average CPU utilization threshold."
  type        = number
  default     = 80
}

variable "rds_connections_threshold" {
  description = "RDS average database connections threshold."
  type        = number
  default     = 80
}

variable "rds_free_storage_threshold_bytes" {
  description = "RDS free storage threshold in bytes."
  type        = number
  default     = 5368709120
}

variable "tags" {
  description = "Tags applied to observability resources."
  type        = map(string)
  default     = {}
}
