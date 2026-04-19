# Terraform Foundation

This directory holds the Terraform codebase for TransitOps.

The cloud architecture, naming/tagging rules, environment conventions, and remote-state bootstrap path are documented together in [../../docs/CloudArchitecture.md](../../docs/CloudArchitecture.md).

## Layout

- `bootstrap/remote_state/`: one-time local-state bootstrap root that creates the shared S3 bucket and DynamoDB lock table for Terraform remote state.
- `modules/platform_foundation/`: VPC, subnet, routing, NAT, and security-group foundation with shared naming/tagging conventions.
- `modules/container_registry/`: ECR repository, scanning, and image lifecycle policy.
- `modules/observability/`: CloudWatch log group baseline.
- `modules/runtime_config/`: Secrets Manager and SSM runtime configuration contract.
- `modules/database/`: RDS PostgreSQL baseline.
- `modules/container_runtime/`: ECS Fargate, IAM roles, ALB, target group, listener, and task/service definition.
- `environments/dev/`: root Terraform configuration for the `dev` environment.
- `environments/prod/`: root Terraform configuration for the future `prod` environment.

## Intent

The current Terraform baseline is split into two responsibilities:

- `bootstrap/remote_state/` creates the remote-state backend resources with local state.
- `environments/dev` and `environments/prod` consume shared modules and are prepared to use that remote backend.

The `dev` environment now defines the full foundation and runtime path as code, but it is still intended to be validated before the first real AWS apply.

It now establishes:

- Terraform repository structure
- provider and Terraform version pinning
- shared locals, variables, outputs, naming, and tags
- environment layout for `dev` and `prod`
- remote Terraform state strategy with S3 backend configuration shape, encryption, versioning, and DynamoDB locking bootstrap
- VPC, subnets, routing, NAT, and least-privilege security groups
- ECR, CloudWatch logs, RDS PostgreSQL, ECS Fargate, ALB, target group, and runtime config/secrets contract

## Expected Next Steps

The next slices will add:

- cloud-safe EF Core migration execution
- cloud-safe first-admin bootstrap procedure
- GitHub Actions Terraform plan/deploy workflow
- first real plan/apply against the `dev` environment

## Basic Commands

From the remote-state bootstrap directory:

```powershell
cd infra\terraform\bootstrap\remote_state
terraform init
terraform fmt -recursive
terraform validate
terraform plan
```

From an environment directory after `backend.hcl` has been created from the bootstrap outputs:

```powershell
terraform init -backend-config=backend.hcl
terraform fmt -recursive
terraform validate
terraform plan
```

As of April 20, 2026, Terraform CLI is installed locally, the remote-state bootstrap has been applied in the isolated `transitops-dev` AWS account, `environments/dev` is initialized against the S3/DynamoDB backend, and a real AWS `terraform plan` succeeds for the `dev` foundation/runtime stack. The `dev` plan includes HTTPS ingress through ACM, Route53, and ALB for `api.dev.transitops.net`; the stack has not been applied yet.
