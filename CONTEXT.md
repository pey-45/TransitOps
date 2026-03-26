# CONTEXT.md

## Purpose

This file stores evolving project context for future sessions.

It should contain the current state of the project, recent decisions, relevant assumptions, and short notes that help resume work without depending on session memory.

## Repository Snapshot

- Project: `TransitOps`
- Reference date: 2026-03-25
- Repository status: early bootstrap phase
- Solution: `TransitOps.slnx`
- Main projects:
  - `TransitOps.Api`
  - `TransitOps.Tests`

## Current Understanding

- The project goal is to build a transport management backend with a deliberately small functional scope and stronger focus on cloud architecture and DevOps practices.
- The current repository already includes the initial .NET solution, a base API, a test project, and planning documentation.
- The functional MVP is not implemented yet; the codebase is still at a setup/bootstrap stage.

## MVP Direction

According to the repository documentation, the MVP is intended to include:

- Transport, vehicle, and driver management
- Assignments between transports, vehicles, and drivers
- Transport lifecycle/state transitions
- Shipment/logistics events
- JWT authentication and basic roles
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
- `docs/MVP-Backlog.md`
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

## Open Notes

- The repository still contains the default starter API/testing artifacts and has not yet been shaped into the intended domain model.
- Future sessions should update this file when meaningful project decisions, architecture changes, or scope adjustments are made.
