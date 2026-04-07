# TransitOps · Local Verification Guide

## Purpose

Provide one clear local verification path for the current MVP so the API, PostgreSQL, migrations, manual requests, and smoke tests can be exercised without hidden steps.

## 1. Prepare Local Docker Configuration

From the repository root:

```powershell
Copy-Item .env.example .env
```

Then edit `.env` and set:

- `TRANSITOPS_JWT_SIGNING_KEY` to a local development key with at least 32 characters.
- `TRANSITOPS_BOOTSTRAP_ADMIN_TOKEN` only if you want to call `POST /api/v1/auth/bootstrap-admin`.

## 2. Start a Fresh Local Stack

```powershell
docker compose down -v
docker compose up --build
```

Expected result:

- PostgreSQL starts healthy.
- The API starts on `http://localhost:8080`.
- Pending EF Core migrations are applied automatically by the API startup path.

## 3. Run the Automated Live Smoke Flow

This is the most reliable local verification path for the repository as it exists today:

```powershell
.\scripts\testing\postman\run_local_api_smoke.bat
```

Expected result:

- The script starts Docker if needed.
- The deterministic seed is reset.
- Newman runs the live API flow against PostgreSQL.
- Runtime and seeded data are removed on exit.

## 4. Manual Verification with the HTTP File

Use [TransitOps.Api.http](../TransitOps.Api/TransitOps.Api.http).

Recommended order:

1. On a fresh local stack, call `POST /api/v1/auth/bootstrap-admin`.
2. Then call the `POST /api/v1/auth/login` example that uses the bootstrapped admin credentials.
3. Copy the returned bearer token into `@accessToken`.
4. Exercise `/api/v1/users`, `/api/v1/transports`, `/api/v1/vehicles`, `/api/v1/drivers`, and `/api/v1/transports/{id}/shipment-events`.

Alternative path:

- If you manually apply the deterministic local sample seed first, use the seeded-admin login example instead of the bootstrap flow.

Deterministic local seed credentials:

- `seed.admin` / `SeedAdmin!123`
- `seed.operator` / `SeedOperator!123`

## 5. Manual Verification with Postman

Import:

- [TransitOps.Api.postman_collection.json](../TransitOps.Api/TransitOps.Api.postman_collection.json)
- [TransitOps.Api.manual.local.postman_environment.json](../scripts/testing/postman/environments/TransitOps.Api.manual.local.postman_environment.json)

Recommended order:

1. On a fresh local stack, run `Auth / Bootstrap First Admin`.
2. Then run `Auth / Login With Bootstrapped Admin`.
3. Verify that `accessToken` is stored in the active Postman environment.
4. Run the protected folders in order: `Users`, `Transports`, `Vehicles`, `Drivers`, `Shipment Events`.

Alternative path:

- If you manually apply the deterministic local sample seed first, skip bootstrap and run `Auth / Login With Seeded Admin` instead.

## 6. Explicit Migration Consistency Check

Run this when the model changes or before committing new persistence work:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef migrations has-pending-model-changes --project .\TransitOps.Api\TransitOps.Api.csproj --startup-project .\TransitOps.Api\TransitOps.Api.csproj
```

Expected result:

- `No changes have been made to the model since the last migration.`

## 7. Local Build/Test Baseline

```powershell
dotnet restore TransitOps.slnx
dotnet build TransitOps.slnx -c Release
dotnet test .\TransitOps.Tests\TransitOps.Tests.csproj -c Release
```

The GitHub Actions baseline mirrors this path and also validates migrations drift plus `docker compose config`.
