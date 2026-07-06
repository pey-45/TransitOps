# TransitOps

Transport management application, developed as a full software development lifecycle project: requirements, design, backend, frontend, testing, and deployment.

This is the Final Degree Project (TFG) of the Bachelor's in Computer Engineering (UDC, Software Engineering track). Title: *"Design and development of a transport-management application: complete software development lifecycle"*.

## Current Status

Reference date: July 7, 2026.

The project is **starting fresh**. It previously followed a different direction — an AWS cloud-platform thesis — which was superseded by a signed TFG modification request (2026-06-19). The decision (2026-07-07) is to rebuild the application from scratch, applying the new iterative full-lifecycle methodology cleanly, while keeping the previous iteration only as an archived reference.

As a result, the repository root currently contains **only planning documentation**. There is no active application code, solution, or build yet — it is recreated fresh as the sprints progress.

```text
TransitOps/
|-- README.md
|-- AGENTS.md
|-- CONTEXT.md
|-- docs/
|   |-- ClientRequirements.md   (simulated client interview)
|   |-- Requirements.md         (formal functional/non-functional requirements)
|   `-- Roadmap.md              (sprint plan — pending rewrite to the iterative methodology)
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
- Frontend test tooling — to be decided when the frontend is scaffolded
- Docker / Docker Compose — local reproducibility
- GitHub Actions — CI
- Deployment target — to be decided during the deployment sprint; intentionally not a Terraform-managed cloud platform this time

The stack is intentionally kept the same as the previous iteration, so the archived reference implementation is directly consultable.

## Methodology

Development follows an iterative, incremental approach organized in sprints, as stated in the TFG proposal. Each sprint adds concrete functionality and goes through the full development cycle for that slice (design, implementation, testing), rather than grouping work into horizontal phases. The requirements process is: client interview (`docs/ClientRequirements.md`) → formal requirements (`docs/Requirements.md`) → iterative sprints (`docs/Roadmap.md`).

## Documentation

- Simulated client interview: [docs/ClientRequirements.md](docs/ClientRequirements.md)
- Software requirements specification: [docs/Requirements.md](docs/Requirements.md)
- Sprint roadmap: [docs/Roadmap.md](docs/Roadmap.md) *(pending rewrite to the iterative methodology)*
- Stable agent instructions: [AGENTS.md](AGENTS.md)
- Evolving project context and decision log: [CONTEXT.md](CONTEXT.md)
- Archived materials (previous direction + previous-iteration reference code): [archive/README.md](archive/README.md)

## Reference Implementation

A complete previous-iteration backend (ASP.NET Core, EF Core/PostgreSQL, JWT auth, transport/vehicle/driver/shipment-event management, user administration, xUnit tests, Docker Compose, Postman/Newman smoke flow) is preserved, self-contained and still buildable, under [archive/cloud-phase/](archive/README.md). It is a **reference oracle** for the rebuild — consult it for business rules, EF migrations, test cases, and decisions already made — but it is not the base of the active project and is not edited.

## Local Setup

Setup and run instructions will be added here once the new backend and frontend are scaffolded during the first implementation sprints. Local requirements will include the .NET SDK 10, Docker Desktop, and Node.js (for the frontend).
