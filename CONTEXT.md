# CONTEXT.md

## Purpose

This file stores evolving project context for future sessions.

It should contain the current state of the project, recent decisions, relevant assumptions, and short notes that help resume work without depending on session memory.

## Repository Snapshot

- Project: `TransitOps`
- Reference date: 2026-04-02
- Repository status: local backend baseline established, transport/vehicle/driver CRUD now operational on PostgreSQL with numeric enum persistence, transport list filters/pagination available for demo use, explicit transport assignment available on planned transports, and the delivery roadmap organized into one-week sprints with the local MVP compressed into Sprint 1
- Solution: `TransitOps.slnx`
- Main projects:
  - `TransitOps.Api`
  - `TransitOps.Tests`

## Current Understanding

- The project goal is to build a transport management backend with a deliberately small functional scope and stronger focus on cloud architecture and DevOps practices.
- The current repository already includes the API bootstrap, EF Core migrations-managed PostgreSQL setup, local Docker composition, and planning documentation.
- The API now includes EF Core PostgreSQL persistence wiring with a `TransitOpsDbContext`, entity configurations, and a first baseline migration under `TransitOps.Api/Infrastructure/Persistence/Migrations`, which is now the canonical schema source.
- Enum-like fields (`transport.status`, `shipment_event.event_type`, `app_user.user_role`) now use `smallint` columns with explicit check constraints instead of native PostgreSQL enums, preserving the .NET enums in code while avoiding provider-specific runtime friction.
- The API readiness endpoint now validates real PostgreSQL connectivity through `TransitOpsDbContext.Database.CanConnectAsync()`.
- The functional MVP is not implemented yet, but the API surface, persistence layer, and simplified internal folder structure now support the next CRUD and command/query steps without reworking the baseline.
- Transport create, update, logical delete, filtered listing, basic pagination, and explicit vehicle+driver assignment are now implemented on top of `TransitOpsDbContext`, using explicit validation and active-reference conflict checks, so the transport slice has coherent list/detail/create/update/delete/assign behavior against PostgreSQL.
- Vehicle and driver CRUD are now implemented on top of `TransitOpsDbContext`, including explicit validation, active-row conflict checks on business identifiers, and logical delete behavior consistent with transport soft deletion.
- Planning is now anchored in `docs/Requirements.md` for scope and acceptance, and `docs/Roadmap.md` for sprint execution from the real repository state as of March 29, 2026.
- The roadmap now uses one-week sprints instead of day-by-day planning, with each sprint defining a dominant phase, mandatory scope, explicit artifacts, and a strict definition of done.
- The requirements specification now explicitly defines user management, role permissions for `admin` and `operator`, main operational flows, and stronger acceptance detail.
- The roadmap is now structured from March 30, 2026 through May 31, 2026 as weekly sprints, with Sprint 1 intentionally compressing the full local-MVP closure plus cloud design baseline so the rest of the calendar can focus on AWS platform, delivery, observability, reliability, security, and final defense quality.
- The current planning target end date is 2026-05-31, with cloud credibility now treated as the main differentiator of the project.

## MVP Direction

According to the repository documentation, the MVP is intended to include:

- Transport, vehicle, and driver management
- Assignments between transports, vehicles, and drivers
- Transport lifecycle/state transitions
- Shipment/logistics events
- JWT authentication and basic roles
- Basic user bootstrap and admin user management
- PostgreSQL persistence
- Initial tests
- Local Docker-based reproducibility

## Excluded From MVP

These are outside the local functional MVP, but they are central to the overall project delivery and now receive dedicated planning time as soon as the minimum backend surface is stable enough to deploy:

- AWS infrastructure
- Terraform deployment
- ECS/ECR/RDS rollout
- CloudWatch dashboards and alarms
- Full CI/CD deployment automation
- Advanced observability/resilience features
- Frontend

## Architecture Direction

- Backend-first, modular and maintainable
- Minimum local MVP first, then immediate cloud-first implementation and hardening
- Strong emphasis on reproducibility, testing, and operational clarity
- Documentation quality matters because the project is also intended to be defensible in an academic context

## Source Documents

- `README.md`
- `docs/Requirements.md`
- `docs/Roadmap.md`

## Working Convention

