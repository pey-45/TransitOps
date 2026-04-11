provider "aws" {
  region  = var.aws_region
  profile = trimspace(var.aws_profile) != "" ? var.aws_profile : null
}
