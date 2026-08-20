# Plan de implementación — Sprint 3 · Envíos (Alta, Edición, Listado y Filtros)

## Context

El repositorio tiene los **Sprints 1 y 2 cerrados**: esqueleto autenticado con JWT (`AppUser`, bootstrap, login, políticas por rol) y los tres catálogos base (vehículos, conductores, clientes) con CRUD, baja lógica y unicidad entre activos. Estado verificado: 29 pruebas backend y 7 frontend en verde, 2 migraciones aplicadas (`InitialCreate`, `AddCatalogTables`), stack completo en Docker Compose.

El **Sprint 3** (`docs/Roadmap.md:92-111`) implementa la rebanada vertical que da sentido a los catálogos: **gestión de envíos** (RF-08) y **visibilidad con filtros** (RF-12). Es el primer módulo del proyecto con relaciones entre entidades, con fechas de negocio y con listado paginado, así que introduce tres problemas técnicos que los catálogos no tenían: persistencia de fechas UTC contra `timestamptz`, validación cruzada entre campos (RN-06) y composición de consultas filtradas.

Entregable demostrable: crear un envío, verlo en el listado, filtrarlo por estado/fecha/vehículo/conductor y editarlo. Todo envío nace en estado `planificado`; las transiciones de estado y la asignación de recursos son Sprint 4.

## Decisiones confirmadas con el usuario

1. **Referencia introducida y única**: el operador escribe `reference`; obligatoria y única entre **todos** los envíos (sin `HasFilter`, a diferencia de los catálogos). Conflicto → 409 `shipment_reference_conflict`.
2. **Paginación dentro de `data`**: `data: { items, page, pageSize, totalCount, totalPages }`. **No se toca** `Common/ApiContracts.cs` ni el sobre común.
3. **Vehículo y conductor**: la migración crea `VehicleId`/`DriverId` y el listado filtra por ellos, pero **no hay forma de asignarlos** en S3 (llega en S4). Los tests siembran esos valores en el DbContext.

## Alcance y no-objetivos

**Dentro**: entidad `Shipment` + migración incremental; CRUD sin borrado (crear/listar/consultar/editar); validación RN-06 y enlace a cliente activo (RN-03); listado filtrado y paginado; SPA con listado+filtros+paginación, detalle y formulario; pruebas backend y frontend; actualización de `DataModel.md`, Postman, Roadmap y CONTEXT.

**Fuera**: asignación de vehículo/conductor y transiciones de estado (S4); historial de eventos (S5); administración de usuarios e indicadores (S6); baja o borrado de envíos (la retirada es `Cancelled`, un cambio de estado de S4).

---

## Backend — `TransitOps.Api/`

Se replica el patrón de vertical slice de `Features/Vehicles/` (2 archivos + controlador + entidad POCO). Referencias de estilo: `Features/Vehicles/VehicleService.cs`, `Features/Vehicles/VehicleContracts.cs`, `Controllers/VehiclesController.cs`, `Persistence/TransitOpsDbContext.cs`.

### 1. Entidad `Domain/Shipment.cs`

POCO plano al estilo `Domain/Vehicle.cs`, con `enum ShipmentStatus : short { Planned = 0, InProgress = 1, Delivered = 2, Cancelled = 3 }` en el mismo archivo (patrón de `Domain/AppUser.cs`).

Campos: `Id`, `required Reference`, `required Origin`, `required Destination`, `required DateTime PlannedPickupAt`, `DateTime? PlannedDeliveryAt`, `Guid? CustomerId`, `Customer? Customer` (navegación, ver §4), `decimal? EstimatedLoad`, `string? Notes`, `ShipmentStatus Status = Planned`, `Guid? VehicleId`, `Guid? DriverId`, `CreatedAt`, `UpdatedAt`.

