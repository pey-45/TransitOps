# TransitOps

Transport management backend as a personal project focused on cloud architecture and DevOps practices.

## Current Status

Reference date: March 24, 2026.

The repository contains an initial ASP.NET Core solution with two projects:

- `TransitOps.Api`: base API.
- `TransitOps.Tests`: test project.

With the current state of the repository, the March 24 milestone can be considered complete: scope, stack, solution baseline, and initial documentation are already defined. The folder structure should be understood as indicative, not as a rigid acceptance criterion.

The code is still in a bootstrap phase with respect to the functional MVP. The documentation already defines the MVP scope and the full roadmap toward a delivery with AWS, Terraform, and CI/CD.

## Project Objective

Build a backend that is small in functionality and strong in operation:

- transport management;
- vehicle and driver assignment;
- traceability through logistics events;
- basic authentication and authorization;
- later deployment to AWS with infrastructure as code;
- observability, security, and defensible documentation.

## MVP Scope

The MVP covers a functional backend that can run locally with:

- API ASP.NET Core;
- PostgreSQL persistence;
- CRUD for transports, vehicles, and drivers;
- assignments and state transitions;
- JWT authentication with basic roles;
- initial tests;
- local packaging with Docker.

Advanced cloud work is outside the MVP: Terraform, ECS, ECR, RDS on AWS, CloudWatch, alarms, and full automated deployment.

The fixed scope details are in [docs/MVP-Backlog.md](docs/MVP-Backlog.md).

## Target Stack

- ASP.NET Core
- PostgreSQL
- xUnit
- Docker
- Terraform
- GitHub Actions
- AWS ECS Fargate
- Amazon RDS
- Amazon ECR
- ALB
- CloudWatch

## Solution Structure

```text
TransitOps/
|-- TransitOps.slnx
|-- README.md
|-- docker-compose.yml
|-- docs/
|   |-- MVP-Backlog.md
|   |-- DailyRoadmap.pdf
|   `-- Roadmap.md
|-- TransitOps.Api/
|   |-- Controllers/
|   |-- Properties/
|   |-- Dockerfile
|   |-- Program.cs
|   `-- TransitOps.Api.csproj
`-- TransitOps.Tests/
    |-- UnitTest1.cs
    `-- TransitOps.Tests.csproj
```

The exact folder distribution may evolve. What matters at this stage is that the solution, the baseline documentation, and the main projects already exist and are consistent with the roadmap.

## Available Documentation

- Full roadmap in Markdown: [docs/Roadmap.md](docs/Roadmap.md)
- Fixed MVP backlog: [docs/MVP-Backlog.md](docs/MVP-Backlog.md)
- Original roadmap source: [docs/DailyRoadmap.pdf](docs/DailyRoadmap.pdf)

## Local Requirements

- .NET SDK 10
- Docker Desktop
- PostgreSQL 16 or later, if run without containers

## Local Startup

### Current Repository Status

The repository already covers the expected baseline for the initial milestone: solution created, base API, test project, `README`, MVP backlog, and roadmap versioned in GitHub.

The repository now includes an initial PostgreSQL schema under `database/postgres/` and a `docker-compose.yml` for local API + database startup. Infrastructure as code and CI/CD automation still belong to later milestones.

### Base Commands

Restore dependencies:

```powershell
dotnet restore TransitOps.slnx
```

Build the solution:

```powershell
dotnet build TransitOps.slnx
```

Run the API:

```powershell
dotnet run --project .\TransitOps.Api\TransitOps.Api.csproj
```

Run API + PostgreSQL with Docker Compose:

```powershell
docker compose up --build
```

Reset the local database volume and rerun the initialization script:

```powershell
docker compose down -v
docker compose up --build
```

Run tests:

```powershell
dotnet test .\TransitOps.Tests\TransitOps.Tests.csproj
```

## Next Milestones

1. Finalize the domain model and business rules.
2. Add real PostgreSQL persistence and migrations.
3. Implement the functional core of the MVP.
4. Refine the reproducible local environment and keep it aligned with the evolving application.
5. Prepare the transition to AWS, Terraform, and CI/CD.

## Roadmap Quality Criteria

- The solution must build cleanly.
- Tests must be runnable locally and in CI.
- The environment must be reproducible.
- Cloud infrastructure must be versioned.
- Each week must end with a verifiable result.

## Verification Note

The documentation was generated from the existing roadmap PDF and adjusted to the actual state of the repository on this date. The project is still in an initial phase, so some sections of the roadmap describe planned work rather than functionality that has already been implemented.
