# TransitOps · Software Requirements Specification

## Purpose

Define the current functional and non-functional requirements for TransitOps. This document is the canonical specification for scope, behavior, permissions, and acceptance criteria.

## Planning Context

- Reference date: March 28, 2026.
- Product type: backend-only transport management API.
- Primary objective: deliver a small but credible backend that can reach AWS without inflating functional scope.
- Current baseline already completed: solution bootstrap, PostgreSQL schema, EF Core wiring, health endpoints, initial documentation, and baseline transport state rules.

## Product Goal

TransitOps must allow a small operations team to manage transports, vehicles, drivers, and shipment events through a secured API, while keeping the system simple enough to package, deploy, and defend technically in a cloud-oriented academic/professional context.

## Scope Statement

### In Scope

- Transport, vehicle, and driver operational management.
- Assignments between transports, vehicles, and drivers.
- Transport lifecycle transitions.
- Shipment event registration and history.
- Basic user management with `admin` and `operator` roles.
- JWT-based authentication and role-based authorization.
- PostgreSQL persistence through EF Core.
- Docker-based local reproducibility.
- Minimal CI and a first AWS deployment path.

### Out of Scope for the Current Phase

- Frontend or administration panel.
- Self-service registration, forgot-password, or password reset flows.
- Route optimization, billing, invoicing, or advanced fleet management.
- Multi-region deployment, autoscaling policy tuning, or microservice decomposition.
- Advanced observability beyond the minimum needed to operate and defend the project.

## Actors

- `Unauthenticated caller`: can reach public health endpoints and the login entry point.
- `Operator`: performs day-to-day operational work on transports, assignments, and shipment events.
- `Admin`: can perform all operator actions and is the only actor allowed to manage users.
- `Platform`: Docker, CI, ECS, ALB, and related deployment/runtime tooling interact with the service through startup, health, and deployment contracts.

## Domain Scope Summary

| Entity | Purpose | Main Fields / Notes |
| --- | --- | --- |
| `AppUser` | Authenticated API user | `username`, `email`, `password_hash`, `user_role`, `is_active`, audit fields, logical deletion |
| `Transport` | Main operational record | `reference`, origin/destination, planned and actual dates, status, optional vehicle and driver assignment |
| `Vehicle` | Assignable transport resource | `plate_number`, optional internal code, optional brand/model, optional capacities, active flag, audit fields |
| `Driver` | Assignable human resource | names, `license_number`, optional employee code/contact data, active flag, audit fields |
| `ShipmentEvent` | Chronological operational event | `transport_id`, `created_by_user_id`, `event_type`, `event_date`, optional location/notes |

## Role and Permission Model

| Capability | Unauthenticated | Operator | Admin | Notes |
| --- | --- | --- | --- | --- |
| `GET /api/v1/health/live` and `GET /api/v1/health/ready` | Yes | Yes | Yes | Public operational endpoints |
| Bootstrap first admin | No | No | No | One-time controlled setup mechanism outside normal API use |
| Login | Yes | Yes | Yes | Public endpoint, valid credentials required |
| List/detail users | No | No | Yes | Admin-only |
| Create users | No | No | Yes | Admin-only |
| Change user role | No | No | Yes | Admin-only |
| Activate/deactivate users | No | No | Yes | Admin-only |
| Transport CRUD | No | Yes | Yes | Protected operational endpoints |
| Vehicle CRUD | No | Yes | Yes | Protected operational endpoints |
| Driver CRUD | No | Yes | Yes | Protected operational endpoints |
| Assign vehicle and driver | No | Yes | Yes | Protected operational endpoint |
| Transition transport status | No | Yes | Yes | Protected operational endpoint |
| Create shipment event | No | Yes | Yes | Protected operational endpoint |
| Read shipment event history | No | Yes | Yes | Protected operational endpoint |

`Admin` inherits all `operator` permissions. `Operator` is intentionally limited to operational work and cannot manage users or privileged setup flows.

## Functional Requirements Overview

