# TransitOps Cloud Reliability

## Purpose

This document captures the Sprint 7 operational procedures for rollback, RDS restore validation, runtime tuning, and cost-safe teardown of the AWS `dev` environment.

The procedures target:

- AWS account `661000947340`
- Region `eu-west-1`
- Environment `dev`
- API hostname `https://api.dev.transitops.net`
- Terraform root `infra/terraform/environments/dev`

## Sprint 7 Runtime Baseline

`dev` is recreated with:

- `enable_https = true`
- `root_domain = "transitops.net"`
- `hosted_zone_id = "Z0844787W37HXN9FIJR"`
- `alarm_email = "pablomlopez03@gmail.com"`
- `ecs_desired_count = 0` for the first infrastructure apply
- `database_backup_retention_days = 0`
- `ecr_force_delete = true`

The first apply creates infrastructure without running ECS tasks. After the API image is pushed to ECR, migrations run as a one-off ECS task using `--migrate-only`, and the service is scaled to `ecs_desired_count = 1`.

## Rollback Runbook

Rollback means redeploying a known-good ECR image tag through Terraform, keeping the same infrastructure and setting ECS desired count to `1`.

The preferred manual workflow is `.github/workflows/rollback-dev.yml`.

Inputs:

- `api_image_tag`: a known-good image tag already present in ECR, for example `sprint7-good-<sha>`.

What the workflow does:

1. Assumes the GitHub OIDC deployment role.
2. Initializes Terraform against the remote state backend.
3. Applies Terraform with `TF_VAR_api_image_tag=<input>` and `TF_VAR_ecs_desired_count=1`.
4. Waits for ECS service stability.
5. Confirms the active task definition image contains the requested tag.
6. Runs `GET /api/v1/health/ready`.

Manual local equivalent:

```powershell
cd infra\terraform\environments\dev
terraform init -backend-config=backend.hcl
terraform apply `
  -var="api_image_tag=sprint7-good-<sha>" `
  -var="ecs_desired_count=1"
aws ecs wait services-stable `
  --cluster transitops-dev-cluster `
  --services transitops-dev-api-svc `
  --region eu-west-1 `
  --profile aws-pey-v1
```

Evidence to capture:

- task definition ARN before rollback;
- task definition ARN after rollback;
- container image tag before and after;
- ECS deployment events showing stable service;
- `GET https://api.dev.transitops.net/api/v1/health/ready = 200`;
- login status `200`.

## Controlled Bad Image Test

The ECS deployment circuit breaker is tested by applying a tag that does not exist in ECR, then returning Terraform state to the known-good tag.

Procedure:

```powershell
cd infra\terraform\environments\dev
terraform apply `
  -var="api_image_tag=sprint7-missing-<sha>" `
  -var="ecs_desired_count=1"

aws ecs describe-services `
  --cluster transitops-dev-cluster `
  --services transitops-dev-api-svc `
  --query "services[0].events[0:10].[createdAt,message]" `
  --output table `
  --region eu-west-1 `
  --profile aws-pey-v1

terraform apply `
  -var="api_image_tag=sprint7-good-<sha>" `
  -var="ecs_desired_count=1"
```

The expected result is that ECS cannot pull the missing image, the deployment fails, the circuit breaker rolls runtime back to the last healthy deployment, and a final apply with the good tag restores Terraform state and ECS task definition alignment.

Evidence to capture:

- failed deployment event for missing image;
- circuit breaker rollback event;
- service still or again stable on the good task definition;
- health endpoint `200` after recovery.

## RDS Restore Test

`dev` keeps automated backup retention at `0` days to avoid recurring cost. Sprint 7 validates restore capability through a temporary manual snapshot.

Script:

```powershell
$env:DATABASE_USERNAME = "<dev-db-username>"
$env:DATABASE_PASSWORD = "<dev-db-password>"

.\scripts\cloud\aws\Invoke-RdsRestoreTest.ps1
```

What the script does:

1. Creates manual snapshot `transitops-dev-db-sprint7-restore-test` from `transitops-dev-db`.
2. Restores temporary instance `transitops-dev-db-restore-sprint7`.
3. Creates temporary secret `transitops/dev/app/db-connection-string-restore-sprint7`.
4. Registers temporary task definition `transitops-dev-api-restore-sprint7`.
5. Runs an ECS Fargate task with `--migrate-only` against the restored database.
6. Requires container exit code `0`.
7. Deletes the temporary task definition revision, secret, restored DB, and snapshot.

Use `-KeepEvidence` only when screenshots or live AWS inspection are needed before cleanup.

Evidence to capture:

- snapshot available;
- restored DB available;
- temporary ECS task ARN;
- migrate-only task exit code `0`;
- cleanup confirmation showing no restored DB, temporary secret, or manual snapshot remains.

## Runtime Tuning Decisions

The Sprint 7 runtime posture is intentionally small:

