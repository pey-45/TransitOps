# TransitOps Final Verification And Rehearsal

## Purpose

This document is the final Sprint 9 rehearsal guide. It gives a compact path to verify the repository locally, optionally recreate AWS `dev`, and explain the project in a technical defense without improvising the order.

## Local Verification

Run from the repository root unless a command says otherwise.

```powershell
dotnet test TransitOps.Tests/TransitOps.Tests.csproj --no-restore
terraform fmt -check -recursive infra/terraform
cd infra\terraform\environments\dev
terraform validate
cd ..\..\..
docker build -f TransitOps.Api/Dockerfile -t transitops-api:sprint9-local .
docker run --rm --entrypoint id transitops-api:sprint9-local
```

Expected result:

- all .NET tests pass;
- Terraform formatting and validation pass;
- Docker image builds;
- runtime user is non-root, expected as `uid=1654(app)`.

Basic repository risk scan:

```powershell
rg -n "AKIA|BEGIN PRIVATE KEY|aws_secret_access_key|database_password\s*=|jwt_signing_key\s*=|bootstrap.*token\s*=" .
rg -n "TODO|FIXME|pendiente|Pending|Partial|142966787103|Z0675583|Z0844787W37HXN9FJR" README.md docs memoria-tfg CONTEXT.md infra
```

Interpretation:

- real secrets must not appear in tracked files;
- `PendingConfirmation` is acceptable only for SNS email subscription documentation;
- old account IDs may appear only as historical context, never as the current target;
- old hosted zone `Z0844787W37HXN9FJR` is historical only; current zone is `Z0844787W37HXN9FIJR`.

Memory build:

```powershell
cd memoria-tfg
latexmk -interaction=nonstopmode memoria_tfg.tex
cd ..
```

Expected result: PDF builds without unresolved required files. Placeholder figures are text boxes, not missing image references.

## Optional AWS Final Recreate

Use this only when fresh evidence is needed. It creates cost-bearing resources temporarily and must end with destroy.

Context:

- Account: `661000947340`
- Region: `eu-west-1`
- Profile: `aws-pey-v1`
- Hostname: `https://api.dev.transitops.net`
- Terraform root: `infra/terraform/environments/dev`
- Image tag pattern: `sprint9-final-<sha>`

High-level sequence:

1. Confirm identity:

```powershell
aws sts get-caller-identity --profile aws-pey-v1 --region eu-west-1
```

2. Initialize and apply infrastructure with ECS at zero:

```powershell
cd infra\terraform\environments\dev
terraform init -backend-config=backend.hcl
terraform apply -auto-approve -var="api_image_tag=sprint9-final-<sha>" -var="ecs_desired_count=0"
```

3. Build and push image:

```powershell
cd ..\..\..
docker build -f TransitOps.Api/Dockerfile -t transitops-api:sprint9-final-<sha> .
aws ecr get-login-password --region eu-west-1 --profile aws-pey-v1 | docker login --username AWS --password-stdin 661000947340.dkr.ecr.eu-west-1.amazonaws.com
docker tag transitops-api:sprint9-final-<sha> 661000947340.dkr.ecr.eu-west-1.amazonaws.com/transitops/api:sprint9-final-<sha>
docker push 661000947340.dkr.ecr.eu-west-1.amazonaws.com/transitops/api:sprint9-final-<sha>
```

4. Load runtime secrets in Secrets Manager using values kept outside Git.
5. Run the ECS `--migrate-only` task and require container exit code `0`.
6. Scale service:

```powershell
cd infra\terraform\environments\dev
terraform apply -auto-approve -var="api_image_tag=sprint9-final-<sha>" -var="ecs_desired_count=1"
aws ecs wait services-stable --cluster transitops-dev-cluster --services transitops-dev-api-svc --profile aws-pey-v1 --region eu-west-1
```

7. Smoke checks:

```powershell
curl.exe -i -H "X-Correlation-ID: sprint9-health" https://api.dev.transitops.net/api/v1/health/ready
```

Then verify bootstrap/login, CloudWatch logs by `sprint9-health`, dashboard, alarms, SNS subscription, and posture audit:

```powershell
.\scripts\cloud\aws\Test-Sprint8AwsPosture.ps1
```

8. Destroy and audit:

```powershell
cd infra\terraform\environments\dev
terraform destroy -auto-approve
terraform state list
cd ..\..\..
.\scripts\cloud\aws\Test-Sprint8DestroyAudit.ps1
```

Expected final state: no cost-bearing `transitops-dev` ALB, ECS, RDS, ECR, NAT, ACM certificate, `api.dev` records, CloudWatch resources, runtime secrets, or temporary snapshots remain.

## Defense Narrative

Recommended order:

1. Explain scope discipline: small backend, deep cloud/DevOps quality.
2. Show functional API: auth, roles, transports, vehicles, drivers, assignment, lifecycle, events.
3. Show local reproducibility: Docker Compose, migrations, tests and smoke flow.
4. Show infrastructure: Terraform modules, remote state, VPC, ALB/ECS/RDS, Secrets Manager, Route53/ACM.
5. Show delivery: GitHub Actions with OIDC, image build/push, ECS migration task, deployment and rollback workflow.
6. Show observability: JSON logs, `X-Correlation-ID`, CloudWatch dashboard, alarms and SNS.
7. Show reliability: bad-image rollback and RDS restore test.
8. Show security and cost: private networking, secrets, posture audit, cost-bearing resources, destroy audit.
9. Close with traceability: `docs/RequirementsTraceability.md` proves the delivered system maps back to requirements.

Key message: TransitOps is intentionally small as a product, but complete as a cloud-operable backend slice.