| ID | Requirement | Priority | Current Status |
| --- | --- | --- | --- |
| FR-01 | Health and platform endpoints | Must | Completed baseline |
| FR-02 | First admin bootstrap | Must | Pending |
| FR-03 | User administration | Must | Pending |
| FR-04 | Authentication | Must | Pending |
| FR-05 | Authorization | Must | Pending |
| FR-06 | Transport management | Must | Partial |
| FR-07 | Vehicle management | Must | Partial |
| FR-08 | Driver management | Must | Partial |
| FR-09 | Assignment workflow | Must | Pending |
| FR-10 | Transport lifecycle | Must | Partial |
| FR-11 | Shipment events | Must | Partial |
| FR-12 | Listings and filters | Should | Pending |
| FR-13 | Validation, response contract, and conflicts | Must | Partial |
| FR-14 | Audit trail and logical deletion | Must | Partial |

## Detailed Functional Specification

### FR-01 · Health and Platform Endpoints

The API shall expose:

- `GET /api/v1/health/live` for process liveness.
- `GET /api/v1/health/ready` for dependency readiness.

Acceptance criteria:

- Liveness returns `200`.
- Readiness returns `200` when PostgreSQL connectivity is available.
- Readiness returns `503` when PostgreSQL connectivity is unavailable.

### FR-02 · First Admin Bootstrap

The system shall provide a documented mechanism to create the first active `admin` user.

Behavior:

- The bootstrap path is outside normal user administration and may be implemented through seed data, a script, or a controlled setup procedure.
- The bootstrap path must only succeed when no active, non-deleted admin user already exists.
- The bootstrap process must not require committed secrets or hardcoded credentials in repository code.

Acceptance criteria:

- Local setup documentation explains how to obtain the first admin user.
- Cloud setup documentation explains the equivalent first-admin path.
- Re-running the bootstrap path after an admin already exists is rejected or becomes a no-op with clear behavior.

### FR-03 · User Administration

The system shall provide basic user administration for admins.

Behavior:

- `Admin` can list active users and inspect user detail.
- `Admin` can create new users with `username`, `email`, `password`, and role `admin` or `operator`.
- `Admin` can change a user's role.
- `Admin` can activate or deactivate a user.
- Password hashes are stored, but never returned in response payloads.
- Self-service registration and forgot-password flows are out of scope.

Acceptance criteria:

- Only admins can reach user-administration endpoints.
- Username and email must be unique among active, non-deleted users.
- Inactive or deleted users cannot authenticate.
- The system prevents deactivation of the last active admin user.

### FR-04 · Authentication

The system shall authenticate users through JWT.

Behavior:

- Login uses `username` and `password` in the MVP.
- Successful login returns a JWT that contains enough claims to identify the user and enforce role-based access.
- Invalid credentials return `401`.
- Deleted or inactive users are rejected even if credentials otherwise match.

Acceptance criteria:

- An active user can obtain a valid token.
- Invalid credentials return a coherent error response.
- Token generation is based on externalized configuration, not hardcoded secrets.

### FR-05 · Authorization

The system shall authorize protected endpoints using the `admin` and `operator` roles.

Behavior:

- All business endpoints are protected.
- `Admin` can perform all operator actions plus user administration.
- `Operator` can perform operational actions only.

Acceptance criteria:

- Public endpoints remain limited to health and login.
- A request with missing or invalid token is rejected.
- A request from an authenticated user without sufficient role is rejected with a coherent authorization error.

### FR-06 · Transport Management

The system shall manage transports as the main operational entity.

Behavior:

- Create, list, detail, update, and logical delete shall be supported.
- A transport includes at least: `reference`, `origin`, `destination`, `planned_pickup_at`, optional `planned_delivery_at`, optional `description`, status, optional assigned vehicle, optional assigned driver, and audit data.
- A new transport starts in `planned`.
- Generic transport update is separate from assignment and lifecycle actions.

Acceptance criteria:

- `reference` is unique among active, non-deleted transports.
- Deleted transports do not appear in active listings.
- `planned_delivery_at` cannot be earlier than `planned_pickup_at`.

### FR-07 · Vehicle Management

The system shall manage vehicles as assignable resources.

Behavior:

- Create, list, detail, update, and logical delete shall be supported.
- Vehicles include at least `plate_number`, optional `internal_code`, optional `brand`, optional `model`, optional capacities, active flag, and audit data.

