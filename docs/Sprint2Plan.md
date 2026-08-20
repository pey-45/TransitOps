# Plan — Sprint 2 · Catálogos (Vehículos, Conductores, Clientes)

## Contexto

El repositorio tiene el **Sprint 1 cerrado** (`docs/Roadmap.md:65`): esqueleto autenticado de punta a punta — API .NET 10 + EF Core/PostgreSQL con `AppUser`, bootstrap y JWT; SPA React/TS con login, rutas protegidas y navegación por rol; Docker Compose y CI en verde. El modelo de datos **completo** ya está diseñado y versionado en `docs/design/DataModel.md`, pero solo la tabla `app_users` está migrada.

El **Sprint 2** (`docs/Roadmap.md:68-88`) implementa la primera rebanada vertical de dominio: gestión CRUD de los tres catálogos base (**vehículos RF-05, conductores RF-06, clientes RF-07**) que después consumirán los envíos. Entregable demostrable: alta, edición, consulta, listado y baja de las tres entidades desde la interfaz, con baja lógica que preserva historial (RN-15) y unicidad de identificadores de negocio solo entre activos.

**Decisiones confirmadas con el usuario:**
- **Baja lógica solo-activos, sin reactivación**: los listados muestran solo `IsActive=true`; "dar de baja" = `IsActive=false` (fiel a RF-05/06/07; reactivación fuera de alcance).
- **Frontend minimalista** (sin librerías nuevas): se sigue el estilo actual (React manual, `fetch` nativo, CSS global) con helpers ligeros compartidos y páginas explícitas por entidad.

## Alcance y no-objetivos

**Dentro**: entidades `Vehicle`/`Driver`/`Customer` + migración; CRUD backend con unicidad y baja lógica; endpoints REST autorizados; vistas listado/detalle/alta/edición por entidad; validación cliente + avisos de conflicto (RF-13); pruebas backend (servicio + integración) y frontend; nota de cierre en Roadmap/CONTEXT.

**Fuera**: envíos y sus FKs a catálogos (Sprint 3+); reactivación de bajas; filtro "ver dados de baja"; administración de usuarios (Sprint 6).

## Patrones existentes a reutilizar (mapa verificado)

El Sprint 1 usa **vertical slices por feature**, POCO planos y config EF inline — el Sprint 2 replica ese estilo, **no** la estructura por capas del `archive/` (que es solo oráculo de consulta y usa `Entity` base, `DeletedAt`, `IEntityTypeConfiguration`, excepciones separadas — NO copiar su forma).

| Pieza | Patrón a seguir (archivo real Sprint 1) |
| --- | --- |
| Entidad POCO plana | `TransitOps.Api/Domain/AppUser.cs` (Id/IsActive/CreatedAt/UpdatedAt inline, enum en el mismo archivo) |
| Config EF inline | `TransitOps.Api/Persistence/TransitOpsDbContext.cs:10-21` (`ToTable`, `HasMaxLength`, `HasIndex().IsUnique()`) |
| Contratos + interfaz de servicio en un archivo | `TransitOps.Api/Features/Auth/AuthContracts.cs` (records con DataAnnotations + `IAuthService`) |
| Servicio | `TransitOps.Api/Features/Auth/AuthService.cs` (primary ctor, `.Trim()`, `AnyAsync`→`ApiException`, `MapUser` privado) |
| Excepción tipada única | `TransitOps.Api/Common/ApiException.cs` — `new ApiException(status, code, message)` |
| Controlador | `TransitOps.Api/Controllers/AuthController.cs` (`[ApiController]`, `[Route("api/v1/...")]`, `[Authorize(Policy=...)]`, `ApiResponse<T>.Success(data, HttpContext.TraceIdentifier)`) |
| Sobre respuesta/error | `TransitOps.Api/Common/ApiContracts.cs` |
| DI manual | `TransitOps.Api/Program.cs:54-55` (`AddScoped<IAuthService, AuthService>()`) |
| Políticas | `TransitOps.Api/Security/SecurityOptions.cs:32-36` — catálogos usan `Policies.Operational` (admin **y** operador) |
| Test de servicio (InMemory directo) | `TransitOps.Tests/Services/AuthServiceTests.cs` |
| Test de integración (WebApplicationFactory + InMemory) | `TransitOps.Tests/Controllers/AuthControllerTests.cs` + `TransitOps.Tests/Support/TransitOpsApiFactory.cs` |
| Índice único filtrado + baja lógica (referencia oráculo) | `archive/.../Configurations/VehicleConfiguration.cs:76-84`, `archive/.../Vehicles/VehicleService.cs` (adaptar a `IsActive`, no `DeletedAt`) |

**Dato crítico (tests)**: las pruebas usan **EF Core InMemory**, que **no aplica índices únicos**. La unicidad se garantiza en el **servicio** (`AnyAsync` → `ApiException(409)`), igual que `AuthService.cs:32-33`; el índice parcial de Postgres es defensa en profundidad.

---

## Backend — `TransitOps.Api/`

