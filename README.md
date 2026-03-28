# TransitOps

Transport management backend as a personal project focused on cloud architecture and DevOps practices.

## Current Status

Reference date: March 28, 2026.

The repository contains an ASP.NET Core solution with the local backend baseline already in place:

- `TransitOps.Api`: HTTP entry point, EF Core PostgreSQL persistence, versioned controllers, common response contracts, and minimal domain structure.
- `TransitOps.Tests`: test project.

The solution is intentionally kept small and KISS-oriented: only the API and tests exist as projects, while the internal API structure stays limited to the folders that already provide concrete value.

The code is still before the functional MVP, but it is no longer only a baseline. PostgreSQL-backed transport reads and health endpoints already work, while the remaining CRUD/query behavior, authentication, and the cloud rollout are still pending.

Planning has now been restructured around an explicit requirements specification and a daily roadmap so the remaining work stays aligned with the real repository state and the AWS deployment objective.

## Project Objective

Build a backend that is small in functionality and strong in operation:

- transport management;
- vehicle and driver assignment;
- traceability through logistics events;
- basic authentication and authorization;
- later deployment to AWS with infrastructure as code;
- observability, security, and defensible documentation.

The local MVP is not the final objective by itself. It is the minimum credible base for the cloud deployment phase, so scope must stay intentionally tight.

## MVP Scope

The MVP covers a functional backend that can run locally with:

- API ASP.NET Core;
- PostgreSQL persistence;
- CRUD for transports, vehicles, and drivers;
- assignments and state transitions;
- JWT authentication with basic roles;
- basic user bootstrap and admin user management;
- initial tests;
- local packaging with Docker.

Advanced cloud work is outside the MVP: Terraform, ECS, ECR, RDS on AWS, CloudWatch, alarms, and full automated deployment.

The detailed requirements baseline is in [docs/Requirements.md](docs/Requirements.md), and the current day-by-day execution plan is in [docs/Roadmap.md](docs/Roadmap.md).

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
|   |-- Requirements.md
|   `-- Roadmap.md
|-- scripts/
|   `-- postgres/
|       `-- manual/
|-- TransitOps.Api/
|   |-- Common/
|   |-- Controllers/
|   |-- Contracts/
|   |-- Domain/
|   |-- Errors/
|   |-- Middleware/
|   |-- Application/
|   |-- Infrastructure/
|   |-- Properties/
|   |-- Dockerfile
|   |-- TransitOps.Api.http
|   |-- TransitOps.Api.postman_collection.json
|   |-- Program.cs
|   `-- TransitOps.Api.csproj
`-- TransitOps.Tests/
    |-- HealthEndpointsTests.cs
    |-- TransportEndpointsTests.cs
    |-- TransportStateMachineTests.cs
    |-- TransitOpsApiFactory.cs
    `-- TransitOps.Tests.csproj
```

The exact folder distribution may evolve. What matters at this stage is that the solution, the baseline documentation, and the main projects already exist and are consistent with the roadmap.

## Available Documentation

- Software requirements specification: [docs/Requirements.md](docs/Requirements.md)
- Daily delivery roadmap: [docs/Roadmap.md](docs/Roadmap.md)
- Architecture/model source: [docs/ClassDiagramV1.drawio](docs/ClassDiagramV1.drawio)

## Local Requirements

- .NET SDK 10
- Docker Desktop
- PostgreSQL 16 or later, if run without containers

## Local Startup

### Current Repository Status

The repository already includes:

- `docker-compose.yml` for local API + PostgreSQL startup;
- EF Core PostgreSQL persistence under `TransitOps.Api/Infrastructure/Persistence`;
- a baseline `InitialCreate` migration;
- implemented `GET /api/v1/health/live`, `GET /api/v1/health/ready`, `GET /api/v1/transports`, and `GET /api/v1/transports/{id}`;
- integration tests for the implemented health and transport read endpoints;
- manual request artifacts in `TransitOps.Api/TransitOps.Api.http` and `TransitOps.Api/TransitOps.Api.postman_collection.json`;
- optional manual sample-data scripts under `scripts/postgres/manual/`;
- a real readiness check at `GET /api/v1/health/ready` that verifies PostgreSQL connectivity.

The API structure remains intentionally simple: `Controllers`, `Contracts`, `Domain`, `Common`, `Errors`, `Middleware`, `Application`, and `Infrastructure`.

`TransitOps.Api/Infrastructure/Persistence/Migrations` is now the only source of truth for the database schema. Local Docker startup relies on EF Core migrations, not on separate SQL schema bootstrap files.

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

Reset the local database volume and rerun the stack with a fresh migrations-managed database:

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
dotnet tool run dotnet-ef migrations add <MigrationName> --project .\TransitOps.Api\TransitOps.Api.csproj --startup-project .\TransitOps.Api\TransitOps.Api.csproj --output-dir Infrastructure\Persistence\Migrations
```

Apply migrations manually to a local PostgreSQL database:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project .\TransitOps.Api\TransitOps.Api.csproj --startup-project .\TransitOps.Api\TransitOps.Api.csproj
```

`docker compose up --build` starts PostgreSQL on a fresh named volume and the API applies pending EF Core migrations automatically on startup. If you still have an old local volume from the retired SQL-bootstrap flow, reset it with `docker compose down -v`.

Check API readiness against PostgreSQL:

```text
GET http://localhost:8080/api/v1/health/ready
```

## Next Milestones

1. Replace the remaining placeholder controller behavior for vehicles, drivers, and shipment events with real CRUD/query flows on top of `TransitOpsDbContext`.
2. Introduce user bootstrap, basic admin user management, JWT authentication, and role-based authorization.
3. Harden Docker-based local startup, tests, and CI so the backend becomes a credible local release candidate.
4. Move immediately into Terraform, AWS runtime, and delivery automation once the local MVP core is closed.

## Roadmap Quality Criteria

- The solution must build cleanly.
- Tests must be runnable locally and in CI.
- The environment must be reproducible.
- Cloud infrastructure must be versioned.
- Each week must end with a verifiable result.

## Verification Note

As of March 28, 2026, the API project builds, EF Core persistence is configured, the baseline migration exists, the local Docker flow is migrations-managed, health endpoints work, transport list/detail reads are backed by PostgreSQL, and automated tests cover both domain rules and the implemented read endpoints. Remaining CRUD behavior, authentication, and AWS deployment are still pending.
