locals {
  name_prefix = "${var.project_slug}-${var.environment}"

  common_tags = merge(
    {
      Project     = "TransitOps"
      Environment = var.environment
      ManagedBy   = "Terraform"
      Owner       = var.owner
      Repository  = var.repository
      Service     = "platform"
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

  secrets_prefix    = "${var.project_slug}/${var.environment}/app"
  parameters_prefix = "/${var.project_slug}/${var.environment}/app"
  terraform_state_key = "${var.environment}/foundation.tfstate"
}
