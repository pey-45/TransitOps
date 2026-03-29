# TransitOps · Daily Delivery Roadmap

## Purpose

Translate the requirements into a daily plan with consistent workload. Each day is intentionally sized as one coherent slice of approximately one hour, not as a single tiny endpoint task.

## Planning Rules

- Dates through March 29, 2026 are treated as completed historical baseline.
- The target daily load is about `45-75 minutes`.
- A daily slice should normally include implementation or configuration, a quick verification pass, and a small documentation/context update if something material changed.
- If a task cannot reasonably fit into about one hour, it should be split before execution.
- The scope stays small so cloud deployment happens early enough and the rest of the time goes to verification, operations, and delivery quality.

## Estimated Remaining Effort

- Estimated remaining effort from March 30 onward: about `44 hours`.
- Planned end date for the current roadmap: `May 12, 2026`.
- This is still shorter than the earlier longer plans, but now includes explicit user-administration work that the stronger requirements specification demands.

## Historical Baseline Already Completed

| Date | Status | Focus | Result Already Present |
| --- | --- | --- | --- |
| 24 Mar | Completed | Scope, repository, and initial documentation | Repository structure, README, and initial planning baseline exist. |
| 25 Mar | Completed | Domain and PostgreSQL schema | Main entities, transport states, and initial schema script are already defined. |
| 26 Mar | Completed | API bootstrap and common contract | The API starts, common response/error structure exists, and controllers are in place. |
| 27 Mar | Completed | EF Core persistence baseline | `TransitOpsDbContext`, entity configurations, baseline migration, and DB-backed readiness are implemented. |
| 28 Mar | Completed | Transport read slice | `GET /transports` and `GET /transports/{id}` now read active rows from PostgreSQL and return coherent `404` behavior. |
| 29 Mar | Completed | Transport write slice | Transport create and update now persist through EF Core with validation and duplicate-reference checks. |

## Phase 1 · Close the Local MVP Core

| Date | Focus | Requirements | Specific Work | Exit Criterion |
| --- | --- | --- | --- | --- |
| 30 Mar | Transport close slice | FR-06, FR-13, FR-14 | Implement logical delete and polish transport validation or mapping gaps found while closing CRUD. | Transport CRUD behaves coherently end to end. |
| 31 Mar | Vehicle read slice | FR-07, FR-13, FR-14 | Implement vehicle list and detail reads with active-row filtering. | Vehicle reads work from PostgreSQL. |
| 01 Apr | Vehicle write slice | FR-07, FR-13, FR-14 | Implement vehicle create and update with basic constraints. | Vehicle create and update are operational. |
| 02 Apr | Vehicle close slice | FR-07, FR-13, FR-14 | Implement logical delete and close the main uniqueness or active-state gaps for vehicles. | Vehicle CRUD behaves coherently end to end. |
| 03 Apr | Driver read slice | FR-08, FR-13, FR-14 | Implement driver list and detail reads with active-row filtering. | Driver reads work from PostgreSQL. |
| 04 Apr | Driver write slice | FR-08, FR-13, FR-14 | Implement driver create and update with the minimum constraints needed by the MVP. | Driver create and update are operational. |
| 05 Apr | Driver close slice | FR-08, FR-13, FR-14 | Implement logical delete and close the main uniqueness or active-state gaps for drivers. | Driver CRUD behaves coherently end to end. |
| 06 Apr | Assignment rules | FR-09, FR-13 | Implement the core assignment preconditions between transport, vehicle, and driver. | Assignment rules are explicit in code. |
| 07 Apr | Assignment API flow | FR-09, FR-13 | Implement the API flow that assigns a vehicle and driver to a planned transport. | Valid assignments work through the API. |
| 08 Apr | Lifecycle transition API | FR-10, FR-13 | Expose transport state transitions through an explicit API flow. | The API can move transport state through valid transitions. |
| 09 Apr | Lifecycle verification | FR-10, FR-13 | Add tests and business error handling for invalid transitions and terminal states. | Lifecycle rules are verified and clearly rejected when invalid. |
| 10 Apr | Shipment event create | FR-11, FR-13, FR-14 | Implement shipment event registration tied to a valid transport and the authenticated actor. | Events can be recorded correctly. |
| 11 Apr | Shipment event history | FR-11, FR-12, FR-14 | Implement chronological shipment event retrieval for a transport. | Event history is returned in the right order. |
| 12 Apr | Transport filters | FR-12, NFR-10 | Add basic state, assignment, and date filters plus minimal pagination for transport listings. | The main listing is usable beyond raw CRUD. |

## Phase 2 · Identity, Security, and Local Release Candidate

