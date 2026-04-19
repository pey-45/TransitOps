# Runtime Config Module

Defines the runtime configuration contract for the AWS application runtime.

This module creates names and references for secrets/configuration only. Secret values are intentionally not committed to git and must be populated during deployment/bootstrap.

Secrets Manager is used for sensitive values. SSM Parameter Store is used for non-secret runtime values, and the ECS task consumes both through ARN references instead of embedding values directly in the task definition.
