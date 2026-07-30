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
- The project direction changed on 2026-06-19 (signed TFG modification request) from an AWS cloud-platform thesis to this full-lifecycle application thesis. The prior cloud/AWS/Terraform work, the previous TFG memoria/presentation, and the previous-iteration application code are all preserved under `archive/cloud-phase/`.
- The application is being **rebuilt from scratch** following the new iterative full-lifecycle methodology, on the same stack (.NET + PostgreSQL backend, React frontend). The archived previous-iteration code is a **reference oracle**: consult it (business rules, EF migrations, test cases, decisions already made) while re-implementing, but do not edit it or build the active project directly on top of it.

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
- `archive/cloud-phase/` holds documentation and code superseded by the direction change. Treat it as reference only (a consultable oracle for the rewrite), never as current state and never as something to edit.

## Engineering Rules

- Favor clear folder and module responsibilities, in both the backend and the frontend, once each is scaffolded.
- Avoid hidden magic and implicit behavior when a straightforward implementation is available.
- Add tests when behavior or business rules justify them, on whichever layer the behavior lives in.
- Prefer validating changes with build or tests when feasible.
- Do not introduce secrets into committed files.
- Local runtime configuration is sourced from the ignored root `.env`, created from the committed `.env.example`; keep real credentials out of `appsettings*.json` and other tracked files.

- Main backend: active ASP.NET Core / .NET 10 project in `TransitOps.Api/`; a working previous-iteration backend exists in `archive/cloud-phase/` as reference only.
- Persistence: PostgreSQL via EF Core.
- Backend test stack: xUnit.
- Frontend: active Vite + React + TypeScript SPA in `frontend/`, integrated with the backend REST API.
- Frontend test stack: Vitest + React Testing Library.
- Operational direction: Docker/Docker Compose for local reproducibility. Deployment target and CI/CD depth are deliberately lightweight and decided later in the roadmap; do not assume AWS, ECS, or Terraform going forward.
- Current priority: continue from the completed Sprints 1–3 into Sprint 4, preserving the iterative full-cycle approach (design, implementation, tests) for each slice.

## Session Start Checklist

Before substantial work:

1. Read `AGENTS.md`.
2. Read `CONTEXT.md`.
3. Check `README.md` and relevant files only if needed for the current task.
