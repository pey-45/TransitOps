# CONTEXT.md

## Purpose

This file stores evolving project context for future sessions.

It should contain the current state of the project, recent decisions, relevant assumptions, and short notes that help resume work without depending on session memory.

## Repository Snapshot

- Project: `TransitOps`
- Reference date: 2026-07-31
- Direction: a complete transport-management application covering the full software development lifecycle (requirements, design, backend, frontend, testing, deployment), per the signed TFG modification request (2026-06-19). The project previously centered on an AWS cloud-platform thesis; that direction and all its artifacts are archived, not deleted, under `archive/cloud-phase/` (see `archive/README.md`).
- Repository status: **Sprints 1–6 implemented; RF-01…RF-14 integrated.** The active .NET/React application now covers authentication/bootstrap, catalogs, shipment CRUD and filters, assignment/lifecycle, immutable event traceability, admin-only user management, self-service password change and a period-aware operational summary. PostgreSQL migrations, automated tests, Docker Compose, CI, design documentation and the reoriented LaTeX thesis remain aligned. The previous implementation remains an archived reference oracle only.
- Stack (unchanged from the previous iteration, deliberately): ASP.NET Core (.NET 10) + PostgreSQL/EF Core backend, React SPA frontend.
- Reference implementation: the full previous-iteration .NET solution (`TransitOps.slnx`, `TransitOps.Api`, `TransitOps.Tests`, plus its `docker-compose.yml`, seed/smoke `scripts/`, and `docs/LocalVerification.md`) lives self-contained and still buildable under `archive/cloud-phase/`, to consult while re-implementing.

## Current Understanding

- The project goal is now to build and demonstrate a complete transport-management application: backend + frontend + full software development lifecycle discipline. Cloud/DevOps depth is no longer the differentiator.
- The previous-iteration backend covered most of the same domain, but the project is being rebuilt from scratch (see decision log 2026-07-07) so that each sprint genuinely goes through the full development cycle, which the new thesis defends. The archived code is consulted as a reference, not reused directly.
- Planning is anchored in `docs/ClientRequirements.md` (simulated client interview), `docs/Requirements.md` (formal, non-technical functional requirements RF-01..RF-14 derived from the interview), and `docs/Roadmap.md` (iterative full-cycle sprints S1..S8, vertical feature slices; rewritten 2026-07-07). Target: ready well before the end-of-September 2026 defense window, without committing to fixed per-sprint calendar dates.
- Deployment target and CI/CD-for-deploy depth are intentionally undecided for now. The modification request does not name a cloud provider, and `AGENTS.md` no longer orients decisions toward AWS.
- All prior cloud/AWS/Terraform work (Terraform IaC, AWS validation scripts, `Cloud*.md` docs, cloud-specific GitHub Actions workflows) and the previous TFG memoria/presentation are preserved verbatim under `archive/cloud-phase/`.

## MVP Direction (carried over from the backend baseline, still valid)

- Transport, vehicle, and driver management
- Assignments between transports, vehicles, and drivers
- Transport lifecycle/state transitions
- Shipment/logistics events
- JWT authentication and basic roles
- Basic user bootstrap and admin user management
- PostgreSQL persistence
- Automated backend tests
- Local Docker-based reproducibility

## New Scope Additions

- A React SPA frontend covering login and the main operational flows (transports, vehicles, drivers, assignment, lifecycle, shipment events, and admin-only user management), consuming the existing REST API.
- Frontend testing with Vitest and React Testing Library.
- A simple, accessible deployment target for backend + frontend (decided during the deployment sprint; not a Terraform-managed cloud platform this time).
- Lightweight CI/CD for build and deploy, plus minimal monitoring — not CloudWatch-scale observability.
- Full-lifecycle documentation for the TFG memoria: requirements, design, architecture, implementation, testing, deployment, and results analysis.

## Architecture Direction

- Backend and frontend as two clearly separated projects, integrated over a REST API.
- Rebuild fresh; use the archived previous-iteration code as a reference oracle, not as the base to build on.
- Strong emphasis on reproducibility, testing, and full-lifecycle documentation, since the thesis now defends the development process end-to-end rather than cloud depth.

