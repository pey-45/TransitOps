# TransitOps Cloud Deployment

## Purpose

This document defines the Sprint 4 code-to-cloud path for the `dev` environment. The goal is a repeatable route from repository code to a running AWS deployment without long-lived AWS access keys.

The deployment target is:

- AWS account: `661000947340` (`Pablo`, alias `aws-pey-v1`)
- Region: `eu-west-1`
- Environment: `dev`
- Hostname: `api.dev.transitops.net` through Route53, ACM, ALB HTTPS, and HTTP-to-HTTPS redirect.
- Terraform state bucket: `transitops-tfstate-661000947340-eu-west-1`
- Terraform lock table: `transitops-tfstate-locks`
- Terraform state key: `dev/foundation.tfstate`

## One-Time Local Bootstrap

In account `661000947340`, run the remote-state bootstrap first so the S3 state bucket and DynamoDB lock table exist. Before GitHub Actions can deploy, the `dev` Terraform root must also create the GitHub OIDC deployment role.

Run the first apply locally with the AWS SSO profile `aws-pey-v1` and keep ECS at desired count `0` so the platform can be created before ECR contains an image. The hosted zone now exists in this account, so Sprint 7 recreations should keep HTTPS enabled:

```powershell
cd infra\terraform\environments\dev
terraform init -backend-config=backend.hcl
terraform fmt -recursive ..\..
terraform validate
terraform apply `
  -var="root_domain=transitops.net" `
  -var="hosted_zone_id=Z0844787W37HXN9FIJR" `
  -var="enable_https=true" `
  -var="ecs_desired_count=0"
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
| `ROOT_DOMAIN` | `transitops.net` |
| `HOSTED_ZONE_ID` | `Z0844787W37HXN9FIJR` |
| `ENABLE_HTTPS` | `true` |
| `ALARM_EMAIL` | `pablomlopez03@gmail.com` for Sprint 7 alarm validation |
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
11. Verify the CloudWatch log group, dashboard, alarms, optional SNS topic, and at least one correlated API log entry.
12. Bootstrap the first admin, accepting `201 Created` or `409 first_admin_already_exists`.
13. Verify admin login returns a JWT.

### Rollback Dev

Workflow: `.github/workflows/rollback-dev.yml`

Use this workflow when a known-good image tag must be redeployed without rebuilding the application. It accepts `api_image_tag`, applies Terraform with `ecs_desired_count=1`, waits for ECS stability, verifies the task definition image tag, and runs the readiness smoke test.

The detailed rollback and restore runbooks live in `docs/CloudReliability.md`. Sprint 8 operational runbooks and recreate-from-scratch steps live in `docs/CloudOperations.md`.

## Migration Strategy

AWS migrations must be explicit and must run inside the VPC because RDS is private. The API supports:

```bash
dotnet TransitOps.Api.dll --migrate-only
```

In AWS this command is executed by `aws ecs run-task` with the normal ECS task definition and an overridden container command. The process applies EF Core migrations and exits. A non-zero container exit code fails the deployment workflow.

`Database:ApplyMigrationsOnStartup` remains available for local Docker only. Production ECS tasks should keep it disabled.

## Smoke Evidence To Capture

For cloud deployment completion, capture:

- GitHub workflow run URL for `deploy-dev`.
- ECR image tag equal to the deployed commit SHA.
- Terraform output showing ALB/ECS/RDS/Route53 resources.
- ECS migration task ARN and exit code `0`.
- Health endpoint response from `https://api.dev.transitops.net/api/v1/health/ready`, or the ALB DNS fallback while the hosted zone is absent.
- Bootstrap response status: `201` first run or `409 first_admin_already_exists` on rerun.
- Login response status `200` with non-empty `data.accessToken`.

For Sprint 6 observability completion, also capture:

- CloudWatch log group `/aws/ecs/transitops/dev/api`.
- A log event containing `State.CorrelationId`.
- Dashboard name `transitops-dev-api`.
- Alarm names from Terraform output `observability.alarm_names`.
- SNS topic ARN from `observability.alarm_topic_arn` when `ALARM_EMAIL` is configured.
- ECS service deployment evidence showing the service reached stable state after circuit-breaker-enabled deployment settings were applied.