### 1. Entidades (`Domain/`), campos según `docs/design/DataModel.md:25-53`
POCO planos al estilo `AppUser.cs` (Id `Guid`, `IsActive=true`, `CreatedAt`/`UpdatedAt=DateTime.UtcNow`):
- `Domain/Vehicle.cs`: `LicensePlate` (req), `InternalCode?`, `Brand?`, `Model?`, `LoadCapacity?` (`decimal?`).
- `Domain/Driver.cs`: `Name` (req), `LicenseNumber` (req), `EmployeeCode?`, `ContactDetails?`.
- `Domain/Customer.cs`: `Name` (req), `ContactDetails?`.

### 2. DbContext (`Persistence/TransitOpsDbContext.cs`)
Añadir 3 `DbSet` y bloques de config **inline** en `OnModelCreating` (mismo estilo que `app_users`):
- `ToTable("vehicles"|"drivers"|"customers")`, `HasMaxLength` por columna, `HasPrecision(12,2)` en `LoadCapacity`.
- **Índices únicos parciales (solo activos)** — columnas por defecto PascalCase (la migración confirma `IsActive`, `LicensePlate`…):
  - Vehículo: `HasIndex(v => v.LicensePlate).IsUnique().HasFilter("\"IsActive\"")`; `InternalCode` igual con filtro `"\"IsActive\" AND \"InternalCode\" IS NOT NULL"`.
  - Conductor: `HasIndex(d => d.LicenseNumber).IsUnique().HasFilter("\"IsActive\"")`.
  - Cliente: sin índice de unicidad (RF-07 no exige clave de negocio).

### 3. Migración
`dotnet tool restore` y `dotnet ef migrations add AddCatalogTables --project TransitOps.Api` (usa `TransitOpsDbContextFactory`). Se aplica sola por `Database:ApplyMigrationsOnStartup`/`--migrate-only` (`Program.cs:93-98`). Una sola migración con las tres tablas.

### 4. Feature slices (una carpeta por entidad, molde `Features/Auth/`)
Por cada entidad, dos archivos:
- `Features/Vehicles/VehicleContracts.cs`: `UpsertVehicleRequest` (record con `[Required]`/`[StringLength]`/`[Range]`), `VehicleResponse` (record), `IVehicleService` (GetAll, GetById, Create, Update, Deactivate).
- `Features/Vehicles/VehicleService.cs`: `sealed class VehicleService(TransitOpsDbContext dbContext) : IVehicleService`:
  - `GetAllAsync`: `Where(IsActive)`, `OrderBy`, `AsNoTracking`, proyecta a `VehicleResponse`.
  - `GetByIdAsync`: activo o `null`.
  - `CreateAsync`: normaliza (`.Trim()`, opcionales vacíos→`null`), unicidad vía `AnyAsync` → `ApiException(409, "vehicle_plate_conflict", …)` / `"vehicle_internal_code_conflict"`, `Add`, `SaveChanges`, map.
  - `UpdateAsync`: carga activo o `ApiException(404, "vehicle_not_found", …)`; unicidad excluyendo el propio Id; set campos + **`UpdatedAt = DateTime.UtcNow`** (no hay override de SaveChanges); `SaveChanges`.
  - `DeactivateAsync`: carga activo o 404; `IsActive=false` + `UpdatedAt`; `SaveChanges` (NO borra fila → preserva historial, RN-15).
- Análogo para `Features/Drivers/` (`driver_not_found`, `driver_license_conflict`) y `Features/Customers/` (`customer_not_found`, sin conflicto de unicidad).

### 5. Controladores (`Controllers/`, molde `AuthController.cs`)
`VehiclesController` / `DriversController` / `CustomersController`: `[ApiController]`, `[Route("api/v1/vehicles")]`, `[Authorize(Policy = Policies.Operational)]` a nivel de clase. Endpoints: `GET` (lista), `GET {id}` (404 si no activo), `POST` (`StatusCode(201, …)`), `PUT {id}`, `DELETE {id}` (baja lógica). Todo envuelto en `ApiResponse<T>.Success(data, HttpContext.TraceIdentifier)`.

### 6. DI (`Program.cs:55`)
Registrar junto a `IAuthService`: `AddScoped<IVehicleService, VehicleService>()`, `IDriverService`, `ICustomerService`.

---

## Frontend — `frontend/src/`

### 1. Capa API (`api/client.ts`) — cerrar el hueco del token autenticado
Hoy `login` es la única llamada y **nunca envía `Authorization`** (el token se guarda pero no se reenvía). Añadir:
- `getAccessToken()`: lee `localStorage['transitops.session']` (misma `STORAGE_KEY` que `auth/AuthContext.tsx:5`) y devuelve `session.accessToken` (documentar el acoplamiento).
- `request<T>(path, options)`: helper genérico que inyecta `Authorization: Bearer` + `Content-Type`, parsea el sobre y lanza `ApiClientError` con `code`, `message` y **`details`** (extender `ApiClientError` para transportar `error.details`, hoy ignorado — necesario para errores por campo).
- Tipos de dominio (`Vehicle`, `Driver`, `Customer` + payloads de alta/edición) y funciones tipadas por entidad: `listVehicles`, `getVehicle`, `createVehicle`, `updateVehicle`, `deactivateVehicle` (y equivalentes drivers/customers), todas sobre `request`.

