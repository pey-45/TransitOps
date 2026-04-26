# TransitOps Cloud Deployment

## Purpose

This document defines the Sprint 4 code-to-cloud path for the `dev` environment. The goal is a repeatable route from repository code to a running AWS deployment without long-lived AWS access keys.

The deployment target is:

- AWS account: `661000947340` (`Pablo`, alias `aws-pey-v1`)
- Region: `eu-west-1`
- Environment: `dev`
- Hostname: `api.dev.transitops.net` when the Route53 hosted zone is available; otherwise the first AWS smoke run uses the ALB DNS name over HTTP.
- Terraform state bucket: `transitops-tfstate-661000947340-eu-west-1`
- Terraform lock table: `transitops-tfstate-locks`
- Terraform state key: `dev/foundation.tfstate`

## One-Time Local Bootstrap

In account `661000947340`, run the remote-state bootstrap first so the S3 state bucket and DynamoDB lock table exist. Before GitHub Actions can deploy, the `dev` Terraform root must also create the GitHub OIDC deployment role.

Run the first apply locally with the AWS SSO profile `aws-pey-v1` and keep ECS at desired count `0` so the platform can be created before ECR contains an image. If the `transitops.net` hosted zone is not present in this AWS account yet, keep HTTPS disabled for the first deployment:

```powershell
cd infra\terraform\environments\dev
terraform init -backend-config=backend.hcl
terraform fmt -recursive ..\..
terraform validate
terraform apply `
  -var="root_domain=" `
  -var="hosted_zone_id=" `
  -var="enable_https=false" `
  -var="ecs_desired_count=0"
```

After the hosted zone is created or delegated into this account, rerun Terraform with:

```powershell
terraform apply `
  -var="root_domain=transitops.net" `
  -var="hosted_zone_id=<hosted-zone-id>" `
  -var="enable_https=true"
```

Expected result:

- ECR repository exists.
- RDS PostgreSQL exists in private data subnets.
- Secrets Manager and SSM runtime configuration entries exist.
- ALB, ACM, Route53, ECS cluster, task definition, and ECS service exist.
- ECS service has desired count `0`.
- Output `github_actions.deploy_role_arn` points to `transitops-dev-github-actions-deploy-role`.

## GitHub Environment

Create a GitHub Environment named `dev`.

Required environment variables:

| Name | Value |
| --- | --- |
| `AWS_ACCOUNT_ID` | `661000947340` |
| `AWS_REGION` | `eu-west-1` |
| `TF_STATE_BUCKET` | `transitops-tfstate-661000947340-eu-west-1` |
| `TF_LOCK_TABLE` | `transitops-tfstate-locks` |
| `TF_STATE_KEY` | `dev/foundation.tfstate` |
| `ROOT_DOMAIN` | Empty until the hosted zone exists in this account; then `transitops.net` |
| `HOSTED_ZONE_ID` | Empty until the hosted zone exists in this account |
| `ENABLE_HTTPS` | `false` until Route53 is available; then `true` |
| `DATABASE_USERNAME` | RDS master username used by Terraform |
| `CLOUD_ADMIN_USERNAME` | first cloud admin username |
| `CLOUD_ADMIN_EMAIL` | first cloud admin email |

Required environment secrets:

| Name | Purpose |
| --- | --- |
| `DATABASE_PASSWORD` | RDS master password and application DB password |
| `JWT_SIGNING_KEY` | JWT signing key, at least 32 characters |
| `BOOTSTRAP_ADMIN_TOKEN` | token required by `/api/v1/auth/bootstrap-admin` |
| `CLOUD_ADMIN_PASSWORD` | first cloud admin password |

Do not configure AWS access keys. GitHub Actions authenticates by assuming:

```text
arn:aws:iam::661000947340:role/transitops-dev-github-actions-deploy-role
```

## Manual Workflows

### Terraform Dev

Workflow: `.github/workflows/terraform-dev.yml`

Use this workflow for manual infrastructure validation and optional apply. It runs:

1. OIDC AWS authentication.
2. Terraform init against the S3 backend.
3. `terraform fmt -check`.
4. `terraform validate`.
5. `terraform plan`.
6. Optional `terraform apply`.

The workflow keeps `ecs_desired_count=0` by default. Runtime rollout is handled by `deploy-dev.yml`.

### Deploy Dev

Workflow: `.github/workflows/deploy-dev.yml`

This is the main Sprint 4 delivery path:

1. Restore, build, and test the .NET solution.
2. Build the API Docker image.
3. Apply Terraform with `ecs_desired_count=0` and `api_image_tag=<commit-sha>`.
4. Read Terraform outputs.
5. Push the image to ECR.
6. Populate Secrets Manager values for DB connection string, JWT signing key, and bootstrap token.
7. Run EF Core migrations as a one-off ECS Fargate task using `--migrate-only`.
8. Apply Terraform with `ecs_desired_count=1`.
9. Wait for ECS service stability.
10. Verify `GET /api/v1/health/ready` through `https://api.dev.transitops.net` when DNS/HTTPS exists, otherwise through the ALB DNS name over HTTP.
11. Bootstrap the first admin, accepting `201 Created` or `409 first_admin_already_exists`.
12. Verify admin login returns a JWT.

## Migration Strategy

AWS migrations must be explicit and must run inside the VPC because RDS is private. The API supports:

```bash
dotnet TransitOps.Api.dll --migrate-only
```

In AWS this command is executed by `aws ecs run-task` with the normal ECS task definition and an overridden container command. The process applies EF Core migrations and exits. A non-zero container exit code fails the deployment workflow.

`Database:ApplyMigrationsOnStartup` remains available for local Docker only. Production ECS tasks should keep it disabled.

## Smoke Evidence To Capture

For Sprint 4 completion, capture:

- GitHub workflow run URL for `deploy-dev`.
- ECR image tag equal to the deployed commit SHA.
- Terraform output showing ALB/ECS/RDS/Route53 resources.
- ECS migration task ARN and exit code `0`.
- Health endpoint response from `https://api.dev.transitops.net/api/v1/health/ready`, or the ALB DNS fallback while the hosted zone is absent.
- Bootstrap response status: `201` first run or `409 first_admin_already_exists` on rerun.
- Login response status `200` with non-empty `data.accessToken`.
