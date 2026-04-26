locals {
  name_prefix = "${var.project_slug}-${var.environment}"

  common_tags = merge(
    {
      Project        = "TransitOps"
      Environment    = var.environment
      ManagedBy      = "Terraform"
      Owner          = var.owner
      Repository     = var.repository
      ResourceGroup  = local.name_prefix
      Service        = "platform"
      TerraformStack = local.name_prefix
    },
    var.tags_override
  )

  api_service_tags = merge(
    local.common_tags,
    {
      Service = var.service_slug
    }
  )

  dns_api_hostname = trimspace(var.root_domain) == "" ? null : (
    var.environment == "prod"
    ? "api.${var.root_domain}"
    : "api.${var.environment}.${var.root_domain}"
  )

  secrets_prefix      = "${var.project_slug}/${var.environment}/app"
  parameters_prefix   = "/${var.project_slug}/${var.environment}/app"
  terraform_state_key = "${var.environment}/foundation.tfstate"

  # Subnet CIDRs computed from the VPC CIDR block.
  # /16 split into /24 slices: public 0-9, app 10-19, data 20-29.
  public_subnet_cidrs = [
    for i in range(length(var.availability_zones)) :
    cidrsubnet(var.vpc_cidr, 8, i)
  ]

  private_app_subnet_cidrs = [
    for i in range(length(var.availability_zones)) :
    cidrsubnet(var.vpc_cidr, 8, i + 10)
  ]

  private_data_subnet_cidrs = [
    for i in range(length(var.availability_zones)) :
    cidrsubnet(var.vpc_cidr, 8, i + 20)
  ]
}