**Sin `IsActive` y sin `DELETE`**: `docs/design/DataModel.md:85` establece que el ciclo del envío lo lleva `Status`, y RF-08 solo pide crear/listar/consultar/editar. Consecuencias: `IShipmentService` no tiene `DeactivateAsync`, el controlador no tiene `[HttpDelete]`, la query base no filtra por `IsActive`, y el helper `Active(id, ct)` del patrón se renombra a **`Existing(id, ct)`** (404 `shipment_not_found`, mensaje "El envío no existe." sin el "o está dado de baja").

`PlannedPickupAt` es `required` en la entidad para que ningún punto de construcción —incluidos los tests que siembran directamente— persista `default(DateTime)` con `Kind=Unspecified`.

### 2. Configuración EF inline en `Persistence/TransitOpsDbContext.cs`

`DbSet<Shipment> Shipments` + bloque `shipment` en `OnModelCreating`, mismo estilo que `vehicle`:

- `ToTable("shipments")`; longitudes coherentes con los catálogos: `Reference` 50 (como `InternalCode`), `Origin`/`Destination` 160 (como `Name`), `Notes` 500 (como `ContactDetails`); `EstimatedLoad` con `HasPrecision(12, 2)`; `Status` con `HasConversion<short>()`.
- `HasIndex(Reference).IsUnique()` **sin `HasFilter`** (unicidad global: no hay `IsActive` que filtrar). Contrasta deliberadamente con `vehicles`/`drivers`, que sí filtran.
- Índices de filtro/orden: `HasIndex(Status)`, `HasIndex(PlannedPickupAt)`. **No** declarar índices sobre las FKs: EF los crea solo.
- **Primeras relaciones del modelo** (hoy no hay ninguna): `HasOne(item => item.Customer).WithMany().HasForeignKey(item => item.CustomerId)` y `HasOne<Vehicle>().WithMany().HasForeignKey(item => item.VehicleId)` / ídem `Driver`, todas con **`OnDelete(DeleteBehavior.Restrict)`**.
- `Restrict` es la traducción de RN-15 al esquema: la BD impide borrar un catálogo con envíos. `SetNull` (lo que hacía la implementación archivada) **borraría el vínculo histórico**, justo lo contrario; el `ClientSetNull` por defecto lo haría en silencio sobre entidades rastreadas.
- Solo `Customer` lleva propiedad de navegación, porque el contrato expone `customerName` (§6); `Vehicle`/`Driver` se quedan en FK escalar hasta S4.
- Opcional recomendado: `CHECK` en tabla como tercera capa de RN-06 — `table.HasCheckConstraint("ck_shipments_planned_dates", "\"PlannedDeliveryAt\" IS NULL OR \"PlannedDeliveryAt\" >= \"PlannedPickupAt\"")`.

### 3. Migración

```bash
dotnet tool restore && dotnet ef migrations add AddShipments --project TransitOps.Api
```

`--output-dir` no debería hacer falta (EF deriva el directorio de la última migración; `docs/Sprint2Plan.md:60` documenta el mismo comando desnudo). **Verificar** que el archivo cae en `Persistence/Migrations/` y no en `Migrations/`; si no, repetir con `--output-dir Persistence/Migrations`.

Comprobar en la migración generada: fechas como `timestamp with time zone`, `Status` como `smallint`, `Reference` como `character varying(50)` con índice unique **sin `filter:`**, y las tres FKs con `onDelete: ReferentialAction.Restrict`.

### 4. Punto crítico — fechas UTC con Npgsql (lo más importante del sprint)

EF mapea `DateTime` a `timestamp with time zone` (verificado en `20260719121642_AddCatalogTables.cs:22`), y Npgsql 10.0.2 **lanza excepción** si el `Kind` no es `Utc`: `"Cannot write DateTime with Kind={0} to PostgreSQL type '{1}', only UTC is supported."`

Lo que llega según el formato de entrada (medido, no supuesto):

