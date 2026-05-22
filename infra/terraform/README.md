# Terraform Foundation

This directory holds the Terraform codebase for TransitOps.

The cloud architecture, naming/tagging rules, environment conventions, and remote-state bootstrap path are documented together in [../../docs/CloudArchitecture.md](../../docs/CloudArchitecture.md).

## Layout

- `bootstrap/remote_state/`: one-time local-state bootstrap root that creates the shared S3 bucket and DynamoDB lock table for Terraform remote state.
- `modules/platform_foundation/`: VPC, subnet, routing, NAT, and security-group foundation with shared naming/tagging conventions.
- `modules/container_registry/`: ECR repository, scanning, and image lifecycle policy.
- `modules/observability/`: CloudWatch log group, dashboard, alarms, metric filter, and optional SNS email notification.
- `modules/runtime_config/`: Secrets Manager and SSM runtime configuration contract.
- `modules/github_oidc/`: IAM OIDC trust and deployment role for GitHub Actions.
- `modules/database/`: RDS PostgreSQL baseline.
- `modules/container_runtime/`: ECS Fargate, IAM roles, ALB, target group, listener, and task/service definition.
- `environments/dev/`: root Terraform configuration for the `dev` environment.
- `environments/prod/`: root Terraform configuration for the future `prod` environment.

## Intent

The current Terraform baseline is split into two responsibilities:

- `bootstrap/remote_state/` creates the remote-state backend resources with local state.
- `environments/dev` and `environments/prod` consume shared modules and are prepared to use that remote backend.

The `dev` environment defines the full foundation and runtime path as code and has been validated through real create/deploy/destroy cycles in AWS account `661000947340`.

It now establishes:

- Terraform repository structure
- provider and Terraform version pinning
- shared locals, variables, outputs, naming, and tags
- environment layout for `dev` and `prod`
- remote Terraform state strategy with S3 backend configuration shape, encryption, versioning, and DynamoDB locking bootstrap
- VPC, subnets, routing, NAT, and least-privilege security groups
- ECR, CloudWatch logs/dashboard/alarms, RDS PostgreSQL, ECS Fargate, ALB, target group, Route53/ACM HTTPS, and runtime config/secrets contract
- GitHub OIDC deployment role for keyless CI/CD access to AWS

## Current Operational Shape

The current cloud workflow is:

- create the `dev` platform with `ecs_desired_count=0`;
- publish the API image to ECR;
- load runtime secrets into Secrets Manager;
- run EF Core migrations as a one-off ECS task with `--migrate-only`;
- scale ECS to `ecs_desired_count=1`;
- validate HTTPS readiness, bootstrap/login, CloudWatch observability, and posture checks;
- run `terraform destroy` and post-destroy audit after evidence capture.

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

As of Sprint 9, the Terraform target account is AWS account `661000947340` (`Pablo`, alias `aws-pey-v1`). The `dev` backend configuration points at `transitops-tfstate-661000947340-eu-west-1`, which must be bootstrapped in that account before the environment root is initialized. The previous account `142966787103` appears only in historical documentation.

## Dev Apply Shape

The first `dev` apply in a recreate cycle should create the cloud platform without starting API tasks, because ECR is initially empty:

```powershell
terraform apply `
  -var="root_domain=transitops.net" `
  -var="hosted_zone_id=Z0844787W37HXN9FIJR" `
  -var="enable_https=true" `
  -var="ecs_desired_count=0"
```

After the image is pushed to ECR, the GitHub Actions deployment workflow or local runbook updates `api_image_tag`, runs the EF Core migration task, and applies `ecs_desired_count=1`. The current hosted zone for `transitops.net` in account `661000947340` is `Z0844787W37HXN9FIJR`, so HTTPS should remain enabled for normal `dev` recreations.

Terraform-managed resources carry the cleanup tags `TerraformStack` and `ResourceGroup`. Filter by `tag:TerraformStack = transitops-dev` in AWS Resource Explorer or Resource Groups to audit environment resources before and after `terraform destroy`; filter by `tag:TerraformStack = transitops-bootstrap-remote-state` for the remote-state bootstrap resources that intentionally survive environment destroys.