## Source Documents

- `README.md`
- `docs/ClientRequirements.md`
- `docs/Requirements.md`
- `docs/Roadmap.md`
- `archive/README.md` (index of everything superseded by the direction change, including the reference implementation)

## Working Convention

- `AGENTS.md` contains stable agent instructions and user preferences.
- `CONTEXT.md` should be updated as work progresses and decisions are made.
- If a decision is temporary, uncertain, or likely to change soon, record it here instead of in `AGENTS.md`.

## Recent Decision Log

- 2026-06-19: TFG modification request signed and approved. Direction changes from "Design, deployment, and operation of a cloud transport-management platform on AWS via infrastructure as code and DevOps practices" to "Design and development of a transport-management application: complete software development lifecycle" (backend + frontend + full SDLC), keeping the same director and the same transport-management domain.
- 2026-07-06: Archived all cloud/AWS/Terraform-specific content under `archive/cloud-phase/` using `git mv` (history preserved, nothing deleted): the root `README.md`/`CONTEXT.md`/`AGENTS.md`, `docs/CloudArchitecture.md`, `docs/CloudDeployment.md`, `docs/CloudOperations.md`, `docs/CloudReliability.md`, `docs/Sprint2TerraformFoundationExplanation.md`, `docs/Requirements.md`, `docs/Roadmap.md`, `docs/RequirementsTraceability.md`, `docs/FinalEvidence.md`, `docs/FinalVerification.md`, `docs/plan_presentacion.md`, `infra/terraform/`, `scripts/cloud/`, the three cloud-specific GitHub Actions workflows (`deploy-dev.yml`, `rollback-dev.yml`, `terraform-dev.yml`), and `tfg/memoria` + `tfg/presentacion`.
- 2026-07-06: Regenerated `README.md`, `CONTEXT.md`, `AGENTS.md`, `docs/Requirements.md`, and `docs/Roadmap.md` for the new direction: reused the existing backend functional requirements, business rules, and repository conventions verbatim where still accurate, and rewrote scope, target stack, and roadmap framing around the full-lifecycle application with a frontend, targeting the end-of-September 2026 defense window without committing to fixed per-sprint calendar dates.
- 2026-07-06: Committed the archive move + regenerated base docs (commit `25b3a3b`).
- 2026-07-07: Adopted an explicit requirements methodology at the project owner's request: `docs/ClientRequirements.md` (simulated client interview, plain language) -> `docs/Requirements.md` (formal non-technical functional requirements) -> iterative full-cycle sprints. Wrote `docs/ClientRequirements.md` as an interview with a fictional client (Marta Souto, transport-SME operations manager).
- 2026-07-07: Rewrote `docs/Requirements.md` from scratch, derived from and traceable to the interview: 14 functional requirements (RF-01..RF-14), 16 business rules (RN-01..RN-16), 6 non-functional requirements, at business level with no technical detail. This replaced the earlier (2026-07-06) technically-tinged, non-interview-derived requirements.
- 2026-07-07: After a coherence/sufficiency review, fixed gaps where the requirements did not trace to the interview and added scope: shipment "carga estimada" so vehicle capacity has a real use (capacity warning), an anti-double-booking rule (a vehicle/driver cannot be on two unfinished shipments at once), first-admin bootstrap requirement, removal of invented uniqueness constraints, plus two new features chosen by the owner — customers as a managed entity (RF-07) and self-service password change (RF-03). Updated both the interview and the requirements to keep them consistent.
- 2026-07-07: Decided to **rebuild the application from scratch** rather than reuse the previous-iteration code, to apply the new iterative full-cycle methodology cleanly; keep the same stack (.NET + PostgreSQL, React). Archived the whole previous-iteration implementation as a self-contained reference oracle under `archive/cloud-phase/` via `git mv` (history preserved): `TransitOps.Api`, `TransitOps.Tests`, `TransitOps.slnx`, `docker-compose.yml`, `dotnet-tools.json`, `.dockerignore`, `.env`/`.env.example`, `scripts/database` + `scripts/testing`, `.github/workflows/ci.yml`, and `docs/LocalVerification.md`. The repository root now holds only planning docs and `archive/`.
- 2026-07-07: Documentation-coherence pass before starting the sprints. Added a "Flujos de Negocio" section to `docs/Requirements.md` — four end-to-end scenarios (bootstrap first admin, admin creates operator, operator executes a shipment end-to-end, admin deactivates a user), each traceable to RF/RN; Flujo 3 is the operator shipment-execution scenario the Roadmap references for S4/S7. This resolves the dangling "cuatro flujos de negocio"/"Flujo 3" references in `docs/Roadmap.md`. Also removed the stale "pending rewrite to the iterative methodology" notes about the Roadmap from `README.md` (the Roadmap was already rewritten).
- 2026-07-18: Implemented Sprint 1 greenfield. Chose Vite + React 19 + TypeScript, React Router, Vitest/RTL; ASP.NET Core .NET 10 with EF Core/PostgreSQL and JWT; localStorage session for the S1 skeleton with an explicit XSS tradeoff to review in S7. Added the complete domain design, initial users-only migration, common error contract, controlled first-admin bootstrap, login, role policies, protected SPA, Docker Compose, and CI. Local validation passed 16 backend tests, 4 frontend tests, both production builds, and a containerized bootstrap/login/protected-session flow through Nginx. Added a reoriented UDC-FIC LaTeX thesis structure with real S1 content; PDF compilation remains unverified because XeLaTeX/latexmk is not installed locally.
- 2026-07-19: Prepared the Sprint 1 repository for its first safe push. Made `.env` mandatory for Docker Compose, with a committed `.env.example`, an ignored local `.env`, explicit missing-variable failures, no tracked runtime credentials in `appsettings.json`, and `.env` excluded from the Docker build context. Reorganized backend tests into `Controllers/`, `Services/`, and `Support/`; controller tests cover HTTP contracts and policies, while `AuthServiceTests` covers authentication/bootstrap business behavior directly. CI now runs frontend lint in addition to build and tests. PostgreSQL uses configurable host port `POSTGRES_PORT` (5433 in the example) to avoid collisions with a native PostgreSQL on 5432; EF design-time tooling reads the same environment/`.env` settings, and applying/listing the initial migration was validated against a fresh Compose database.
- 2026-07-19: Implemented Sprint 2 as the first domain vertical slice. Added vehicle, driver and customer entities plus the `AddCatalogTables` migration; active-only CRUD services and authorized REST endpoints; business-key uniqueness for vehicle plate/internal code and driver licence; soft deletion with reuse of identifiers after deactivation; and React list/detail/create/edit/deactivate flows for all three catalogs. Extended the API client with authenticated requests and validation details. Validation passed 29 backend tests, 7 frontend tests, lint, both production builds, and a full-stack Docker flow through Nginx against PostgreSQL real; the migration and preservation of inactive rows were explicitly verified and temporary verification data was removed. Sprint 3 (shipments CRUD and filters) is now the next priority.
- 2026-07-26: Implemented Sprint 3 shipment management. Added `Shipment` and `AddShipments` with globally unique references, restrictive catalog FKs, UTC `timestamptz` handling, cross-field date validation, optional active-customer association, authenticated create/read/update and stable filtered pagination. Added SPA list/detail/form routes, URL-backed filters, local/UTC conversion and inactive-customer-safe editing. Validation passes 45 backend and 13 frontend tests, frontend lint, both production builds, and a Docker/PostgreSQL flow covering `Z`, naive, offset and date-only filter inputs; browser evidence was added to the thesis. Sprint 4 (resource assignment and lifecycle) is now the next priority.
- 2026-07-30: Implemented Sprint 4 shipment operation. Confirmed four design decisions: explicit assignment/status action endpoints; joint (never partial) assignment; capacity insufficiency returned as a transient `capacityWarning` on a successful assignment instead of a two-step confirmation; and automatic UTC sealing of actual pickup/delivery dates, with shipment events deferred wholly to S5. Added `AddShipmentOperation`, RN-02..RN-05/RN-07/RN-08 service rules, complete resource projections, state-aware SPA controls and visual evidence. Validation passes 74 backend and 19 frontend tests, frontend lint, both production builds, and Docker/PostgreSQL/browser flows including capacity warning, double-booking rejection and terminal state.
- 2026-07-31: Implemented Sprint 5 shipment traceability. Events are immutable, distinguish business `OccurredAt` from audit `CreatedAt`, and combine system-created lifecycle events with manually entered checkpoints/incidents. `ICurrentUser` reads the preserved JWT `sub` claim behind a testable abstraction; automatic events share the same `SaveChanges` as the shipment operation. Added `AddShipmentEvents`, nested event API, chronological SPA timeline and event form. Validation passes 96 backend and 24 frontend tests, lint, both production builds, and a Docker/PostgreSQL/browser flow covering a naive timestamp, actor projection, chronological order, mixed timeline, form rendering and a pre-migration shipment with an empty history. All high-priority requirements are now implemented; Sprint 6 is next.
- 2026-07-31: Implemented Sprint 6 administration and indicators without a migration. User management is protected by the real admin policy and preserves RN-12 across deactivation and role changes; inactive users remain addressable for reactivation and credentials are globally unique. `AuthService` now supports self-service password changes through `ICurrentUser`. The operational summary keeps shipment-state counts global while applying a configurable/default 30-day period to resource activity (`PlannedPickupAt`) and incidents (`OccurredAt`). The SPA adds `AdminRoute`, user administration, password form and an operational home dashboard. Validation passes 127 backend and 30 frontend tests, lint, both production builds, and Docker/PostgreSQL/browser flows including date-only aggregate queries, 403 for an operator, last-admin 409, password replacement and direct-route redirection. All RF-01…RF-14 are now implemented; Sprint 7 is next.

