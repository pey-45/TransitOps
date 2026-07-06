# TransitOps Requirements Traceability

## Purpose

This document closes Sprint 9 by mapping the delivered system back to `docs/Requirements.md`. It is intended for final review, demo preparation, and TFG defense: each requirement has an implementation reference, verification path, and evidence source.

Status values:

- `Completed`: implemented and verified in the repository or in the AWS `dev` validation records.
- `Accepted dev scope`: implemented enough for the disposable `dev` environment, with a documented tradeoff.
- `Out of scope`: intentionally not implemented in this project phase.

## Functional Requirements

| ID | Status | Implementation | Verification | Evidence |
| --- | --- | --- | --- | --- |
| FR-01 Health and platform endpoints | Completed | `GET /api/v1/health/live` and `GET /api/v1/health/ready`; readiness checks PostgreSQL. | `HealthEndpointsTests`; AWS health smoke in Sprints 6-8. | `docs/CloudDeployment.md`, `docs/CloudOperations.md`, `memoria-tfg/contido/validacion.tex`. |
| FR-02 First admin bootstrap | Completed | `POST /api/v1/auth/bootstrap-admin` with external bootstrap token and one-active-admin guard. | Auth integration tests; local/cloud bootstrap smoke. | `docs/CloudDeployment.md`, `docs/CloudOperations.md`, `docs/FinalEvidence.md`. |
| FR-03 User administration | Completed | Admin-only user list/detail/create/role/activation flows with last-active-admin protection. | User-management integration tests. | `README.md`, `docs/Requirements.md`. |
| FR-04 Authentication | Completed | Username/password login, password hashing, JWT issuance from externalized config. | Auth integration tests; cloud login smoke. | `docs/CloudDeployment.md`, `docs/CloudOperations.md`. |
| FR-05 Authorization | Completed | Bearer authentication on business controllers; admin/operator role policy. | Integration tests for protected/admin-only paths. | `docs/Requirements.md`, `README.md`. |
| FR-06 Transport management | Completed | PostgreSQL-backed create/list/detail/update/delete with active-row uniqueness. | Transport endpoint tests; local smoke flow. | `README.md`, `docs/LocalVerification.md`. |
| FR-07 Vehicle management | Completed | PostgreSQL-backed vehicle CRUD with active-row uniqueness and validation. | Vehicle endpoint tests; local smoke flow. | `README.md`, `docs/LocalVerification.md`. |
| FR-08 Driver management | Completed | PostgreSQL-backed driver CRUD with active-row uniqueness and validation. | Driver endpoint tests; local smoke flow. | `README.md`, `docs/LocalVerification.md`. |
| FR-09 Assignment workflow | Completed | Explicit vehicle+driver assignment on planned transports only. | Assignment integration tests in transport test coverage. | `README.md`, `docs/Requirements.md`. |
| FR-10 Transport lifecycle | Completed | Explicit state transitions with terminal-state and assignment-prerequisite rules. | `TransportStateMachineTests`; endpoint tests. | `README.md`, `docs/Requirements.md`. |
| FR-11 Shipment events | Completed | Authenticated event creation/history; actor comes from authenticated user context. | Shipment-event integration tests; local smoke. | `README.md`, `docs/Requirements.md`. |
| FR-12 Listings and filters | Completed | Transport filtering by status/date/vehicle/driver plus pagination. | Transport list tests; demo-capable API. | `README.md`, `docs/LocalVerification.md`. |
| FR-13 Validation, response contract, and conflicts | Completed | Common success/error envelopes and distinct validation/auth/forbidden/not-found/conflict responses. | Endpoint tests cover representative `400`, `401`, `403`, `404`, `409` paths. | `docs/Requirements.md`, API tests. |
| FR-14 Audit trail and logical deletion | Completed | `created_at`, `updated_at`, `deleted_at`, partial unique indexes, shipment-event actor traceability. | EF Core migration/schema, CRUD delete/list tests, event tests. | `docs/Requirements.md`, `memoria-tfg/contido/requisitos.tex`. |

## Non-Functional Requirements

