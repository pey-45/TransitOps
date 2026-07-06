output "api_log_group_name" {
  description = "CloudWatch log group name for API container logs."
  value       = aws_cloudwatch_log_group.api.name
}

output "api_log_group_arn" {
  description = "CloudWatch log group ARN for API container logs."
  value       = aws_cloudwatch_log_group.api.arn
}

output "dashboard_name" {
  description = "CloudWatch dashboard name for the API runtime."
  value       = aws_cloudwatch_dashboard.runtime.dashboard_name
}

output "alarm_names" {
  description = "CloudWatch alarm names created for the API runtime."
  value = [
    aws_cloudwatch_metric_alarm.api_application_errors.alarm_name,
    aws_cloudwatch_metric_alarm.alb_target_5xx.alarm_name,
    aws_cloudwatch_metric_alarm.alb_response_time.alarm_name,
    aws_cloudwatch_metric_alarm.alb_unhealthy_hosts.alarm_name,
    aws_cloudwatch_metric_alarm.ecs_cpu.alarm_name,
    aws_cloudwatch_metric_alarm.ecs_memory.alarm_name,
    aws_cloudwatch_metric_alarm.rds_cpu.alarm_name,
    aws_cloudwatch_metric_alarm.rds_connections.alarm_name,
    aws_cloudwatch_metric_alarm.rds_free_storage.alarm_name
  ]
}

output "alarm_topic_arn" {
  description = "SNS topic ARN for alarm notifications when configured."
  value       = try(aws_sns_topic.runtime_alarms[0].arn, null)
}
