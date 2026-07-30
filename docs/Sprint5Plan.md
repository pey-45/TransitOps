# Plan de implementación — Sprint 5 · Trazabilidad (Historial de Eventos)

## Contexto

Este plan asume los **Sprints 1–4 cerrados**: esqueleto autenticado con JWT, los tres catálogos con baja lógica, envíos con alta/edición/listado filtrado, y la operación del envío (asignación con RN-01..RN-05 y ciclo de estados con RN-07/RN-08, más las fechas reales `ActualPickupAt`/`ActualDeliveryAt`). Si S4 aún no está cerrado al empezar, este plan se ejecuta después: sus dos puntos más delicados dependen directamente de código que S4 introduce.

El **Sprint 5** (`docs/Roadmap.md:137-155`) implementa **RF-11, el historial de eventos del envío**: la última pieza de prioridad alta y, según la entrevista, «casi tan imprescindible» como el resto (`docs/ClientRequirements.md:99`). Es la respuesta al segundo de los tres problemas que la clienta describe: «no queda constancia clara de qué ha pasado con un envío si hay una incidencia» (`docs/ClientRequirements.md:28`).

Es el primer sprint con dos características nuevas para el proyecto:

- **Es la primera entidad hija.** `ShipmentEvent` no es un agregado independiente: no tiene listado global ni URL propia de primer nivel, y solo existe colgando de un envío. Eso cambia la forma de la API (sub-recurso anidado) y del servicio (todo método recibe el `shipmentId`).
- **Es el primer requisito que necesita saber *quién* está haciendo la petición.** RN-09 exige que cada evento quede vinculado a quien lo registró. Hoy **ningún servicio del proyecto lee la identidad**: los cuatro servicios reciben solo `TransitOpsDbContext` y el `ClaimsPrincipal` no sale nunca de los controladores (`AuthController.cs:36` es el único sitio que lo toca). Resolver eso limpiamente es el trabajo de diseño central del sprint (§4).

Entregable demostrable: registrar eventos sobre un envío (punto de control, incidencia…) y ver su historial ordenado, con quién lo registró, incluyendo los eventos que el sistema anotó por su cuenta al crear, asignar y mover el envío.

## Decisiones confirmadas con el usuario

1. **Eventos automáticos y manuales.** El backend registra por su cuenta creación, asignación, salida, entrega y cancelación desde los servicios de S3/S4; el operador añade a mano puntos de control e incidencias. Es lo que pide la entrevista literalmente (`docs/ClientRequirements.md:68`) y lo que evita que el historial dependa de que alguien se acuerde de escribirlo — el problema original de la clienta.
2. **Historial inmutable**: solo alta y consulta. Ni `PUT` ni `DELETE`, ni para los eventos manuales. Coincide con lo que `docs/design/DataModel.md` ya documenta («historial inmutable del envío») y es lo que da valor probatorio a la traza.
3. **`occurredAt` lo indica el operador** (por defecto «ahora»), separado del `createdAt` de auditoría. Refleja la realidad de que una incidencia de la mañana se anota por la tarde. Añade validación de fecha futura (§6).

## Alcance y no-objetivos

**Dentro**: entidad `ShipmentEvent` + migración incremental; captura de la identidad del usuario autenticado en la capa de servicio; alta y consulta de eventos por envío; registro automático desde `CreateAsync`, `AssignAsync`, `UnassignAsync` y `ChangeStatusAsync`; línea de tiempo en el detalle del envío con formulario de alta sin recargar; pruebas backend y frontend; actualización de `DataModel.md`, Postman, Roadmap, CONTEXT, README, AGENTS y memoria LaTeX.

**Fuera**: administración de usuarios, cambio de contraseña e indicadores (S6) — incluido el **recuento de incidencias de RF-14**, que consumirá estos eventos pero se calcula en S6; edición y borrado de eventos (decisión 2); adjuntos o fotos en un evento (no está en los requisitos); notificaciones; paginación del historial (§7).

**No-objetivo deliberado**: no se retiran ni se duplican las fechas reales que S4 sella en `Shipment`. `ActualPickupAt` sigue siendo el dato consultable del envío; el evento de salida es la traza de *quién* lo registró y *cuándo*. Son dos cosas distintas que coinciden en el tiempo, y unificarlas obligaría a leer el historial para pintar el detalle.

---

## Backend — `TransitOps.Api/`

### 1. Entidad — `Domain/ShipmentEvent.cs`

Archivo nuevo, con el enum en el mismo archivo (patrón de `Domain/Shipment.cs`):

```csharp
public enum ShipmentEventType : short
{
    Created = 0, Assigned = 1, Unassigned = 2, Departed = 3,
    Checkpoint = 4, Incident = 5, Delivered = 6, Cancelled = 7
}
```

Los ocho tipos cubren la lista de la entrevista (`creación, asignación, salida, punto de control, incidencia, entrega, cancelación`) más `Unassigned`, que S4 hizo posible al dar a la retirada de asignación su propio verbo. Los valores se fijan explícitamente porque persisten como `smallint`: **reordenar el enum después sería un cambio de datos silencioso**.

