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

# ─── Network outputs ──────────────────────────────────────────────────────────

output "vpc_id" {
  description = "ID of the environment VPC."
  value       = aws_vpc.this.id
}

output "vpc_cidr" {
  description = "CIDR block of the environment VPC."
  value       = aws_vpc.this.cidr_block
}

output "public_subnet_ids" {
  description = "IDs of the public subnets (ALB, NAT)."
  value       = aws_subnet.public[*].id
}

output "app_subnet_ids" {
  description = "IDs of the private app subnets (ECS tasks)."
  value       = aws_subnet.app[*].id
}

output "data_subnet_ids" {
  description = "IDs of the private data subnets (RDS)."
  value       = aws_subnet.data[*].id
}

output "internet_gateway_id" {
  description = "ID of the internet gateway."
  value       = aws_internet_gateway.this.id
}

output "nat_gateway_id" {
  description = "ID of the NAT gateway."
  value       = aws_nat_gateway.this.id
}

# ─── Security group outputs ───────────────────────────────────────────────────

output "alb_security_group_id" {
  description = "ID of the ALB security group."
  value       = aws_security_group.alb.id
}

output "ecs_security_group_id" {
  description = "ID of the ECS security group."
  value       = aws_security_group.ecs.id
}

output "rds_security_group_id" {
  description = "ID of the RDS security group."
  value       = aws_security_group.rds.id
}