| Entrada | Vía | `Kind` | ¿Escribe? |
|---|---|---|---|
| `"2026-08-01T08:00:00Z"` (body) | System.Text.Json | `Utc` | Sí |
| `"2026-08-01T08:00:00"` (body, naive) | System.Text.Json | `Unspecified` | **500** |
| `"2026-08-01T10:00:00+02:00"` (body) | System.Text.Json | `Local` | **500** |
| `?pickupFrom=2026-08-01` (date-only) | `DateTimeModelBinder` | `Unspecified` | **500** |

El camino de fallo más probable **no es el POST, es el filtro del listado**: un `<input type="date"`> produce `2026-08-01`, que llega `Unspecified` y revienta al comparar contra `timestamptz`. Y `<input type="datetime-local">` produce `2026-08-01T08:00` (naive), que rompe el POST. **InMemory no reproduce ninguno de los dos**, y el CI no levanta PostgreSQL: es el escenario clásico "verde en tests, 500 en Docker".

**Solución: normalización explícita en el slice**, no value converter en EF (sería la magia implícita que prohíbe `AGENTS.md:40`, y la versión ingenua con `SpecifyKind` **corrompería** los valores `Local` desplazando el instante sin lanzar error). Helper `internal static class ShipmentTime` en `ShipmentContracts.cs`:

- `Utc` → tal cual; `Local` → `ToUniversalTime()` (conserva el instante); `Unspecified` → `SpecifyKind(value, Utc)` (contrato: naive se interpreta como UTC).
- Sobrecarga para `DateTime?`.

Tres puntos de aplicación, exhaustivos: `Normalize(request)` (junto a los `Trim()`), los dos límites del rango en `GetAllAsync` **antes** de construir el `IQueryable`, y `Validate()` de RN-06 (§5).

**No copiar** `archive/cloud-phase/.../DateTimePersistence.cs`: resolvía esto con columnas `timestamp` sin zona + `Kind=Unspecified`; aquí todo es `timestamptz` y aplicar `AsUnspecified` dispararía el error inverso sobre `CreatedAt`.

Como la normalización corre igual bajo InMemory, la invariante **sí es testeable en verde**: los tests deben afirmar `Assert.Equal(DateTimeKind.Utc, item.PlannedPickupAt.Kind)`, en los de servicio directamente y en los de controlador leyendo el DbContext vía `factory.Services.CreateScope()`. La garantía final es el paso manual con Docker de la §Verificación.

### 5. Punto crítico — validación RN-06 (entrega no anterior a recogida)

Dos capas:

- **`IValidatableObject` en `UpsertShipmentRequest`** → es la única vía para que el `InvalidModelStateResponseFactory` (`Program.cs:31-45`) produzca `validation_error` con `details["PlannedDeliveryAt"]`, que el SPA ya sabe pintar bajo el campo desde S2 vía `fieldErrors`. Cero código nuevo en frontend.
- **Guarda en el servicio** → `ApiException(400, "shipment_dates_invalid", ...)`, porque las DataAnnotations solo las aplica el pipeline MVC y el servicio se invoca directo desde los tests y desde S4.

Tres trampas a respetar:

- **Comparar `DateTime` ignora `Kind`**: `Validate()` debe normalizar con `ShipmentTime.Utc` antes de comparar, o un envío válido con `+02:00` en un campo y `Z` en el otro se rechazaría.
- **`[Required]` sobre `DateTime` no anulable nunca falla** (`0001-01-01` no es null): `PlannedPickupAt` debe ser `DateTime?` en el request, con guarda defensiva `shipment_pickup_required` en el servicio.
- **MVC cortocircuita `IValidatableObject`** si falla una DataAnnotation de propiedad: el test de RN-06 debe enviar un payload por lo demás válido.

Igualdad permitida: `entrega == recogida` es válido ("no anterior"). Documentar que **el mismo requisito produce dos códigos** según el punto de entrada: `validation_error` por HTTP, `shipment_dates_invalid` por llamada directa.

### 6. Contratos — `Features/Shipments/ShipmentContracts.cs`

