# CONTEXT.md

## Purpose

This file stores evolving project context for future sessions.

It should contain the current state of the project, recent decisions, relevant assumptions, and short notes that help resume work without depending on session memory.

## Repository Snapshot

- Project: `TransitOps`
- Reference date: 2026-03-28
- Repository status: local backend baseline established, planning artifacts restructured, CRUD/auth/cloud rollout still pending
- Solution: `TransitOps.slnx`
- Main projects:
  - `TransitOps.Api`
  - `TransitOps.Tests`

## Current Understanding

- The project goal is to build a transport management backend with a deliberately small functional scope and stronger focus on cloud architecture and DevOps practices.
- The current repository already includes the API bootstrap, EF Core migrations-managed PostgreSQL setup, local Docker composition, and planning documentation.
- The API now includes EF Core PostgreSQL persistence wiring with a `TransitOpsDbContext`, entity configurations, and a first baseline migration under `TransitOps.Api/Infrastructure/Persistence/Migrations`, which is now the canonical schema source.
- The API readiness endpoint now validates real PostgreSQL connectivity through `TransitOpsDbContext.Database.CanConnectAsync()`.
- The functional MVP is not implemented yet, but the API surface, persistence layer, and simplified internal folder structure now support the next CRUD and command/query steps without reworking the baseline.
- Planning is now anchored in `docs/Requirements.md` for scope and acceptance, and `docs/Roadmap.md` for daily execution from the real repository state as of March 28, 2026.
- The project still follows a local-MVP-first sequence, but the planning emphasis now makes the cloud rollout start immediately after the minimum usable business flows are closed.
- The requirements specification now explicitly defines user management, role permissions for `admin` and `operator`, main operational flows, and stronger acceptance detail.
- The roadmap is now structured around coherent slices of roughly one hour of work per day, avoiding endpoint-sized tasks that are too small to represent a real day of work.
- The current planning target end date is 2026-05-12, which is still shorter than the earlier longer plans while now covering explicit user-administration work.

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

These are planned later and should not distort near-term implementation priorities:

- AWS infrastructure
- Terraform deployment
- ECS/ECR/RDS rollout
- CloudWatch dashboards and alarms
- Full CI/CD deployment automation
- Advanced observability/resilience features
- Frontend

## Architecture Direction

- Backend-first, modular and maintainable
- Local MVP first, cloud phase second
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
- 2026-03-28: Refactored the transport read slice so `TransportsController` delegates to `Application/Queries/Transports/ITransportQueries`, implemented in `Infrastructure/Queries/Transports/TransportQueries`, keeping the controller as a thin HTTP entry point while still using EF Core-backed query projection.
- 2026-03-28: Added `TransitOps.Api/TransitOps.Api.postman_collection.json` with all current API endpoints for manual testing, including placeholders and the implemented health/transport reads.
- 2026-03-28: Added integration tests for the implemented health and transport read endpoints using `WebApplicationFactory` and an in-memory EF Core test database.
- 2026-03-28: Moved PostgreSQL sample-data scripts to `scripts/postgres/manual/` and retired `database/` as a schema source so `TransitOps.Api/Infrastructure/Persistence/Migrations` is the only canonical definition of the database schema.
- 2026-03-28: Updated `docker-compose.yml` and API startup so the local Docker flow uses a fresh PostgreSQL volume and applies EF Core migrations on startup when `Database:ApplyMigrationsOnStartup` is enabled.
- 2026-03-28: Moved `Persistence` under `Infrastructure` so EF Core persistence, migrations, and database-facing queries live under the same infrastructure layer.

## Open Notes

- Persistence is now wired at infrastructure level. Transport list/detail reads are database-backed, but the remaining CRUD/query flows for transports, vehicles, drivers, shipment events, and users are still pending.
- Legacy local PostgreSQL volumes created from the retired SQL-bootstrap flow should be reset before using the new migrations-managed Docker startup.
- `GET /api/v1/health/ready` already confirms whether the API can connect to PostgreSQL in the current environment.
- Roadmap entries through 2026-03-27 should be treated as completed history, while remaining dates in `docs/Roadmap.md` are the actionable one-hour-per-day plan.
- Future sessions should update this file when meaningful project decisions, architecture changes, or scope adjustments are made.
