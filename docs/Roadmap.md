# TransitOps · Sprint Roadmap

## Purpose

Translate `docs/Requirements.md` into a sequential sprint plan that closes the full software development lifecycle for the application-focused direction: design, backend closure, frontend, deployment, CI/CD, testing, and documentation.

## Planning Model

- Planning uses sequential numbered sprints instead of fixed calendar weeks, at the project owner's request. Each sprint defines mandatory scope, required artifacts, a definition of done, and explicit items that cannot remain open at close — the same discipline as before, just without a pre-assigned date range.
- Actual sprint duration is not pinned in advance; track how long each sprint really took as it closes (a short note per sprint is enough) instead of estimating hours upfront. Replan the remaining sprints if the observed pace makes that necessary.
- Target: the engineering work (Sprints 1-9) finished with comfortable buffer before the end-of-September 2026 defense window, leaving Sprint 10 (documentation and close) and rehearsal time before the deadline rather than up against it.
- Starting reference date: 2026-07-06.
- This roadmap supersedes the previous cloud-platform-oriented roadmap, preserved at `archive/cloud-phase/docs/Roadmap.md`. That roadmap's Sprints 1-9 already delivered and validated a complete backend; this roadmap starts from that baseline and does not redo backend work already implemented.

## Historical Baseline Already Completed

The following is already implemented and verified, carried over from the previous direction without rework:

| Area | Result Already Present |
| --- | --- |
| Backend domain & persistence | PostgreSQL schema via EF Core migrations for `AppUser`/`Transport`/`Vehicle`/`Driver`/`ShipmentEvent` |
| Auth & authorization | JWT auth, first-admin bootstrap, role-based access (`admin`/`operator`) |
| Core CRUD & workflows | Transport/vehicle/driver CRUD, explicit assignment, lifecycle transitions, shipment events, filters/pagination |
| User administration | Admin-only list/detail/create/role-change/activation, last-active-admin protection |
| Local reproducibility | Docker Compose, `.env.example`, migrations-on-startup |
| Automated tests | xUnit integration tests covering health, auth, users, transports, vehicles, drivers, shipment events |
| CI baseline | GitHub Actions restore/build/test, migration drift check, `docker compose config` validation |
| Manual verification | Postman/Newman smoke flow, `.http` file, documented in `docs/LocalVerification.md` |

This baseline satisfies Fase 2 (Backend) from the TFG modification request essentially as delivered. Remaining backend work in this roadmap is limited to gaps surfaced while integrating the frontend, not a rebuild.

## Sprint Cadence

| Sprint | Dominant Phase (per TFG modification request) |
| --- | --- |
| Sprint 1 | Fase 1 — Requirements refresh, architecture note, data model confirmation, UI prototype/wireframes |
| Sprint 2 | Fase 2 — Close backend gaps for frontend-readiness |
| Sprint 3 | Fase 3 — Frontend scaffold, auth flow, protected routing |
| Sprint 4 | Fase 3 — Transport management UI |
| Sprint 5 | Fase 3 — Vehicle & driver management UI |
| Sprint 6 | Fase 3 — Shipment events UI, admin user management UI, UI polish |
| Sprint 7 | Fase 4 — Deployment target decision and setup |
| Sprint 8 | Fase 5 — Lightweight CI/CD for deploy, minimal monitoring |
| Sprint 9 | Fase 6 — End-to-end functional testing, bug fixing |
| Sprint 10 | Fase 7 — Documentation and close: memoria, diagrams, results analysis, defense prep |

Treat this table as a planning baseline, not a fixed contract. Replan it the same way the previous roadmap was replanned when scope or pace made that necessary — update this file rather than tracking the drift only in conversation.

## Sprint 1 · Requirements, Design, and Prototype

**Phase**
Fase 1 — Analysis and design.

**Mandatory Scope**

- Confirm the requirements in `docs/Requirements.md` reflect the actual intended scope (FR-15 through FR-20, revised NFRs).
- Write a short architecture note: how the frontend and backend fit together (SPA calling the existing REST API, where the token is stored, how roles gate navigation).
- Produce UI wireframes/prototype for the main screens: login, transport list/detail, vehicle list, driver list, shipment-event history, user administration.
- Decide the frontend project layout (folder name/location) and base tooling (bundler, routing, HTTP client, styling approach).

**Artifacts That Must Exist By Sprint End**

- Confirmed `docs/Requirements.md` (already drafted in this pass; revisit if scope shifts).
- An architecture note (can live in `docs/` or as a new `docs/Architecture.md`) describing the frontend/backend integration.
- Wireframes or a low-fidelity prototype for the main screens, in whatever format is fastest to produce and iterate on (paper/Figma/hand-drawn, exported into the repo or linked).
- A decided frontend tooling stack, recorded in `CONTEXT.md`.