- Docker image context excludes build outputs, Terraform state, local secrets, documentation, and memory artifacts through `.dockerignore`.
- The API container exposes only port `8080`; port `8081` is not part of the ECS runtime contract.
- The runtime image still installs only `libgssapi-krb5-2` with `--no-install-recommends`, because Npgsql/Kerberos-related runtime dependencies can otherwise fail in Linux containers.
- ECS task definition sets `stopTimeout = 30` seconds, giving ASP.NET Core a bounded graceful shutdown period.
- ALB readiness uses `/api/v1/health/ready`.
- ECS health-check grace period is `60` seconds.
- Target group deregistration delay is `30` seconds in `dev`, reducing replacement time while still draining in-flight requests.
- Cloud connection strings use explicit Npgsql runtime values: `Timeout=15`, `Command Timeout=30`, `Pooling=true`, and `Maximum Pool Size=20`.

## Final Destroy

At the end of Sprint 7, destroy `dev`:

```powershell
cd infra\terraform\environments\dev
terraform destroy -auto-approve
terraform state list
```

Verify absence of:

- ALB and listeners;
- ECS cluster, service, and running tasks;
- RDS `transitops-dev-db`;
- RDS restore DB `transitops-dev-db-restore-sprint7`;
- NAT Gateway;
- ECR repository `transitops/api`;
- ACM certificate for `api.dev.transitops.net`;
- Route53 records for `api.dev.transitops.net`;
- CloudWatch log group, dashboard, alarms, and metric filters;
- Secrets Manager runtime secrets;
- temporary restore snapshot and temporary restore secret.

Intentionally retained resources:

- registered domain `transitops.net`;
- public hosted zone `transitops.net`;
- Terraform remote-state S3 bucket;
- Terraform lock DynamoDB table.

## Sprint 7 Validation Record

Validation was executed against AWS account `661000947340` in `eu-west-1` and the environment was destroyed afterwards.

Local validation:

- `dotnet test TransitOps.Tests\TransitOps.Tests.csproj --no-restore`: 87 tests passed.
- `terraform fmt -check -recursive infra\terraform`: passed.
- `terraform validate` in `infra/terraform/environments/dev`: passed.
- `docker build -f TransitOps.Api\Dockerfile -t transitops-api:sprint7-local .`: passed.
- Runtime user check: `docker run --rm --entrypoint id transitops-api:sprint7-local` returned `uid=1654(app) gid=1654(app)`.

AWS validation:

- Good image tag: `sprint7-good-3dca035`.
- Good image digest: `sha256:be2cf29b22971853c5dd003912d954aa09daea5094481ce5aa29b06039ec4bbb`.
- Migration task: `arn:aws:ecs:eu-west-1:661000947340:task/transitops-dev-cluster/1ec56ac77fbb4142a380b9e4881e3d24`, exit code `0`.
- ECS service after rollout: desired/running `1/1`, task definition `transitops-dev-api:4`.
- HTTPS readiness: `GET https://api.dev.transitops.net/api/v1/health/ready = 200`, correlation id `sprint7-health-good`.
- Bootstrap admin: `201`, correlation id `sprint7-bootstrap-good`.
- Login admin: `200`, correlation id `sprint7-login-good`.
- Observability: log group `/aws/ecs/transitops/dev/api`, dashboard `transitops-dev-api`, 9 CloudWatch alarms, and JSON logs filtered by `State.CorrelationId`.
- SNS email subscription for `pablomlopez03@gmail.com`: created and left in `PendingConfirmation`, which is expected until the email is manually confirmed.

Rollback and failure evidence:

- Missing image tag tested: `sprint7-missing-3dca035`.
- Failed task: `arn:aws:ecs:eu-west-1:661000947340:task/transitops-dev-cluster/cc06e30b1b7e42eab5aaae55e4155427`.
- Failure reason: `CannotPullContainerError` because the ECR tag did not exist.
- Recovery: Terraform was reapplied with `sprint7-good-3dca035`; readiness returned `200` with correlation id `sprint7-health-after-bad-image`.

RDS restore evidence:

- Restore script: `scripts/cloud/aws/Invoke-RdsRestoreTest.ps1`.
- Manual snapshot: `transitops-dev-db-sprint7-restore-test`.
- Temporary DB: `transitops-dev-db-restore-sprint7`.
- Temporary task definition: `transitops-dev-api-restore-sprint7:2`.
- Successful restore migration task: `arn:aws:ecs:eu-west-1:661000947340:task/transitops-dev-cluster/7f53d7309acf42349137213deca05080`, exit code `0`.
- Operational finding: the temporary restore secret required explicit temporary `secretsmanager:GetSecretValue` permission on the ECS execution role. The script now grants and removes that inline policy as part of the test.
- Cleanup verified no temporary restored DB, temporary secret, or manual snapshot remained.

Final teardown evidence:

- `terraform destroy -auto-approve` completed with `72 destroyed`.
- `terraform state list` returned no resources.
- Verified absent: ALB, ECS cluster, RDS instance, ECR repository, ACM certificate, `api.dev.transitops.net` Route53 records, CloudWatch log group, dashboard, alarms, RDS snapshots, and runtime Secrets Manager secrets.
- NAT Gateway `nat-0ad9138624d4b344e` reached `deleted`.
- Local Sprint 7 temporary files under `infra\.sprint7-*.tmp` were removed.