## Open Notes

- The archived reference implementation already solved most of the RF domain (login, first-admin bootstrap, user administration, vehicle/driver/transport management, assignment, lifecycle, event history, filters) — useful to consult when rebuilding those. Requirements with **no precedent** in the reference (must be designed anew) include: RF-07 (customers as a managed entity + linking a customer to a shipment), RF-03 (self-service password change), the anti-double-booking rule RN-04, the shipment "carga estimada" field with the capacity warning (RN-05), and RF-14 (statistics summary). There is no UI precedent at all: the reference is backend-only.
- `docs/Roadmap.md` is now the iterative full-cycle plan (S1 foundations + auth walking skeleton; S2 catalogs vehicles/drivers/customers; S3 shipments CRUD + filters; S4 assignment + lifecycle; S5 event history; S6 user admin + password change + statistics; S7 hardening/system-testing/deployment; S8 documentation). The full data model is designed up front in S1; features are then built as vertical slices. Deployment to an accessible environment lands in S7 (skeleton may be deployed earlier).
- The frontend lives in `frontend/` and uses Vite, React 19, TypeScript, React Router, Vitest and React Testing Library.
- The active solution is `TransitOps.slnx`, with `TransitOps.Api/` and `TransitOps.Tests/`; it was created fresh and does not reference archived projects.
- Deployment target is intentionally undecided; do not assume AWS, ECS, or Terraform going forward.
- Full history of the cloud-phase direction (detailed day-by-day decision log, AWS account details, Terraform decisions) remains available at `archive/cloud-phase/CONTEXT.md` for reference, if ever needed.
- Current priority: Sprint 7, covering security/dependency hardening, the four end-to-end system flows, lightweight deployment and minimal operational checks.
- `ICurrentUser` is the shared, testable way for application services to access the authenticated user id from the JWT `sub` claim.
- Sprint 7 hardening must review four known limitations together: JWTs remain valid until expiry after password changes or user deactivation; the SPA stores its token in `localStorage` (XSS tradeoff); simultaneous requests can race both RN-04 resource booking and RN-12 last-admin protection; and role changes do not alter the claims of an already-issued token.
- `npm ci` currently reports two high-severity audit findings in the existing frontend dependency tree. Sprint 4 added no dependencies; inspect and triage the concrete advisories during Sprint 7 hardening before applying any potentially breaking automated fix.
- Future sessions should update this file when meaningful project decisions, architecture changes, or scope adjustments are made.
