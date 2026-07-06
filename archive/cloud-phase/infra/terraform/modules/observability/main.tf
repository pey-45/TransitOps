locals {
  dashboard_name = "${var.project_slug}-${var.environment}-${var.service_slug}"
  alarm_prefix   = "${var.project_slug}-${var.environment}-${var.service_slug}"
  alarm_actions  = var.enable_alarm_actions && var.alarm_email != "" ? [aws_sns_topic.runtime_alarms[0].arn] : []
}

resource "aws_cloudwatch_log_group" "api" {
  name              = "/aws/ecs/${var.project_slug}/${var.environment}/${var.service_slug}"
  retention_in_days = var.log_retention_days

  tags = merge(var.tags, {
    Name = "${var.project_slug}-${var.environment}-${var.service_slug}-logs"
  })
}

resource "aws_sns_topic" "runtime_alarms" {
  count = var.alarm_email != "" ? 1 : 0

  name = "${local.alarm_prefix}-alarms"

  tags = merge(var.tags, {
    Name = "${local.alarm_prefix}-alarms"
  })
}

resource "aws_sns_topic_subscription" "runtime_alarm_email" {
  count = var.alarm_email != "" ? 1 : 0

  topic_arn = aws_sns_topic.runtime_alarms[0].arn
  protocol  = "email"
  endpoint  = var.alarm_email
}

resource "aws_cloudwatch_log_metric_filter" "api_errors" {
  name           = "${local.alarm_prefix}-application-errors"
  log_group_name = aws_cloudwatch_log_group.api.name
  pattern        = "{ ($.LogLevel = \"Error\") || ($.LogLevel = \"Critical\") }"

  metric_transformation {
    name      = "${local.alarm_prefix}-application-errors"
    namespace = "TransitOps/${var.environment}"
    value     = "1"
  }
}

resource "aws_cloudwatch_metric_alarm" "api_application_errors" {
  alarm_name          = "${local.alarm_prefix}-application-errors"
  alarm_description   = "Application Error or Critical log entries were emitted by the API."
  comparison_operator = "GreaterThanOrEqualToThreshold"
  evaluation_periods  = 1
  metric_name         = aws_cloudwatch_log_metric_filter.api_errors.metric_transformation[0].name
  namespace           = "TransitOps/${var.environment}"
  period              = 300
  statistic           = "Sum"
  threshold           = 1
  treat_missing_data  = "notBreaching"
  alarm_actions       = local.alarm_actions
  ok_actions          = local.alarm_actions

  tags = merge(var.tags, {
    Name = "${local.alarm_prefix}-application-errors"
  })
}

resource "aws_cloudwatch_metric_alarm" "alb_target_5xx" {
  alarm_name          = "${local.alarm_prefix}-alb-target-5xx"
  alarm_description   = "ALB target 5XX responses exceeded the configured threshold."
  comparison_operator = "GreaterThanOrEqualToThreshold"
  evaluation_periods  = 1
  metric_name         = "HTTPCode_Target_5XX_Count"
  namespace           = "AWS/ApplicationELB"
  period              = 300
  statistic           = "Sum"
  threshold           = var.alb_5xx_threshold
  treat_missing_data  = "notBreaching"
  alarm_actions       = local.alarm_actions
  ok_actions          = local.alarm_actions

  dimensions = {
    LoadBalancer = var.alb_arn_suffix
  }

  tags = merge(var.tags, {
    Name = "${local.alarm_prefix}-alb-target-5xx"
  })
}

resource "aws_cloudwatch_metric_alarm" "alb_response_time" {
  alarm_name          = "${local.alarm_prefix}-alb-response-time"
  alarm_description   = "ALB target response time exceeded the configured threshold."
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 2
  metric_name         = "TargetResponseTime"
  namespace           = "AWS/ApplicationELB"
  period              = 300
  statistic           = "Average"
  threshold           = var.alb_response_time_threshold_seconds
  treat_missing_data  = "notBreaching"
  alarm_actions       = local.alarm_actions
  ok_actions          = local.alarm_actions

  dimensions = {
    LoadBalancer = var.alb_arn_suffix
  }

  tags = merge(var.tags, {
    Name = "${local.alarm_prefix}-alb-response-time"
  })
}

resource "aws_cloudwatch_metric_alarm" "alb_unhealthy_hosts" {
  alarm_name          = "${local.alarm_prefix}-alb-unhealthy-hosts"
  alarm_description   = "ALB reports at least one unhealthy API target."
  comparison_operator = "GreaterThanOrEqualToThreshold"
  evaluation_periods  = 2
  metric_name         = "UnHealthyHostCount"
  namespace           = "AWS/ApplicationELB"
  period              = 60
  statistic           = "Maximum"
  threshold           = 1
  treat_missing_data  = "notBreaching"
  alarm_actions       = local.alarm_actions
  ok_actions          = local.alarm_actions

  dimensions = {
    LoadBalancer = var.alb_arn_suffix
    TargetGroup  = var.target_group_arn_suffix
  }

  tags = merge(var.tags, {
    Name = "${local.alarm_prefix}-alb-unhealthy-hosts"
  })
}

resource "aws_cloudwatch_metric_alarm" "ecs_cpu" {
  alarm_name          = "${local.alarm_prefix}-ecs-cpu"
  alarm_description   = "ECS service CPU utilization exceeded the configured threshold."
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 2
  metric_name         = "CPUUtilization"
  namespace           = "AWS/ECS"
  period              = 300
  statistic           = "Average"
  threshold           = var.ecs_cpu_threshold_percent
  treat_missing_data  = "notBreaching"
  alarm_actions       = local.alarm_actions
  ok_actions          = local.alarm_actions

  dimensions = {
    ClusterName = var.ecs_cluster_name
    ServiceName = var.ecs_service_name
  }

  tags = merge(var.tags, {
    Name = "${local.alarm_prefix}-ecs-cpu"
  })
}

