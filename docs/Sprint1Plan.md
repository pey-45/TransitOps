# Plan — Sprint 1 completo

## Contexto

TransitOps es una aplicación de gestión de transportes desarrollada recorriendo el ciclo de vida completo del software. Tras el cambio de dirección de 2026-06-19, el proyecto dejó de centrarse en una plataforma cloud para construir la aplicación completa: backend, frontend y disciplina de proceso.

Este plan cubre el Sprint 1 completo:

1. **Sprint 1 completo** — el "esqueleto andante": diseño del modelo de datos completo + arquitectura de integración, backend ASP.NET Core/.NET 10 + EF Core/PostgreSQL con arranque del primer administrador y login JWT, frontend React con login y enrutado protegido, contenerización, CI y pruebas. Cubre RF-01, RF-02 y el contrato base de RF-13 (`docs/Roadmap.md:43-63`).

Resultado esperado: una aplicación mínima real y ejecutable ---login de punta a punta, contenerizada y con la CI en verde--- sobre la que los sprints siguientes cuelgan cada rebanada de funcionalidad.

## Decisiones fijadas (confirmadas con el usuario)

- **Frontend:** Vite + React + **TypeScript**; pruebas con **Vitest + React Testing Library**.

---

## A0. Layout del repositorio (raíz, greenfield)

Se crea fresco, mirando el archivo pero sin copiarlo ni construir sobre él. Nombres alineados con `.claude/launch.json` (que ya apunta a `TransitOps.Api/TransitOps.Api.csproj` y a `docker-compose.yml` en la raíz):

```
TransitOps/
├── TransitOps.slnx                 # solución .NET
├── TransitOps.Api/                 # backend ASP.NET Core (.NET 10)
├── TransitOps.Tests/               # pruebas xUnit
├── frontend/                       # SPA Vite + React + TypeScript
├── docker-compose.yml              # servicios: db, api (web opcional)
├── .env.example                    # plantilla de config (sin secretos reales)
├── .dockerignore
├── dotnet-tools.json               # dotnet-ef como herramienta local
├── .github/workflows/ci.yml        # CI: build + pruebas back y front
└── docs/design/DataModel.md        # diseño del modelo de datos completo (versionado)
```

## A1. Diseño (entregable versionado, además del código)

Dos artefactos de diseño, versionados junto al código:

- **Modelo de datos COMPLETO** de todo el dominio de `docs/Requirements.md` — entidades `AppUser`, `Vehicle`, `Driver`, `Customer`, `Shipment`(Transport), `ShipmentEvent`, con relaciones, enums de estado, borrado lógico (`IsActive`/soft-delete) y las FK opcionales del envío (cliente, vehículo, conductor). Se documenta en `docs/design/DataModel.md` + un diagrama ER. **Novedades sin precedente en el archivo:** entidad `Customer` (RF-07) y el campo `EstimatedLoad`/carga estimada del envío (RN-05) con FK opcional envío→cliente.
- **Arquitectura de integración front/back:** SPA React contra API REST; emisión y ubicación del token JWT (recomendado: token en `localStorage` para persistir sesión en S1, documentando el tradeoff XSS y dejando cookie httpOnly como endurecimiento para S7); propagación de roles (claim `role` → navegación adaptada) y de errores (contrato JSON común RF-13); política CORS para el origen del frontend en desarrollo.

**Nota sobre migraciones (según `docs/Roadmap.md:10`):** el modelo se **diseña** completo en S1, pero la **migración inicial implementa solo la tabla de usuarios**; cada sprint posterior añade sus tablas con migraciones incrementales. DoD: "modelo completo diseñado y versionado, aunque solo esté implementada la parte de usuarios".

## A2. Backend — `TransitOps.Api/` (ASP.NET Core .NET 10 + EF Core/PostgreSQL)