```csharp
public sealed class ShipmentEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required Guid ShipmentId { get; set; }
    public Shipment? Shipment { get; set; }
    public required ShipmentEventType EventType { get; set; }
    public required DateTime OccurredAt { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
    public Guid? RecordedByUserId { get; set; }
    public AppUser? RecordedByUser { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

Dos decisiones que hay que argumentar porque no son obvias:

- **`RecordedByUserId` es `Guid?`, no obligatorio**, aunque RN-09 diga «siempre vinculado a quien lo registró». La razón: `AppUser` tiene baja lógica pero **también podría borrarse** en el futuro, y sobre todo los eventos automáticos se disparan desde un servicio que en los **tests unitarios se invoca sin identidad**. Hacerlo obligatorio forzaría a inventar un usuario en cada test de `ShipmentServiceTests`. La regla se cumple en la práctica porque todos los endpoints están tras `[Authorize]`; el `null` queda reservado para trazas de sistema y así se documenta.
- **`OccurredAt` y `CreatedAt` son campos distintos y ambos se guardan.** `OccurredAt` es cuándo pasó (lo dice el operador, decisión 3), `CreatedAt` es cuándo se anotó (auditoría). Coinciden en los eventos automáticos y divergen cuando alguien registra una incidencia a posteriori. Colapsarlos haría imposible detectar un registro tardío, que es información real.

### 2. Configuración EF — `Persistence/TransitOpsDbContext.cs`

`DbSet<ShipmentEvent> ShipmentEvents` más un bloque `shipmentEvent` al final de `OnModelCreating`, mismo estilo que los anteriores:

- `ToTable("shipment_events")`; `Location` 160 (como `Origin`/`Destination`), `Notes` 500 (como el de `Shipment`); `EventType` con `HasConversion<short>()`.
- `HasIndex(item => new { item.ShipmentId, item.OccurredAt })` — índice **compuesto**, no dos sueltos. Toda consulta del historial es «los eventos de este envío ordenados por fecha», exactamente el patrón que un compuesto `(ShipmentId, OccurredAt)` sirve entero. Es el primer índice compuesto del proyecto y merece la línea de justificación.
- `HasIndex(item => item.EventType)` — lo pedirá RF-14 en S6 para contar incidencias. Se añade ahora porque cuesta una línea y la migración ya se está generando; anotarlo como preparación de S6 en el plan es más honesto que fingir que S5 lo necesita.
- **Relación con `Shipment`: `OnDelete(DeleteBehavior.Cascade)`.** Es la **única excepción** al `Restrict` que S3 fijó para todo el modelo, y por eso hay que justificarla: el evento es parte del agregado del envío, no una entidad con vida propia. Si un envío se borrara (hoy no hay `DELETE`, y S3/S4 decidieron que la retirada es `Cancelled`), sus eventos no tendrían ningún sentido conservados. `Restrict` aquí solo produciría un envío imborrable con un error de FK incomprensible.
- **Relación con `AppUser`: `OnDelete(DeleteBehavior.Restrict)`**, coherente con el resto. Un usuario con eventos registrados no se puede borrar; RN-13 ya establece que los usuarios se desactivan, no se borran.
- **Navegación `Shipment` declarada pero sin `WithMany` en la contraparte.** Es decir, `HasOne(item => item.Shipment).WithMany().HasForeignKey(item => item.ShipmentId)`: **no** se añade `ICollection<ShipmentEvent> Events` a `Shipment`. Razón: una colección en el agregado invita a `Include(s => s.Events)` en el listado de envíos, que traería el historial completo de cada fila. El historial se pide por su propio endpoint (§5), siempre explícitamente.

### 3. Migración

```bash
dotnet tool restore && dotnet ef migrations add AddShipmentEvents --project TransitOps.Api
```

Verificar que el archivo cae en `Persistence/Migrations/` y que la migración contiene: `OccurredAt`/`CreatedAt` como `timestamp with time zone`, `EventType` como `smallint`, la FK a `shipments` con **`onDelete: ReferentialAction.Cascade`** y la FK a `app_users` con `Restrict`. Si la de `shipments` sale como `Restrict`, el `OnDelete` no se aplicó y hay que corregirlo antes de seguir: es el punto donde un despiste deja el modelo al revés.

### 4. Punto crítico — la identidad del usuario en la capa de servicio

Es el trabajo de diseño central del sprint. Hoy los cuatro servicios reciben **solo** `TransitOpsDbContext`, y el `ClaimsPrincipal` no sale de los controladores. RN-09 obliga a que la identidad llegue al servicio, y encima llegue a `ShipmentService`, que registra los eventos automáticos.

**Datos verificados del proyecto** (importan para elegir):

- El token lleva el id del usuario en el claim **`sub`** (`AuthService.cs:63`).
- `options.MapInboundClaims = false` (`Program.cs:71`), así que el claim **conserva el nombre `sub`** y **no** se renombra a `ClaimTypes.NameIdentifier`. Buscar `ClaimTypes.NameIdentifier` devolvería `null` — es la trampa clásica de este escenario, y aquí está desactivada explícitamente.
- `NameClaimType = "unique_name"` y `RoleClaimType = "role"` (`Program.cs:82-83`), que es por lo que `User.Identity!.Name` funciona en `AuthController.cs:36`. Del id no hay atajo equivalente.

**Opción elegida: un `ICurrentUser` inyectado, implementado sobre `IHttpContextAccessor`.**

```csharp
// Security/CurrentUser.cs
public interface ICurrentUser { Guid? Id { get; } }

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? Id => Guid.TryParse(accessor.HttpContext?.User.FindFirst("sub")?.Value, out var id) ? id : null;
}
```

`Program.cs` añade `builder.Services.AddHttpContextAccessor()` y `AddScoped<ICurrentUser, CurrentUser>()`. `ShipmentService` y el nuevo `ShipmentEventService` lo reciben por constructor.

Por qué esta y no las alternativas:

- **Pasar `Guid userId` como parámetro a cada método** es más explícito y no necesita `IHttpContextAccessor`, pero contamina la firma de **todos** los métodos de `IShipmentService` que registran eventos automáticos (`CreateAsync`, `AssignAsync`, `UnassignAsync`, `ChangeStatusAsync`), obliga a que cada controlador repita el parseo del claim, y rompe las llamadas existentes en los tests de S3/S4. Es la opción defendible que se descarta por coste de propagación.
- **Leer `HttpContext` directamente en el servicio** acopla el slice a ASP.NET y no se puede sustituir en tests. Descartada.
- `ICurrentUser` con un `Guid?` es una superficie mínima (una propiedad), sustituible por un doble trivial en los tests unitarios, y **no cambia ninguna firma pública existente**. La contrapartida —que la identidad viaja implícita— se acepta a cambio de eso y se documenta; es la razón por la que `Id` es `Guid?` y no `Guid`, para que la ausencia sea un caso previsto y no una excepción en tiempo de ejecución.

**Consecuencia en los tests existentes**: `ShipmentServiceTests` construye `new ShipmentService(dbContext)` directamente. Al añadir un parámetro al constructor, **todas** esas construcciones dejan de compilar. Es un cambio mecánico pero afecta a un archivo entero: conviene extraer un helper `CreateService(dbContext, userId = null)` en el propio archivo de tests y usarlo en todos los casos, para que el siguiente parámetro que llegue (S6) se cambie en un solo sitio. Los tests de controlador no se ven afectados: el `ClaimsPrincipal` real viaja en la petición.

### 5. Punto crítico — los eventos automáticos y el orden de guardado

Decisión 1: cinco puntos de `ShipmentService` pasan a registrar un evento.

| Método | Tipo de evento | Notas del evento |
|---|---|---|
| `CreateAsync` | `Created` | `null` (los datos ya están en el envío) |
| `AssignAsync` | `Assigned` | matrícula y nombre asignados, p. ej. `"Vehículo 1234-ABC · Conductor Ana Pérez"` |
| `UnassignAsync` | `Unassigned` | `null` |
| `ChangeStatusAsync` → `in_progress` | `Departed` | `null` |
| `ChangeStatusAsync` → `delivered` | `Delivered` | `null` |
| `ChangeStatusAsync` → `cancelled` | `Cancelled` | `null` |

En los automáticos, `OccurredAt = DateTime.UtcNow` (no lo indica nadie) y `RecordedByUserId = currentUser.Id`.

Tres trampas concretas:

- **Un solo `SaveChangesAsync`, no dos.** El evento se añade con `dbContext.ShipmentEvents.Add(...)` **antes** del `SaveChangesAsync` que ya existe en cada método, de modo que envío y evento se graban en la misma transacción implícita. Si se guardaran por separado, un fallo intermedio dejaría un envío en curso sin su evento de salida — precisamente la inconsistencia que el historial existe para evitar. En `CreateAsync` esto exige cuidado: el `ShipmentId` del evento debe ser el `item.Id` **ya asignado** en el POCO (lo está: `Guid.NewGuid()` en el inicializador, `Shipment.cs:13`), no un valor generado por la base de datos. Ese detalle es lo que permite el guardado único.
- **El evento de `Assigned` se construye con datos que `AssignAsync` ya tiene en mano** (matrícula y nombre, traídos en su paso 4 según el plan de S4). Cero consultas extra. Si el mensaje se armara releyendo los catálogos, la asignación pasaría de dos consultas a cuatro.
- **`ChangeStatusAsync` mapea estado destino → tipo de evento**, y ese mapeo debe vivir junto a la máquina de estados del §5 de S4, no en otro `switch` aparte. Un estado nuevo que alguien añada tiene que fallar a compilar en un único sitio.

**Riesgo de acoplamiento, dicho claramente**: esto mete escritura de eventos dentro del slice de envíos. La alternativa —un manejador de dominio, un `MediatR`, un `SaveChanges` interceptor— sería la respuesta «arquitectónicamente correcta» y es exactamente la magia implícita que `AGENTS.md:40` prohíbe. Con seis puntos de registro y un dominio de este tamaño, cuatro llamadas explícitas a un helper privado `RecordAsync(...)` son más legibles y más fáciles de probar. Se documenta como decisión consciente, no como descuido.

### 6. Contratos y servicio — `Features/Shipments/ShipmentEventContracts.cs` y `ShipmentEventService.cs`

Archivos nuevos **dentro del slice `Features/Shipments/`**, no en una carpeta propia: el evento es parte del agregado del envío y no tiene ciclo de vida independiente. Es la primera vez que un slice tiene dos servicios, y es la señal correcta de que son dos comportamientos de una misma cosa.

- **`CreateShipmentEventRequest(string? EventType, DateTime? OccurredAt, string? Location, string? Notes)`**:
  - `EventType`: `[Required]` y `[RegularExpression("^(checkpoint|incident)$")]` con mensaje en español. **Solo dos valores admitidos por HTTP**: los otros seis los registra el sistema (§5) y permitir que un cliente inyecte un `delivered` falso corrompería el historial y descuadraría el recuento de RF-14. El regex, no un enum, por la misma razón ya establecida en S3: `Enum.TryParse` no reconoce `snake_case` y el binder de enums produce mensajes en inglés (`ShipmentContracts.cs:32`).
  - `OccurredAt`: `DateTime?` opcional. Si no llega, el servicio usa `DateTime.UtcNow` (decisión 3, «por defecto ahora»).
  - `Location`: `[StringLength(160)]`. `Notes`: `[StringLength(500)]`.
  - **Validación de fecha futura**: `IValidatableObject` que rechaza `OccurredAt` posterior a *ahora* con `details["OccurredAt"]`, más guarda en el servicio (`shipment_event_future`, 400). El doble camino es el patrón ya establecido en S3 para RN-06 (`docs/Sprint3Plan.md:88-101`) y por la misma razón: el `IValidatableObject` solo lo aplica el pipeline MVC, y el servicio se llama directo desde los tests. **Con un margen de tolerancia** (p. ej. dos minutos): un reloj de cliente adelantado no debe hacer fallar un registro legítimo, y el navegador manda la hora del puesto. Sin ese margen es un fallo intermitente imposible de reproducir.
  - **No se valida contra las fechas del envío.** Un punto de control anterior a la recogida prevista es raro pero posible (el envío se adelantó), y RF-11 no lo prohíbe. Inventar la restricción sería añadir requisitos.
  - **Normalización de `Kind`**: `OccurredAt` pasa por `ShipmentTime.Utc` (§ el helper ya existe, `ShipmentContracts.cs:56`) tanto en el servicio como en el `Validate()`. Sin eso, un `datetime-local` naive del navegador llega `Unspecified` y Npgsql lanza excepción contra `timestamptz` — el fallo que S3 documentó y que **InMemory no reproduce**.
- **`ShipmentEventResponse(Guid Id, Guid ShipmentId, string EventType, DateTime OccurredAt, string? Location, string? Notes, Guid? RecordedByUserId, string? RecordedByUsername, DateTime CreatedAt)`**:
  - `EventType` como **`string`** en `snake_case`, mismo criterio que `ShipmentResponse.Status`: no hay `JsonStringEnumConverter` en el proyecto, así que un enum saldría como número. El mapeo en ambos sentidos va en un `ShipmentEventTypes.Parse`/`.Token` junto a `ShipmentStatuses` (que S4 extrajo por la misma razón).
  - **`RecordedByUsername` incluido**: sin él, la línea de tiempo tendría que resolver un id por fila. Se obtiene por proyección, **no** con `Include(RecordedByUser)`, siguiendo el criterio de S4 para matrícula y conductor. `null` cuando `RecordedByUserId` es `null` (trazas de sistema) — el front pinta «Sistema».
- **`IShipmentEventService`**: `GetByShipmentAsync(Guid shipmentId, ct)` → `IReadOnlyList<ShipmentEventResponse>`, y `CreateAsync(Guid shipmentId, CreateShipmentEventRequest request, ct)`. **Sin `GetByIdAsync`, sin `UpdateAsync`, sin `DeleteAsync`** (decisión 2). Un evento no tiene URL propia: se lee dentro de su historial.
- **Servicio**: `sealed class ShipmentEventService(TransitOpsDbContext dbContext, ICurrentUser currentUser)`, lambdas con variable `item`, helpers privados al final.
  - Ambos métodos **verifican primero que el envío existe** → 404 `shipment_not_found`, reutilizando el código y el mensaje ya establecidos (`ShipmentService.cs:130`). Sin esa comprobación, pedir el historial de un id inventado devolvería una lista vacía y `POST` grabaría un huérfano — este último lo pararía la FK, pero con un 500 en vez de un 404 legible.
  - Orden del historial: `OrderBy(OccurredAt).ThenBy(CreatedAt)`. El desempate por `CreatedAt` es **necesario**: los eventos automáticos de una misma operación pueden compartir `OccurredAt` al milisegundo, y sin orden total el historial se reordena entre peticiones. Es la misma lección que S3 aprendió con la paginación (`docs/Sprint3Plan.md:125`).
  - `CreateAsync` **no** actualiza `Shipment.UpdatedAt`: añadir una nota al historial no modifica el envío. Es una decisión pequeña con consecuencias visibles en la UI, y conviene fijarla.
  - **`CreateAsync` no comprueba el estado del envío.** Registrar una incidencia sobre un envío ya entregado es legítimo (una reclamación posterior), y RN-08 habla de *cambiar de estado*, no de anotar. Es exactamente el tipo de restricción que sería fácil añadir «por simetría» con S4 y que no está en los requisitos.

### 7. Controlador — `Controllers/ShipmentEventsController.cs`

Controlador propio con la ruta anidada, en lugar de dos acciones más en `ShipmentsController` (que ya tiene siete tras S4):

```csharp
[ApiController]
[Route("api/v1/shipments/{shipmentId:guid}/events")]
[Authorize(Policy = Policies.Operational)]
public sealed class ShipmentEventsController(IShipmentEventService service) : ControllerBase
```

Dos acciones: `[HttpGet]` → 200 con la lista, y `[HttpPost]` → 201 con el evento creado. Ambas con `ApiResponse<T>.Success(..., HttpContext.TraceIdentifier)`. `shipmentId` viene de la ruta y se pasa al servicio.

**Sin paginación**, a diferencia del listado de envíos: el historial de un envío son unidades o decenas de eventos, no miles, y el propio S3 razonó que el listado paginado sería el único de la aplicación (`docs/Sprint3Plan.md:110`). Devolver el historial completo evita replicar `ShipmentPageResponse` y el estado de paginación en el detalle. Se anota como límite conocido: si un envío acumulara cientos de eventos, esto se revisaría — no ocurre en el dominio descrito.

`Program.cs`: `AddHttpContextAccessor()`, `AddScoped<ICurrentUser, CurrentUser>()` y `AddScoped<IShipmentEventService, ShipmentEventService>()`.

---

## Frontend — `frontend/src/`

Cero dependencias nuevas. El trabajo se concentra otra vez en el detalle del envío, que tras S4 ya es la pantalla de operación y ahora gana su historial.

### 1. `api/client.ts`

```ts
export type ShipmentEventType = 'created' | 'assigned' | 'unassigned' | 'departed'
  | 'checkpoint' | 'incident' | 'delivered' | 'cancelled'
