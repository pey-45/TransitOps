output "cluster_name" {
  description = "ECS cluster name."
  value       = aws_ecs_cluster.this.name
}

output "cluster_arn" {
  description = "ECS cluster ARN."
  value       = aws_ecs_cluster.this.arn
}

output "service_name" {
  description = "ECS service name."
  value       = aws_ecs_service.api.name
}

output "task_definition_arn" {
  description = "API task definition ARN."
  value       = aws_ecs_task_definition.api.arn
}

output "execution_role_arn" {
  description = "ECS task execution role ARN."
  value       = aws_iam_role.execution.arn
}

output "task_role_arn" {
  description = "ECS task role ARN."
  value       = aws_iam_role.task.arn
}

output "alb_arn" {
  description = "Application Load Balancer ARN."
  value       = aws_lb.api.arn
}

output "alb_dns_name" {
  description = "Application Load Balancer DNS name."
  value       = aws_lb.api.dns_name
}

output "target_group_arn" {
  description = "API target group ARN."
  value       = aws_lb_target_group.api.arn
}

output "http_listener_arn" {
  description = "HTTP listener ARN."
  value       = try(aws_lb_listener.http_forward[0].arn, aws_lb_listener.http_redirect[0].arn)
}

output "https_listener_arn" {
  description = "HTTPS listener ARN when enabled."
  value       = try(aws_lb_listener.https[0].arn, null)
}

output "api_dns_record_fqdn" {
  description = "Route53 API DNS record when HTTPS/DNS is enabled."
  value       = try(aws_route53_record.api[0].fqdn, null)
}