For Sprint 7 reliability completion, also capture:

- known-good ECR image tag `sprint7-good-<sha>`;
- rollback workflow or local rollback command output;
- ECS events for the missing-image circuit-breaker test;
- restored RDS snapshot/temporary DB evidence and `--migrate-only` exit code `0`;
- Docker hardening evidence: `.dockerignore`, single exposed runtime port, non-root `id` output;
- final `terraform destroy` and post-destroy absence checks.

## Sprint 7 Evidence Captured

Sprint 7 was validated in AWS account `661000947340` and the `dev` environment was destroyed afterwards.

- Known-good image: `sprint7-good-3dca035`.
- Migration ECS task: `1ec56ac77fbb4142a380b9e4881e3d24`, exit code `0`.
- ECS service after rollout: desired/running `1/1`, task definition `transitops-dev-api:4`.
- HTTPS smoke: `GET https://api.dev.transitops.net/api/v1/health/ready = 200`.
- Bootstrap/login: bootstrap returned `201`; login returned `200`.
- Correlation evidence: `sprint7-health-good`, `sprint7-bootstrap-good`, `sprint7-login-good`, and `sprint7-health-after-bad-image` appeared in CloudWatch JSON logs.
- Observability: dashboard `transitops-dev-api`, log group `/aws/ecs/transitops/dev/api`, 9 alarms, SNS topic, and email subscription for `pablomlopez03@gmail.com` in `PendingConfirmation`.
- Bad-image test: missing tag `sprint7-missing-3dca035` produced task `cc06e30b1b7e42eab5aaae55e4155427` with `CannotPullContainerError`; reapplying `sprint7-good-3dca035` recovered readiness to `200`.
- Restore test: temporary restore task `7f53d7309acf42349137213deca05080` against `transitops-dev-db-restore-sprint7` exited with code `0`; temporary DB, secret, task definition, IAM policy, and snapshot were cleaned up.
- Final destroy: Terraform destroyed 72 resources, state became empty, and absence checks confirmed no ALB, ECS, RDS, ECR, ACM certificate, `api.dev` records, log group, dashboard, alarms, runtime secrets, or temporary RDS snapshots remained.

## Sprint 8 Recreate And Operations Closure

Sprint 8 treats the Sprint 7 destroyed environment as the starting point. The recreate-from-scratch path is:

1. confirm AWS identity with profile `aws-pey-v1`;
2. initialize the remote Terraform backend;
3. apply the platform with `ecs_desired_count=0`;
4. publish a known-good image tag `sprint8-good-<sha>`;
5. update runtime Secrets Manager values;
6. run the ECS `--migrate-only` task and require exit code `0`;
7. apply Terraform with `ecs_desired_count=1`;
8. verify HTTPS readiness, bootstrap/login, CloudWatch dashboard, 9 alarms, SNS topic/subscription, and correlated JSON logs;
9. run `scripts/cloud/aws/Test-Sprint8AwsPosture.ps1`;
10. destroy `dev` and run `scripts/cloud/aws/Test-Sprint8DestroyAudit.ps1`.

The detailed command-level runbooks for normal deploy, rollback, restart, bootstrap-admin, correlation-id log lookup, CloudWatch alarm response, restore, and destroy/audit are in `docs/CloudOperations.md`.

Sprint 8 validation record:

- Image tag: `sprint8-good-fdf98b9`.
- Image digest: `sha256:39fb9a78726912414acbdb1af2f27e59c4ea8552b0a023a4ceab51ef74fdd6bd`.
- Migration ECS task: `cc374ade9c144745bd7566fb9289ffe3`, exit code `0`.
- ECS after rollout: desired/running `1/1`, task definition `transitops-dev-api:9`.
- HTTPS readiness: `200` with `X-Correlation-ID: sprint8-health`.
- Bootstrap/login: `201` and `200` with correlation ids `sprint8-bootstrap` and `sprint8-login`.
- Observability: dashboard, 9 alarms, log group, SNS topic, and email subscription in `PendingConfirmation`.
- Security posture script: passed.
- Final destroy: 72 resources destroyed, state empty, secrets force-deleted, destroy audit passed.
