# AGENTS.md

## Purpose

This file stores stable instructions for the coding agent working in this repository.

Use `CONTEXT.md` for evolving project context, current status, recent decisions, and work-in-progress notes.

## Working Agreement

- Treat this repository as a backend-first academic/professional project with strong emphasis on cloud architecture, DevOps, and defensible engineering decisions.
- Prefer pragmatic, maintainable solutions over premature complexity.
- Keep the project small in functional scope and deep in operational quality.
- Keep the functional scope intentionally small and orient decisions toward a credible AWS deployment, not feature breadth.
- Preserve consistency with the documented requirements and roadmap unless the user explicitly changes scope.

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
- `docs/Roadmap.md` is the canonical daily execution plan and should preserve completed history while replanning pending work.
- Keep `README.md` focused on repository-facing documentation, setup, and high-level project description.
- Keep requirements and roadmap documents aligned with actual project decisions when they materially change.

## Engineering Rules

- Favor clear folder and module responsibilities.
- Avoid hidden magic and implicit behavior when a straightforward implementation is available.
- Add tests when behavior or business rules justify them.
- Prefer validating changes with build or tests when feasible.
- Do not introduce secrets into committed files.

## Project Orientation

- Main backend: ASP.NET Core / .NET 10.
- Planned persistence: PostgreSQL.
- Test stack: xUnit.
- Operational direction: Docker, Terraform, GitHub Actions, AWS ECS Fargate, RDS, ECR, ALB, CloudWatch.
- Current priority is to reach a solid local MVP before the cloud phase.

## Session Start Checklist

Before substantial work:

1. Read `AGENTS.md`.
2. Read `CONTEXT.md`.
3. Check `README.md` and relevant files only if needed for the current task.