Estructura por capas (misma que el oráculo, reimplementada): `Domain/` (entidades, enums, `Common/Entity`), `Application/` (interfaces de servicio), `Infrastructure/` (`Persistence/` DbContext + configuraciones + migración inicial; `Auth/AuthService`), `Controllers/`, `Contracts/` (requests/responses), `Common/` (contrato de respuesta), `Errors/` (excepciones tipadas), `Middleware/`, `Security/`, `Program.cs`, `appsettings*.json`, `Dockerfile`.

Piezas del Sprint 1 (endpoints solo para auth/salud; el resto del modelo existe en diseño, no en API):

- **Contrato común de respuestas/errores (RF-13 base):** reimplementar el patrón de `archive/.../Common/ApiResponse.cs`, `ApiErrorResponse.cs`, `ApiError.cs`; excepciones `Errors/{ApiException,ConflictException,ResourceNotFoundException,UnauthorizedException}.cs`; `Middleware/ExceptionHandlingMiddleware.cs` + `CorrelationIdMiddleware.cs`; `InvalidModelStateResponseFactory` para errores de validación (ver `archive/.../Program.cs:50-72`).
- **Persistencia:** `TransitOpsDbContext` con `IsActive`/soft-delete; migración inicial `InitialCreate` (solo tabla `AppUsers`); `TransitOpsDbContextFactory` para `dotnet ef`; flag `--migrate-only` y `Database:ApplyMigrationsOnStartup` (patrón de `archive/.../Program.cs:169-212`).
- **Arranque del primer administrador (RF-02, RN-11/RN-12):** `Security/BootstrapOptions.cs` + endpoint de bootstrap que solo funciona si no hay admin activo; hash con `PasswordHasher<AppUser>` (Identity).
- **Autenticación JWT (RF-01, RN-13):** `Security/{JwtOptions,RoleNames,AuthorizationPolicies,UserRoleExtensions}.cs`; `AddJwtBearer` con validación completa y respuestas 401/403 en el contrato JSON (patrón de `archive/.../Program.cs:106-163`); `Application/Auth/IAuthService` + `Infrastructure/Auth/AuthService` (login valida credenciales y estado activo, emite token con claims de rol); `Controllers/AuthController` (`bootstrap`, `login`) + `Controllers/HealthController`.
- **Nuevo respecto al archivo:** política **CORS** para el origen del frontend (el oráculo era backend-only, no la tenía).

## A3. Frontend — `frontend/` (Vite + React + TypeScript) — greenfield total

Sin precedente en el archivo. Estructura mínima y clara:

- Scaffold Vite (`react-ts`); estructura `src/` con `api/` (cliente REST + inyección del token + manejo del contrato de error), `auth/` (contexto de sesión, almacenamiento del token, hook `useAuth`), `routes/` (enrutado con `react-router`, rutas protegidas que redirigen a login si no autenticado), `components/` (patrón común de aviso de errores, layout con navegación **adaptada al rol**), `pages/` (`LoginPage`, `HomePage` autenticada vacía).
- Proxy de Vite (o `VITE_API_URL`) hacia la API; login llama a `/auth/login`, guarda el token y redirige; navegación muestra opciones según el rol del claim.

## A4. Contenerización y CI

- **`docker-compose.yml`:** `db` (postgres:16, puerto 5432), `api` (build de `TransitOps.Api`, puerto 8080, `ApplyMigrationsOnStartup=true` en dev, depende de `db`). Servicio `web` opcional (perfil) para el frontend; en S1 el frontend puede servirse con `npm run dev`. Alineado con `.claude/launch.json`.
- **`.env.example`:** cadena de conexión, `Jwt__SigningKey` (solo dev), `Bootstrap__*`. Sin secretos reales comprometidos (AGENTS.md).
- **`.github/workflows/ci.yml`:** job backend (`dotnet build` + `dotnet test`, con Postgres de servicio) y job frontend (`npm ci` + `npm run build` + `npm run test`). DoD: "la CI valida build + pruebas".

## A5. Pruebas

