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