- `AGENTS.md` contains stable agent instructions and user preferences.
- `CONTEXT.md` should be updated as work progresses and decisions are made.
- If a decision is temporary, uncertain, or likely to change soon, record it here instead of in `AGENTS.md`.

## Recent Decision Log

- 2026-03-25: Established repository convention to use `AGENTS.md` for stable agent instructions/preferences and `CONTEXT.md` for evolving project context.
- 2026-03-25: Added initial PostgreSQL schema script under `database/postgres/` to keep database artifacts outside the API project code and aligned with the documented stack.
- 2026-03-26: Updated `database/postgres/001_initial_schema.sql` to use partial unique indexes on active rows (`deleted_at IS NULL`) so business keys can be reused after soft delete.
- 2026-03-26: Hardened `database/postgres/001_initial_schema.sql` with transactional execution and idempotent named-constraint guards so reruns are safer after partial setup attempts.
- 2026-03-26: Added `docker-compose.yml` to run the API and PostgreSQL together locally, with the initial schema mounted into `/docker-entrypoint-initdb.d/` for first-time database initialization.
- 2026-03-26: Replaced the template weather endpoint with versioned transport/vehicle/driver/shipment-event controllers, a common API response envelope, and initial domain contracts inside `TransitOps.Api`.
- 2026-03-26: Simplified the solution to a KISS structure with only `TransitOps.Api` and `TransitOps.Tests`, removing bootstrap service abstractions and keeping only minimal internal folders (`Controllers`, `Contracts`, `Domain`, `Common`, `Middleware`, `Errors`).
- 2026-03-26: Integrated EF Core with PostgreSQL inside `TransitOps.Api/Infrastructure/Persistence`, added `TransitOpsDbContext`, entity configurations, a design-time factory, and the baseline `InitialCreate` migration aligned with the current schema, including partial unique indexes and `set_updated_at` triggers.
- 2026-03-26: Updated `api/v1/health/ready` to check PostgreSQL connectivity through `TransitOpsDbContext.Database.CanConnectAsync()` and return `503 Service Unavailable` when the database is unreachable.
- 2026-03-28: Replaced backlog-based planning with `docs/Requirements.md` as the formal scope and acceptance baseline, and `docs/Roadmap.md` as the daily execution plan, preserving March 24-27 as completed historical baseline.
- 2026-03-28: Expanded `docs/Requirements.md` with explicit user-management requirements, admin/operator permission boundaries, main flows, and stronger functional detail.
- 2026-03-28: Replanned `docs/Roadmap.md` again so each day represents a coherent slice of roughly one hour, incorporates the new user-administration work, and adjusts the target end date to 2026-05-12.
- 2026-03-28: Implemented the March 28 transport read slice: `GET /api/v1/transports` and `GET /api/v1/transports/{id}` now query PostgreSQL through `TransitOpsDbContext`, filter soft-deleted rows, and return real data or `404` as appropriate.
- 2026-03-28: Refactored the transport read slice so `TransportsController` delegates to a transport-specific application service, keeping the controller as a thin HTTP entry point while still using EF Core-backed projection for the transport read model.
- 2026-03-28: Added `TransitOps.Api/TransitOps.Api.postman_collection.json` with all current API endpoints for manual testing, including placeholders and the implemented health/transport reads.
- 2026-03-28: Added integration tests for the implemented health and transport read endpoints using `WebApplicationFactory` and an in-memory EF Core test database.
- 2026-03-28: Moved PostgreSQL sample-data scripts to `scripts/postgres/manual/` and retired `database/` as a schema source so `TransitOps.Api/Infrastructure/Persistence/Migrations` is the only canonical definition of the database schema.
- 2026-03-28: Updated `docker-compose.yml` and API startup so the local Docker flow uses a fresh PostgreSQL volume and applies EF Core migrations on startup when `Database:ApplyMigrationsOnStartup` is enabled.
- 2026-03-28: Moved `Persistence` under `Infrastructure` so EF Core persistence, migrations, and database-facing queries live under the same infrastructure layer.
- 2026-03-29: Implemented the March 29 transport write slice: `POST /api/v1/transports` and `PUT /api/v1/transports/{id}` now persist through `TransitOpsDbContext`, keep new transports in `planned`, validate planned-date ordering, reject duplicate active references with `409 Conflict`, and return `404` for missing or deleted transports.
- 2026-03-29: Added transport write integration tests covering successful create/update plus validation, not-found, and duplicate-reference scenarios, and updated the HTTP/Postman request artifacts to include the new transport write endpoints.
- 2026-03-29: Fixed the remaining Docker API startup error by installing `libgssapi-krb5-2` in the runtime image; this removes the Linux shared-library warning from Npgsql during container startup while keeping the API healthy against PostgreSQL.
- 2026-03-30: Removed the two empty enum-alignment migrations (`SyncEnumMappings` and `AlignPublicEnumModel`) plus their designers, leaving only the baseline migration and the real enum column-type qualification migration in the repository.
- 2026-03-30: Confirmed that with the current `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.1` provider, EF Core still sends `Transport.Status` as an integer on PostgreSQL inserts despite the enum registrations; transport creation therefore intentionally keeps the targeted SQL insert workaround for `status` until the provider/runtime mapping issue is resolved with a validated alternative.
- 2026-03-30: Replaced native PostgreSQL enums with `smallint` columns plus explicit check constraints for `transport.status`, `shipment_event.event_type`, and `app_user.user_role`; the new migration preserves existing rows with `USING CASE`, drops the old enum types after conversion, removes the transport SQL insert workaround, and leaves EF Core using its normal enum-to-number mapping.
- 2026-04-02: Removed the artificial `Application/Commands` vs `Application/Queries` split for the implemented transport slice, consolidating it into a single `ITransportService` and `TransportService` so the current CRUD-oriented codebase is easier to navigate and maintain.
- 2026-04-02: Implemented full vehicle and driver CRUD on top of `TransitOpsDbContext`, plus transport list filters by status/date/vehicle/driver and basic pagination metadata, and added integration tests covering those new slices.
- 2026-04-02: Implemented the explicit transport assignment flow on planned transports, requiring an active, non-deleted vehicle and driver together, and added integration tests for successful assignment, reassignment, validation, inactive resources, and missing-resource cases.
- 2026-04-01: Replanned `docs/Roadmap.md` through 2026-05-31 so the backend functional surface is closed quickly and the majority of remaining time is explicitly allocated to Terraform, AWS deployment, CI/CD, observability, security, runbooks, evidence capture, and final technical defense material.
- 2026-04-02: Refined `docs/Roadmap.md` again so each day has a more even time budget, a deterministic primary artifact, a mandatory verification step, and explicit coverage of cloud-engineering concerns such as remote Terraform state, tagging, Route53/ACM, ECR scanning, GitHub OIDC, rollback, and backup/restore.
- 2026-04-02: Reworked `docs/Roadmap.md` from daily planning into weekly sprints starting on 2026-03-30, each with a dominant phase, bounded scope, deliverables, and exit criteria, because sprint-level planning better fits the project than day-level task slicing.
- 2026-04-02: Reworked `docs/Roadmap.md` again into three equal-duration sprints of three weeks each, compressing the local-MVP work into Sprint 1 and making Sprint 2 and Sprint 3 explicitly cloud-heavy with stronger done criteria for platform, delivery, observability, rollback, restore, security, and evidence.
- 2026-04-02: Reworked `docs/Roadmap.md` again into one-week sprints, explicitly compressing the whole March 30 to April 12 local-MVP block into Sprint 1 because that scope is considered feasible in one week, leaving nearly all remaining sprints cloud-heavy.

## Open Notes

- Persistence is now wired at infrastructure level. Transport, vehicle, and driver list/detail/create/update/logical delete are database-backed, transport listing supports the demo filters/pagination requested in `docs/Requirements.md`, and transport assignment is now available as an explicit flow. Shipment events, users, auth, and lifecycle flows are still pending.
- Legacy local PostgreSQL volumes created from the retired SQL-bootstrap flow should be reset before using the new migrations-managed Docker startup.
- `GET /api/v1/health/ready` already confirms whether the API can connect to PostgreSQL in the current environment.
- Manual sample-data scripts under `scripts/postgres/manual/` are now aligned with the `smallint` enum mapping strategy used by the live schema.
- Roadmap entries through 2026-03-29 should be treated as completed history. From 2026-03-30 onward, the actionable plan now runs as one-week sprints through 2026-05-31 with explicit cloud-first prioritization.
- Future sessions should update this file when meaningful project decisions, architecture changes, or scope adjustments are made.