export interface ShipmentEvent {
  id: string; shipmentId: string; eventType: ShipmentEventType; occurredAt: string
  location: string | null; notes: string | null
  recordedByUserId: string | null; recordedByUsername: string | null; createdAt: string
}
export interface ShipmentEventInput { eventType: 'checkpoint' | 'incident'; occurredAt?: string; location?: string; notes?: string }

export const listShipmentEvents = (shipmentId: string) =>
  request<ShipmentEvent[]>(`/api/v1/shipments/${shipmentId}/events`)
export const createShipmentEvent = (shipmentId: string, input: ShipmentEventInput) =>
  request<ShipmentEvent>(`/api/v1/shipments/${shipmentId}/events`, { method: 'POST', body: JSON.stringify(input) })
```

El tipo de `ShipmentEventInput.eventType` es la unión **de dos**, no de ocho: el compilador impide desde el front pedir un `delivered` que el backend rechazaría. Que la restricción de negocio se vea en el tipo es gratis aquí.

### 2. `pages/shipments/ShipmentPages.tsx` — `ShipmentDetailPage`

Se amplía lo que S4 dejó. La carga inicial pasa de tres peticiones a cuatro: `Promise.all([getShipment(id), listVehicles(), listDrivers(), listShipmentEvents(id)])`.

**Línea de tiempo**, bajo el panel de operación:

- Lista (`<ol>`) en orden cronológico, cada entrada con: etiqueta del tipo (traducida y con color por familia), `occurredAt` formateado con el `formatDate` ya existente, ubicación y notas si las hay, y el autor — `recordedByUsername ?? 'Sistema'`.
- **Distinción visual entre automático y manual.** Los seis tipos de sistema y los dos manuales se leen distinto: los automáticos son la espina dorsal del envío, los manuales son lo que alguien observó. Un icono o un borde lateral basta; `.incident` en rojo, porque una incidencia es lo que la clienta va a buscar cuando un cliente llame (`docs/ClientRequirements.md:68`).
- `Empty` si no hay eventos — situación que en la práctica no ocurrirá con eventos automáticos activos, pero que se da con los envíos creados **antes** de esta migración (§Verificación, punto 5). No dejar ese caso sin tratar.
- **Sin `toLocaleString` en aserciones de test**: regla transversal ya vigente desde S3.

**Formulario de alta de evento**, colapsado tras un botón «Registrar evento» para no competir con las acciones de operación:

- `<select>` con **dos opciones**: punto de control e incidencia. Ubicación (opcional), notas (opcional), y fecha/hora con `<input type="datetime-local">` **preinicializado a ahora**, en hora local del puesto.
- **Trampa de conversión, idéntica a la del formulario de envío** (`ShipmentPages.tsx:18-22`): el valor para el `datetime-local` se calcula desplazando por `getTimezoneOffset()` y cortando a `YYYY-MM-DDTHH:mm`; al enviar, `new Date(local).toISOString()`. Los helpers `toLocalInput` ya existen en el archivo y se reutilizan; **no** duplicarlos.
- **Trampa de la clave omitida, también ya conocida**: `occurredAt`, `location` y `notes` vacíos deben **omitirse del cuerpo** con spread condicional, no enviarse como `''` — `''` no es válido para un `DateTime?` y System.Text.Json devolvería un 400 crudo (`docs/Sprint3Plan.md:161`).
- **Validación de futuro en cliente** antes de enviar, con el mismo margen de tolerancia que el backend, y aviso bajo el campo. Que el error de reloj se vea antes de la petición.
- Al recibir el 201: **añadir el evento al estado local** e insertarlo en su posición cronológica, sin recargar el historial completo. Es lo que pide el roadmap («reflejada sin recargar», `docs/Roadmap.md:148`). Insertar por posición y no al final importa: un evento fechado en el pasado va en medio.
- Tras las acciones de operación de S4 (asignar, transición), el historial **también cambia** porque el backend registró un evento automático. Esas acciones deben **refrescar el historial** con `listShipmentEvents(id)`. Es la interacción entre S4 y S5 que se olvida con facilidad: sin ella, asignar un vehículo actualiza la ficha pero la línea de tiempo se queda vieja hasta recargar la página.

### 3. Estilos — `index.css`

- `.timeline` como `<ol>` sin marcadores, con línea vertical (borde izquierdo `#d9e1ec`) y entradas separadas; `.timeline li` con el punto sobre la línea (`::before` circular, fondo blanco y borde) — sin librerías ni SVG.
- `.event-type` reutilizando la forma de `.status-chip` (radio `999px`, `.8rem`, peso `750`), con variante `.event-incident` en el rojo `#a51414` ya usado en el proyecto y una neutra para el resto.
- `.event-meta` para autor y fecha, en `#56627a` y `.9rem`, misma pareja que `.hint`.
- En el `@media`, la línea de tiempo pasa a márgenes reducidos.