- **`UpsertShipmentRequest`** (record posicional con DataAnnotations inline, molde `UpsertVehicleRequest`): `Reference` `[Required, StringLength(50), RegularExpression(@".*\S.*")]`, `Origin`/`Destination` ídem con 160, `PlannedPickupAt` `DateTime?` `[Required]`, `PlannedDeliveryAt` `DateTime?`, `CustomerId` `Guid?`, `EstimatedLoad` `[Range(0.01, 9999999999.99)]`, `Notes` `[StringLength(500)]`. Implementa `IValidatableObject`.
- **`ShipmentResponse`**: `Id, Reference, Origin, Destination, PlannedPickupAt, PlannedDeliveryAt, CustomerId, CustomerName, EstimatedLoad, Notes, Status (string), VehicleId, DriverId, CreatedAt, UpdatedAt`.
  - **`Status` es `string`, no el enum.** No hay `JsonStringEnumConverter` en el proyecto (verificado), así que un enum serializaría como número `0..3`. Se mapea a mano a `planned`/`in_progress`/`delivered`/`cancelled`, exactamente el precedente de `UserResponse.Role` → `"admin"`/`"operator"` (`AuthService.cs:76`).
  - **`CustomerName` incluido** (resuelve un conflicto entre los dos diseños en favor del frontend): `GET /api/v1/customers/{id}` es solo-activos, así que un envío histórico de un cliente dado de baja no podría mostrar su nombre sin esto, y el listado necesitaría una petición por fila. Se obtiene con `.Include(item => item.Customer)` + `item.Customer?.Name` en `Map`.
  - `VehiclePlate`/`DriverName` **no** se añaden en S3 (serían siempre null); entran en S4 con la asignación.
