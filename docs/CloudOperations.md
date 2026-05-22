# TransitOps Cloud Operations

## Purpose

This document closes the Sprint 8 operational layer for TransitOps: security review, cost review, recreate-from-scratch, and day-two runbooks for the disposable AWS `dev` environment.

Target context:

- AWS account: `661000947340`
- Region: `eu-west-1`
- Profile: `aws-pey-v1`
- Environment: `dev`
- API hostname: `https://api.dev.transitops.net`
- Terraform root: `infra/terraform/environments/dev`

## Security Review

Current controls:

- ALB is the only public application ingress.
- HTTP `80` is used only for redirect when HTTPS is enabled; HTTPS `443` terminates at ALB with ACM.
- ECS tasks run in private app subnets with `assign_public_ip = false`.
- RDS runs in private data subnets with `publicly_accessible = false`.
- Security-group path is restricted to Internet -> ALB, ALB -> ECS port `8080`, and ECS -> RDS port `5432`.
- Runtime secrets are stored in Secrets Manager and referenced by ARN in the ECS task definition.
- Non-secret runtime configuration is stored in SSM Parameter Store or environment variables.
- GitHub Actions uses OIDC role assumption instead of static AWS access keys.
- API authentication uses JWT, hashed passwords, role authorization, and externalized first-admin bootstrap token.
- API responses and logs use `X-Correlation-ID` without exposing internal exception traces.

Findings and decisions:

| Area | Finding | Decision |
| --- | --- | --- |
| ECS execution role | Runtime config policy is scoped to the exact secret ARNs passed into the task definition. | Accepted. |
| ECS task role | No application AWS API permissions are currently attached. | Accepted. |
| GitHub OIDC role | Terraform deployment role uses broad service permissions for the managed stack. | Accepted for `dev`; trust is restricted to `pey-45/TransitOps` and the configured branch. Fine-grained deployment policy remains a future hardening item because over-tightening now risks breaking Terraform apply/destroy. |
| Security groups | ECS egress allows HTTPS to `0.0.0.0/0` for ECR and AWS APIs through NAT. | Accepted for this VPC design; a future production design can replace NAT with VPC endpoints and narrower egress. |
| Backups | `dev` uses RDS automated retention `0` and skips final snapshots. | Accepted because restore is validated with manual temporary snapshots and `dev` is destroyed after evidence capture. |
| Notifications | SNS email may remain `PendingConfirmation` until the email is accepted. | Accepted and documented as an operational state. |

Security verification script:

```powershell
.\scripts\cloud\aws\Test-Sprint8AwsPosture.ps1
```

Expected result while `dev` is running: all hard checks pass, with a documented warning for the broad GitHub OIDC Terraform deployment role.

## Cost Review

Cost-bearing resources during an active `dev` apply:

- NAT Gateway and Elastic IP.
- Application Load Balancer.
- ECS Fargate tasks.
- RDS PostgreSQL instance and storage.
- CloudWatch logs, dashboards, alarms, and custom metric filters.
- Secrets Manager secrets.
- ECR image storage.
- Route 53 hosted zone and DNS queries.

Cost controls for `dev`:

- `ecs_desired_count = 0` for the first infrastructure apply.
- `database_backup_retention_days = 0`.
- `database_skip_final_snapshot = true`.
- `database_deletion_protection = false`.
- `ecr_force_delete = true`.
- Temporary restore DB, secret, IAM policy, task definition revision, and snapshot are cleaned up by the restore script.
- Final `terraform destroy` is mandatory after Sprint evidence capture.

Resources intentionally retained outside application destroy:

- Registered domain `transitops.net`.
- Public hosted zone `transitops.net`.
- Terraform remote-state S3 bucket.
- Terraform lock DynamoDB table.

Post-destroy verification script:

```powershell
.\scripts\cloud\aws\Test-Sprint8DestroyAudit.ps1
```

Expected result after destroy: Terraform state is empty and no `transitops-dev` ALB, ECS cluster, RDS instance, ECR repository, ACM certificate, `api.dev` DNS record, CloudWatch log group/dashboard/alarms, runtime secrets, NAT Gateway, or temporary restore snapshot remains.

## Recreate From Scratch

Use this path after the `dev` stack has been destroyed but the remote Terraform backend still exists.

1. Validate AWS identity:

```powershell
aws sts get-caller-identity --profile aws-pey-v1 --region eu-west-1
```

Expected account: `661000947340`.

2. Initialize Terraform:

```powershell
cd infra\terraform\environments\dev
terraform init -backend-config=backend.hcl
terraform validate
```

3. Apply the platform without running ECS tasks:

```powershell
terraform apply -auto-approve `
  -var="api_image_tag=sprint8-good-<sha>" `
  -var="ecs_desired_count=0"
```

4. Build and push the API image:

```powershell
docker build -f ..\..\..\TransitOps.Api\Dockerfile -t transitops-api:sprint8-good-<sha> ..\..\..
aws ecr get-login-password --region eu-west-1 --profile aws-pey-v1 |
  docker login --username AWS --password-stdin 661000947340.dkr.ecr.eu-west-1.amazonaws.com
docker tag transitops-api:sprint8-good-<sha> 661000947340.dkr.ecr.eu-west-1.amazonaws.com/transitops/api:sprint8-good-<sha>
docker push 661000947340.dkr.ecr.eu-west-1.amazonaws.com/transitops/api:sprint8-good-<sha>
```

5. Load runtime secret values into Secrets Manager:

```powershell
aws secretsmanager put-secret-value --secret-id transitops/dev/app/db-connection-string --secret-string "<connection-string>" --profile aws-pey-v1 --region eu-west-1
aws secretsmanager put-secret-value --secret-id transitops/dev/app/jwt-signing-key --secret-string "<jwt-signing-key>" --profile aws-pey-v1 --region eu-west-1
aws secretsmanager put-secret-value --secret-id transitops/dev/app/bootstrap-first-admin-token --secret-string "<bootstrap-token>" --profile aws-pey-v1 --region eu-west-1
```

6. Run migrations as a one-off ECS task using `--migrate-only`.

7. Scale the service:

```powershell
terraform apply -auto-approve `
  -var="api_image_tag=sprint8-good-<sha>" `
  -var="ecs_desired_count=1"
aws ecs wait services-stable --cluster transitops-dev-cluster --services transitops-dev-api-svc --profile aws-pey-v1 --region eu-west-1
```

8. Validate:

```powershell
curl.exe -i -H "X-Correlation-ID: sprint8-health" https://api.dev.transitops.net/api/v1/health/ready
```

Then verify bootstrap/login, CloudWatch logs, dashboard, alarms, SNS topic, and the security posture script.

## Runbooks

### Normal Deploy

Objective: deploy a new API image and run migrations safely.

Preferred path: `.github/workflows/deploy-dev.yml`.

Success signals: build/test pass, image pushed to ECR, migration task exit code `0`, ECS desired/running `1/1`, readiness `200`, login `200`, and observability resources verified.

Cleanup: run `terraform destroy` after evidence capture unless the environment must stay alive temporarily.

### Rollback To Known Tag

Objective: restore service to a previously validated ECR image.

Preferred path: `.github/workflows/rollback-dev.yml`.

Manual command:

```powershell
terraform apply -auto-approve -var="api_image_tag=<known-good-tag>" -var="ecs_desired_count=1"
aws ecs wait services-stable --cluster transitops-dev-cluster --services transitops-dev-api-svc --profile aws-pey-v1 --region eu-west-1
```

Success signals: ECS stable, active task definition image contains the known-good tag, readiness `200`, login `200`.

### Restart ECS Service

Objective: restart running tasks without changing the image tag.

```powershell
aws ecs update-service `
  --cluster transitops-dev-cluster `
  --service transitops-dev-api-svc `
  --force-new-deployment `
  --profile aws-pey-v1 `
  --region eu-west-1
aws ecs wait services-stable --cluster transitops-dev-cluster --services transitops-dev-api-svc --profile aws-pey-v1 --region eu-west-1
```

Success signals: deployment reaches stable state and readiness returns `200`.

### Bootstrap First Admin

Objective: create the first admin only when the database has no active admin.

```powershell
curl.exe -i `
  -H "Content-Type: application/json" `
  -H "X-Bootstrap-Token: <bootstrap-token>" `
  -d "{\"username\":\"<admin>\",\"email\":\"<email>\",\"password\":\"<password>\"}" `
  https://api.dev.transitops.net/api/v1/auth/bootstrap-admin
```

Success signals: `201 Created` on first run, or `409 first_admin_already_exists` on a later run.

### Investigate By Correlation ID

Objective: find all logs for one request.

```powershell
aws logs filter-log-events `
  --log-group-name /aws/ecs/transitops/dev/api `
  --filter-pattern "<correlation-id>" `
  --profile aws-pey-v1 `
  --region eu-west-1