---

## Pruebas

### Backend — servicio (`TransitOps.Tests/Services/ShipmentEventServiceTests.cs`, nuevo)

Molde `ShipmentServiceTests`, helper `CreateDatabase()` con prefijo `shipment-event-tests-`, siembra siempre con `DateTimeKind.Utc`. Doble de `ICurrentUser` trivial (un `sealed record StubCurrentUser(Guid? Id) : ICurrentUser`).

Ocho tests:

1. **Alta con los campos normalizados**, `OccurredAt` con `Kind == Utc` partiendo de `Unspecified` y de `Local` (el `Local` convertido, no reetiquetado), y `RecordedByUserId` tomado de `ICurrentUser` — es RN-09.
2. **`OccurredAt` ausente** → se usa *ahora*, con `Kind == Utc`.
3. **Fecha futura** más allá del margen → 400 `shipment_event_future`; dentro del margen → aceptada.
4. **Envío inexistente** → 404 `shipment_not_found`, en alta y en consulta.
5. **Orden del historial**: sembrar eventos desordenados, incluidos **dos con el mismo `OccurredAt`**, y comprobar que el desempate por `CreatedAt` da un orden total y estable en dos llamadas consecutivas.
6. **`RecordedByUsername`** se resuelve, y es `null` cuando `RecordedByUserId` es `null`.
7. **Aislamiento por envío**: los eventos de un envío no aparecen en el historial de otro.
8. **`Shipment.UpdatedAt` no cambia** al registrar un evento.

