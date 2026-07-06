provider "aws" {
  region  = var.aws_region
  profile = trimspace(var.aws_profile) != "" ? var.aws_profile : null

  default_tags {
    tags = {
      Project        = "TransitOps"
      Environment    = "prod"
      ManagedBy      = "Terraform"
      Owner          = var.owner
      Repository     = var.repository
      ResourceGroup  = "${var.project_slug}-prod"
      TerraformStack = "${var.project_slug}-prod"
    }
  }
}