**Definition of Done**

- Anyone picking up Sprint 3 can start scaffolding the frontend without re-deciding architecture or tooling.
- The requirements/design docs describe the system as it will actually be built, not the previous cloud-first direction.

**What Must Not Remain Open At Sprint End**

- Undecided frontend tooling stack.
- Missing wireframes for any of the main screens listed above.

## Sprint 2 · Backend Frontend-Readiness

**Phase**
Fase 2 — Backend (closing gaps, not rebuilding).

**Mandatory Scope**

- Review the existing API contract from a frontend-consumer point of view: response shapes, error envelope, pagination metadata, and confirm they are workable for UI binding.
- Add or verify CORS configuration so the frontend's local dev server can call the API.
- Add OpenAPI/Swagger (or equivalent) documentation for the API surface, if not already present, so frontend work doesn't require reading controller source to know the contract.
- Fix any small backend gaps discovered during the Sprint 1 design pass (e.g., a missing filter, an inconsistent DTO field) — scoped strictly to what the frontend design actually needs.

**Artifacts That Must Exist By Sprint End**

- CORS configured and verified against a local frontend dev server origin.
- API documentation (OpenAPI/Swagger UI or equivalent) reachable locally.
- Any backend gap fixes, each covered by a test.

**Definition of Done**

- A frontend developer can discover and call every endpoint needed for FR-15 through FR-20 without reading backend source code.
- All existing backend tests still pass; new fixes have test coverage.

**What Must Not Remain Open At Sprint End**

- Missing CORS configuration.
- Undocumented API surface needed by the frontend.

## Sprint 3 · Frontend Scaffold and Authentication

**Phase**
Fase 3 — Frontend.

**Mandatory Scope**

- Scaffold the React SPA project with the tooling decided in Sprint 1.
- Implement the login screen (FR-15): form, call to `POST /api/v1/auth/login`, token storage, error display.
- Implement protected routing: unauthenticated access redirects to login; role is available to gate navigation.
- Implement logout.
- Wire a basic app shell/navigation so subsequent sprints can add screens without re-touching routing/layout.

**Artifacts That Must Exist By Sprint End**

- A runnable frontend project with its own README/start instructions (to be folded into the root `README.md` once stable).
- A working login -> protected shell -> logout loop against the real local backend.

**Definition of Done**

- FR-15 acceptance criteria are met end-to-end against the local backend.
- The frontend project builds and runs locally with documented commands, mirroring the rigor already applied to the backend.

**What Must Not Remain Open At Sprint End**

- Missing FR-15 acceptance criteria (error display, session persistence across reload, clean handling of an expired/invalid token).
- No documented way to start the frontend locally.

## Sprint 4 · Transport Management UI

**Phase**
Fase 3 — Frontend.

**Mandatory Scope**

- Implement the transport list view with the API's filters and pagination (status, planned date range, vehicle, driver).
- Implement the transport detail view, including shipment-event history.
- Implement create/edit forms with client-side validation mirroring FR-06's acceptance criteria.
- Implement the assignment action (FR-09) and lifecycle transition actions (FR-10) from the detail/list view, respecting valid-state constraints in the UI.

**Artifacts That Must Exist By Sprint End**

- FR-16 implemented and integrated against the real backend.

**Definition of Done**

- Flow 3 (`Operator Executes a Transport`) from `docs/Requirements.md` is fully exercisable through the UI, not only through the API.
- API validation/conflict errors surface as readable UI feedback.

**What Must Not Remain Open At Sprint End**

- Any FR-16 acceptance criterion left unmet.

## Sprint 5 · Vehicle and Driver Management UI

**Phase**
Fase 3 — Frontend.

**Mandatory Scope**

- Implement list/detail/create/edit views for vehicles (FR-17).
- Implement list/detail/create/edit views for drivers (FR-18).
- Ensure both surface backend uniqueness conflicts (`409`) as readable UI feedback.

**Artifacts That Must Exist By Sprint End**

- FR-17 and FR-18 implemented and integrated against the real backend.

**Definition of Done**

- A user can fully manage vehicles and drivers from the UI, including the create/edit/soft-delete paths.

**What Must Not Remain Open At Sprint End**

- Any FR-17 or FR-18 acceptance criterion left unmet.

## Sprint 6 · Shipment Events, User Administration, and UI Polish

**Phase**
Fase 3 — Frontend (close-out).

**Mandatory Scope**

- Implement the shipment-event history and creation form on the transport detail view (FR-19).
- Implement the admin-only user administration screens (FR-20), hidden from `operator` sessions.
- Pass over the whole frontend for consistency: shared error handling, loading states, empty states, and basic responsive layout.

**Artifacts That Must Exist By Sprint End**

- FR-19 and FR-20 implemented and integrated against the real backend.
- A frontend that feels coherent end-to-end, not a set of disconnected screens.