### Backend — eventos automáticos (`TransitOps.Tests/Services/ShipmentServiceTests.cs`, se amplía)

Aquí está el riesgo de regresión del sprint, y hay que tocar el archivo entero: **todas** las construcciones de `ShipmentService` cambian de firma (§4). Extraer `CreateService(dbContext, userId)` primero, luego añadir cuatro tests:

1. **`CreateAsync` registra `Created`** con el envío recién creado y el usuario actual, en **un solo `SaveChanges`** (comprobable afirmando que ambos existen tras la llamada).
2. **`AssignAsync` registra `Assigned`** con matrícula y conductor en las notas; **`UnassignAsync` registra `Unassigned`**.
3. **`ChangeStatusAsync` registra el tipo correcto** por cada transición válida: `in_progress`→`Departed`, `delivered`→`Delivered`, `cancelled`→`Cancelled` (`[Theory]`).
4. **Una transición rechazada no registra nada** — el caso que se olvida. Intentar mover un envío terminal debe dejar el historial intacto; si el evento se añadiera antes de validar, quedaría una traza de algo que no pasó.

### Backend — integración (`TransitOps.Tests/Controllers/ShipmentEventsControllerTests.cs`, nuevo)

Molde `ShipmentsControllerTests` (`Factory`, `Client`, `Json`, asserts sobre `JsonNode`). Cinco tests:

1. **Ambos endpoints exigen token** (`[Theory]` con `GET` y `POST`).
2. **Alta y consulta por HTTP**: 201 con el sobre común, y el `GET` posterior lo devuelve con `recordedByUsername` **igual al usuario del token** — la comprobación de RN-09 de punta a punta, y la única que verifica que el claim `sub` se lee de verdad. Es el test que fallaría si alguien buscara `ClaimTypes.NameIdentifier` (§4).
3. **Tipos no admitidos**: `{"eventType":"delivered"}` y `{"eventType":"foo"}` → 400 `validation_error` con `details["EventType"]`. Documenta que el historial no se puede falsear desde el cliente.
4. **`GET` sobre un envío inexistente** → 404, no lista vacía.
5. **El historial incluye los eventos automáticos**: crear un envío, asignar, poner en curso por HTTP, y comprobar que el `GET` del historial devuelve `created`, `assigned` y `departed` **en ese orden**. Es el entregable del sprint verificado por la API.

Total esperado, partiendo de ~58 tras S4: **en torno a 75 pruebas backend**.

### Frontend (`frontend/src/App.test.tsx`)

Se amplía el `describe('envíos')`. El despachador por URL debe distinguir `/api/v1/shipments/{id}/events` de `/api/v1/shipments/{id}` y de `/api/v1/shipments/{id}/assignment` — con cuatro rutas que comparten prefijo, el orden de las comprobaciones del mock ya importa de verdad; hacer coincidir por sufijo exacto.

