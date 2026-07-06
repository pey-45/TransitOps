# CONTEXT.md

## Purpose

This file stores evolving project context for future sessions.

It should contain the current state of the project, recent decisions, relevant assumptions, and short notes that help resume work without depending on session memory.

## Repository Snapshot

- Project: `TransitOps`
- Reference date: 2026-07-06
- Direction: a complete transport-management application covering the full software development lifecycle (requirements, design, backend, frontend, testing, deployment), per the signed TFG modification request (2026-06-19). The project previously centered on an AWS cloud-platform thesis; that direction and all its artifacts are archived, not deleted, under `archive/cloud-phase/` (see `archive/README.md`).
- Repository status: the backend baseline is fully implemented and reusable as-is: transport/vehicle/driver CRUD on PostgreSQL, transport list filters/pagination, explicit assignment and lifecycle transitions, shipment-event creation/history with actor traceability, first-admin bootstrap/login/JWT, admin-only user management, local Docker Compose startup, xUnit integration tests (health, auth, users, transports, vehicles, drivers, shipment events), a Postman/Newman smoke flow, and a GitHub Actions CI workflow. The frontend, the deployment target, and lightweight CI/CD-for-deploy have not started yet.
- Solution: `TransitOps.slnx`
- Main projects:
  - `TransitOps.Api`
  - `TransitOps.Tests`
  - A frontend project (React SPA) will be added under a new top-level folder once the frontend sprints start (see `docs/Roadmap.md`); no frontend code exists yet.

## Current Understanding

- The project goal is now to build and demonstrate a complete transport-management application: backend + frontend + full software development lifecycle discipline. Cloud/DevOps depth is no longer the differentiator.
- The already-implemented backend functional surface (transports, vehicles, drivers, assignments, lifecycle, shipment events, auth, user administration) satisfies the new direction's backend requirement essentially as-is. No backend rework is expected beyond gaps discovered while integrating the frontend.
- Planning is anchored in `docs/Requirements.md` (rewritten for the new direction: the backend FR/NFR set is carried over, and frontend scope is added) and `docs/Roadmap.md` (rewritten as sequential sprints without fixed calendar dates, per the project owner's preference, targeting readiness well before the end-of-September 2026 defense window).
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
- Frontend testing (tooling to be decided when the frontend is scaffolded).
- A simple, accessible deployment target for backend + frontend (decided during the deployment sprint; not a Terraform-managed cloud platform this time).
- Lightweight CI/CD for build and deploy, plus minimal monitoring — not CloudWatch-scale observability.
- Full-lifecycle documentation for the TFG memoria: requirements, design, architecture, implementation, testing, deployment, and results analysis.

## Architecture Direction

- Backend and frontend as two clearly separated projects, integrated over the existing REST API.
- Reuse the existing backend as-is; do not restructure it without a concrete reason surfaced by frontend integration.
- Strong emphasis on reproducibility, testing, and full-lifecycle documentation, since the thesis now defends the development process end-to-end rather than cloud depth.

## Source Documents

- `README.md`
- `docs/Requirements.md`
- `docs/Roadmap.md`
- `docs/LocalVerification.md`
- `archive/README.md` (index of everything superseded by the direction change)

## Working Convention

- `AGENTS.md` contains stable agent instructions and user preferences.
- `CONTEXT.md` should be updated as work progresses and decisions are made.
- If a decision is temporary, uncertain, or likely to change soon, record it here instead of in `AGENTS.md`.

## Recent Decision Log

- 2026-06-19: TFG modification request signed and approved. Direction changes from "Design, deployment, and operation of a cloud transport-management platform on AWS via infrastructure as code and DevOps practices" to "Design and development of a transport-management application: complete software development lifecycle" (backend + frontend + full SDLC), keeping the same director and the same transport-management domain.
- 2026-07-06: Archived all cloud/AWS/Terraform-specific content under `archive/cloud-phase/` using `git mv` (history preserved, nothing deleted): the root `README.md`/`CONTEXT.md`/`AGENTS.md`, `docs/CloudArchitecture.md`, `docs/CloudDeployment.md`, `docs/CloudOperations.md`, `docs/CloudReliability.md`, `docs/Sprint2TerraformFoundationExplanation.md`, `docs/Requirements.md`, `docs/Roadmap.md`, `docs/RequirementsTraceability.md`, `docs/FinalEvidence.md`, `docs/FinalVerification.md`, `docs/plan_presentacion.md`, `infra/terraform/`, `scripts/cloud/`, the three cloud-specific GitHub Actions workflows (`deploy-dev.yml`, `rollback-dev.yml`, `terraform-dev.yml`), and `tfg/memoria` + `tfg/presentacion`.
- 2026-07-06: Regenerated `README.md`, `CONTEXT.md`, `AGENTS.md`, `docs/Requirements.md`, and `docs/Roadmap.md` for the new direction: reused the existing backend functional requirements, business rules, and repository conventions verbatim where still accurate, and rewrote scope, target stack, and roadmap framing around the full-lifecycle application with a frontend, targeting the end-of-September 2026 defense window without committing to fixed per-sprint calendar dates.

## Open Notes

- The existing backend endpoints, domain model, and tests are unaffected by the direction change and remain the implementation baseline for FR-01 through FR-14 in the new `docs/Requirements.md`.
- No frontend code exists yet; the frontend project name/location, build tooling, and test stack are open decisions for the first frontend sprint.
- Deployment target is intentionally undecided; do not assume AWS, ECS, or Terraform going forward.
- Full history of the cloud-phase direction (detailed day-by-day decision log, AWS account details, Terraform decisions) remains available at `archive/cloud-phase/CONTEXT.md` for reference, if ever needed.
- Future sessions should update this file when meaningful project decisions, architecture changes, or scope adjustments are made.