- **Backend (`TransitOps.Tests/`, xUnit):** arranque de admin (solo cuando no hay admin; bloqueo si ya existe — RN-11/12), login válido/ inválido, usuario desactivado no entra (RN-13), protección de rutas (401 sin token, 403 sin rol), forma del contrato de error (RF-13). Base de datos de test (Postgres efímero o `WebApplicationFactory`).
- **Frontend (Vitest + RTL):** render de `LoginPage`, envío de credenciales, redirección tras login, redirección a login en ruta protegida sin sesión, navegación adaptada al rol.

## Mapa del oráculo de referencia (`archive/cloud-phase/`) — consultar, no editar

| Pieza S1 | Referencia en el archivo |
| --- | --- |
| Contrato respuestas/errores | `TransitOps.Api/Common/*`, `Errors/*`, `Middleware/ExceptionHandlingMiddleware.cs` |
| JWT + roles + bootstrap | `TransitOps.Api/Security/*`, `Infrastructure/Auth/AuthService.cs`, `Controllers/AuthController.cs`, `Program.cs` |
| Modelo de datos / migración | `TransitOps.Api/Domain/*`, `Infrastructure/Persistence/*` (añadir `Customer` + carga estimada) |
| Compose / EF tools | `archive/.../docker-compose.yml`, `dotnet-tools.json`, `.env.example` |
| Frontend | Sin precedente (greenfield) |

## Definición de hecho del Sprint 1

- [x] Criterios de aceptación de RF-01 y RF-02 cumplidos.
- [x] Modelo de datos completo diseñado y versionado (solo usuarios implementado en migración).
- [x] La app se levanta localmente sin pasos manuales ocultos (`docker compose up`).
- [x] Login funcionando de punta a punta contra el backend real y a través del proxy web.
- [x] Workflow de CI definido para build, migración y pruebas de backend y frontend; el equivalente local está en verde (la ejecución remota requiere un push).

---
## Orden de ejecución

1. **Implementación primero**: modelo, backend, frontend, infraestructura y pruebas, en ese orden.
   1. Solución + backend (modelo, migración usuarios, contrato de error, bootstrap, JWT, salud).
   2. Frontend (login, rutas protegidas, navegación por rol).
   3. Compose + `.env.example` + CI + `dotnet-tools.json`.
   4. Pruebas backend y frontend.
   5. Verificación de punta a punta (ver abajo) y captura de evidencias.
2. Actualizar `CONTEXT.md` (cierre de S1: duración real, decisiones) y `docs/Roadmap.md` (nota de cierre de S1), según convención del repo.

## Verificación

**Sprint 1 (extremo a extremo):**
- Backend: `dotnet build` y `dotnet test` en verde. `docker compose up` levanta `db` + `api`; la migración se aplica sola; endpoint de salud responde.
- Flujo real: bootstrap del primer admin → falla si se repite (RN-11/12); login válido devuelve token; login inválido y usuario desactivado rechazados (RN-13); ruta protegida da 401 sin token y 403 sin rol; todo con el contrato de error JSON (RF-13).
- Frontend: `npm run build` y `npm run test` en verde; con la API arriba, servir el frontend y **conducir el login desde el navegador** (Browser MCP): navegar al login, autenticar, confirmar redirección a la zona autenticada y navegación adaptada al rol. Captura de pantalla como evidencia.
- CI: workflow ejecuta build + pruebas de ambos lados (validar localmente; verde al hacer push).

## Riesgos y notas

- **Alcance amplio:** S1 es una rebanada vertical completa (back + front + infra + CI + pruebas). Se ejecuta de forma incremental y verificable por etapas.
- **.NET 10 / preview:** confirmar SDK disponible; si falta, se ajusta el `TargetFramework` documentándolo.
- **No editar `archive/`:** es solo oráculo de consulta (AGENTS.md); todo lo nuevo vive en la raíz.
- **Sin secretos reales** en ficheros versionados; solo `.env.example` con valores de desarrollo.
