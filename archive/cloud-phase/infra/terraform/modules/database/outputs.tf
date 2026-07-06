output "db_instance_identifier" {
  description = "RDS instance identifier."
  value       = aws_db_instance.postgres.identifier
}

output "db_instance_arn" {
  description = "RDS instance ARN."
  value       = aws_db_instance.postgres.arn
}

output "db_endpoint" {
  description = "RDS instance endpoint."
  value       = aws_db_instance.postgres.endpoint
}

output "db_name" {
  description = "Application database name."
  value       = aws_db_instance.postgres.db_name
}

output "db_subnet_group_name" {
  description = "RDS subnet group name."
  value       = aws_db_subnet_group.postgres.name
}
