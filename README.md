# TransitOps

Transport management application, developed as a full software development lifecycle project: requirements, design, backend, frontend, testing, and deployment.

## Current Status

Reference date: July 6, 2026.

The project direction changed on 2026-06-19 (signed TFG modification request) from an AWS cloud-platform thesis to this full-lifecycle application thesis. The previous cloud/AWS/Terraform work and TFG memoria/presentation are preserved for reference under [archive/cloud-phase/](archive/README.md) and are not part of the active project.

The repository contains an ASP.NET Core solution with the backend baseline already implemented:

- `TransitOps.Api`: HTTP entry point, EF Core PostgreSQL persistence, versioned controllers, common response contracts, JWT authentication, and a small, deliberate domain structure.
- `TransitOps.Tests`: xUnit integration test project covering health, auth, users, transports, vehicles, drivers, and shipment events.

PostgreSQL-backed CRUD exists for transports, vehicles, and drivers, including soft delete on the main operational entities. Transport filters and pagination support demo use, explicit vehicle+driver assignment and lifecycle transitions are implemented, shipment events provide chronological traceability, first-admin bootstrap/login/JWT protection are in place, and admin-only user management covers list, detail, create, role changes, and activation/deactivation.

The frontend, the deployment target, and lightweight CI/CD-for-deploy are the main remaining work. No frontend code exists yet.

## Project Objective

Build and demonstrate a complete transport-management application, covering the full software development lifecycle:

- analyze the functional and non-functional requirements of the application;
- design the system architecture: data model, components, and interfaces;
- implement a backend exposing vehicle, driver, transport, and operations management (already done);
- develop a frontend for visual, intuitive interaction with the application;
- implement user authentication and authorization (done on the backend; frontend integration pending);
- define and execute a functional and integration test plan;
- deploy the application to an accessible environment and document the process;
- document the architecture, design decisions, and development process.

## MVP Scope

The MVP covers a functional full-stack application with:

- API ASP.NET Core (implemented);
- PostgreSQL persistence (implemented);
- CRUD for transports, vehicles, and drivers (implemented);
- assignments and state transitions (implemented);
- JWT authentication with basic roles (implemented);
- basic user bootstrap and admin user management (implemented);
- backend tests (implemented);
- local packaging with Docker (implemented);
- a React SPA frontend covering login and the main operational flows (planned);
- deployment to a simple, accessible environment (planned, target to be decided).

The detailed requirements baseline is in [docs/Requirements.md](docs/Requirements.md), and the current sprint plan is in [docs/Roadmap.md](docs/Roadmap.md).

## Target Stack

- ASP.NET Core (.NET 10) — implemented
- PostgreSQL — implemented
- xUnit — implemented
- Docker / Docker Compose — implemented (local reproducibility)
- GitHub Actions — implemented for CI (restore/build/test)
- React (SPA) — planned, frontend
- Frontend test tooling — planned, decided when the frontend is scaffolded
- Deployment target — to be decided during the deployment phase; intentionally not a Terraform-managed cloud platform this time

## Solution Structure

