# Observability Module

Creates the CloudWatch observability baseline for the ECS API runtime.

Current scope:

- CloudWatch log group for ECS container logs.
- CloudWatch dashboard for ALB, ECS, RDS, and recent API logs.
- Metric filter for structured API error logs.
- CloudWatch alarms for application errors, ALB failures/latency/target health, ECS CPU/memory, and RDS CPU/connections/free storage.
- Optional SNS email notification path when `alarm_email` is configured.
