# TransitOps · Sprint Roadmap

## Purpose

Translate the requirements into a weekly sprint plan that keeps the backend functionally small and pushes the project as far as possible into real cloud engineering, delivery automation, operations, and technical defensibility.

## Planning Model

- Dates through March 29, 2026 remain the completed historical baseline.
- From March 30, 2026 onward, planning is organized into `1-week sprints`.
- Each sprint runs from `Monday` to `Sunday`.
- Sprint 1 intentionally compresses all work previously considered for the March 30 to April 12 window into a single week, because the project owner considers that scope feasible in one sprint.
- Each sprint defines mandatory scope, required artifacts, a strict definition of done, and explicit non-negotiable items that cannot remain open at sprint close.

## Sprint Cadence

| Sprint | Dates | Dominant Phase |
| --- | --- | --- |
| Sprint 1 | 30 Mar - 05 Apr | Close the full local MVP and freeze the cloud design baseline |
| Sprint 2 | 06 Apr - 12 Apr | Build the Terraform and AWS foundation layer |
| Sprint 3 | 13 Apr - 19 Apr | Build the AWS runtime layer |
| Sprint 4 | 20 Apr - 26 Apr | Build the cloud delivery and bootstrap path |
| Sprint 5 | 27 Apr - 03 May | Execute the first real AWS deployment |
| Sprint 6 | 04 May - 10 May | Add observability and deployment hardening |
| Sprint 7 | 11 May - 17 May | Close rollback, restore, runtime tuning, and platform reliability |
| Sprint 8 | 18 May - 24 May | Close security, cost, recreate-from-scratch, and runbooks |
| Sprint 9 | 25 May - 31 May | Close evidence, traceability, rehearsal, and final hardening |

## Cloud Engineering Coverage

This roadmap is intentionally designed to cover as much relevant cloud engineering as is reasonable for the project scope:

- Terraform structure, provider/version pinning, naming, tagging, remote state, and environment conventions
- VPC, subnet layout, routing, egress strategy, and least-privilege security groups
- ECR repository, image publication path, retention policy, and image scanning
- ECS Fargate runtime, task definition, service deployment behavior, and health checks
- ALB, Route53, and ACM for reachable HTTPS ingress
- RDS PostgreSQL provisioning, backup posture, deletion protection policy, and restore story
- Secrets Manager and/or Parameter Store for runtime configuration and secret handling
- IAM execution roles, runtime roles, and GitHub OIDC-based deployment access
- EF Core migration strategy and first-admin bootstrap strategy for AWS
- GitHub Actions for build/test, Terraform validation/plan, image push, and deployment
- CloudWatch logs, log retention, request correlation, dashboard, metrics, and alarms
- Rollback, recreate-from-scratch, security review, cost review, runbooks, evidence capture, and final technical rehearsal

## Estimated Remaining Effort

- Estimated remaining effort from March 30 onward: about `60-66 hours`.
- Planned end date for the current roadmap: `May 31, 2026`.
- The plan deliberately compresses the whole local-MVP closure into Sprint 1 so that almost the entire remaining schedule is dominated by cloud engineering work.

## Historical Baseline Already Completed

| Date | Status | Focus | Result Already Present |
| --- | --- | --- | --- |
| 24 Mar | Completed | Scope, repository, and initial documentation | Repository structure, README, and initial planning baseline exist. |
| 25 Mar | Completed | Domain and PostgreSQL schema | Main entities, transport states, and initial schema script are already defined. |
| 26 Mar | Completed | API bootstrap and common contract | The API starts, common response/error structure exists, and controllers are in place. |
| 27 Mar | Completed | EF Core persistence baseline | `TransitOpsDbContext`, entity configurations, baseline migration, and DB-backed readiness are implemented. |
| 28 Mar | Completed | Transport read slice | `GET /transports` and `GET /transports/{id}` read active rows from PostgreSQL with coherent `404` behavior. |
| 29 Mar | Completed | Transport write slice | Transport create and update persist through EF Core with validation and duplicate-reference checks. |

## Sprint 1 · 30 Mar to 05 Apr

**Phase**
Close the full local MVP and freeze the cloud design baseline.

**Mandatory Scope**

