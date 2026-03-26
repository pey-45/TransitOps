# TransitOps

Transport management backend as a personal project focused on cloud architecture and DevOps practices.

## Current Status

Reference date: March 26, 2026.

The repository contains an ASP.NET Core solution with the local backend baseline already in place:

- `TransitOps.Api`: HTTP entry point, EF Core PostgreSQL persistence, versioned controllers, common response contracts, and minimal domain structure.
- `TransitOps.Tests`: test project.

The solution is intentionally kept small and KISS-oriented: only the API and tests exist as projects, while the internal API structure stays limited to the folders that already provide concrete value.

The code is still before the functional MVP. Persistence is wired and validated at connectivity level, but CRUD/query behavior is still largely pending.

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
|-- database/
|-- docs/
|   |-- MVP-Backlog.md
|   |-- DailyRoadmap.pdf
|   `-- Roadmap.md
|-- TransitOps.Api/
|   |-- Common/
|   |-- Controllers/
|   |-- Contracts/
|   |-- Domain/
|   |-- Errors/
|   |-- Middleware/
|   |-- Persistence/
|   |-- Properties/
|   |-- Dockerfile
|   |-- Program.cs
|   `-- TransitOps.Api.csproj
`-- TransitOps.Tests/
    |-- TransportStateMachineTests.cs
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

The repository already includes:

- an initial PostgreSQL schema under `database/postgres/`;
- `docker-compose.yml` for local API + PostgreSQL startup;
- EF Core PostgreSQL persistence under `TransitOps.Api/Persistence`;
- a baseline `InitialCreate` migration;
- a real readiness check at `GET /api/v1/health/ready` that verifies PostgreSQL connectivity.

The API structure remains intentionally simple: `Controllers`, `Contracts`, `Domain`, `Common`, `Errors`, `Middleware`, and `Persistence`.

### Base Commands

Restore dependencies:

```powershell
dotnet restore TransitOps.slnx
```

Build the solution:

```powershell
dotnet build TransitOps.slnx
```

Or build only the API:

```powershell
dotnet build .\TransitOps.Api\TransitOps.Api.csproj
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

Create a new EF Core migration:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef migrations add <MigrationName> --project .\TransitOps.Api\TransitOps.Api.csproj --startup-project .\TransitOps.Api\TransitOps.Api.csproj --output-dir Persistence\Migrations
```

Note: the local PostgreSQL database may already exist and may already have the schema created from `database/postgres/001_initial_schema.sql`. In that case, do not apply the baseline migration blindly without reconciling EF migration history with the existing database.

Check API readiness against PostgreSQL:

```text
GET http://localhost:8080/api/v1/health/ready
```

## Next Milestones

1. Implement real CRUD/query behavior on top of `TransitOpsDbContext`.
2. Replace placeholder GET endpoints with real database-backed queries.
3. Add write flows for transports, vehicles, drivers, and shipment events.
4. Introduce authentication and authorization for the MVP.
5. Keep the local environment and migrations aligned before moving to the cloud phase.

## Roadmap Quality Criteria

- The solution must build cleanly.
- Tests must be runnable locally and in CI.
- The environment must be reproducible.
- Cloud infrastructure must be versioned.
- Each week must end with a verifiable result.

## Verification Note

As of March 26, 2026, the API project builds, EF Core persistence is configured, the baseline migration exists, and the readiness endpoint can confirm PostgreSQL connectivity. Functional CRUD behavior is still pending.