**Definition of Done**

- All of FR-15 through FR-20 are met; Gate B (`Frontend MVP Ready` in `docs/Requirements.md`) is closed.
- A non-admin cannot reach the user administration screen even by direct navigation.

**What Must Not Remain Open At Sprint End**

- Any FR-19 or FR-20 acceptance criterion left unmet.
- Inconsistent error/loading handling across screens.

## Sprint 7 · Deployment

**Phase**
Fase 4 — Deployment.

**Mandatory Scope**

- Decide the deployment target (a single host, a PaaS free/low-cost tier, or similar — deliberately not a Terraform-managed multi-environment cloud platform).
- Configure the runtime environment for backend + frontend + PostgreSQL in that target.
- Deploy and verify the application is reachable end-to-end over the internet.
- Document the deployment procedure so it is repeatable, not a one-off manual sequence only the project owner remembers.

**Artifacts That Must Exist By Sprint End**

- A deployed, internet-reachable instance of the application.
- A deployment guide (new `docs/Deployment.md` or equivalent) covering the target, configuration, and redeploy steps.

**Definition of Done**

- NFR-04 (`Deployability to an accessible environment`) is met.
- Someone other than the project owner could redeploy following the documented steps.

**What Must Not Remain Open At Sprint End**

- An undocumented or manual-only deployment path.
- Secrets committed to the repository as part of making deployment work.

## Sprint 8 · Lightweight CI/CD and Observability

**Phase**
Fase 5 — CI/CD and observability.

**Mandatory Scope**

- Extend the existing CI workflow (or add a new one) to build and deploy to the target chosen in Sprint 7, at least for a single environment.
- Add basic structured logging and request correlation if not already sufficient for the deployed environment.
- Add minimal monitoring appropriate for a small demo deployment (e.g., uptime/health check, basic log visibility) — explicitly not CloudWatch-scale dashboards/alarms.

**Artifacts That Must Exist By Sprint End**

- A GitHub Actions workflow that can build and deploy on demand or on push, documented in `docs/Deployment.md`.
- Basic, documented visibility into whether the deployed application is healthy.

**Definition of Done**

- NFR-08 (`Basic observability`) is met.
- A deploy can be triggered without manual, undocumented steps.

**What Must Not Remain Open At Sprint End**

- A deployment process that only works "on the project owner's machine."

## Sprint 9 · End-to-End Testing and Evaluation

**Phase**
Fase 6 — Testing and evaluation.

**Mandatory Scope**

- Run a full functional pass of Flows 1-4 from `docs/Requirements.md` against the deployed environment, frontend included.
- Add or close gaps in frontend automated tests, if the chosen tooling and remaining time allow it.
- Triage and fix bugs found during this pass; re-verify after fixes.

**Artifacts That Must Exist By Sprint End**

- A short test/evaluation report: what was exercised, what was found, what was fixed.

**Definition of Done**

- All four main business flows work end-to-end in the deployed environment.
- No known Must-priority requirement is left broken.

**What Must Not Remain Open At Sprint End**

- Any known-broken Must-priority flow.

## Sprint 10 · Documentation and Close

**Phase**
Fase 7 — Documentation and close.

**Mandatory Scope**

- Write the TFG memoria for the new direction: introduction, objectives, methodology, requirements, architecture, backend implementation, frontend implementation, testing, deployment, results, and conclusions.
- Produce the architecture/data-model/flow diagrams referenced by the memoria.
- Prepare a defense presentation and rehearsal guide for the new title, mirroring the rigor of the previous `archive/cloud-phase/docs/plan_presentacion.md` but for this direction's content.
- Final pass on `README.md`/`CONTEXT.md`/`docs/Requirements.md`/`docs/Roadmap.md` so they describe the delivered system accurately.

**Artifacts That Must Exist By Sprint End**

- A complete, compiled TFG memoria for the new direction.
- A defense presentation and guide.
- Documentation that matches the actually delivered application.

**Definition of Done**

- The project is ready to defend before the end-of-September 2026 window, with rehearsal time still available afterward.

**What Must Not Remain Open At Sprint End**

- Placeholder or TODO content in the memoria.
- Documentation describing planned-but-not-delivered functionality as if it were done.

## Pacing Note

No calendar dates are assigned to these sprints by design. As each sprint closes, add a one-line note here (or in `CONTEXT.md`) with how long it actually took, so the remaining sprints can be resized realistically instead of guessed twice. As a sanity check only: ten sprints finishing at a pace of roughly one week each would land around mid-September, leaving buffer before the end-of-September window for Sprint 10 and rehearsal — if real pace runs slower, compress scope (e.g., merge Sprints 5-6, or narrow FR-19/FR-20 to their acceptance-critical core) rather than letting Sprint 10 get squeezed.
