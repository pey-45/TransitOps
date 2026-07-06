output "aws_account_id" {
  description = "AWS account id where the remote-state resources were created."
  value       = data.aws_caller_identity.current.account_id
}

output "terraform_state_bucket_name" {
  description = "S3 bucket name used for Terraform remote state."
  value       = aws_s3_bucket.terraform_state.bucket
}

output "terraform_lock_table_name" {
  description = "DynamoDB table name used for Terraform state locking."
  value       = aws_dynamodb_table.terraform_locks.name
}

output "dev_backend_config" {
  description = "Concrete backend.hcl content for the dev environment."
  value = {
    bucket         = aws_s3_bucket.terraform_state.bucket
    key            = "dev/foundation.tfstate"
    region         = var.aws_region
    dynamodb_table = aws_dynamodb_table.terraform_locks.name
    encrypt        = true
  }
}

output "prod_backend_config" {
  description = "Concrete backend.hcl content for the prod environment."
  value = {
    bucket         = aws_s3_bucket.terraform_state.bucket
    key            = "prod/foundation.tfstate"
    region         = var.aws_region
    dynamodb_table = aws_dynamodb_table.terraform_locks.name
    encrypt        = true
  }
}
