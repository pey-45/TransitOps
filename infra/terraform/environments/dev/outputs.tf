output "foundation" {
  description = "Shared environment foundation values for the dev root."
  value = {
    project_slug        = module.platform_foundation.project_slug
    service_slug        = module.platform_foundation.service_slug
    environment         = module.platform_foundation.environment
    aws_region          = module.platform_foundation.aws_region
    availability_zones  = module.platform_foundation.availability_zones
    name_prefix         = module.platform_foundation.name_prefix
    common_tags         = module.platform_foundation.common_tags
    api_service_tags    = module.platform_foundation.api_service_tags
    dns_api_hostname    = module.platform_foundation.dns_api_hostname
    secrets_prefix      = module.platform_foundation.secrets_prefix
    parameters_prefix   = module.platform_foundation.parameters_prefix
    terraform_state_key = module.platform_foundation.terraform_state_key
  }
}

output "network" {
  description = "Network resource IDs for the dev environment."
  value = {
    vpc_id              = module.platform_foundation.vpc_id
    vpc_cidr            = module.platform_foundation.vpc_cidr
    public_subnet_ids   = module.platform_foundation.public_subnet_ids
    app_subnet_ids      = module.platform_foundation.app_subnet_ids
    data_subnet_ids     = module.platform_foundation.data_subnet_ids
    internet_gateway_id = module.platform_foundation.internet_gateway_id
    nat_gateway_id      = module.platform_foundation.nat_gateway_id
  }
}

output "security_groups" {
  description = "Security group IDs for the dev environment."
  value = {
    alb_id = module.platform_foundation.alb_security_group_id
    ecs_id = module.platform_foundation.ecs_security_group_id
    rds_id = module.platform_foundation.rds_security_group_id
  }
}

output "container_registry" {
  description = "ECR repository details for the dev API image path."
  value = {
    repository_name = module.container_registry.repository_name
    repository_url  = module.container_registry.repository_url
  }
}

output "observability" {
  description = "CloudWatch observability resources for the dev runtime."
  value = {
    api_log_group_name = module.observability.api_log_group_name
    api_log_group_arn  = module.observability.api_log_group_arn
  }
}

output "database" {
  description = "RDS PostgreSQL baseline for the dev runtime."
  value = {
    db_instance_identifier = module.database.db_instance_identifier
    db_endpoint            = module.database.db_endpoint
    db_name                = module.database.db_name
    db_subnet_group_name   = module.database.db_subnet_group_name
  }
}

output "runtime_config" {
  description = "Runtime configuration references for the dev ECS task."
  value = {
    secret_arns     = module.runtime_config.secret_arns
    parameter_names = module.runtime_config.parameter_names
  }
}

output "github_actions" {
  description = "GitHub Actions deployment identity for the dev environment."
  value = {
    deploy_role_arn   = module.github_oidc.deploy_role_arn
    deploy_role_name  = module.github_oidc.deploy_role_name
    oidc_provider_arn = module.github_oidc.oidc_provider_arn
  }
}

output "container_runtime" {
  description = "ECS and ALB runtime resources for the dev API."
  value = {
    cluster_name        = module.container_runtime.cluster_name
    service_name        = module.container_runtime.service_name
    task_definition_arn = module.container_runtime.task_definition_arn
    alb_dns_name        = module.container_runtime.alb_dns_name
    target_group_arn    = module.container_runtime.target_group_arn
    http_listener_arn   = module.container_runtime.http_listener_arn
    https_listener_arn  = module.container_runtime.https_listener_arn
    api_dns_record_fqdn = module.container_runtime.api_dns_record_fqdn
  }
}
