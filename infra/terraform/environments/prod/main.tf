module "platform_foundation" {
  source = "../../modules/platform_foundation"

  project_slug       = var.project_slug
  service_slug       = var.service_slug
  environment        = "prod"
  aws_region         = var.aws_region
  owner              = var.owner
  repository         = var.repository
  root_domain        = var.root_domain
  availability_zones = var.availability_zones
  tags_override      = var.tags_override
}