| Date | Focus | Requirements | Specific Work | Exit Criterion |
| --- | --- | --- | --- | --- |
| 13 Apr | First admin bootstrap | FR-02, NFR-05 | Implement and document the bootstrap path that creates the first admin user. | The project has a predictable first-admin path. |
| 14 Apr | User administration slice I | FR-03, FR-13, FR-14 | Implement admin-only user list, detail, and create behavior without exposing password hashes. | Admin can inspect and create users. |
| 15 Apr | User administration slice II | FR-03, FR-05, FR-13, FR-14 | Implement role change plus activate/deactivate behavior, including protection against removing the last active admin. | Admin can safely manage user state and role. |
| 16 Apr | JWT primitives | FR-04, NFR-05 | Add JWT configuration and token generation building blocks. | Authentication primitives are in place. |
| 17 Apr | Login endpoint | FR-04, FR-13 | Implement login and invalid-credential behavior. | Active users can obtain a valid token. |
| 18 Apr | Authorization rules | FR-05, NFR-05 | Protect business endpoints and apply the minimum role access matrix. | Protected routes enforce role-based access correctly. |
| 19 Apr | Validation and error polish | FR-13, NFR-05 | Close the most important validation and error contract gaps left by the CRUD and auth work. | The API contract is more consistent and defensible. |
| 20 Apr | CRUD regression tests | NFR-08 | Add or extend tests for transport, vehicle, and driver CRUD behavior. | Core CRUD flows have regression coverage. |
| 21 Apr | Assignment and lifecycle tests | NFR-08 | Add or extend tests for assignment and state transition behavior. | Critical business rules are covered by tests. |
| 22 Apr | Auth and user-admin tests | NFR-08 | Add tests for login, role enforcement, and user-administration critical paths. | Identity and authorization have minimum automated coverage. |
| 23 Apr | Docker local verification | NFR-03, NFR-04 | Verify the Dockerfile and current API behavior in local container form. | The current API runs correctly in a container. |
| 24 Apr | Startup and migration strategy | NFR-02, NFR-03, NFR-06 | Freeze the local startup, schema bootstrap, and first-user strategy in documented form. | Local bootstrap is explicit and reproducible. |
| 25 Apr | CI baseline | NFR-08, NFR-11 | Create the minimal restore-build-test workflow. | The repository has a working CI baseline. |

## Phase 3 · Reach the First Cloud Deployment

| Date | Focus | Requirements | Specific Work | Exit Criterion |
| --- | --- | --- | --- | --- |
| 26 Apr | Terraform skeleton | NFR-04, NFR-11 | Create the Terraform layout, providers, variables, and outputs baseline. | Infrastructure as code has a usable starting structure. |
| 27 Apr | VPC and subnets | NFR-04, NFR-11 | Define the minimal VPC and subnet layout for ECS and RDS. | The network baseline exists. |
| 28 Apr | Security groups and routing | NFR-04, NFR-05, NFR-11 | Define minimum traffic rules and related routing pieces. | The AWS network security baseline exists. |
| 29 Apr | RDS baseline | NFR-04, NFR-06, NFR-11 | Define PostgreSQL on AWS with pragmatic settings for the project scope. | The cloud database layer is defined. |
| 30 Apr | ECR and image path | NFR-04, NFR-11 | Define image registry resources and the image publication path. | Container publication to AWS is structured. |
| 01 May | ECS cluster and task definition | NFR-04, NFR-11 | Define the ECS runtime baseline for the API. | The cloud runtime baseline exists. |
| 02 May | ALB and service wiring | NFR-04, NFR-11 | Add ALB and ECS service integration using the existing health endpoints. | Traffic can reach the service through AWS. |
| 03 May | Runtime config and secrets | NFR-05, NFR-11 | Externalize runtime configuration and secrets for the cloud deployment path. | Cloud configuration is not hardcoded. |
| 04 May | Deploy workflow | NFR-04, NFR-08, NFR-11 | Extend automation to build, publish, and deploy the image in a controlled way. | A repeatable deployment path exists. |
| 05 May | Smoke checks and recovery note | NFR-06, NFR-11 | Verify health, login, and one business flow after deployment, and document the basic recovery path. | The first cloud deployment is validated. |

## Phase 4 · Operability, Documentation, and Closure

| Date | Focus | Requirements | Specific Work | Exit Criterion |
| --- | --- | --- | --- | --- |
| 06 May | Request correlation and logs | NFR-09 | Add request correlation and make logs useful enough for debugging. | Logs are operationally usable. |
| 07 May | Metrics and dashboard | NFR-09 | Freeze the minimum metrics set and build a small CloudWatch dashboard. | Runtime visibility is good enough for the project. |
| 08 May | Alarms and IAM review | NFR-05, NFR-09 | Add a small alarm set and review IAM or secret handling for obvious gaps. | Basic operational and security guardrails exist. |
| 09 May | Architecture diagrams | NFR-07, NFR-11 | Update the architecture and deployment diagrams to match the implemented system. | Visual documentation matches reality. |
| 10 May | Requirements traceability | NFR-07 | Map the implemented system back to `docs/Requirements.md` and close scope-story inconsistencies. | Requirements traceability is documented. |
| 11 May | Evidence and repo polish | NFR-07 | Capture evidence and clean repository-facing documentation, links, and setup notes. | The repository is presentable and evidence is organized. |
| 12 May | Demo and final verification | NFR-03, NFR-08, NFR-11 | Prepare the demo path and rerun the checks that matter for final delivery, using any remaining time as closure buffer. | The project is ready for submission or defense. |

## Weekly Review Checklist

| # | Review Point | What Must Be True |
| --- | --- | --- |
| 1 | Even daily load | Each day still fits roughly into one hour. |
| 2 | Clean build | The solution builds without hidden local fixes. |
| 3 | Runnable tests | Existing automated tests pass locally and in CI. |
| 4 | Reproducible startup | Another machine can run the current phase with documented steps. |
| 5 | Requirements traceability | Implemented work still maps back to `docs/Requirements.md`. |
| 6 | Scope discipline | No extra feature work delays deployment-critical items. |

## Target End State

By May 12, 2026, the project should have a small transport backend with a closed local MVP, basic user administration, a first AWS deployment path through Terraform, minimum useful automation and observability, and repository documentation aligned with the delivered system.