- **`ShipmentPageResponse(IReadOnlyList<ShipmentResponse> Items, int Page, int PageSize, int TotalCount, int TotalPages)`** — específico del slice, **no** genérico en `Common/`: la decisión 2 pide no ampliar el sobre común, y recorriendo el roadmap éste es el único listado paginado de toda la aplicación (S5 es por envío, S6 no pagina). Si S5/S6 lo necesitaran, promoverlo es mecánico.
- **`ListShipmentsQuery`**: `Status` como **`string?` con `[RegularExpression("^(planned|in_progress|delivered|cancelled)$")]`** y mensaje en español — si se declarara `ShipmentStatus?`, `?status=foo` filtraría el mensaje **en inglés** del binder, y `in_progress` **no atría** con `Enum.TryParse` (espera `InProgress`). Más `PickupFrom`/`PickupTo` (`DateTime?`), `CustomerId`/`VehicleId`/`DriverId` (`Guid?`), y `Page`/`PageSize` como **`int?`** con `[Range(1, ...)]` / `[Range(1, 100)]`, resolviendo los defectos en el servicio (`?? 1`, `?? 20`). Implementa `IValidatableObject` para rechazar rango invertido con `details["PickupTo"]`.
- `IShipmentService`: `GetAllAsync(ListShipmentsQuery, ct)`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`. Sin `DeactivateAsync`.

### 7. Servicio — `Features/Shipments/ShipmentService.cs`

`sealed class ShipmentService(TransitOpsDbContext dbContext) : IShipmentService`, lambdas con variable `item`, helpers privados en el orden del patrón:

- `Normalize`: `Reference` a `Trim().ToUpperInvariant()` (clave de negocio, como `LicensePlate`); `Origin`/`Destination`/`Notes` con `Trim()`/`Optional()`; fechas por `ShipmentTime.Utc`.
- `EnsureUnique(reference, excludedId, ct)` → 409 `shipment_reference_conflict`.
- `EnsureCustomer(customerId, ct)`: un único `AnyAsync(item => item.Id == id && item.IsActive)` → **409** `shipment_customer_not_found` ("El cliente indicado no existe o está dado de baja."). 409 y no 404 porque el recurso direccionado (`/shipments`) sí existe; lo que choca es el estado del catálogo (RN-03). Código nuevo, no se reutiliza `customer_not_found` (ya ligado a 404 en `CustomerService`).
- `Existing(id, ct)` → 404 `shipment_not_found`.
- `CreateAsync`: `Normalize` → `EnsureUnique(null)` → `EnsureCustomer` incondicional → `Status = Planned` → `Add`/`SaveChanges` → recargar navegación o mapear el nombre ya conocido.
- `UpdateAsync`: `Existing` → `Normalize` → `EnsureUnique(id)` → **`EnsureCustomer` solo si `normalized.CustomerId != item.CustomerId`** → campos + `UpdatedAt = DateTime.UtcNow` → `SaveChanges`. Nunca toca `Status`.
  - Esa condición resuelve un caso real: reenviar el mismo cliente ya dado de baja debe **permitirse** (RN-15: editar las notas de un envío histórico no puede estar bloqueado); apuntar a *otro* cliente inactivo → 409 (RN-03 habla de asignaciones **nuevas**); `null` siempre permitido.
- `GetAllAsync`: normalizar límites y resolver defectos primero; `IQueryable` con `AsNoTracking().Include(Customer)` y `Where` condicionales; **`CountAsync` antes de `Skip/Take`** y sobre la query ya filtrada; `OrderBy(PlannedPickupAt).ThenBy(Reference)` — orden **total y estable**, imprescindible con paginación (sin desempate único Postgres puede repetir u omitir filas entre páginas); `TotalPages = totalCount == 0 ? 0 : Ceiling(totalCount / (double)pageSize)`. Página fuera de rango devuelve `items` vacío, no error.
- `Map(item)` como último método, posicional y manual.

### 8. Controlador y DI

`Controllers/ShipmentsController.cs`: `[ApiController]`, `[Route("api/v1/shipments")]`, `[Authorize(Policy = Policies.Operational)]`. Cuatro acciones: `GET` (lista), `GET {id:guid}`, `POST` (`StatusCode(201, ...)`), `PUT {id:guid}`. Todo en `ApiResponse<T>.Success(data, HttpContext.TraceIdentifier)`; el 404 de `GetById` lo lanza el controlador con `?? throw`.

**`[FromQuery]` es obligatorio y explícito** en el parámetro de listado: con `[ApiController]` un tipo complejo se infiere como `[FromBody]` y el GET respondería 400 "A non-empty request body is required".

`Program.cs`: `using TransitOps.Api.Features.Shipments;` + `AddScoped<IShipmentService, ShipmentService>()` junto a la línea 61.

---

## Frontend — `frontend/src/`

Estilo minimalista existente, **cero dependencias nuevas**. Molde literal: `pages/vehicles/VehiclePages.tsx`; se reutilizan `components/CatalogUi.tsx`, `ErrorAlert.tsx` y `form-errors.ts` sin tocarlos.

### 1. `api/client.ts`

Tipos `ShipmentStatus` (unión de los 4 tokens), `Page<T>`, `Shipment`, `ShipmentInput`, `ShipmentFilters`; helper privado `query(params)` con `URLSearchParams` que omite `undefined` y `''`; cuatro funciones one-liner `listShipments`/`getShipment`/`createShipment`/`updateShipment`. **Sin `deactivateShipment`** (no hay baja en S3).

### 2. `pages/shipments/ShipmentPages.tsx`

**`ShipmentListPage`** — filtros en la URL con **`useSearchParams`** (ya disponible en react-router-dom), no en `useState`: permite recargar, compartir por enlace y usar el botón atrás; sustituye dos estados (filtros aplicados + página) y hace innecesario un `refetch()` manual. Nombres de parámetro idénticos a los de la API, con una única asimetría documentada: en la URL las fechas son **días** (`2026-08-01`) y hacia la API se expanden a **instantes**.

- Tres efectos separados: catálogos una sola vez (`Promise.all([listVehicles(), listDrivers()])` con dependencia `[]`, para no recargarlos en cada filtrado), sincronización de los borradores cuando cambia la URL, y carga de envíos dependiente de `params.toString()` con bandera `ignore` en la limpieza (evita que una respuesta lenta pise a otra; `StrictMode` duplica efectos en dev, el escenario es real).
- **Submit explícito** ("Filtrar" + "Limpiar"), no aplicar al cambio: los `<input type="date">` emiten `change` por segmento tecleado. Filtrar **no escribe `page`** → siempre vuelve a la página 1.
- Helpers `dayStart`/`dayEnd` que **deben** construir `new Date(\`${valor}T00:00\`)`: `new Date('2026-08-01')` se parsea como **UTC** por especificación mientras `'2026-08-01T00:00'` se parsea como **local**. Fin de día inclusivo (`23:59:59.999`) para que "hasta" no se coma el último día.
- Tabla: Referencia (enlace) · Estado (chip) · Origen → Destino · Recogida · Entrega · Cliente (`customerName ?? '—'`) · Editar. **Sin columnas de vehículo/conductor** en S3 (serían una columna entera de `—`); entran en S4.
- Paginación: "Página X de Y · N envíos", Anterior/Siguiente deshabilitados en los extremos (`page <= 1`, `page >= totalPages`). La página 1 no escribe `page=1` en la URL.
- `Empty` distingue "todavía no hay envíos" de "ninguno coincide con los filtros".

**`ShipmentFormPage`** — un componente para alta y edición. Conversión de fechas sin librerías, con los dos helpers exactos:

- **Al cargar**: desplazar por `getTimezoneOffset()` del propio instante (respeta horario de verano) y cortar a `YYYY-MM-DDTHH:mm`. Un `iso.slice(0,16)` directo pintaría la hora UTC como local (−2 h en verano español) y volvería a guardarla mal.
- **Al enviar**: `new Date(local).toISOString()` — la forma sin offset se parsea como local por especificación, así que el viaje de ida y vuelta es estable.
- **Trampa distinta del molde de catálogos**: `VehicleFormPage` manda `''` en los opcionales y el backend los normaliza a `null`. Aquí `''` **no es válido** para `customerId` (`Guid?`), `plannedDeliveryAt` (`DateTime?`) ni `estimatedLoad` (`decimal?`) — System.Text.Json devolvería un 400 crudo. Hay que **omitir la clave** con spread condicional, como ya hace `loadCapacity`. `origin`/`destination`/`notes` sí pueden ir como `''`.
- RN-06 se comprueba **también en cliente** antes de enviar (comparar dos cadenas `YYYY-MM-DDTHH:mm` con `<` es orden cronológico correcto) → aviso bajo el campo sin ir al servidor. El 409 de referencia duplicada se pinta en `ErrorAlert` (mensaje del backend) **y** bajo el campo Referencia, con textos **distintos** para que las aserciones no sean ambiguas.
- `<select>` de clientes activos con `<option value="">Sin cliente</option>`, más una opción extra `"{customerName} (dado de baja)"` cuando el envío apunta a un cliente que ya no está en el catálogo — si no, editar perdería la asociación en silencio.
- `loading` arranca en `true` también en el alta (a diferencia del molde), porque el `<select>` de clientes no puede renderizarse vacío y rellenarse después.

**`ShipmentDetailPage`** — `DetailList` con estado (chip), origen, destino, fechas, cliente (texto plano, **sin enlace**: `/clientes/:id` daría 404 para clientes dados de baja), carga estimada, notas, y vehículo/conductor vacíos con una nota `«Se asignarán al operar el envío (próximamente)»` — redacción de producto, sin mencionar "Sprint 4", igual que el "Usuarios (próximamente)" de `AppLayout.tsx:15`.

### 3. Rutas, navegación y estilos

- `App.tsx`: `/envios`, `/envios/nuevo`, `/envios/:id`, `/envios/:id/editar` dentro del bloque `<Route element={<AppLayout/>}>`.
- `AppLayout.tsx`: `<NavLink to="/envios">Envíos</NavLink>` justo tras "Inicio" (es el módulo operativo, va antes de los catálogos).
- `index.css`: añadir `select` al reset de tipografía de la línea 4 (hoy falta) y un bloque nuevo antes del `@media` con `select`/`select:focus`, `.filter-bar` (grid `auto-fit minmax(11rem, 1fr)`; no necesita `display: grid`, lo hereda de la regla global `form`), `.filter-actions button { margin: 0 }` (anula el `margin-top` global de `form button`), `.status-chip` + una variante por estado, `.pagination` (con `button:disabled { cursor: not-allowed }` para anular el `cursor: wait` global) y `.hint`. Paleta y radios existentes: `#175cd3`, `#c5dcff`, `#56627a`, `#d9e1ec`, `#aebbd0`, `#a51414`; `.5rem` en controles, `.75rem` en tarjetas. En el `@media` añadir `.pagination { flex-direction: column }`.

---

## Pruebas

### Backend — servicio (`TransitOps.Tests/Services/ShipmentServiceTests.cs`)

InMemory directo, molde `VehicleServiceTests.cs`, helper `CreateDatabase()` con prefijo `shipment-tests-`. Al sembrar a mano, siempre `new DateTime(..., DateTimeKind.Utc)`.

Diez tests densos: normalización + arranque en `planned` + **`Kind == Utc` partiendo de `Unspecified` y de `Local`** (el `Local` debe quedar convertido, no reetiquetado); referencia duplicada ignorando caja y espacios; RN-06 (rechazo, igualdad permitida, entrega nula); cliente inexistente/inactivo/activo; update excluye su propia referencia y preserva `Status`/`CreatedAt`; **update mantiene un cliente dado de baja pero rechaza cambiar a otro inactivo**; 404 en update y `null` en GetById; cada filtro por separado; límites de fecha inclusivos y normalizados; paginación con orden estable y totales.

### Backend — integración (`TransitOps.Tests/Controllers/ShipmentsControllerTests.cs`)

Molde `CatalogControllerTests.cs` (`FactoryWithOperator`, `AuthenticatedClient`, `ReadJson`, asserts sobre `JsonNode`). Seis tests: 401 sin token (`[Theory]`); CRUD completo con sobre común, 409 de referencia, 404 y **`DELETE` → 405** (documenta que S3 no borra); **fechas UTC para los tres formatos JSON** leyendo el DbContext vía `factory.Services`; `validation_error` con `details` por campo (incluido `PlannedDeliveryAt` en petición aparte con payload por lo demás válido); filtros y paginación desde el query string más `?status=foo` y `?pageSize=500` → 400; cliente inactivo → 409.

### Frontend (`frontend/src/App.test.tsx`)

Nuevo `describe('envíos')` en el mismo archivo, subiendo los `beforeEach`/`afterEach` actuales al nivel de archivo. **Mock despachado por URL**, no por orden de `mockResolvedValueOnce`: el listado hace 3 peticiones al montar y encadenar respuestas haría que cualquier cambio de orden rompiera tests ajenos.

Seis tests: listado paginado y navegación a la página siguiente (con `page=2` en la última URL); filtro de estado en el query string **verificando que los catálogos no se recargan**; recuperación de la vista filtrada desde la URL; alta con éxito comprobando el cuerpo enviado (`plannedPickupAt` en ISO, sin `status` ni `customerId`); conflicto 409; y RN-06 bloqueando el envío sin llamar al servidor.

Regla transversal: **ningún assert sobre fechas formateadas ni instantes hardcodeados** (`toLocaleString` depende de ICU y no hay `TZ` fija en `vite.config.ts`, así que el CI corre en UTC y la máquina en UTC+2). Los instantes esperados se calculan dentro del test con `new Date(...).toISOString()`.

---

## Documentación y cierre

1. **`docs/Sprint3Plan.md`** (nuevo, en español): mismo esqueleto que `docs/Sprint2Plan.md` — Contexto, Decisiones confirmadas, Alcance y no-objetivos, Patrones a reutilizar, Backend, Frontend, Pruebas, Verificación, Cierre, Archivos. Es el artefacto documental que el patrón del repo exige por sprint.
2. **`docs/design/DataModel.md`**: añadir `string reference` al bloque `SHIPMENT`, actualizar el "Alcance" de la línea 5 (S3 incorpora `Shipment`) y ampliar la viñeta de la línea 85 con referencia única global, ausencia de `is_active`, FKs con `RESTRICT` y fechas en UTC.
3. **`docs/Roadmap.md`**: nota `**Cierre (YYYY-MM-DD)**` tras la línea 111, con duración real, entregables y recuento de pruebas, en el formato exacto de la del Sprint 2.
4. **`CONTEXT.md`**: entrada en el Recent Decision Log, `Repository Snapshot` a "Sprints 1–3 implemented" y `Open Notes` apuntando a S4. **`README.md`** (`## Current Status`) y **`AGENTS.md:52`** al mismo estado.
5. **`postman/`**: carpeta "Envíos" en la colección con listar (con query de filtros), crear, obtener por id y actualizar, siguiendo la nomenclatura en español ya usada. Corregir `baseUrl` del entorno a `http://localhost:8080` si se valida contra Compose.

## Verificación (extremo a extremo)

1. **Backend**: `dotnet build TransitOps.slnx --configuration Release` y `dotnet test TransitOps.slnx` en verde.
2. **Migración**: generar `AddShipments`, comprobar la ruta del archivo y los tipos de columna, y validar con `dotnet run --project TransitOps.Api -- --migrate-only`.
3. **Frontend**: `npm run lint`, `npm run build` (`tsc -b` con `noUnusedLocals` y `verbatimModuleSyntax` → imports de tipo con `type` inline) y `npm run test` en verde.
4. **PostgreSQL real (paso imprescindible, no cubierto por los tests)**: `docker compose up --build` y, con token de operador, comprobar los tres casos que InMemory no detecta — POST con `"2026-08-01T08:00:00"` (naive), POST con `"+02:00"`, y `GET /api/v1/shipments?pickupFrom=2026-08-01` (date-only). Los tres deben responder 2xx, no 500.
5. **Flujo funcional en el navegador** (`http://localhost:5173`, a través de Nginx): login → Envíos → alta → aparece en el listado → filtrar por estado y por rango de fechas → paginar → abrir detalle → editar.
6. **Casos de error visibles**: referencia duplicada (409), entrega anterior a recogida (aviso bajo el campo), y cliente dado de baja aún editable.
7. **CI**: `.github/workflows/ci.yml` valida ambos lados en el push.

## Archivos

**Crear** — `TransitOps.Api/Domain/Shipment.cs`; `TransitOps.Api/Features/Shipments/{ShipmentContracts,ShipmentService}.cs`; `TransitOps.Api/Controllers/ShipmentsController.cs`; migración `Persistence/Migrations/*_AddShipments.cs` (generada); `TransitOps.Tests/Services/ShipmentServiceTests.cs`; `TransitOps.Tests/Controllers/ShipmentsControllerTests.cs`; `frontend/src/pages/shipments/ShipmentPages.tsx`; `docs/Sprint3Plan.md`.

**Editar** — `TransitOps.Api/Persistence/TransitOpsDbContext.cs`; `TransitOps.Api/Program.cs`; `frontend/src/api/client.ts`; `frontend/src/App.tsx`; `frontend/src/components/AppLayout.tsx`; `frontend/src/index.css`; `frontend/src/App.test.tsx`; `docs/design/DataModel.md`; `docs/Roadmap.md`; `CONTEXT.md`; `README.md`; `AGENTS.md`; `postman/TransitOps.Api.postman_collection.json`.

**Sin tocar** — `Common/ApiContracts.cs` (decisión 2), `components/CatalogUi.tsx`, `ErrorAlert.tsx`, `form-errors.ts`, `package.json` (cero dependencias nuevas), `archive/` (solo oráculo de consulta; su forma **no** se copia).