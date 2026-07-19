# TransitOps

Transport management application, developed as a full software development lifecycle project: requirements, design, backend, frontend, testing, and deployment.

This is the Final Degree Project (TFG) of the Bachelor's in Computer Engineering (UDC, Software Engineering track). Title: *"Design and development of a transport-management application: complete software development lifecycle"*.

## Current Status

Reference date: July 19, 2026.

Sprints 1 and 2 are implemented: the greenfield application has an ASP.NET Core API with PostgreSQL persistence, controlled first-admin bootstrap and JWT authentication, plus end-to-end vehicle, driver and customer catalogs with business-key validation and soft deletion. The React/TypeScript SPA provides login, protected routing, role-aware navigation and list/detail/create/edit/deactivate flows for all three catalogs. Backend and frontend tests, Docker Compose and CI keep the increment reproducible.

The earlier AWS-oriented direction remains archived as a read-only reference; the active solution is independent and lives at the repository root.

```text
TransitOps/
|-- README.md
|-- AGENTS.md
|-- CONTEXT.md
|-- TransitOps.slnx
|-- TransitOps.Api/              (ASP.NET Core API)
|-- TransitOps.Tests/            (xUnit service and controller tests)
|-- frontend/                    (Vite + React + TypeScript)
|-- docker-compose.yml
|-- docs/
|   |-- ClientRequirements.md   (simulated client interview)
|   |-- Requirements.md         (formal functional/non-functional requirements)
|   |-- Roadmap.md              (iterative sprint plan)
|   `-- design/                 (data model and integration architecture)
`-- archive/
    |-- README.md
    `-- cloud-phase/            (previous direction + previous-iteration code, kept as reference)
```

## Project Objective

Build and demonstrate a complete transport-management application, covering the full software development lifecycle:

- analyze the functional and non-functional requirements of the application;
- design the system architecture: data model, components, and interfaces;
- implement a backend exposing vehicle, driver, customer, transport, and operations management;
- develop a frontend for visual, intuitive interaction with the application;
- implement user authentication and authorization;
- define and execute a functional and integration test plan;
- deploy the application to an accessible environment and document the process;
- document the architecture, design decisions, and development process.

The differentiator of this TFG is software engineering discipline across the full lifecycle, applied iteratively, rather than depth in any single layer or in cloud infrastructure.

## Scope

The functional scope is defined in [docs/Requirements.md](docs/Requirements.md), derived from the simulated client interview in [docs/ClientRequirements.md](docs/ClientRequirements.md). In short: access with two roles (operator/admin), management of vehicles, drivers, customers and shipments, vehicle+driver assignment, shipment lifecycle, an event/incident history per shipment, an operational summary, and user administration. Out of scope for now: route optimization, GPS, billing, a mobile app, and external-customer access.

## Target Stack

- ASP.NET Core (.NET 10) + PostgreSQL / EF Core — backend
- React (SPA) — frontend
- xUnit — backend tests
- Vitest + React Testing Library — frontend tests
- Docker / Docker Compose — local reproducibility
- GitHub Actions — CI
- Deployment target — to be decided during the deployment sprint; intentionally not a Terraform-managed cloud platform this time

The stack is intentionally kept the same as the previous iteration, so the archived reference implementation is directly consultable.

## Methodology

Development follows an iterative, incremental approach organized in sprints, as stated in the TFG proposal. Each sprint adds concrete functionality and goes through the full development cycle for that slice (design, implementation, testing), rather than grouping work into horizontal phases. The requirements process is: client interview (`docs/ClientRequirements.md`) → formal requirements (`docs/Requirements.md`) → iterative sprints (`docs/Roadmap.md`).

## Documentation

- Simulated client interview: [docs/ClientRequirements.md](docs/ClientRequirements.md)
- Software requirements specification: [docs/Requirements.md](docs/Requirements.md)
- Sprint roadmap: [docs/Roadmap.md](docs/Roadmap.md)
- Data model: [docs/design/DataModel.md](docs/design/DataModel.md)
- Integration architecture: [docs/design/IntegrationArchitecture.md](docs/design/IntegrationArchitecture.md)
- Stable agent instructions: [AGENTS.md](AGENTS.md)
- Evolving project context and decision log: [CONTEXT.md](CONTEXT.md)
- Archived materials (previous direction + previous-iteration reference code): [archive/README.md](archive/README.md)

## Reference Implementation

A complete previous-iteration backend (ASP.NET Core, EF Core/PostgreSQL, JWT auth, transport/vehicle/driver/shipment-event management, user administration, xUnit tests, Docker Compose, Postman/Newman smoke flow) is preserved, self-contained and still buildable, under [archive/cloud-phase/](archive/README.md). It is a **reference oracle** for the rebuild — consult it for business rules, EF migrations, test cases, and decisions already made — but it is not the base of the active project and is not edited.

## Local Setup

The reproducible path only requires Docker Desktop. Create the ignored local configuration from the committed template before the first start:

```bash
cp .env.example .env
docker compose up --build
```

In PowerShell, use `Copy-Item .env.example .env` for the first command. Docker Compose reads `.env`; startup fails explicitly when a required value is missing.

The web application is then available at `http://localhost:5173` and the API health endpoint at `http://localhost:8080/api/v1/health`. On a new database, create the first administrator once:

```bash
curl -X POST http://localhost:8080/api/v1/auth/bootstrap-admin \
  -H "Content-Type: application/json" \
  -H "X-Bootstrap-Token: transitops-bootstrap-local-only" \
  -d '{"username":"admin","email":"admin@transitops.local","password":"ChangeMe!123"}'
```

The committed `.env.example` contains development-only values so the local flow is reproducible. `.env` is ignored by Git and is the place to replace them. Never reuse these values in a deployed environment.
PostgreSQL is published on host port `5433` by default to avoid collisions with an existing local installation; change `POSTGRES_PORT` in `.env` if needed. Services inside Compose continue to use port `5432`.

For native development, use .NET SDK 10 and Node.js 22:

```bash
dotnet restore TransitOps.slnx
dotnet test TransitOps.slnx
cd frontend
npm ci
npm run test
npm run dev
```

## Testing

Backend tests are organized by responsibility: `TransitOps.Tests/Services/` verifies service business behavior directly, `TransitOps.Tests/Controllers/` verifies HTTP contracts and authorization, and `TransitOps.Tests/Support/` contains shared test infrastructure. CI runs backend build/tests and migration validation, plus frontend lint/build/tests.
