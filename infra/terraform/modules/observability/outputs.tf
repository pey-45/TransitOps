output "api_log_group_name" {
  description = "CloudWatch log group name for API container logs."
  value       = aws_cloudwatch_log_group.api.name
}

output "api_log_group_arn" {
  description = "CloudWatch log group ARN for API container logs."
  value       = aws_cloudwatch_log_group.api.arn
}