```

Success signals: log events include the same `State.CorrelationId` or scope `correlationId` as the HTTP response header.

### Respond To CloudWatch Alarm

Objective: triage a basic runtime alarm.

1. Identify alarm name and affected service.
2. Check ECS service events and running task count.
3. Check ALB target health.
4. Query CloudWatch logs by recent errors and correlation id if available.
5. Use rollback if the alarm followed a deployment.
6. Use restart only when the image is known good and the failure appears transient.

Success signals: alarm returns to `OK`, readiness returns `200`, and ECS is stable.

### Restore RDS

Objective: validate database restore from manual snapshot.

Use:

```powershell
.\scripts\cloud\aws\Invoke-RdsRestoreTest.ps1
```

Success signals: temporary migration task exits `0`; temporary DB, secret, task definition revision, IAM policy, and snapshot are cleaned up.

### Destroy And Audit

Objective: remove cost-bearing `dev` resources.

```powershell
cd infra\terraform\environments\dev
terraform destroy -auto-approve
terraform state list
cd ..\..\..
.\scripts\cloud\aws\Test-Sprint8DestroyAudit.ps1
```

Success signals: state is empty and the destroy audit reports no cost-bearing `transitops-dev` resources.

## Sprint 8 Validation Record

Sprint 8 was validated against AWS account `661000947340` in `eu-west-1` and the `dev` environment was destroyed afterwards.

Local validation:

- `dotnet test TransitOps.Tests\TransitOps.Tests.csproj --no-restore`: 87 tests passed.
- `terraform fmt -check -recursive infra\terraform`: passed.
- `terraform validate` in `infra/terraform/environments/dev`: passed.
- `docker build -f TransitOps.Api\Dockerfile -t transitops-api:sprint8-local .`: passed.
- Runtime user check: `docker run --rm --entrypoint id transitops-api:sprint8-local` returned `uid=1654(app) gid=1654(app)`.

AWS recreate evidence:

- Terraform initial apply with `ecs_desired_count=0`: 72 resources created.
- Good image tag: `sprint8-good-fdf98b9`.
- ECR image digest: `sha256:39fb9a78726912414acbdb1af2f27e59c4ea8552b0a023a4ceab51ef74fdd6bd`.
- Task definition before scaling service: `transitops-dev-api:9`.
- Migration task: `arn:aws:ecs:eu-west-1:661000947340:task/transitops-dev-cluster/cc374ade9c144745bd7566fb9289ffe3`, exit code `0`.
- ECS service after rollout: desired/running `1/1`.
- HTTPS readiness: `GET https://api.dev.transitops.net/api/v1/health/ready = 200`, correlation id `sprint8-health`.
- Bootstrap admin: `201`, correlation id `sprint8-bootstrap`.
- Login admin: `200`, correlation id `sprint8-login`.
- Observability: log group `/aws/ecs/transitops/dev/api`, dashboard `transitops-dev-api`, 9 alarms, SNS topic, and email subscription for `pablomlopez03@gmail.com` in `PendingConfirmation`.
- Terraform convergence check: `terraform plan -detailed-exitcode` returned no changes.

Security posture evidence:

- `scripts/cloud/aws/Test-Sprint8AwsPosture.ps1`: passed.
- Confirmed RDS `PubliclyAccessible = false`.
- Confirmed ECS `assignPublicIp = DISABLED`.
- Confirmed ALB public ports limited to `80` and `443`.
- Confirmed ECS ingress from ALB only and RDS ingress from ECS only.
- Confirmed ECS task role has no inline policies.
- Confirmed ECS execution role runtime config policy does not use `Resource = *`.
- Confirmed GitHub deploy role broad service permissions are documented as the accepted Terraform `dev` deployment tradeoff.

Final teardown evidence:

- `terraform destroy -auto-approve`: 72 resources destroyed.
- `terraform state list`: empty.
- Runtime secrets were force-deleted after Terraform scheduled deletion, so they do not remain in Secrets Manager.
- `scripts/cloud/aws/Test-Sprint8DestroyAudit.ps1`: passed.
- Verified absent: ECS cluster, RDS database, temporary restore DB, ECR repository, API log group, CloudWatch dashboard, CloudWatch alarms, ACM API certificate, runtime secrets, manual dev snapshots, dev ALB, and `api.dev.transitops.net` Route 53 records.
- Local Sprint 8 temporary files under `infra\.sprint8-*.tmp` were removed.
