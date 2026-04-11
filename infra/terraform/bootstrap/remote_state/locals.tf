data "aws_caller_identity" "current" {}

locals {
  bucket_name    = "${var.project_slug}-tfstate-${data.aws_caller_identity.current.account_id}-${var.aws_region}"
  dynamodb_table = "${var.project_slug}-tfstate-locks"

  common_tags = merge(
    {
      Project     = "TransitOps"
      Environment = "shared"
      ManagedBy   = "Terraform"
      Owner       = var.owner
      Repository  = var.repository
      Service     = "platform"
    },
    var.tags_override
  )

  state_bucket_tags = merge(
    local.common_tags,
    {
      Name = local.bucket_name
    }
  )

  lock_table_tags = merge(
    local.common_tags,
    {
      Name = local.dynamodb_table
    }
  )
}