```text
TransitOps/
|-- TransitOps.slnx
|-- AGENTS.md
|-- CONTEXT.md
|-- README.md
|-- .env.example
|-- docker-compose.yml
|-- dotnet-tools.json
|-- archive/
|   |-- README.md
|   `-- cloud-phase/          (superseded cloud/AWS/Terraform work and prior TFG memoria, kept for reference)
|-- .github/
|   `-- workflows/
|       `-- ci.yml
|-- docs/
|   |-- Requirements.md
|   |-- Roadmap.md
|   `-- LocalVerification.md
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
|   |-- Extensions/
|   |-- Middleware/
|   |-- Application/
|   |-- Infrastructure/
|   |-- Security/
|   |-- Properties/
|   |-- Dockerfile
|   |-- TransitOps.Api.http
|   |-- TransitOps.Api.postman_collection.json
|   |-- Program.cs
|   `-- TransitOps.Api.csproj
|-- TransitOps.Tests/
|   |-- AuthEndpointsTests.cs
|   |-- DriverEndpointsTests.cs
|   |-- HealthEndpointsTests.cs
|   |-- ShipmentEventEndpointsTests.cs
|   |-- TransportEndpointsTests.cs
|   |-- TransportStateMachineTests.cs
|   |-- UserEndpointsTests.cs
|   |-- VehicleEndpointsTests.cs
|   |-- TestAuthenticationHandler.cs
|   |-- TransitOpsApiFactory.cs
|   `-- TransitOps.Tests.csproj
`-- (planned) frontend project, added once the frontend sprints start
```

The exact folder distribution may evolve, especially once the frontend project is scaffolded. What matters at this stage is that the solution, the baseline documentation, and the backend are already consistent with the roadmap.

## Available Documentation

- Software requirements specification: [docs/Requirements.md](docs/Requirements.md)
- Sprint delivery roadmap: [docs/Roadmap.md](docs/Roadmap.md)
- Local verification guide: [docs/LocalVerification.md](docs/LocalVerification.md)
- Archived cloud-phase materials (reference only, not current): [archive/README.md](archive/README.md)

## Local Requirements

- .NET SDK 10
- Docker Desktop
- PostgreSQL 16 or later, if run without containers
- Node.js (planned, once the frontend is scaffolded)

## Local Startup

### Current Repository Status

The repository already includes:

- `docker-compose.yml` for local API + PostgreSQL startup;
- EF Core PostgreSQL persistence under `TransitOps.Api/Infrastructure/Persistence`;
- a migrations-managed schema under `TransitOps.Api/Infrastructure/Persistence/Migrations`;
- implemented `GET /api/v1/health/live` and `GET /api/v1/health/ready`;
- implemented database-backed transport CRUD, including filtered/paginated `GET /api/v1/transports`, `GET /api/v1/transports/{id}`, `POST /api/v1/transports`, `PUT /api/v1/transports/{id}`, `PUT /api/v1/transports/{id}/assignment`, `PUT /api/v1/transports/{id}/status`, and `DELETE /api/v1/transports/{id}`;
- implemented database-backed vehicle CRUD on `GET /api/v1/vehicles`, `GET /api/v1/vehicles/{id}`, `POST /api/v1/vehicles`, `PUT /api/v1/vehicles/{id}`, and `DELETE /api/v1/vehicles/{id}`;
- implemented database-backed driver CRUD on `GET /api/v1/drivers`, `GET /api/v1/drivers/{id}`, `POST /api/v1/drivers`, `PUT /api/v1/drivers/{id}`, and `DELETE /api/v1/drivers/{id}`;
- implemented shipment-event creation/history on `POST/GET /api/v1/transports/{transportId}/shipment-events`, with actor traceability resolved from the authenticated user context;
- implemented auth endpoints on `POST /api/v1/auth/bootstrap-admin` and `POST /api/v1/auth/login`, with password hashing, JWT issuance, and protected business endpoints;
- implemented admin-only user-management on `GET /api/v1/users`, `GET /api/v1/users/{id}`, `POST /api/v1/users`, `PUT /api/v1/users/{id}/role`, and `PUT /api/v1/users/{id}/activation`, including last-active-admin protection;
- integration tests for the implemented health, auth, transport, vehicle, driver, shipment-event, and user-management endpoints;
- manual request artifacts in `TransitOps.Api/TransitOps.Api.http` and `TransitOps.Api/TransitOps.Api.postman_collection.json`;
- a runner-safe Postman/Newman smoke flow under `scripts/testing/postman/` that starts from deterministic seed data and exercises the live local API against real PostgreSQL;
- optional manual sample-data scripts under `scripts/database/postgres/seed/`.

The API structure remains intentionally simple: `Controllers`, `Contracts`, `Domain`, `Common`, `Errors`, `Extensions`, `Middleware`, `Application`, `Infrastructure`, and `Security`.

`TransitOps.Api/Infrastructure/Persistence/Migrations` is the only source of truth for the database schema.

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

For the full step-by-step local path, including manual `.http` and Postman verification, see [docs/LocalVerification.md](docs/LocalVerification.md).

Deterministic local seed credentials:

- `seed.admin` / `SeedAdmin!123`
- `seed.operator` / `SeedOperator!123`

These credentials exist only in the manual local seed dataset under `scripts/database/postgres/seed/002_seed_sample_data.sql`.

## Next Milestones

1. Close/confirm requirements and high-level design for the new scope: data model is already stable, so this is mainly architecture notes and UI wireframes/prototype.
2. Scaffold the React frontend and integrate the authentication flow end-to-end.
3. Build the main operational frontend views: transports, vehicles, drivers, assignment, lifecycle, shipment events, and user administration.
4. Decide and set up a simple, accessible deployment target for backend + frontend.
5. Add lightweight CI/CD for deployment and minimal monitoring.
6. Run an end-to-end functional test pass across backend and frontend, and close any gaps.
7. Write the TFG memoria for the new direction.

## Roadmap Quality Criteria

- The solution must build cleanly, backend and frontend.
- Tests must be runnable locally and in CI.
- The environment must be reproducible.
- Each sprint must end with a verifiable, demonstrable result.

## Verification Note

As of July 6, 2026, the backend builds, its functional surface is implemented, and tests cover the critical API behavior; see [docs/LocalVerification.md](docs/LocalVerification.md) for the full local verification path. No frontend and no deployment environment currently exist, by design: the project direction changed on 2026-06-19, and this is the fresh documentation baseline for the new scope. The previous AWS `dev` validation history is preserved under [archive/cloud-phase/](archive/README.md) for reference only.