Cinco tests:

1. **El detalle pinta la línea de tiempo** con los eventos del mock, mostrando el autor y «Sistema» cuando `recordedByUsername` es `null`.
2. **Registrar un evento** llama al endpoint con el cuerpo correcto (`eventType`, `occurredAt` en ISO, sin claves vacías) y **añade la entrada sin volver a pedir el historial** (aserción sobre el número de llamadas a `GET /events`).
3. **Inserción cronológica**: un evento fechado en el pasado aparece **antes** de uno ya existente más reciente, no al final.
4. **Fecha futura** bloquea el envío sin llamar al servidor, con el aviso bajo el campo.
5. **Una acción de operación de S4 refresca el historial**: tras asignar, se vuelve a pedir `GET /events`. Es la interacción entre sprints, y el test existe precisamente porque es lo que se olvida.

Total esperado, partiendo de ~18 tras S4: **en torno a 23 pruebas frontend**.

---

## Documentación y cierre

1. **`docs/Sprint5Plan.md`** — este documento.
2. **`docs/design/DataModel.md`**: el bloque `SHIPMENT_EVENT` y la relación `APP_USER ||--o{ SHIPMENT_EVENT` **ya están en el diagrama** desde S1, así que hay que **verificar que coinciden** con lo implementado (el diagrama dice `recorded_by_user_id FK`, coherente) y ajustar `location`/`notes` si difieren. Actualizar el «Alcance» de la línea 5 (S5 incorpora `ShipmentEvent`, y con eso el modelo diseñado en S1 queda **implementado al completo** — un hito del roadmap que merece decirse). Ampliar la viñeta de `ShipmentEvent` con: inmutabilidad, `Cascade` como única excepción al `Restrict` general y su justificación, la distinción `OccurredAt`/`CreatedAt`, y los tipos reservados al sistema.
3. **`docs/Roadmap.md`**: nota `**Cierre (YYYY-MM-DD)**` tras la línea 155, en el formato de las de S2–S4, con duración real, entregables y recuento de pruebas. Añadir que con S5 **todos los requisitos de prioridad alta** (`docs/Requirements.md:289`) quedan implementados.
4. **`CONTEXT.md`**: entrada en el Recent Decision Log con las tres decisiones; `Repository Snapshot` a "Sprints 1–5 implemented"; en `Open Notes`, retirar RF-11 de la lista de requisitos sin precedente en el archivo y anotar que `ICurrentUser` queda disponible para S6 (RN-10/RN-14 lo necesitarán). **`README.md`** (`## Current Status`) y **`AGENTS.md:52`** al mismo estado.
5. **`postman/`**: carpeta «Eventos» o dos peticiones en «Envíos» — «Historial del envío» y «Registrar evento» — con la nomenclatura en español. Documentar en la descripción que solo `checkpoint` e `incident` se admiten y por qué.
6. **Memoria LaTeX** (`tfg/memoria/`): sección de S5 en `contido/desarrollo_iterativo.tex`, filas en `contido/validacion.tex`, párrafo en `contido/resultados.tex`, y `anexos/trazabilidad.tex` con `RF-11` pasando a evidencia real. Capturas de la línea de tiempo con eventos automáticos y manuales mezclados, y del formulario de registro. **Dos puntos merecen desarrollo escrito**, porque son las decisiones de ingeniería más citables del sprint: la captura de identidad con `ICurrentUser` (con las alternativas descartadas) y el registro automático en la misma transacción que el cambio de estado.