- Complete vehicle CRUD including list, detail, create, update, and logical delete.
- Complete driver CRUD including list, detail, create, update, and logical delete.
- Complete the missing transport list filters and basic pagination needed for demo use.
- Implement the explicit assignment flow for vehicle and driver together on planned transports.
- Implement transport lifecycle transitions with terminal-state validation.
- Implement shipment-event creation and chronological event history with actor traceability.
- Implement first-admin bootstrap, password hashing, login, JWT issuance, and endpoint protection.
- Implement admin-only user-management flows for list, detail, create, role change, and activate/deactivate, including last-active-admin protection.
- Stabilize the local Docker-based MVP path, migrations, Postman/manual verification path, and CI build/test baseline.
- Define the AWS target architecture, naming rules, tagging rules, environment conventions, and configuration conventions used by all cloud work.

**Artifacts That Must Exist By Sprint End**

- A locally runnable MVP that covers the full core flow: bootstrap admin, login, create resources, assign, transition, and record events.
- Verification coverage for the critical CRUD, auth, assignment, lifecycle, and user-admin paths, either automated or clearly repeatable.
- A stable local Docker startup path with migrations behavior documented.
- A working CI baseline for restore, build, and test.
- An AWS architecture note that fixes the target topology: ALB, ECS Fargate, RDS PostgreSQL, ECR, CloudWatch, Route53/ACM, and secrets handling.
- A shared naming/tagging/configuration convention document that Terraform and GitHub Actions will reuse.

**Definition of Done**

- The local MVP is functionally complete for the scope defined in `docs/Requirements.md`.
- Protected endpoints enforce the intended role model.
- The project can be started locally without hidden manual fixes.
- Cloud implementation can begin without unresolved architectural ambiguity.

**What Must Not Remain Open At Sprint End**

- Missing vehicle or driver CRUD behavior.
- Missing auth bootstrap/login path.
- Missing assignment or lifecycle flow.
- Missing admin user-management baseline.
- Missing transport filters/pagination needed for demo use.
- Unclear AWS target topology or unclear environment/naming conventions.

## Sprint 2 · 06 Apr to 12 Apr

**Phase**
Build the Terraform and AWS foundation layer.

**Mandatory Scope**

- Create the Terraform repository structure, provider/version pinning, shared locals, variables, outputs, and environment layout.
- Implement the remote Terraform state strategy with S3 backend, encryption, versioning, and DynamoDB state locking, plus bootstrap documentation.
- Implement VPC, subnet layout across at least two availability zones, route tables, internet ingress path, and the selected egress strategy.
- Implement mandatory AWS tags, naming locals, and environment-specific conventions in Terraform.
- Implement least-privilege security groups for ALB, ECS, and RDS.

**Artifacts That Must Exist By Sprint End**

- A Terraform codebase with a clean structure ready for at least one deployable environment.
- A documented remote-state bootstrap path.
- A VPC design encoded in Terraform with public and private subnets.
- A routing model and security-group model that match the intended ALB/ECS/RDS topology.

**Definition of Done**

- `terraform fmt` and `terraform validate` pass for the foundation layer.
- The network design supports public ingress only through the ALB and private connectivity for ECS and RDS.
- The Terraform baseline is reusable and not built around one-off local assumptions.

**What Must Not Remain Open At Sprint End**

- Missing state backend strategy.
- Missing subnet layout.
- Missing route model.
- Missing least-privilege security groups.
- Missing naming/tagging conventions inside Terraform.

## Sprint 3 · 13 Apr to 19 Apr

**Phase**
Build the AWS runtime layer.

**Mandatory Scope**

- Implement ECR repository, image retention policy, and image scanning.
- Implement CloudWatch log groups and retention configuration.
- Implement RDS PostgreSQL with subnet group, parameter group, backup posture, and deletion-protection policy appropriate to the environment.
- Implement ECS cluster, IAM execution/runtime roles, task definition, ECS service settings, and health-check wiring.
- Implement ALB, target group, listener configuration, Route53 record, and ACM certificate for reachable HTTPS ingress.
- Implement Secrets Manager and/or Parameter Store integration for runtime secrets and app configuration.

**Artifacts That Must Exist By Sprint End**

