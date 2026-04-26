variable "aws_region" {
  description = "AWS region for the dev environment."
  type        = string
  default     = "eu-west-1"
}

variable "aws_profile" {
  description = "Optional local AWS CLI profile for manual Terraform usage."
  type        = string
  default     = ""
}

variable "aws_account_id" {
  description = "AWS account ID for IAM trust and deployment role ARNs."
  type        = string
  default     = "661000947340"

  validation {
    condition     = can(regex("^\\d{12}$", var.aws_account_id))
    error_message = "AWS account ID must be the 12-digit canonical account ID without separators."
  }
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

variable "deployment_branch" {
  description = "Git branch allowed to deploy the dev environment through GitHub OIDC."
  type        = string
  default     = "main"
}

variable "terraform_state_bucket_name" {
  description = "S3 bucket used by the dev Terraform remote backend."
  type        = string
  default     = "transitops-tfstate-661000947340-eu-west-1"
}

variable "terraform_lock_table_name" {
  description = "DynamoDB table used by the dev Terraform remote backend."
  type        = string
  default     = "transitops-tfstate-locks"
}

variable "create_github_oidc_provider" {
  description = "Whether Terraform should create the GitHub OIDC provider for this AWS account."
  type        = bool
  default     = true
}

variable "existing_github_oidc_provider_arn" {
  description = "Existing GitHub OIDC provider ARN when create_github_oidc_provider is false."
  type        = string
  default     = ""
}

variable "root_domain" {
  description = "Optional DNS root domain for the dev environment."
  type        = string
  default     = ""
}

variable "availability_zones" {
  description = "Target availability zones for the dev environment."
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

variable "api_image_tag" {
  description = "Container image tag used by the ECS task definition."
  type        = string
  default     = "latest"
}

variable "enable_https" {
  description = "Whether to create ACM/Route53 validation and an HTTPS ALB listener."
  type        = bool
  default     = false
}

variable "hosted_zone_id" {
  description = "Route53 hosted zone ID used when HTTPS is enabled."
  type        = string
  default     = ""
}

variable "log_retention_days" {
  description = "CloudWatch log retention in days."
  type        = number
  default     = 30
}

variable "jwt_issuer" {
  description = "JWT issuer used by the AWS runtime."
  type        = string
  default     = "TransitOps.Api"
}

variable "jwt_audience" {
  description = "JWT audience used by the AWS runtime."
  type        = string
  default     = "TransitOps.Client"
}

variable "jwt_expiration_minutes" {
  description = "JWT expiration in minutes used by the AWS runtime."
  type        = number
  default     = 60
}

variable "database_name" {
  description = "Initial PostgreSQL database name."
  type        = string
  default     = "transitops"
}

variable "database_username" {
  description = "RDS master username. Provide outside git when planning/applying."
  type        = string
  sensitive   = true
}

variable "database_password" {
  description = "RDS master password. Provide outside git when planning/applying."
  type        = string
  sensitive   = true
}

variable "database_instance_class" {
  description = "RDS instance class for the dev environment."
  type        = string
  default     = "db.t4g.micro"
}

variable "database_allocated_storage_gb" {
  description = "Initial RDS allocated storage in GiB."
  type        = number
  default     = 20
}

variable "database_max_allocated_storage_gb" {
  description = "Maximum RDS storage autoscaling limit in GiB."
  type        = number
  default     = 100
}

variable "database_backup_retention_days" {
  description = "RDS backup retention in days."
  type        = number
  default     = 7
}

variable "database_deletion_protection" {
  description = "Whether RDS deletion protection is enabled."
  type        = bool
  default     = false
}

variable "database_skip_final_snapshot" {
  description = "Whether final snapshot is skipped when destroying dev RDS."
  type        = bool
  default     = true
}

variable "ecs_cpu" {
  description = "Fargate task CPU units."
  type        = number
  default     = 512
}

variable "ecs_memory" {
  description = "Fargate task memory in MiB."
  type        = number
  default     = 1024
}

variable "ecs_desired_count" {
  description = "Desired ECS task count."
  type        = number
  default     = 1
}