## Verificación (extremo a extremo)

1. **Backend**: `dotnet build TransitOps.slnx --configuration Release` y `dotnet test TransitOps.slnx` en verde. Atención al primer intento: el cambio de constructor de `ShipmentService` romperá la compilación de los tests hasta que se aplique el helper del §Pruebas.
2. **Migración**: generar `AddShipmentEvents`, comprobar la ruta, los tipos de columna y —en particular— **`Cascade` en la FK a `shipments` y `Restrict` en la de `app_users`**; aplicar con `dotnet run --project TransitOps.Api -- --migrate-only`.
3. **Frontend**: `npm run lint`, `npm run build` y `npm run test` en verde.
4. **PostgreSQL real** (`docker compose up --build`), los casos que InMemory no cubre:
   - `POST` de un evento con `occurredAt` **naive** (`"2026-08-01T08:00:00"`) → 2xx, no 500. Es el fallo que S3 documentó y que reaparece con cada campo `DateTime` nuevo.
   - La **proyección de `recordedByUsername`** se traduce a SQL válido: el `GET` del historial responde 200 con el nombre poblado.
   - El `GET` del historial de un envío con eventos automáticos y manuales devuelve el **orden correcto** contra Postgres, que no garantiza orden sin `ORDER BY` y es donde un desempate ausente se nota.
5. **Envíos preexistentes**: los envíos creados antes de esta migración **no tienen evento `Created`**. Comprobar que su detalle se pinta sin errores (línea de tiempo vacía o solo con eventos posteriores). No se hace relleno retroactivo: inventar eventos con fechas falsas contradice el valor probatorio del historial (decisión 2). Se documenta en el cierre.
6. **Flujo funcional en el navegador** (`http://localhost:5173`, a través de Nginx): login → crear envío → abrir detalle y ver el evento `created` ya presente → asignar y ver aparecer `assigned` **sin recargar** → registrar una incidencia con fecha de esta mañana y comprobar que se **inserta en su posición** → poner en curso y ver `departed`. Capturas para la memoria aquí.
7. **Casos de error visibles**: fecha futura (aviso bajo el campo, sin petición), y `GET` del historial de un id inventado → 404 legible.
8. **CI**: `.github/workflows/ci.yml` valida ambos lados en el push.

## Archivos

**Crear** — `TransitOps.Api/Domain/ShipmentEvent.cs`; `TransitOps.Api/Security/CurrentUser.cs`; `TransitOps.Api/Features/Shipments/{ShipmentEventContracts,ShipmentEventService}.cs`; `TransitOps.Api/Controllers/ShipmentEventsController.cs`; migración `Persistence/Migrations/*_AddShipmentEvents.cs` (generada); `TransitOps.Tests/Services/ShipmentEventServiceTests.cs`; `TransitOps.Tests/Controllers/ShipmentEventsControllerTests.cs`; `docs/Sprint5Plan.md` (este documento).

**Editar** — `TransitOps.Api/Persistence/TransitOpsDbContext.cs`; `TransitOps.Api/Program.cs`; `TransitOps.Api/Features/Shipments/ShipmentService.cs` (eventos automáticos + `ICurrentUser`); `TransitOps.Api/Features/Shipments/ShipmentContracts.cs` (`ShipmentEventTypes` junto a `ShipmentStatuses`); `TransitOps.Tests/Services/ShipmentServiceTests.cs` (firma del constructor en todo el archivo + cuatro tests); `frontend/src/api/client.ts`; `frontend/src/pages/shipments/ShipmentPages.tsx`; `frontend/src/index.css`; `frontend/src/App.test.tsx`; `docs/design/DataModel.md`; `docs/Roadmap.md`; `CONTEXT.md`; `README.md`; `AGENTS.md`; `postman/TransitOps.Api.postman_collection.json`; `tfg/memoria/contido/{desarrollo_iterativo,validacion,resultados}.tex`; `tfg/memoria/anexos/trazabilidad.tex`.

**Sin tocar** — `Common/ApiContracts.cs`; `Domain/Shipment.cs` (**no** se le añade colección de eventos, §2); `ShipmentsController.cs` (los eventos tienen su propio controlador); `ShipmentFormPage`; `components/{CatalogUi,ErrorAlert}.tsx` y `form-errors.ts`; `package.json` (cero dependencias nuevas); `archive/` (oráculo de consulta: su historial de eventos es backend-only y no hay precedente de UI).
