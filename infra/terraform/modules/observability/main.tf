resource "aws_cloudwatch_log_group" "api" {
  name              = "/aws/ecs/${var.project_slug}/${var.environment}/${var.service_slug}"
  retention_in_days = var.log_retention_days

  tags = merge(var.tags, {
    Name = "${var.project_slug}-${var.environment}-${var.service_slug}-logs"
  })
}
