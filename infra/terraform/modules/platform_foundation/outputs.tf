output "project_slug" {
  description = "Canonical project slug."
  value       = var.project_slug
}

output "service_slug" {
  description = "Canonical primary service slug."
  value       = var.service_slug
}

output "environment" {
  description = "Environment name."
  value       = var.environment
}

output "aws_region" {
  description = "AWS region used by the environment."
  value       = var.aws_region
}

output "availability_zones" {
  description = "Availability zones selected for the environment."
  value       = var.availability_zones
}

output "name_prefix" {
  description = "Canonical shared resource-name prefix."
  value       = local.name_prefix
}

output "common_tags" {
  description = "Mandatory baseline tags for shared platform resources."
  value       = local.common_tags
}

output "api_service_tags" {
  description = "Baseline tags for application-serving resources."
  value       = local.api_service_tags
}

output "dns_api_hostname" {
  description = "Expected API hostname when a root domain is configured."
  value       = local.dns_api_hostname
}

output "secrets_prefix" {
  description = "Secrets Manager prefix for application secrets."
  value       = local.secrets_prefix
}

output "parameters_prefix" {
  description = "SSM Parameter Store prefix for application parameters."
  value       = local.parameters_prefix
}

output "terraform_state_key" {
  description = "Conventional remote-state key for this environment root."
  value       = local.terraform_state_key
}
