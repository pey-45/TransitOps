# Container Runtime Module

Defines the ECS Fargate runtime path for TransitOps.

Current scope:

- ECS cluster.
- ECS task execution role and task role.
- Fargate task definition.
- ECS service in private app subnets.
- Public ALB, target group, and HTTP listener.
- Optional HTTPS/ACM/Route53 path for when a real domain is available.

The default `dev` posture uses HTTP on port 80 because no Route53-managed domain is available yet.
