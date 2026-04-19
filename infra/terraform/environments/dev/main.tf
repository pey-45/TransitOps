module "platform_foundation" {
  source = "../../modules/platform_foundation"

  project_slug       = var.project_slug
  service_slug       = var.service_slug
  environment        = "dev"
  aws_region         = var.aws_region
  owner              = var.owner
  repository         = var.repository
  root_domain        = var.root_domain
  availability_zones = var.availability_zones
  api_container_port = var.api_container_port
  tags_override      = var.tags_override
}

module "container_registry" {
  source = "../../modules/container_registry"

  project_slug = var.project_slug
  service_slug = var.service_slug
  tags         = module.platform_foundation.api_service_tags
}

module "observability" {
  source = "../../modules/observability"

  project_slug       = var.project_slug
  environment        = module.platform_foundation.environment
  service_slug       = var.service_slug
  log_retention_days = var.log_retention_days
  tags               = module.platform_foundation.api_service_tags
}

module "runtime_config" {
  source = "../../modules/runtime_config"

  secrets_prefix         = module.platform_foundation.secrets_prefix
  parameters_prefix      = module.platform_foundation.parameters_prefix
  jwt_issuer             = var.jwt_issuer
  jwt_audience           = var.jwt_audience
  jwt_expiration_minutes = var.jwt_expiration_minutes
  tags                   = module.platform_foundation.api_service_tags
}

module "database" {
  source = "../../modules/database"

  name_prefix              = module.platform_foundation.name_prefix
  data_subnet_ids          = module.platform_foundation.data_subnet_ids
  rds_security_group_id    = module.platform_foundation.rds_security_group_id
  database_name            = var.database_name
  database_username        = var.database_username
  database_password        = var.database_password
  instance_class           = var.database_instance_class
  allocated_storage_gb     = var.database_allocated_storage_gb
  max_allocated_storage_gb = var.database_max_allocated_storage_gb
  backup_retention_days    = var.database_backup_retention_days
  deletion_protection      = var.database_deletion_protection
  skip_final_snapshot      = var.database_skip_final_snapshot
  tags                     = module.platform_foundation.api_service_tags
}

module "container_runtime" {
  source = "../../modules/container_runtime"

  name_prefix           = module.platform_foundation.name_prefix
  project_slug          = var.project_slug
  environment           = module.platform_foundation.environment
  service_slug          = var.service_slug
  aws_region            = var.aws_region
  image_uri             = "${module.container_registry.repository_url}:${var.api_image_tag}"
  container_port        = var.api_container_port
  public_subnet_ids     = module.platform_foundation.public_subnet_ids
  app_subnet_ids        = module.platform_foundation.app_subnet_ids
  vpc_id                = module.platform_foundation.vpc_id
  alb_security_group_id = module.platform_foundation.alb_security_group_id
  ecs_security_group_id = module.platform_foundation.ecs_security_group_id
  log_group_name        = module.observability.api_log_group_name
  cpu                   = var.ecs_cpu
  memory                = var.ecs_memory
  desired_count         = var.ecs_desired_count
  enable_https          = var.enable_https
  domain_name           = module.platform_foundation.dns_api_hostname
  hosted_zone_id        = var.hosted_zone_id
  tags                  = module.platform_foundation.api_service_tags

  secrets = merge(
    module.runtime_config.secret_arns,
    module.runtime_config.parameter_arns
  )
}
