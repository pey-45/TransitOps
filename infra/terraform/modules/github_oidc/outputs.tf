output "deploy_role_arn" {
  description = "IAM role ARN assumed by GitHub Actions for TransitOps dev deployment."
  value       = aws_iam_role.deploy.arn
}

output "deploy_role_name" {
  description = "IAM role name assumed by GitHub Actions for TransitOps dev deployment."
  value       = aws_iam_role.deploy.name
}

output "oidc_provider_arn" {
  description = "GitHub OIDC provider ARN used by the deployment role."
  value       = local.oidc_provider_arn
}