Acceptance criteria:

- `plate_number` is unique among active, non-deleted vehicles.
- Optional `internal_code` is unique among active, non-deleted vehicles when present.
- Capacity fields cannot be negative.

### FR-08 · Driver Management

The system shall manage drivers as assignable resources.

Behavior:

- Create, list, detail, update, and logical delete shall be supported.
- Drivers include at least name fields, `license_number`, optional `employee_code`, optional expiry/contact data, active flag, and audit data.

Acceptance criteria:

- `license_number` is unique among active, non-deleted drivers.
- Optional `employee_code` and optional `email` are unique among active, non-deleted drivers when present.

### FR-09 · Assignment Workflow

The system shall assign a vehicle and a driver to a transport through an explicit operational action.

Behavior:

- Assignment is allowed only while the transport is in `planned`.
- The target vehicle and driver must exist, be active, and not be deleted.
- The assignment action in the MVP handles vehicle and driver together to keep the rule set simple.
- Reassignment is allowed only while the transport remains in `planned`.

Acceptance criteria:

- Invalid assignments are rejected with a clear business error.
- Successful assignment updates the transport record without bypassing lifecycle rules.

### FR-10 · Transport Lifecycle

The system shall manage transport state transitions through explicit business rules.

Supported transitions:

- `planned -> in_transit`
- `planned -> cancelled`
- `in_transit -> delivered`
- `in_transit -> cancelled`

Behavior:

- `delivered` and `cancelled` are terminal.
- Transition to `in_transit` requires an assigned vehicle and driver.
- Transition to `in_transit` should capture the actual pickup timestamp.
- Transition to `delivered` should capture the actual delivery timestamp.

Acceptance criteria:

- Invalid transitions are rejected with a coherent business error.
- Delivered or cancelled transports cannot be moved back into an active operational state.

### FR-11 · Shipment Events

The system shall register and query shipment events attached to a transport.

Behavior:

- Event creation is available to authenticated admins and operators.
- Each event stores `transport_id`, `created_by_user_id`, `event_type`, `event_date`, and optional `location` and `notes`.
- Supported event types in the MVP are `created`, `assigned`, `departed`, `checkpoint`, `incident`, `delivered`, and `cancelled`.
- Event history is retrieved by transport and returned chronologically.

Acceptance criteria:

- Event creation fails if the target transport does not exist or is deleted.
- Event history is ordered by `event_date`.
- The actor who created the event is persisted for traceability.

### FR-12 · Listings and Filters

The system shall support useful operational queries, not only raw CRUD listings.

Behavior:

- Transport list supports filters by status, planned date range, assigned vehicle, and assigned driver.
- Transport list supports basic pagination.
- Active lists exclude logically deleted rows by default.

Acceptance criteria:

- Filtered transport listing can support the demo and minimum operational use.
- Pagination parameters are validated and behave predictably.

### FR-13 · Validation, Response Contract, and Conflicts

The system shall expose a consistent API contract for success and failure cases.

Behavior:

- Successful responses use the common API response envelope.
- Error responses use the common API error envelope with a machine-readable code.
- The API distinguishes validation errors, authentication failures, authorization failures, not found cases, and business conflicts.

Acceptance criteria:

- Validation errors return `400`.
- Missing or invalid authentication returns `401`.
- Forbidden role access returns `403`.
- Missing resource returns `404`.
- Business or uniqueness conflict returns `409`.

### FR-14 · Audit Trail and Logical Deletion

The system shall preserve operational traceability through audit metadata and soft deletion.

Behavior:

- Primary operational entities and users keep `created_at`, `updated_at`, and `deleted_at`.
- Shipment events keep `created_at` and `created_by_user_id`.
- Logical deletion hides active rows without physically removing historical references.

Acceptance criteria:

- Active-row uniqueness rules are compatible with soft delete.
- Existing references to users or transports remain valid for historical records.

## Main Business Flows

### Flow 1 · Bootstrap First Admin

1. System checks whether an active admin already exists.
2. If none exists, the bootstrap mechanism creates the first admin user.
3. Credentials or access path are communicated through the documented setup procedure.