### 2. Vistas (páginas explícitas por entidad, molde `pages/LoginPage.tsx`)
Por entidad (p. ej. `pages/vehicles/`): **ListPage** (tabla de activos + botón "Nuevo" + acciones editar/dar de baja), **DetailPage** (ficha de solo lectura), **FormPage** (alta y edición compartidas). Patrón de formulario: inputs controlados con `useState`, estado `pending`, `error` string + `ErrorAlert` (`components/ErrorAlert.tsx`), errores por campo desde `details`, `try/catch/finally` con `reason instanceof ApiClientError`, avisos de conflicto (409) legibles.

### 3. Piezas compartidas ligeras
- Un helper/patrón de **campo de formulario con error** (muestra `details[campo]`) y una **tabla** simple; clases nuevas en `src/index.css` (reutilizando tokens y `.alert`/`.secondary` existentes).
- Sin librerías de formularios/validación/data-fetching (decisión confirmada).

### 4. Rutas y navegación
- `App.tsx`: añadir rutas hermanas de `HomePage` dentro del bloque `<Route element={<AppLayout/>}>` (`App.tsx:13-15`): `/vehiculos`, `/vehiculos/nuevo`, `/vehiculos/:id`, `/vehiculos/:id/editar`; ídem `/conductores`, `/clientes`. Heredan guardia + layout.
- `components/AppLayout.tsx`: añadir entradas de menú **incondicionales** (ambos roles) para "Vehículos/Conductores/Clientes" en el `<nav>` (`AppLayout.tsx:10-13`), junto a "Inicio" — sin la guardia `role==='admin'`. Migrar los `<a href>` a `<Link>` de react-router para evitar recarga completa.

---

## Pruebas

**Backend — servicio (InMemory directo, molde `AuthServiceTests.cs`)** por entidad: alta normaliza/recorta; matrícula/carné duplicado entre activos → `ApiException` 409 con code correcto; `InternalCode` único solo si se aporta; update excluye el propio Id; no encontrado → 404; **baja lógica** retira del listado pero conserva la fila (consulta directa al `DbSet`).

**Backend — integración (WebApplicationFactory + InMemory, molde `AuthControllerTests.cs`)**: sembrar operador con `TransitOpsApiFactory.CreateUser`, login, Bearer; recorrer CRUD verificando sobre `data`/`requestId`; `401` sin token; `validation_error` con `details` al enviar payload inválido; `409` en conflicto de unicidad.

**Frontend (Vitest + RTL, molde `App.test.tsx`)**: `vi.stubGlobal('fetch', …)` devolviendo el sobre `{data|error, requestId}`; sesión sembrada en `localStorage`; probar render de listado, alta con éxito (redirige/aparece), y aviso de conflicto (409 → mensaje visible).

---

## Verificación (extremo a extremo)

1. **Backend**: `dotnet build` y `dotnet test` en verde. Generar la migración; `docker compose up --build` la aplica sola; comprobar endpoints con token (bootstrap admin → login → `POST/GET/PUT/DELETE /api/v1/vehicles`), confirmando sobre común, 201/200/404/409 y que la baja no borra fila.
2. **Frontend**: `npm run lint`, `npm run build`, `npm run test` en verde. Con el stack arriba (`localhost:5173`), conducir el flujo real: login → menú Vehículos → alta → aparece en listado → edición → baja (desaparece del listado). Repetir spot-check en conductores y clientes.
3. **Full stack**: Docker Compose (ya operativo) sirviendo web+api+db; captura opcional como evidencia.
4. **CI**: el workflow existente (`.github/workflows/ci.yml`) valida build+test de ambos lados en el push.

## Cierre (al terminar, dentro de esta tarea)

- Nota de cierre del Sprint 2 en `docs/Roadmap.md` (una línea con duración real, convención `docs/Roadmap.md:213-215`) y actualización de `CONTEXT.md`.
- Commit + push de la rebanada (código + migración + pruebas).

## Archivos representativos a crear/editar

**Crear**: `Domain/{Vehicle,Driver,Customer}.cs`; `Features/Vehicles/{VehicleContracts,VehicleService}.cs` (+ Drivers, Customers); `Controllers/{Vehicles,Drivers,Customers}Controller.cs`; `Persistence/Migrations/*_AddCatalogTables.cs` (generada); `TransitOps.Tests/Services/{Vehicle,Driver,Customer}ServiceTests.cs`; `TransitOps.Tests/Controllers/{Vehicles,Drivers,Customers}ControllerTests.cs`; `frontend/src/pages/{vehicles,drivers,customers}/*`.
**Editar**: `Persistence/TransitOpsDbContext.cs` (DbSets + config), `Program.cs` (DI), `frontend/src/api/client.ts` (helper autenticado + tipos + CRUD), `frontend/src/App.tsx` (rutas), `frontend/src/components/AppLayout.tsx` (nav), `frontend/src/index.css` (estilos tabla/form), `frontend/src/App.test.tsx` o nuevos tests.