- ECR path ready to receive application images.
- RDS baseline ready to host the application database.
- ECS cluster and service-ready task definition.
- HTTPS ingress path defined in Terraform from DNS and certificate through ALB to the service.
- Runtime secret/configuration contract wired into the AWS runtime definition.

**Definition of Done**

- The AWS platform is defined end to end from HTTPS entry point to database connectivity.
- No runtime secret needs to be committed in source control.
- The infrastructure definition contains all core runtime layers needed to host the API.

**What Must Not Remain Open At Sprint End**

- Missing ECR or image strategy.
- Missing database layer.
- Missing ECS runtime/service definition.
- Missing HTTPS ingress path.
- Missing secret/config strategy.

## Sprint 4 · 20 Apr to 26 Apr

**Phase**
Build the cloud delivery and bootstrap path.

**Mandatory Scope**

- Define and implement the AWS migration strategy for EF Core.
- Define and document the cloud-safe first-admin bootstrap strategy.
- Create GitHub Actions workflows for build/test, Terraform validate/plan, image build/push, and deploy.
- Implement GitHub OIDC trust so CI/CD can authenticate to AWS without depending on long-lived static keys.
- Execute at least one real Terraform plan and one real infrastructure apply for the dev environment.

**Artifacts That Must Exist By Sprint End**

- Executable migration path for the AWS environment.
- Cloud bootstrap-admin procedure.
- GitHub Actions workflows for CI and CD.
- OIDC-based AWS access for the intended CI/CD path.
- Evidence of at least one real `plan` and one real `apply` against the target environment or a narrowly explained blocker limited to account/credential availability.

**Definition of Done**

- The repository contains one coherent code-to-cloud path rather than disconnected cloud artifacts.
- CI/CD can build, validate, publish, and deploy through the intended automation model.
- Long-lived AWS credentials are not required for the intended CI/CD path unless explicitly justified.

**What Must Not Remain Open At Sprint End**

- Missing migration strategy.
- Missing bootstrap-admin strategy for AWS.
- Missing CI workflows.
- Missing CD workflow.
- Missing OIDC-based AWS access unless a documented external blocker exists.
- Missing first real plan/apply evidence.

## Sprint 5 · 27 Apr to 03 May

**Phase**
Execute the first real AWS deployment.

**Mandatory Scope**

- Execute the first real end-to-end deployment to AWS through the intended path.
- Run smoke tests against the deployed environment covering health, login, and at least one full business flow.
- Fix the highest-priority defects found during the first real deployment.

**Artifacts That Must Exist By Sprint End**

- A deployed AWS environment reachable through the intended HTTPS entry point.
- Smoke-test results for health, auth, and one operational flow.
- A first defect-fix pass after contact with the real environment.

**Definition of Done**

- The API is actually running on AWS.
- At least one real business flow works in the deployed environment.
- The most severe deployment blockers found in the first release are closed or narrowly documented.

**What Must Not Remain Open At Sprint End**

- No real AWS deployment.
- No smoke-test evidence.
- No validated business flow in AWS.
- No first deployment defect-fix pass.

## Sprint 6 · 04 May to 10 May

**Phase**
Add observability and deployment hardening.

**Mandatory Scope**

- Add structured logging and request correlation usable inside CloudWatch.
- Build the minimum CloudWatch observability layer: metrics, dashboard, and alarms for ALB, ECS, API, and RDS.
- Tune deployment behavior, health checks, and service settings based on lessons from the first real deployment.

**Artifacts That Must Exist By Sprint End**

- Structured CloudWatch logs with request correlation.
- One dashboard that exposes the main health signals of the deployed system.
- One alarm set covering the most important runtime failure signals.
- Updated deployment and health-check settings after the first real deployment.

**Definition of Done**

- The system is observable enough to debug and explain runtime behavior.
- The deployment path is more stable than immediately after the first release.
- The minimum operational telemetry surface is available in AWS.

**What Must Not Remain Open At Sprint End**

- No useful logs in CloudWatch.
- No request correlation.
- No dashboard.
- No alarms.
- No post-deployment hardening of health/deployment settings.

## Sprint 7 · 11 May to 17 May

**Phase**
Close rollback, restore, runtime tuning, and platform reliability.

**Mandatory Scope**