### Flow 2 · Admin Creates an Operator

1. Admin authenticates.
2. Admin creates a new user with role `operator`.
3. Operator credentials become usable through the login endpoint.

### Flow 3 · Operator Executes a Transport

1. Operator authenticates.
2. Operator creates a transport.
3. Operator creates or reuses a vehicle and a driver.
4. Operator assigns the vehicle and driver to the planned transport.
5. Operator transitions the transport to `in_transit`.
6. Operator records shipment events as needed.
7. Operator transitions the transport to `delivered` or `cancelled`.

### Flow 4 · Admin Deactivates a User

1. Admin authenticates.
2. Admin marks a target user as inactive.
3. The deactivated user can no longer log in.
4. Historical records remain preserved.

## Business Rules

- BR-01: A transport can have at most one assigned vehicle and one assigned driver at a time.
- BR-02: Assignment is valid only while the transport is in `planned`.
- BR-03: Partial assignment is not supported in the MVP assignment action; vehicle and driver are managed together.
- BR-04: Only active, non-deleted users, vehicles, and drivers may participate in operational flows.
- BR-05: `planned_delivery_at` cannot be earlier than `planned_pickup_at`.
- BR-06: `actual_delivery_at` cannot be earlier than `actual_pickup_at`.
- BR-07: Transition to `in_transit` requires a valid assignment.
- BR-08: `delivered` and `cancelled` are terminal states.
- BR-09: Username and email must be unique among active, non-deleted users.
- BR-10: Inactive or deleted users cannot authenticate.
- BR-11: Only admins can manage users.
- BR-12: The system must always preserve at least one active admin user.
- BR-13: Shipment events belong to exactly one transport and keep the creating user reference.
- BR-14: Business identifiers only need to be unique among active rows, not among logically deleted rows.

## Non-Functional Requirements

| ID | Requirement | Priority | Current Status | Acceptance Summary |
| --- | --- | --- | --- | --- |
| NFR-01 | Scope discipline and simplicity | Must | Completed baseline | The solution remains a small modular monolith with only justified projects, folders, and abstractions. |
| NFR-02 | PostgreSQL as system of record | Must | Partial | PostgreSQL remains the single persistent store, and persistence behavior is consistent between schema, EF Core, and runtime configuration. |
| NFR-03 | Reproducible local execution | Must | Partial | Another developer can start the API and PostgreSQL locally with documented commands and without hidden manual steps. |
| NFR-04 | Cloud deployability | Must | Partial | The API is stateless, containerized, externally configurable, and compatible with ECS, ALB, and RDS. |
| NFR-05 | Security baseline | Must | Pending | Passwords are stored hashed, JWT secrets are externalized, roles are enforced, and no secrets are committed to the repository. |
| NFR-06 | Reliability and controlled failure | Must | Partial | The service fails clearly when dependencies are unavailable, and the database migration/bootstrap strategy is explicit before cloud rollout. |
| NFR-07 | Maintainability and documentation | Must | Partial | Folder responsibilities stay clear, docs remain aligned with reality, and planning artifacts stay up to date. |
| NFR-08 | Testability and CI | Must | Partial | Core rules have automated tests, key flows are covered by integration tests, and build/test can run locally and in CI. |
| NFR-09 | Observability | Should | Pending | Logs are structured, request correlation is available, and runtime output is usable from CloudWatch. |
| NFR-10 | Small-scale performance | Should | Partial | Core list/detail flows are supported by appropriate indexes, pagination, and reasonable query patterns for an academic workload. |
| NFR-11 | Infrastructure as code and controlled delivery | Must | Pending | AWS infrastructure is versioned in Terraform, and the delivery path is repeatable and reviewable through CI/CD. |

## Delivery Gates

### Gate A · Local MVP Ready

The local MVP is considered ready when `FR-02` through `FR-11`, `FR-13`, `FR-14`, and `NFR-02` through `NFR-08` are complete.

### Gate B · Cloud Deployment Ready

The cloud deployment phase is considered ready for defense when `NFR-04`, `NFR-05`, `NFR-09`, and `NFR-11` are complete and the API is reachable on AWS with working health checks.
