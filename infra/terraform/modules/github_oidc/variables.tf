variable "name_prefix" {
  description = "Canonical environment resource-name prefix."
  type        = string
}

variable "repository" {
  description = "GitHub repository in owner/name form."
  type        = string
}

variable "branch" {
  description = "Git branch allowed to assume the deployment role."
  type        = string
  default     = "main"
}

variable "aws_account_id" {
  description = "AWS account ID that owns the deployment role."
  type        = string
}

variable "aws_region" {
  description = "AWS region for regional resource ARNs."
  type        = string
}

variable "terraform_state_bucket_name" {
  description = "S3 bucket used by the Terraform remote backend."
  type        = string
}

variable "terraform_lock_table_name" {
  description = "DynamoDB table used by the Terraform remote backend."
  type        = string
}

variable "create_oidc_provider" {
  description = "Whether Terraform should create the GitHub OIDC provider in this AWS account."
  type        = bool
  default     = true
}

variable "existing_oidc_provider_arn" {
  description = "Existing GitHub OIDC provider ARN to use when create_oidc_provider is false."
  type        = string
  default     = ""
}

variable "github_oidc_thumbprints" {
  description = "Thumbprints accepted for token.actions.githubusercontent.com."
  type        = list(string)
  default = [
    "6938fd4d98bab03faadb97b34396831e3780aea1",
    "1b511abead59c6ce207077c0bf0e0043b1382612"
  ]
}

variable "tags" {
  description = "Tags applied to taggable IAM resources."
  type        = map(string)
  default     = {}
}