| ID | Status | Implementation | Verification | Evidence |
| --- | --- | --- | --- | --- |
| NFR-01 Scope discipline and simplicity | Completed | Backend-only modular monolith with only `TransitOps.Api` and `TransitOps.Tests`. | Repository structure review. | `README.md`, `CONTEXT.md`. |
| NFR-02 PostgreSQL as system of record | Completed | EF Core PostgreSQL migrations are the canonical schema; RDS PostgreSQL used in AWS. | Tests, local Docker, ECS migration tasks. | `docs/CloudDeployment.md`, `docs/CloudOperations.md`. |
| NFR-03 Reproducible local execution | Completed | `.env.example`, Docker Compose, migration-on-startup for local, local verification guide. | `docker compose config`, local smoke flow. | `docs/LocalVerification.md`. |
| NFR-04 Cloud deployability | Completed | Docker image, ECR, ECS Fargate, ALB, Route53/ACM, RDS, Secrets Manager, SSM. | Real AWS deploy/recreate validations in Sprints 6-8. | `docs/CloudDeployment.md`, `docs/CloudOperations.md`. |
| NFR-05 Security baseline | Completed for project scope | Hashed passwords, JWT config externalized, private ECS/RDS, HTTPS, Secrets Manager, OIDC, SG review. | Sprint 8 posture audit script. | `docs/CloudOperations.md`, `scripts/cloud/aws/Test-Sprint8AwsPosture.ps1`. |
| NFR-06 Reliability and controlled failure | Completed | Readiness checks, ECS circuit breaker, rollback workflow, restore script, runtime tuning. | Sprint 7 bad-image and RDS restore validation. | `docs/CloudReliability.md`. |
| NFR-07 Maintainability and documentation | Completed | Requirements, roadmap, cloud architecture/deployment/reliability/operations docs, final evidence and verification docs. | Sprint 9 documentation review. | `docs/FinalEvidence.md`, `docs/FinalVerification.md`. |
| NFR-08 Testability and CI | Completed | xUnit integration tests, GitHub Actions CI, local smoke collection. | `dotnet test`; CI workflow definition. | `.github/workflows/ci.yml`, `docs/LocalVerification.md`. |
| NFR-09 Observability | Completed | JSON console logs, `X-Correlation-ID`, CloudWatch log group, dashboard, metric filter, 9 alarms, SNS optional email. | Sprint 6-8 CloudWatch validation. | `docs/CloudDeployment.md`, `docs/CloudOperations.md`, `memoria-tfg/contido/observabilidad_seguridad.tex`. |
| NFR-10 Small-scale performance | Completed for academic workload | Pagination, filtering, relational indexes/constraints, bounded DB pool settings for cloud. | Endpoint tests and runtime tuning record. | `docs/CloudReliability.md`, EF Core migrations. |
| NFR-11 Infrastructure as code and controlled delivery | Completed | Terraform modules/environments, S3/DynamoDB remote state, GitHub OIDC, deploy/rollback workflows. | `terraform fmt`, `terraform validate`, real AWS applies/destroys. | `infra/terraform/`, `.github/workflows/`, `docs/CloudOperations.md`. |

## Delivery Gates

| Gate | Status | Evidence |
| --- | --- | --- |
| Gate A - Local MVP Ready | Completed | Functional tests, local Docker guide, Postman/Newman smoke flow, repository README. |
| Gate B - Cloud Deployment Ready | Completed | HTTPS AWS `dev` deployments, ECS/RDS migrations, CloudWatch observability, rollback, restore, posture audit, destroy audit. |

## Accepted Tradeoffs

- `dev` keeps RDS automated backup retention at `0` and skips final snapshots to avoid recurring cost; restore is validated through a temporary manual snapshot.
- The GitHub OIDC deploy role remains broad enough for Terraform apply/destroy in `dev`; the trust policy is repository/branch-scoped and the risk is documented.
- SNS email subscriptions may remain `PendingConfirmation` until the mailbox owner confirms the email.
- No frontend, autoscaling tuning, WAF, X-Ray/OpenTelemetry, multi-region design, or production-grade backup policy is included in the current scope.