resource "aws_cloudwatch_metric_alarm" "ecs_memory" {
  alarm_name          = "${local.alarm_prefix}-ecs-memory"
  alarm_description   = "ECS service memory utilization exceeded the configured threshold."
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 2
  metric_name         = "MemoryUtilization"
  namespace           = "AWS/ECS"
  period              = 300
  statistic           = "Average"
  threshold           = var.ecs_memory_threshold_percent
  treat_missing_data  = "notBreaching"
  alarm_actions       = local.alarm_actions
  ok_actions          = local.alarm_actions

  dimensions = {
    ClusterName = var.ecs_cluster_name
    ServiceName = var.ecs_service_name
  }

  tags = merge(var.tags, {
    Name = "${local.alarm_prefix}-ecs-memory"
  })
}

resource "aws_cloudwatch_metric_alarm" "rds_cpu" {
  alarm_name          = "${local.alarm_prefix}-rds-cpu"
  alarm_description   = "RDS CPU utilization exceeded the configured threshold."
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 2
  metric_name         = "CPUUtilization"
  namespace           = "AWS/RDS"
  period              = 300
  statistic           = "Average"
  threshold           = var.rds_cpu_threshold_percent
  treat_missing_data  = "notBreaching"
  alarm_actions       = local.alarm_actions
  ok_actions          = local.alarm_actions

  dimensions = {
    DBInstanceIdentifier = var.db_instance_identifier
  }

  tags = merge(var.tags, {
    Name = "${local.alarm_prefix}-rds-cpu"
  })
}

resource "aws_cloudwatch_metric_alarm" "rds_connections" {
  alarm_name          = "${local.alarm_prefix}-rds-connections"
  alarm_description   = "RDS database connections exceeded the configured threshold."
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 2
  metric_name         = "DatabaseConnections"
  namespace           = "AWS/RDS"
  period              = 300
  statistic           = "Average"
  threshold           = var.rds_connections_threshold
  treat_missing_data  = "notBreaching"
  alarm_actions       = local.alarm_actions
  ok_actions          = local.alarm_actions

  dimensions = {
    DBInstanceIdentifier = var.db_instance_identifier
  }

  tags = merge(var.tags, {
    Name = "${local.alarm_prefix}-rds-connections"
  })
}

resource "aws_cloudwatch_metric_alarm" "rds_free_storage" {
  alarm_name          = "${local.alarm_prefix}-rds-free-storage"
  alarm_description   = "RDS free storage dropped below the configured threshold."
  comparison_operator = "LessThanThreshold"
  evaluation_periods  = 2
  metric_name         = "FreeStorageSpace"
  namespace           = "AWS/RDS"
  period              = 300
  statistic           = "Average"
  threshold           = var.rds_free_storage_threshold_bytes
  treat_missing_data  = "notBreaching"
  alarm_actions       = local.alarm_actions
  ok_actions          = local.alarm_actions

  dimensions = {
    DBInstanceIdentifier = var.db_instance_identifier
  }

  tags = merge(var.tags, {
    Name = "${local.alarm_prefix}-rds-free-storage"
  })
}

resource "aws_cloudwatch_dashboard" "runtime" {
  dashboard_name = local.dashboard_name

  dashboard_body = jsonencode({
    widgets = [
      {
        type   = "metric"
        x      = 0
        y      = 0
        width  = 12
        height = 6
        properties = {
          title   = "ALB health and latency"
          region  = var.aws_region
          view    = "timeSeries"
          stacked = false
          metrics = [
            ["AWS/ApplicationELB", "HTTPCode_Target_5XX_Count", "LoadBalancer", var.alb_arn_suffix, { stat = "Sum", label = "Target 5XX" }],
            [".", "TargetResponseTime", ".", ".", { stat = "Average", label = "Target response time" }],
            [".", "UnHealthyHostCount", ".", ".", "TargetGroup", var.target_group_arn_suffix, { stat = "Maximum", label = "Unhealthy hosts" }]
          ]
        }
      },
      {
        type   = "metric"
        x      = 12
        y      = 0
        width  = 12
        height = 6
        properties = {
          title   = "ECS service utilization"
          region  = var.aws_region
          view    = "timeSeries"
          stacked = false
          metrics = [
            ["AWS/ECS", "CPUUtilization", "ClusterName", var.ecs_cluster_name, "ServiceName", var.ecs_service_name, { stat = "Average", label = "CPU %" }],
            [".", "MemoryUtilization", ".", ".", ".", ".", { stat = "Average", label = "Memory %" }]
          ]
        }
      },
      {
        type   = "metric"
        x      = 0
        y      = 6
        width  = 12
        height = 6
        properties = {
          title   = "RDS PostgreSQL"
          region  = var.aws_region
          view    = "timeSeries"
          stacked = false
          metrics = [
            ["AWS/RDS", "CPUUtilization", "DBInstanceIdentifier", var.db_instance_identifier, { stat = "Average", label = "CPU %" }],
            [".", "DatabaseConnections", ".", ".", { stat = "Average", label = "Connections" }],
            [".", "FreeStorageSpace", ".", ".", { stat = "Average", label = "Free storage bytes", yAxis = "right" }]
          ]
        }
      },
      {
        type   = "log"
        x      = 12
        y      = 6
        width  = 12
        height = 6
        properties = {
          title  = "Recent API logs"
          region = var.aws_region
          view   = "table"
          query  = "SOURCE '${aws_cloudwatch_log_group.api.name}' | fields @timestamp, LogLevel, Category, State.Message, State.CorrelationId, @message | sort @timestamp desc | limit 50"
        }
      }
    ]
  })
}
