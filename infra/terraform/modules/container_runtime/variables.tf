variable "name_prefix" {
  description = "Canonical environment resource-name prefix."
  type        = string
}

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

variable "aws_region" {
  description = "AWS region used by the runtime."
  type        = string
}

variable "image_uri" {
  description = "Container image URI used by the ECS task definition."
  type        = string
}

variable "container_port" {
  description = "API container port."
  type        = number
  default     = 8080
}

variable "public_subnet_ids" {
  description = "Public subnet IDs used by the ALB."
  type        = list(string)
}

variable "app_subnet_ids" {
  description = "Private app subnet IDs used by ECS tasks."
  type        = list(string)
}

variable "vpc_id" {
  description = "VPC ID used by ALB target groups."
  type        = string
}

variable "alb_security_group_id" {
  description = "Security group ID attached to the ALB."
  type        = string
}

variable "ecs_security_group_id" {
  description = "Security group ID attached to ECS tasks."
  type        = string
}

variable "log_group_name" {
  description = "CloudWatch log group name used by the API container."
  type        = string
}

variable "cpu" {
  description = "Fargate task CPU units."
  type        = number
  default     = 512
}

variable "memory" {
  description = "Fargate task memory in MiB."
  type        = number
  default     = 1024
}

variable "desired_count" {
  description = "Desired ECS task count."
  type        = number
  default     = 1
}

variable "health_check_path" {
  description = "HTTP health check path used by ALB target group."
  type        = string
  default     = "/api/v1/health/ready"
}

variable "health_check_interval_seconds" {
  description = "ALB target-group health check interval in seconds."
  type        = number
  default     = 30
}

variable "health_check_timeout_seconds" {
  description = "ALB target-group health check timeout in seconds."
  type        = number
  default     = 5
}

variable "health_check_healthy_threshold" {
  description = "Number of consecutive successful ALB health checks required before a target is healthy."
  type        = number
  default     = 2
}

variable "health_check_unhealthy_threshold" {
  description = "Number of consecutive failed ALB health checks required before a target is unhealthy."
  type        = number
  default     = 3
}

variable "target_group_deregistration_delay_seconds" {
  description = "Time in seconds for the target group to drain requests during deployments."
  type        = number
  default     = 30
}

variable "health_check_grace_period_seconds" {
  description = "ECS service health check grace period in seconds."
  type        = number
  default     = 60
}

variable "environment_variables" {
  description = "Plain environment variables injected into the API container."
  type        = map(string)
  default     = {}
}

variable "secrets" {
  description = "Secret environment variables keyed by .NET configuration key, with Secrets Manager or SSM ARNs as values."
  type        = map(string)
  default     = {}
}

variable "enable_https" {
  description = "Whether to create ACM/Route53 validation and an HTTPS ALB listener."
  type        = bool
  default     = false
}

variable "domain_name" {
  description = "API DNS name used when HTTPS is enabled."
  type        = string
  default     = null
}

variable "hosted_zone_id" {
  description = "Route53 hosted zone ID used when HTTPS is enabled."
  type        = string
  default     = ""
}

variable "tags" {
  description = "Tags applied to runtime resources."
  type        = map(string)
  default     = {}
}
