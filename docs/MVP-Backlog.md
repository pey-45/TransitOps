# TransitOps · Fixed MVP Backlog

## Purpose

Define a fixed, small, and defensible scope for the TransitOps MVP. This backlog is closed: any new addition must replace another item that has already been approved.

## Operational Definition of the MVP

The MVP is a functional transport management backend, runnable locally, with PostgreSQL persistence, basic authentication, core business rules, initial tests, and reproducible packaging.

The MVP does not yet include deployment on AWS, Terraform, observability in CloudWatch, alarms, dashboards, or full cloud CI/CD automation.

## MVP Objectives

- Manage transports, vehicles, and drivers.
- Allow valid assignments between transport, vehicle, and driver.
- Control the transport lifecycle through states.
- Record logistics events associated with the transport.
- Run the system locally with a real database and repeatable startup.
- Leave a technical baseline suitable for the later cloud phase.

## Definition of Done

An MVP item is considered complete when:

- It has code integrated into the solution.
- It has a verifiable acceptance criterion.
- It can be demonstrated manually or through tests.
- It does not introduce structural debt that blocks the next phase.
- It is reflected in the repository's minimum documentation.

## Included Scope

### Epic 1 · Solution Foundation

| ID | Item | Priority | Acceptance Criteria |
| --- | --- | --- | --- |
| MVP-01 | Structure the solution by projects and base folders | Must | An API project and a test project exist. The solution builds. The structure allows evolution toward layers without redoing the bootstrap. |
| MVP-02 | Define configuration by environment | Must | `appsettings` and local execution profiles exist. The API starts in development without ambiguous manual changes. |
| MVP-03 | Document startup and initial scope | Must | A solution `README.md` exists and the fixed MVP backlog is linked from the documentation. |

### Epic 2 · Domain and Persistence

| ID | Item | Priority | Acceptance Criteria |
| --- | --- | --- | --- |
| MVP-04 | Model `Transport` with business states | Must | The entity exists with identifier, operational data, and initial state. Valid transitions are defined and documented. |
| MVP-05 | Model `Vehicle` and `Driver` | Must | Persistable entities exist with basic constraints and a usable relationship from `Transport`. |
| MVP-06 | Model `ShipmentEvent` | Should | Chronological events associated with a transport can be recorded. |
| MVP-07 | Integrate PostgreSQL and initial migrations | Must | The solution creates the schema through migrations. The local PostgreSQL connection is stable and reproducible. |

### Epic 3 · API Use Cases

| ID | Item | Priority | Acceptance Criteria |
| --- | --- | --- | --- |
| MVP-08 | Transport CRUD | Must | Endpoints exist for create, detail, list, update, and logical or physical delete. Contracts return coherent HTTP codes. |
| MVP-09 | Vehicle CRUD | Must | Operational endpoints exist for create, query, edit, and delete. |
| MVP-10 | Driver CRUD | Must | Operational endpoints exist for create, query, edit, and delete. |
| MVP-11 | Assign vehicle and driver to transport | Must | Only assignments coherent with the transport state are allowed. Business errors are clear. |
| MVP-12 | Transport state transitions | Must | The `planned -> in_transit -> delivered/cancelled` flow is implemented and protected against invalid transitions. |
| MVP-13 | Logistics event registration and query | Should | An event can be recorded and the ordered history of a transport can be retrieved. |
| MVP-14 | Listings with minimum filters | Should | Listing by state, date range, and assignment is possible with basic pagination. |

### Epic 4 · Security and Contract

| ID | Item | Priority | Acceptance Criteria |
| --- | --- | --- | --- |
| MVP-15 | JWT authentication with roles `admin` and `operator` | Must | A functional login exists. Sensitive endpoints are protected by role. |
| MVP-16 | Homogeneous error handling | Must | A global middleware or equivalent exists. Error responses follow a consistent contract. |
| MVP-17 | Input validation | Must | The API rejects invalid input with clear messages and without leaving inconsistent states. |
| MVP-18 | Basic structured logging | Should | Each relevant request leaves minimum traceability with a correlation identifier or equivalent. |

### Epic 5 · Quality and Local Operation

| ID | Item | Priority | Acceptance Criteria |
| --- | --- | --- | --- |
| MVP-19 | Core unit tests | Must | Tests exist for state rules and critical validations. |
| MVP-20 | Integration tests for key endpoints | Should | Tests exist for at least the main transport and authentication flows. |
| MVP-21 | Multi-stage Dockerfile | Must | The API can be built into a Docker image in a repeatable way. |
| MVP-22 | Reproducible local environment with API and PostgreSQL | Must | A local composition with containers or an equivalent procedure exists and is clearly documented. |
| MVP-23 | Health checks | Should | Separate health endpoints exist for liveness and readiness. |

## Scope Excluded from the MVP

- Terraform and AWS infrastructure definition.
- Deployment on ECS Fargate.
- ECR, ALB, and RDS in the cloud.
- GitHub Actions with automatic deployment.
- CloudWatch, dashboards, metrics, and alarms.
- Secrets Manager or SSM.
- OpenTelemetry, rate limiting, and advanced resilience.
- Load testing, costs, ADRs, and the final technical report.
- Frontend or visual panel.

## Main Dependencies

- .NET 10 SDK.
- Local PostgreSQL or Docker container.
- Credentials and environment-specific configuration separated from the code.

## Risks to Watch Within the MVP

- Oversizing the functional model and losing time before reaching the cloud part.
- Excessively coupling controllers, logic, and persistence.
- Postponing testing and validation until the end.
- Introducing authentication too late and breaking already used contracts.

## MVP Closure Criterion

The MVP is considered complete when the system can start from scratch locally, authenticate users, manage transports with their assignments and states, persist data in PostgreSQL, run minimum tests, and be exposed in a packageable way for the later deployment phase.
