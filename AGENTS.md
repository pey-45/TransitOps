# AGENTS.md

## Purpose

This file stores stable instructions for the coding agent working in this repository.

Use `CONTEXT.md` for evolving project context, current status, recent decisions, and work-in-progress notes.

## Working Agreement

- Treat this repository as a full-stack academic/professional project covering the complete software development lifecycle: requirements, design, backend, frontend, testing, and deployment.
- Prefer pragmatic, maintainable solutions over premature complexity.
- Keep the functional scope intentionally small, but demonstrable end-to-end across backend and frontend, rather than deep in any single layer.
- The project's differentiator is software engineering discipline across the full lifecycle, not infrastructure or cloud depth. Deployment and CI/CD exist, but stay deliberately lightweight.
- Preserve consistency with the documented requirements and roadmap unless the user explicitly changes scope.
- The project direction changed on 2026-06-19 (signed TFG modification request) from an AWS cloud-platform thesis to this full-lifecycle application thesis. Prior cloud/AWS/Terraform work and the previous TFG memoria/presentation are preserved for reference under `archive/cloud-phase/` and are not part of the active project; do not edit or build on top of that folder.

## User Preferences

- Use `AGENTS.md` for stable instructions and preferences.
- Use `CONTEXT.md` to accumulate project context as work progresses.
- Do not treat session memory as the source of truth when repository files can store the same information.
- When starting a new session, read `AGENTS.md` and `CONTEXT.md` before making assumptions.
- Keep explanations direct and technically rigorous.

## Documentation Rules

- Update `CONTEXT.md` when relevant project context changes during the work.
- Do not overload `AGENTS.md` with temporary or rapidly changing notes.
- `docs/Requirements.md` is the canonical requirements baseline.
- `docs/Roadmap.md` is the canonical sprint execution plan and should preserve completed history while replanning pending work.
- Keep `README.md` focused on repository-facing documentation, setup, and high-level project description.
- Keep requirements and roadmap documents aligned with actual project decisions when they materially change.
- `archive/cloud-phase/` holds documentation and code superseded by the direction change. Treat it as historical reference only, never as current state.

## Engineering Rules

- Favor clear folder and module responsibilities, in both the backend (`TransitOps.Api`) and the frontend once it exists.
- Avoid hidden magic and implicit behavior when a straightforward implementation is available.
- Add tests when behavior or business rules justify them, on whichever layer the behavior lives in.
- Prefer validating changes with build or tests when feasible.
- Do not introduce secrets into committed files.

## Project Orientation

- Main backend: ASP.NET Core / .NET 10 (implemented).
- Persistence: PostgreSQL via EF Core (implemented).
- Backend test stack: xUnit (implemented).
- Frontend: React SPA, not yet started, to integrate with the existing REST API.
- Frontend test stack: to be decided when the frontend is scaffolded.
- Operational direction: Docker/Docker Compose for local reproducibility (implemented). Deployment target and CI/CD depth are deliberately lightweight and decided later in the roadmap; do not assume AWS, ECS, or Terraform going forward.
- Current priority: close requirements/design for the new scope, then build the frontend against the existing, already-implemented backend.

## Session Start Checklist

Before substantial work:

1. Read `AGENTS.md`.
2. Read `CONTEXT.md`.
3. Check `README.md` and relevant files only if needed for the current task.
