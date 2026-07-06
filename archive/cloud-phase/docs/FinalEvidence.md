# TransitOps Final Evidence Index

## Purpose

This index groups the evidence needed to explain TransitOps in a final review, demo, or TFG defense. It separates evidence already captured in documentation from screenshots that can be added later without inventing artifacts.

The latest full AWS validation record is Sprint 8. Sprint 9 closes the repository by organizing evidence and defining the final verification path; a fresh AWS run is optional unless updated screenshots are required.

## Evidence Already Recorded

| Area | Evidence | Source |
| --- | --- | --- |
| Local backend | 87 automated tests passed through Sprint 8; local Docker and Postman/Newman smoke path documented. | `docs/LocalVerification.md`, `docs/CloudOperations.md`. |
| API contract | Health, auth, protected business endpoints, admin user management, validation/conflict behavior, and `X-Correlation-ID`. | `docs/RequirementsTraceability.md`, tests, `README.md`. |
| Docker | Hardened `.dockerignore`, single exposed runtime port, non-root runtime user `uid=1654(app)`. | `docs/CloudReliability.md`, `TransitOps.Api/Dockerfile`. |
| Terraform/IaC | Modular Terraform for foundation, ECR, RDS, runtime config, ECS/ALB, observability, GitHub OIDC, remote state. | `infra/terraform/`, `docs/CloudArchitecture.md`. |
| AWS deployment | Real `dev` applies, ECS migration tasks, service stable `desired/running = 1/1`, HTTPS readiness `200`, bootstrap/login validated. | `docs/CloudDeployment.md`, `docs/CloudOperations.md`. |
| Observability | JSON logs, correlation id, CloudWatch log group, dashboard `transitops-dev-api`, 9 alarms, SNS topic/subscription when configured. | `docs/CloudDeployment.md`, `docs/CloudOperations.md`. |
| Rollback | Known-good image rollback plus controlled missing-image failure and recovery. | `docs/CloudReliability.md`. |
| Restore | Manual RDS snapshot, temporary restored DB, temporary secret, ECS `--migrate-only`, cleanup. | `docs/CloudReliability.md`, `scripts/cloud/aws/Invoke-RdsRestoreTest.ps1`. |
| Security | Private ECS/RDS, public ingress only via ALB, HTTPS, Secrets Manager, OIDC, posture audit script. | `docs/CloudOperations.md`, `scripts/cloud/aws/Test-Sprint8AwsPosture.ps1`. |
| Cost cleanup | Mandatory destroy, state empty, no cost-bearing `transitops-dev` resources after audit. | `docs/CloudOperations.md`, `scripts/cloud/aws/Test-Sprint8DestroyAudit.ps1`. |

## Sprint 8 Reference Values

| Item | Value |
| --- | --- |
| AWS account | `661000947340` |
| Region | `eu-west-1` |
| Profile | `aws-pey-v1` |
| Domain | `https://api.dev.transitops.net` |
| Hosted zone | `Z0844787W37HXN9FIJR` |
| Image tag | `sprint8-good-fdf98b9` |
| Image digest | `sha256:39fb9a78726912414acbdb1af2f27e59c4ea8552b0a023a4ceab51ef74fdd6bd` |
| Migration task | `cc374ade9c144745bd7566fb9289ffe3`, exit code `0` |
| Task definition | `transitops-dev-api:9` |
| Health smoke | `200`, correlation id `sprint8-health` |
| Bootstrap smoke | `201`, correlation id `sprint8-bootstrap` |
| Login smoke | `200`, correlation id `sprint8-login` |
| Observability | log group `/aws/ecs/transitops/dev/api`, dashboard `transitops-dev-api`, 9 alarms, SNS email `PendingConfirmation` |
| Destroy | `72 destroyed`, state empty, destroy audit passed |

## Sprint 9 Validation Record

Sprint 9 did not perform a fresh AWS recreate because the Sprint 8 evidence remained valid and Docker Desktop was not running locally, so a new `sprint9-final-<sha>` image could not be built or pushed from this workstation. Instead, Sprint 9 closed the documentation/evidence layer and verified that the destroyed `dev` posture still holds.

| Check | Result |
| --- | --- |
| `dotnet test TransitOps.Tests/TransitOps.Tests.csproj --no-restore` | Passed, 87 tests. |
| `terraform fmt -check -recursive infra/terraform` | Passed. |
| `terraform validate` in `infra/terraform/environments/dev` | Passed. |
| `latexmk -interaction=nonstopmode memoria_tfg.tex` | Passed; `memoria-tfg/memoria_tfg.pdf` regenerated. |
| `aws sts get-caller-identity --profile aws-pey-v1 --region eu-west-1` | Confirmed account `661000947340`. |
| `terraform state list` in `infra/terraform/environments/dev` | Empty state. |
| `scripts/cloud/aws/Test-Sprint8DestroyAudit.ps1` | Passed; no cost-bearing `transitops-dev` runtime resources found. |
| `docker build -f TransitOps.Api/Dockerfile -t transitops-api:sprint9-local .` | Not executed successfully because Docker Desktop engine was not running on the workstation. |

## Screenshot Placeholders

Add real screenshots only when available. Until then, these descriptions are the canonical placeholders:

| Suggested file | What it should show |
| --- | --- |
| `memoria-tfg/imaxes/github-actions-ci.png` | Green CI workflow run with restore/build/test passing. |
| `memoria-tfg/imaxes/github-actions-deploy.png` | Manual `deploy-dev.yml` run showing image build, Terraform, migration, ECS rollout and smoke checks. |
| `memoria-tfg/imaxes/github-actions-rollback.png` | Manual `rollback-dev.yml` run with known-good image tag and readiness `200`. |
| `memoria-tfg/imaxes/health-ready-https.png` | `GET https://api.dev.transitops.net/api/v1/health/ready = 200` and response header `X-Correlation-ID`. |
| `memoria-tfg/imaxes/bootstrap-login-cloud.png` | Bootstrap returning `201` or `409`, and login returning `200` with JWT present but token value hidden. |
| `memoria-tfg/imaxes/cloudwatch-correlation-log.png` | CloudWatch Logs query filtered by a known correlation id such as `sprint8-login`. |
| `memoria-tfg/imaxes/cloudwatch-dashboard.png` | Dashboard `transitops-dev-api` with ALB, ECS, RDS and application log widgets. |
| `memoria-tfg/imaxes/cloudwatch-alarms.png` | List of the 9 CloudWatch alarms created by Terraform. |
| `memoria-tfg/imaxes/ecs-bad-image-events.png` | ECS service events showing the missing-image deployment failure and recovery path. |
| `memoria-tfg/imaxes/rds-restore-test.png` | Temporary restore DB, snapshot, and ECS `--migrate-only` task exit code `0`. |
| `memoria-tfg/imaxes/security-posture-audit.png` | `Test-Sprint8AwsPosture.ps1` passing while `dev` is running. |
| `memoria-tfg/imaxes/aws-cost-review.png` | AWS Cost Explorer by service for the temporary `dev` run. |
| `memoria-tfg/imaxes/destroy-audit.png` | `terraform state list` empty and `Test-Sprint8DestroyAudit.ps1` passing after destroy. |

## Final Evidence Rules

- Do not commit secrets, JWTs, bootstrap tokens, database passwords, or raw connection strings in screenshots.
- Hide account-user personal data that is not needed for the defense.
- If a screenshot shows an AWS bill, keep the service names and amounts but avoid exposing unrelated account details.
- Keep `dev` destroyed after evidence capture; retained resources are only the registered domain, hosted zone, and remote Terraform backend.