- Define and verify the rollback path for failed releases.
- Define and test the backup/restore story for RDS.
- Harden the container image and review runtime dependencies.
- Tune database/runtime settings based on deployment reality, including relevant connection, timeout, and startup assumptions.

**Artifacts That Must Exist By Sprint End**

- Rollback runbook and verified rollback steps.
- Backup/restore note with explicit recovery assumptions.
- Hardened application container.
- Runtime tuning note covering the most important deployment-era adjustments.

**Definition of Done**

- The project has credible answers to rollback, restore, and runtime-failure questions.
- The runtime image is leaner and the deployed behavior is more predictable than in the first release.

**What Must Not Remain Open At Sprint End**

- No rollback procedure.
- No restore story.
- No container hardening pass.
- No runtime tuning based on actual cloud behavior.

## Sprint 8 · 18 May to 24 May

**Phase**
Close security, cost, recreate-from-scratch, and runbooks.

**Mandatory Scope**

- Review IAM scope, security-group exposure, TLS exposure, and secrets access.
- Review cost footprint and tagging quality.
- Document the recreate-from-scratch path for the dev environment.
- Write runbooks for deploy, rollback, restart, bootstrap-admin, and initial incident response.
- Update architecture, deployment, and CI/CD diagrams to match reality.

**Artifacts That Must Exist By Sprint End**

- Security review note and fixes for the highest-value issues found.
- Cost review note.
- Recreate-from-scratch procedure.
- Final operational runbooks.
- Updated architecture, deployment, and CI/CD diagrams.

**Definition of Done**

- The platform is defensible from security, cost, and operability angles.
- Another developer has enough documentation to understand, operate, and recreate the environment.

**What Must Not Remain Open At Sprint End**

- No security review.
- No cost review.
- No recreate-from-scratch procedure.
- No runbooks.
- No final diagrams matching the deployed system.

## Sprint 9 · 25 May to 31 May

**Phase**
Close evidence, traceability, rehearsal, and final hardening.

**Mandatory Scope**

- Map the delivered system back to `docs/Requirements.md`.
- Capture evidence from workflows, logs, dashboards, Terraform outputs, and AWS resources.
- Rehearse the technical narrative and final verification path.
- Use the remaining time only for the highest-value unresolved issues.

**Artifacts That Must Exist By Sprint End**

- Requirements traceability note.
- Organized evidence set for demo or defense.
- Final verification and rehearsal record.
- Final hardening pass on the remaining highest-risk issues.

**Definition of Done**

- The repository is ready for review, demo, or defense without major undocumented gaps.
- The cloud platform, CI/CD path, observability, rollback, restore, and operational model are explainable with evidence.

**What Must Not Remain Open At Sprint End**

- No traceability note.
- No evidence package.
- No final rehearsal record.
- No final hardening pass on the remaining highest-risk issues.

## Sprint Review Checklist

| # | Review Point | What Must Be True |
| --- | --- | --- |
| 1 | One-week cadence | Every sprint still spans exactly one calendar week. |
| 2 | Phase clarity | Each sprint still has one dominant phase and bounded mandatory scope. |
| 3 | Completion discipline | Each sprint still states clearly what must be fully done by sprint close. |
| 4 | Cloud-first discipline | Application work stays minimal and exists only to support a stronger cloud platform story. |
| 5 | Delivery realism | Terraform and GitHub Actions artifacts keep moving toward a real AWS deployment, not a placeholder architecture. |
| 6 | Operability | Observability, rollback, restore, runbooks, and recreate-from-scratch remain part of the committed scope, not optional polish. |
| 7 | Documentation accuracy | `README.md`, `docs/Requirements.md`, `docs/Roadmap.md`, and `CONTEXT.md` stay aligned with the real system. |

## Target End State

By May 31, 2026, TransitOps should present a deliberately small transport-management backend whose strongest qualities are cloud-engineering quality rather than endpoint count: reproducible local execution, Terraform-managed AWS infrastructure, HTTPS exposure through ALB and Route53/ACM, secure runtime configuration, CI/CD with OIDC-based deployment, a real ECS/RDS deployment path, usable observability, rollback and restore procedures, documented operational runbooks, and enough business functionality to demonstrate the system end to end.
