# TransitOps

Transport management backend as a personal project focused on cloud architecture and DevOps practices.

## Current Status

Reference date: April 7, 2026.

The repository contains an ASP.NET Core solution with the local backend baseline already in place:

- `TransitOps.Api`: HTTP entry point, EF Core PostgreSQL persistence, versioned controllers, common response contracts, and minimal domain structure.
- `TransitOps.Tests`: test project.

The solution is intentionally kept small and KISS-oriented: only the API and tests exist as projects, while the internal API structure stays limited to the folders that already provide concrete value.

The code is still before the functional MVP, but it is no longer only a baseline. PostgreSQL-backed CRUD now exists for transports, vehicles, and drivers, including soft delete on those three main operational entities. Transport list filters and basic pagination are also in place for demo use, explicit vehicle+driver assignment plus lifecycle transitions are implemented on transports, shipment events support creation plus chronological history with actor traceability, the API exposes first-admin bootstrap, password hashing, login, JWT issuance, and protected business endpoints, admin-only user-management flows now cover list, detail, create, role change, and activate/deactivate with last-active-admin protection, the local Docker path is now documented through `.env.example` and a dedicated verification guide, a first GitHub Actions build/test workflow now exists, the target AWS topology plus cloud naming/tagging/configuration conventions are now fixed in project documentation, and Sprint 2 has started with a first Terraform foundation layout for `dev` and `prod`. Enum-like state fields are persisted as `smallint` plus explicit database check constraints instead of native PostgreSQL enums, which keeps EF Core persistence simpler and more stable for the current project scope.

Planning has now been restructured around an explicit requirements specification and a weekly sprint roadmap so the remaining work stays aligned with the real repository state and the AWS deployment objective.

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

The detailed requirements baseline is in [docs/Requirements.md](docs/Requirements.md), and the current sprint plan is in [docs/Roadmap.md](docs/Roadmap.md).

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
|-- AGENTS.md
|-- CONTEXT.md
|-- .env.example
|-- README.md
|-- docker-compose.yml
|-- dotnet-tools.json
|-- infra/
|   `-- terraform/
|       |-- modules/
|       |   `-- platform_foundation/
|       `-- environments/
|           |-- dev/
|           `-- prod/
|-- .github/
|   `-- workflows/
|       `-- ci.yml
|-- docs/
|   |-- AwsTargetArchitecture.md
|   |-- CloudConventions.md
|   |-- LocalVerification.md
|   |-- Requirements.md
|   `-- Roadmap.md
|-- scripts/
|   |-- database/
|   |   `-- postgres/
|   |       `-- seed/
|   `-- testing/
|       `-- postman/
|           |-- collections/
|           |-- environments/
|           |-- sql/
|           `-- run_local_api_smoke.bat
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
    |-- DriverEndpointsTests.cs
    |-- HealthEndpointsTests.cs
    |-- TransportEndpointsTests.cs
    |-- TransportStateMachineTests.cs
    |-- TransitOpsApiFactory.cs
    |-- VehicleEndpointsTests.cs
    `-- TransitOps.Tests.csproj
```

The exact folder distribution may evolve. What matters at this stage is that the solution, the baseline documentation, and the main projects already exist and are consistent with the roadmap.

## Available Documentation

- Software requirements specification: [docs/Requirements.md](docs/Requirements.md)
- Sprint delivery roadmap: [docs/Roadmap.md](docs/Roadmap.md)
- Local verification guide: [docs/LocalVerification.md](docs/LocalVerification.md)
- AWS target architecture note: [docs/AwsTargetArchitecture.md](docs/AwsTargetArchitecture.md)
- Cloud naming/tagging/configuration conventions: [docs/CloudConventions.md](docs/CloudConventions.md)
- Architecture/model source: [docs/ClassDiagramV1.drawio](docs/ClassDiagramV1.drawio)

## Local Requirements

- .NET SDK 10
- Docker Desktop
- Terraform CLI, when starting the cloud phase
- PostgreSQL 16 or later, if run without containers

## Local Startup

### Current Repository Status

The repository already includes:

- `docker-compose.yml` for local API + PostgreSQL startup;
- EF Core PostgreSQL persistence under `TransitOps.Api/Infrastructure/Persistence`;
- a migrations-managed schema under `TransitOps.Api/Infrastructure/Persistence/Migrations`, including the baseline setup plus follow-up alignment and enum-simplification migrations;
- implemented `GET /api/v1/health/live` and `GET /api/v1/health/ready`;
- implemented database-backed transport CRUD, including filtered/paginated `GET /api/v1/transports`, `GET /api/v1/transports/{id}`, `POST /api/v1/transports`, `PUT /api/v1/transports/{id}`, `PUT /api/v1/transports/{id}/assignment`, `PUT /api/v1/transports/{id}/status`, and `DELETE /api/v1/transports/{id}`;
- implemented database-backed vehicle CRUD on `GET /api/v1/vehicles`, `GET /api/v1/vehicles/{id}`, `POST /api/v1/vehicles`, `PUT /api/v1/vehicles/{id}`, and `DELETE /api/v1/vehicles/{id}`;
- implemented database-backed driver CRUD on `GET /api/v1/drivers`, `GET /api/v1/drivers/{id}`, `POST /api/v1/drivers`, `PUT /api/v1/drivers/{id}`, and `DELETE /api/v1/drivers/{id}`;
- implemented shipment-event creation/history on `POST /api/v1/transports/{transportId}/shipment-events` and `GET /api/v1/transports/{transportId}/shipment-events`, with actor traceability now resolved from the authenticated user context;
- implemented auth endpoints on `POST /api/v1/auth/bootstrap-admin` and `POST /api/v1/auth/login`, with password hashing, JWT issuance, and protected business endpoints;
- implemented admin-only user-management on `GET /api/v1/users`, `GET /api/v1/users/{id}`, `POST /api/v1/users`, `PUT /api/v1/users/{id}/role`, and `PUT /api/v1/users/{id}/activation`, including last-active-admin protection;
- integration tests for the implemented health, transport, vehicle, driver, shipment-event, auth, and user-management endpoints;
- manual request artifacts in `TransitOps.Api/TransitOps.Api.http` and `TransitOps.Api/TransitOps.Api.postman_collection.json`;
- a runner-safe Postman/Newman smoke flow under `scripts/testing/postman/` that starts from deterministic seed data and exercises the live local API against real PostgreSQL;
- optional manual sample-data scripts under `scripts/database/postgres/seed/`, aligned with the current numeric enum mapping, plus `.bat` wrappers that execute them against the local Docker PostgreSQL service;
- `smallint`-backed enum storage with check constraints for transport status, shipment event type, and user role;
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

Before `dotnet run`, set at least the JWT signing key through user secrets or environment variables:

```powershell
dotnet user-secrets set --project .\TransitOps.Api\TransitOps.Api.csproj "Jwt:SigningKey" "<long-local-dev-signing-key>"
```

If you want to exercise the first-admin bootstrap endpoint locally, also configure the bootstrap token:

```powershell
dotnet user-secrets set --project .\TransitOps.Api\TransitOps.Api.csproj "Bootstrap:FirstAdminToken" "<local-bootstrap-token>"
```

Prepare local Docker configuration:

```powershell
Copy-Item .env.example .env
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

The Docker path reads `.env` automatically. `TRANSITOPS_JWT_SIGNING_KEY` is required and must be at least 32 characters long. `TRANSITOPS_BOOTSTRAP_ADMIN_TOKEN` is optional and only needed when you want to call `POST /api/v1/auth/bootstrap-admin`.

Validate the compose file before starting the stack:

```powershell
docker compose config
```

Check API readiness against PostgreSQL:

```text
GET http://localhost:8080/api/v1/health/ready
```

Run the local smoke test against the live Docker API and PostgreSQL:

```powershell
.\scripts\testing\postman\run_local_api_smoke.bat
```

This smoke flow:

- starts `db` and `api` with Docker Compose;
- waits for `GET /api/v1/health/ready`;
- removes any leftover runtime smoke data from previous interrupted runs;
- resets the deterministic sample dataset through the existing seed scripts;
- logs in with the deterministic seeded admin user before hitting protected endpoints;
- executes `scripts/testing/postman/collections/TransitOps.Api.smoke.postman_collection.json` against the live API;
- physically removes every runtime transport, vehicle, driver, user, shipment-event, and deterministic seed row generated by the smoke flow before exiting, even if the collection fails midway.

`run_local_api_smoke.bat` prefers a globally installed `newman`, but it can also fall back to `npx newman@6` when Node.js is available.

For the full step-by-step local path, including manual `.http` and Postman verification, see [docs/LocalVerification.md](docs/LocalVerification.md).

Deterministic local seed credentials:

- `seed.admin` / `SeedAdmin!123`
- `seed.operator` / `SeedOperator!123`

These credentials exist only in the manual local seed dataset under `scripts/database/postgres/seed/002_seed_sample_data.sql`.

## Next Milestones

1. Build the remote Terraform state bootstrap path with S3 backend, encryption, versioning, and DynamoDB locking.
2. Implement the first real network layer in Terraform: VPC, subnets, route tables, ingress path, and least-privilege security groups.
3. Extend the CI baseline later with Terraform validation/plan and deployment automation once the cloud artifacts exist.

## Roadmap Quality Criteria

- The solution must build cleanly.
- Tests must be runnable locally and in CI.
- The environment must be reproducible.
- Cloud infrastructure must be versioned.
- Each week must end with a verifiable result.

## Verification Note

As of April 7, 2026, the API project builds, EF Core persistence is configured, the migrations-managed PostgreSQL schema is operational, health endpoints work, transport/vehicle/driver CRUD are backed by PostgreSQL, transport list filters and pagination are available for demo use, explicit transport assignment and lifecycle transitions are implemented, shipment events support creation and chronological history with authenticated actor traceability, first-admin bootstrap plus JWT login are implemented, protected endpoints enforce bearer authentication, admin-only user-management is implemented with last-active-admin protection, transport status and related enum-like fields are stored through `smallint` plus check constraints, a documented local Docker verification path exists, a runner-safe smoke path exists, a manual Postman environment exists, a GitHub Actions restore/build/test baseline is present, the intended AWS topology plus shared cloud conventions are documented, and a first Terraform foundation structure now exists under `infra/terraform/`. Remote state, networking, and real AWS rollout work are still pending.
