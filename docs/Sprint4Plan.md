# Plan de implementación — Sprint 4 · Operación del Envío (Asignación y Ciclo de Estados)

## Contexto

El repositorio tiene los **Sprints 1–3 cerrados**: esqueleto autenticado con JWT, los tres catálogos base con baja lógica, y el módulo de envíos con alta, edición, detalle y listado filtrado/paginado. Estado verificado: 45 pruebas backend y 13 frontend en verde, 3 migraciones aplicadas (`InitialCreate`, `AddCatalogTables`, `AddShipments`), stack completo en Docker Compose.

El Sprint 3 dejó deliberadamente preparado el terreno de este sprint: `Shipment.VehicleId`/`DriverId` ya existen como FK escalares con `RESTRICT`, `Status` ya persiste como `smallint` con los cuatro valores, el listado ya filtra por vehículo y conductor, y el detalle muestra dos filas con el aviso «Se asignará al operar el envío (próximamente)». Nada de eso tiene todavía forma de escribirse: no hay endpoint que asigne ni que cambie de estado, y `UpdateAsync` nunca toca `Status`.

El **Sprint 4** (`docs/Roadmap.md:116-135`) implementa la lógica de negocio central: **asignación de vehículo y conductor** (RF-09) y **ciclo de estados** (RF-10). Es el primer módulo del proyecto que no es CRUD: sus reglas son condiciones sobre el estado actual del agregado y sobre el estado de otros agregados (RN-04, la anti-doble-reserva, es una consulta cruzada contra el resto de envíos). También es el primer requisito con una regla que **avisa sin bloquear** (RN-05), lo que obliga a decidir cómo viaja un aviso no-error por una API cuyo contrato solo contempla éxito o error.

Entregable demostrable: el **Flujo 3** de `docs/Requirements.md:255-263` completo desde la interfaz — crear envío, asignar vehículo y conductor, pasar a en curso, entregar o cancelar — y la comprobación de que un vehículo o conductor ya ocupado no se puede reutilizar.

## Decisiones confirmadas con el usuario

1. **RN-05 como campo de respuesta, no como confirmación en dos pasos**: la asignación devuelve 200 con `capacityWarning` (`string?`) dentro del envío. Una sola llamada; el SPA pinta el aviso *después* de confirmar. Evita el estado intermedio "pendiente de confirmar" en el front y mantiene la regla en el backend, testeable en xUnit.
2. **Sub-recursos de acción explícitos**: `PUT /shipments/{id}/assignment`, `DELETE /shipments/{id}/assignment` y `POST /shipments/{id}/status`. No se amplía el `PUT /shipments/{id}` existente (mezclaría edición de datos con transiciones) ni se abren tres endpoints por transición.
3. **Sin historial de eventos**: `ShipmentEvent` es íntegramente Sprint 5, incluidos los eventos automáticos de asignación/salida/entrega. S4 solo cambia asignación y estado.
4. **Fechas reales automáticas**: `ActualPickupAt` se sella al pasar a en curso y `ActualDeliveryAt` al entregar, con `DateTime.UtcNow`. No son editables ni aparecen en el formulario. Requiere una segunda migración en este sprint.

## Alcance y no-objetivos

**Dentro**: dos campos nuevos en `Shipment` + migración incremental; asignación/reasignación/retirada de vehículo y conductor con RN-01..RN-05; transiciones de estado con RN-07/RN-08; aviso de capacidad; SPA con panel de operación en el detalle del envío, columnas de vehículo/conductor en el listado y filtros ya existentes conectados; pruebas backend y frontend; actualización de `DataModel.md`, Postman, Roadmap, CONTEXT, README, AGENTS y memoria LaTeX.

**Fuera**: historial de eventos (S5, decisión 3); administración de usuarios, cambio de contraseña e indicadores (S6); borrado de envíos (la retirada operativa es `Cancelled`); reapertura de un envío terminal (RN-08 lo prohíbe explícitamente y el requisito indica crear un envío nuevo); asignación desde el formulario de alta (RF-09 la describe como acción sobre un envío ya planificado, y hacerlo en el alta obligaría a duplicar RN-03/RN-04 en `CreateAsync`).

**No-objetivo deliberado**: `PUT /shipments/{id}` sigue **sin tocar** `Status`, `VehicleId` ni `DriverId`. Editar los datos de un envío en curso queda permitido como hasta ahora; lo que las reglas de S4 congelan es la *asignación*, no las notas.

---

## Backend — `TransitOps.Api/`

Todo el trabajo cae dentro del slice existente `Features/Shipments/`. No se crea carpeta nueva: la asignación y el estado son comportamiento del agregado `Shipment`, no un recurso propio. Referencias de estilo: `Features/Shipments/ShipmentService.cs` (helpers privados `Normalize`/`EnsureUnique`/`Existing`/`Map` en ese orden), `Controllers/ShipmentsController.cs`.

### 1. Entidad — `Domain/Shipment.cs`

Dos campos nuevos, junto a las fechas previstas para que el orden del POCO siga contando la historia del envío:

```csharp
public DateTime? ActualPickupAt { get; set; }
public DateTime? ActualDeliveryAt { get; set; }
```

Nada más. `VehicleId`/`DriverId`/`Status` ya existen desde S3 con la forma correcta, y no se añaden propiedades de navegación `Vehicle`/`Driver`: se resuelven por proyección (§5), no por `Include`.

**Sin `IsActive` y sin campo de "asignado en"**: RF-09/RF-10 no lo piden, y la traza temporal de la asignación es exactamente lo que S5 registrará como evento. Añadirlo ahora sería duplicar el historial en dos sitios.

### 2. Configuración EF — `Persistence/TransitOpsDbContext.cs`

Solo hace falta declarar longitudes/precisiones cuando el tipo no basta; `DateTime?` mapea solo a `timestamp with time zone` nullable. Así que el bloque `shipment` **no cambia** salvo por un `CHECK` opcional coherente con el que ya existe para las fechas previstas:

```csharp
table.HasCheckConstraint("ck_shipments_actual_dates",
    "\"ActualDeliveryAt\" IS NULL OR \"ActualPickupAt\" IS NULL OR \"ActualDeliveryAt\" >= \"ActualPickupAt\"")
```

Se añade como segunda llamada dentro del `ToTable("shipments", table => …)` de la línea 54. Es tercera capa de defensa, no la primaria: el orden real lo garantiza la máquina de estados (no se puede entregar sin haber salido).

**Índice para RN-04**: la anti-doble-reserva consulta `WHERE VehicleId = @id AND Status IN (0,1)`. EF ya creó `IX_shipments_VehicleId` y `IX_shipments_DriverId` al declarar las FKs en S3, y el índice de `Status` también existe. Con el volumen de este proyecto es más que suficiente; **no** se añaden índices compuestos ni filtrados. Documentar la decisión en el plan es lo que corresponde, no optimizar a ciegas.

### 3. Migración

```bash
dotnet tool restore && dotnet ef migrations add AddShipmentOperation --project TransitOps.Api
```

Comprobar que el archivo cae en `Persistence/Migrations/` (si no, repetir con `--output-dir Persistence/Migrations`) y que la migración generada contiene **solo** dos `AddColumn<DateTime>` con `timestamp with time zone` nullable más el `CHECK`. Si aparece cualquier otro cambio, es que el snapshot del modelo estaba desincronizado y hay que averiguar por qué antes de seguir.

### 4. Punto crítico — RN-04 (anti-doble-reserva) es una consulta, no una restricción

Es la regla más delicada del sprint y no tiene precedente en el proyecto: es la primera vez que validar una escritura exige mirar **otras filas de la misma tabla**.

La condición: un vehículo (o conductor) no puede estar en más de un envío **sin terminar**, entendiendo por sin terminar `Planned` o `InProgress`. Traducción directa:

```csharp
private static readonly ShipmentStatus[] OpenStatuses = [ShipmentStatus.Planned, ShipmentStatus.InProgress];

await dbContext.Shipments.AnyAsync(item =>
    item.Id != shipmentId &&
    item.VehicleId == vehicleId &&
    (item.Status == ShipmentStatus.Planned || item.Status == ShipmentStatus.InProgress), cancellationToken)
```

Cuatro trampas concretas:

- **`item.Id != shipmentId` es obligatorio.** Sin esa exclusión, reasignar el mismo vehículo al mismo envío (por ejemplo, cambiar solo el conductor y reenviar el formulario completo) choca consigo mismo y devuelve 409. Es el mismo patrón que `EnsureUnique(reference, excludedId, …)` ya usa en `ShipmentService.cs:118`.
- **`Contains` sobre array traduce a `IN` en Postgres pero no bajo InMemory con enums convertidos.** Escribir las dos comparaciones explícitas con `||` evita depender de cómo cada proveedor traduce `OpenStatuses.Contains(item.Status)`. Menos elegante, idéntico en ambos proveedores; el `OpenStatuses` estático se reserva para el filtro en memoria del resumen de S6, si llega.
- **No hay garantía transaccional.** Dos operadores asignando el mismo vehículo a la vez pueden pasar los dos `AnyAsync` y grabar los dos. No se resuelve con un índice único (la condición es sobre un subconjunto de estados y `VehicleId` es nullable: un índice único filtrado `WHERE Status IN (0,1) AND VehicleId IS NOT NULL` **sí** sería posible en Postgres, pero prohibiría a dos envíos planificados compartir vehículo incluso en escenarios legítimos futuros y no es lo que pide RN-04 a nivel de aplicación). Se documenta como limitación conocida, aceptable para un sistema con un puñado de operadores, y se anota en `CONTEXT.md` como candidata a revisión en S7. **Escribirlo en el plan es parte del entregable**: el TFG defiende disciplina, y una limitación identificada y justificada vale más que un candado a medias.
- **El mensaje de error debe decir *qué* recurso choca.** RF-09 exige que "el sistema lo impide y lo avisa". Dos códigos distintos, `shipment_vehicle_busy` y `shipment_driver_busy`, con mensajes que nombran el envío en conflicto: "El vehículo ya está asignado al envío REF-014." Eso obliga a que la consulta devuelva la referencia, no un booleano — usar `Where(...).Select(item => item.Reference).FirstOrDefaultAsync()` en lugar de `AnyAsync`, y tratar `null` como "libre".

Orden de comprobación dentro de la asignación, de más barato a más caro y de más general a más específico: estado del envío (RN-02) → existencia y actividad de los recursos (RN-03) → ocupación (RN-04) → capacidad (RN-05, que no bloquea y por tanto va al final).

### 5. Punto crítico — la máquina de estados, en un solo sitio

RF-10 define cuatro estados y RN-07/RN-08 las condiciones. El grafo completo:

| Desde | Hacia | Condición |
|---|---|---|
| `planned` | `in_progress` | Requiere vehículo **y** conductor asignados (RN-07). Sella `ActualPickupAt`. |
| `planned` | `cancelled` | Sin condiciones. |
| `in_progress` | `delivered` | Sin condiciones. Sella `ActualDeliveryAt`. |
| `in_progress` | `cancelled` | Sin condiciones (una salida puede frustrarse). |
| `delivered` \| `cancelled` | cualquiera | **Prohibido** (RN-08). |
| cualquiera | mismo estado | **Prohibido**: un no-op silencioso oculta un error del cliente. |
| `planned` | `delivered` | **Prohibido**: saltarse la salida vaciaría `ActualPickupAt` y burlaría RN-07. |

Se implementa como **un único `switch` sobre la tupla `(actual, solicitado)`** en un helper privado del servicio, no como una tabla de datos ni como estado esparcido por varios `if`. Un `switch` exhaustivo sobre la tupla hace que el compilador y la lectura del código coincidan con esta tabla, y el `_ =>` final produce el rechazo por defecto: **cualquier transición no listada se deniega**, que es la postura correcta para una regla de negocio.

Tres códigos de error distintos, porque el operador necesita saber *por qué*:

- `shipment_status_terminal` (409) — "Un envío entregado o cancelado no puede cambiar de estado." Se comprueba **primero**, antes que la validez de la transición, para que el mensaje sea el específico de RN-08 y no el genérico.
- `shipment_assignment_required` (409) — "Para poner el envío en curso hay que asignar vehículo y conductor." Es RN-07.
- `shipment_status_transition_invalid` (409) — el resto (mismo estado, salto de `planned` a `delivered`).

**409 y no 400** en los tres: el cuerpo es sintácticamente válido y el recurso existe; lo que falla es el estado del agregado. Es el mismo criterio con el que S3 eligió 409 para `shipment_customer_not_found` (`docs/Sprint3Plan.md:120`), y mantenerlo es más valioso que discutir el matiz.

**Sellado de fechas reales.** `ActualPickupAt = DateTime.UtcNow` en la transición a `in_progress`, `ActualDeliveryAt = DateTime.UtcNow` en la transición a `delivered`. `cancelled` **no sella nada** — no ha habido entrega. Un envío cancelado tras haber salido conserva su `ActualPickupAt`, que es información real. `DateTime.UtcNow` ya produce `Kind = Utc`, así que no pasa por `ShipmentTime.Utc`; aun así conviene una aserción en tests, porque es el punto donde un futuro cambio a `DateTime.Now` reventaría en Postgres y no en InMemory.

### 6. Punto crítico — RN-05, un aviso por un canal de éxito

El contrato común (`Common/ApiContracts.cs`) tiene exactamente dos formas: `ApiResponse<T>` con `data`, o `ApiErrorResponse` con `error`. No hay sitio para "correcto, pero atención". Por decisión 1, el aviso viaja **dentro de `data`**: `ShipmentResponse` gana un campo `string? CapacityWarning`.

Consecuencias que hay que aceptar explícitamente, porque son el precio de la decisión:

- El campo es **`null` en todas las respuestas salvo la de la propia asignación**. `GET`, `POST`, `PUT` y el listado lo devuelven siempre `null`. No se recalcula al leer: el aviso pertenece al momento de asignar, no es un estado persistente del envío. Documentarlo en el plan y en Postman evita que alguien lo interprete como una bandera consultable.
- Alternativa descartada: persistir la bandera en la tabla. Sería un dato derivado de dos campos que pueden cambiar por separado (editar la carga estimada o la capacidad del vehículo lo dejaría obsoleto). Un dato derivado y persistido que puede quedar mentiroso es peor que no tenerlo.

Cálculo, en el servicio:

```csharp
vehicle.LoadCapacity.HasValue && item.EstimatedLoad.HasValue && vehicle.LoadCapacity < item.EstimatedLoad
```

Ambos son `decimal?` con `HasPrecision(12, 2)`, así que la comparación es exacta y no hay que preocuparse por tolerancias de coma flotante. Si **cualquiera** de los dos es `null` no hay aviso: RN-05 dice "si la capacidad conocida … es menor", y capacidad desconocida no es capacidad insuficiente. Mensaje: `"La capacidad del vehículo (3000 kg) es inferior a la carga estimada (4500 kg)."`, formateado con `CultureInfo.InvariantCulture` para que no dependa de la cultura del servidor.

El aviso **no impide** que la asignación se guarde: se calcula después de `SaveChangesAsync`, sobre los valores ya persistidos, y se adjunta al `Map`.

### 7. Contratos — `Features/Shipments/ShipmentContracts.cs`

Se amplía el archivo existente, sin partirlo: sigue siendo un slice pequeño.

- **`ShipmentResponse`** gana cuatro campos al final, antes de `CreatedAt`/`UpdatedAt` para que el registro conserve el orden "datos → operación → auditoría": `string? VehiclePlate`, `string? DriverName`, `DateTime? ActualPickupAt`, `DateTime? ActualDeliveryAt`, y `string? CapacityWarning`. Es un `record` posicional, así que **añadir parámetros rompe toda llamada al constructor**: el `Map` del servicio y los tests que construyan respuestas a mano hay que actualizarlos a la vez. Es un cambio mecánico pero no automático.
  - `VehiclePlate`/`DriverName` entran ahora, tal como S3 anticipó (`docs/Sprint3Plan.md:109`): con la asignación implementada dejan de ser siempre `null`, y sin ellos el listado necesitaría una petición por fila para mostrar la matrícula. Se resuelven **por proyección**, no con propiedades de navegación (§8).
- **`AssignShipmentRequest(Guid? VehicleId, Guid? DriverId)`**: ambos `Guid?`, ambos obligatorios en la práctica pero validados en el servicio, no con `[Required]`. Razón: `[Required]` sobre `Guid?` funciona, pero el mensaje del binder para un GUID mal formado ya es un 400 crudo, y RF-09 trata la asignación como una operación **conjunta** ("asignar un vehículo y un conductor"), así que el error útil es uno solo: `shipment_assignment_incomplete` (400) — "Hay que indicar vehículo y conductor.". Un `[Required]` por campo daría dos mensajes para un único concepto de negocio.
  - **No se admite asignación parcial.** RN-01 dice "un vehículo y un conductor a la vez", y el detalle del envío presenta la asignación como una unidad. Asignar solo vehículo dejaría un envío que no puede salir por RN-07 y complicaría RN-04 con casos mixtos sin ganar nada. La retirada, en cambio, es total y tiene su propio verbo (`DELETE`).
- **`ChangeShipmentStatusRequest(string? Status)`**: `string?` con `[Required]` y `[RegularExpression("^(in_progress|delivered|cancelled)$")]` con mensaje en español. **`planned` no está en la lista**: no existe ninguna transición válida hacia `planned` (nada vuelve atrás), así que aceptarlo solo para rechazarlo después con otro código sería ruido. El regex replica exactamente el criterio ya tomado en `ListShipmentsQuery` (`ShipmentContracts.cs:32`): token `snake_case`, validado por regex y no por enum, porque `Enum.TryParse` no reconoce `in_progress` y el binder de enums produciría un mensaje **en inglés**.
- **`IShipmentService`** gana tres métodos: `AssignAsync(Guid id, AssignShipmentRequest request, ct)`, `UnassignAsync(Guid id, ct)`, `ChangeStatusAsync(Guid id, ChangeShipmentStatusRequest request, ct)`. Los tres devuelven `Task<ShipmentResponse>`: el SPA necesita el envío completo actualizado para repintar el detalle sin una segunda petición.
- **Mapeo de estado token↔enum duplicado.** `GetAllAsync` ya tiene un `switch` de `string` a `ShipmentStatus` (`ShipmentService.cs:20-27`) y `Map` tiene el inverso. Con `ChangeStatusAsync` habría **tres** copias. Extraer los dos sentidos a `ShipmentStatuses.Parse(token)` / `ShipmentStatuses.Token(status)` en `ShipmentContracts.cs`, junto a `ShipmentTime`, y usarlos en los tres puntos. Es refactor de un archivo, no arquitectura, y evita que el cuarto estado que alguien añada se olvide en un sitio.

### 8. Servicio — `Features/Shipments/ShipmentService.cs`

Los tres métodos nuevos van **después** de `UpdateAsync` y antes de los helpers privados, respetando el orden público-luego-privado del archivo.

**`AssignAsync`**:
1. `Existing(id, ct)` → 404 si no hay envío.
2. `EnsureAssignable(item)` → si `Status != Planned`, 409 `shipment_not_assignable` — "Solo se puede asignar mientras el envío está planificado." Es RN-02, y cubre a la vez el "no se puede modificar la asignación una vez que el envío ha salido" de RF-09.
3. Validar que llegan los dos ids → 400 `shipment_assignment_incomplete`.
4. `EnsureVehicle` / `EnsureDriver`: **una consulta por recurso que trae lo que hace falta**, no un `AnyAsync` seguido de otro `SingleAsync`. Para el vehículo: `Where(v => v.Id == id && v.IsActive).Select(v => new { v.LicensePlate, v.LoadCapacity }).SingleOrDefaultAsync()`; `null` → 409 `shipment_vehicle_not_found` ("El vehículo indicado no existe o está dado de baja."). Ídem conductor con `Name`, código `shipment_driver_not_found`. Es RN-03, y sigue el precedente de `EnsureCustomer` (`ShipmentService.cs:122`): 409 porque el recurso direccionado existe y lo que choca es el estado del catálogo.
5. `EnsureNotBusy` para cada uno (§4), con la referencia del envío en conflicto en el mensaje.
6. Asignar, `UpdatedAt = DateTime.UtcNow`, `SaveChangesAsync`.
7. `Map(item)` y adjuntar `with { VehiclePlate = …, DriverName = …, CapacityWarning = … }` usando los datos ya traídos en el paso 4. **Cero consultas extra.**

**`UnassignAsync`**: `Existing` → `EnsureAssignable` (retirar es modificar la asignación, RN-02 aplica igual) → `VehicleId = null; DriverId = null` → `UpdatedAt` → `SaveChanges` → `Map`. Idempotente por naturaleza: retirar de un envío sin asignación devuelve 200 y no hace nada. No merece un error; el estado final es el pedido.

**`ChangeStatusAsync`**: `Existing` → parsear el token (`ShipmentStatuses.Parse`, que lanza 400 si es basura; el regex del contrato ya lo filtra por HTTP, pero el servicio se invoca directo desde los tests) → `EnsureTransition(item, target)` (§5) → sellar la fecha real que corresponda → `item.Status = target` → `UpdatedAt` → `SaveChanges` → `Map`.

**Punto de detalle en el `Map`**: hoy `Map` es `static` y lee `item.Customer?.Name` gracias al `Include`. Con vehículo y conductor **no se añade `Include`**: obligaría a declarar propiedades de navegación en la entidad y traería la fila entera de dos catálogos en cada lectura del listado. En su lugar, `GetAllAsync`/`GetByIdAsync` **proyectan** matrícula y nombre. Dos formas posibles y hay que elegir una y ser consistente:

- **Recomendada**: cambiar la firma a `Map(Shipment item, string? vehiclePlate = null, string? driverName = null)` y, en las lecturas, proyectar a un tipo anónimo `{ Shipment, VehiclePlate, DriverName }` con dos subconsultas correlacionadas (`dbContext.Vehicles.Where(v => v.Id == item.VehicleId).Select(v => v.LicensePlate).FirstOrDefault()`). EF las traduce a `LEFT JOIN LATERAL` en Postgres y funcionan en InMemory. Es explícito, no requiere navegación, y `CreateAsync`/`UpdateAsync` siguen llamando a `Map(item)` sin argumentos (recién creado no tiene asignación; en update la asignación no cambia — pero **sí hay que traer matrícula y nombre en `UpdateAsync`**, porque editar un envío ya asignado debe devolver la respuesta completa: se resuelve con la misma proyección que `GetByIdAsync` o con una relectura, y conviene extraer un helper privado `Detail(id, ct)` que ambos usen).
- Descartada: declarar `Vehicle?`/`Driver?` como navegación y usar `Include`. Más corto de escribir, pero mete dos entidades completas en la memoria de cada listado y contradice la razón por la que S3 dejó estas FKs escalares a propósito.

### 9. Controlador — `Controllers/ShipmentsController.cs`

Tres acciones nuevas, todas bajo la misma `[Authorize(Policy = Policies.Operational)]` de la clase (RF-09/RF-10 son trabajo de operador, no de administrador):

```csharp
[HttpPut("{id:guid}/assignment")]     // AssignShipmentRequest desde el body
[HttpDelete("{id:guid}/assignment")]  // sin body
[HttpPost("{id:guid}/status")]        // ChangeShipmentStatusRequest desde el body
```

Las tres devuelven `Ok(ApiResponse<ShipmentResponse>.Success(…, HttpContext.TraceIdentifier))`. El `DELETE` de asignación devuelve **200 con el envío**, no 204: el SPA repinta el detalle con la respuesta, igual que las otras dos. (El `DELETE` de los catálogos sí es 204 porque allí el recurso desaparece del listado; aquí el recurso sigue existiendo y ha cambiado.)

`PUT` para asignar y `POST` para el estado no es incoherencia: la asignación es idempotente (el mismo cuerpo dos veces deja el mismo estado) y el cambio de estado **no lo es** (el segundo intento debe fallar por RN-08). Los verbos reflejan eso, y conviene decirlo en el plan porque parece una inconsistencia y no lo es.

`Program.cs` **no cambia**: `IShipmentService` ya está registrado.

---

## Frontend — `frontend/src/`

Cero dependencias nuevas. El trabajo se concentra en el detalle del envío, que pasa de ser una ficha de lectura a la pantalla de operación.

### 1. `api/client.ts`

- `Shipment` gana `vehiclePlate: string | null`, `driverName: string | null`, `actualPickupAt: string | null`, `actualDeliveryAt: string | null`, `capacityWarning: string | null`.
- Tres funciones nuevas junto a las de envíos:
  ```ts
  export const assignShipment = (id: string, input: { vehicleId: string; driverId: string }) =>
    request<Shipment>(`/api/v1/shipments/${id}/assignment`, { method: 'PUT', body: JSON.stringify(input) })
  export const unassignShipment = (id: string) =>
    request<Shipment>(`/api/v1/shipments/${id}/assignment`, { method: 'DELETE' })
  export const changeShipmentStatus = (id: string, status: ShipmentStatus) =>
    request<Shipment>(`/api/v1/shipments/${id}/status`, { method: 'POST', body: JSON.stringify({ status }) })
  ```
  `unassignShipment` devuelve `Shipment`, no `void`: el `request` helper solo cortocircuita en 204 (`client.ts:74`) y aquí la respuesta es 200 con cuerpo.

### 2. `pages/shipments/ShipmentPages.tsx`

**`ShipmentDetailPage`** — reescritura, es el corazón del sprint. Deja de ser un `DetailList` de una línea.

- Estado local: `item`, `loading`, `error` (ya existen) más `pending` (acción en vuelo), `warning` (el `capacityWarning` de la última asignación) y los dos borradores del selector, `vehicleId`/`driverId`.
- Carga inicial: `Promise.all([getShipment(id), listVehicles(), listDrivers()])`. Los dos catálogos hacen falta para poblar los `<select>`, y solo se piden una vez (dependencia `[id]`).
- **Filas nuevas en el `DetailList`**: Vehículo (`item.vehiclePlate ?? '—'`), Conductor (`item.driverName ?? '—'`), Recogida real y Entrega real (`formatDate`, que ya devuelve `'—'` para `null`). Se retiran las dos filas con el aviso «próximamente» de `ShipmentPages.tsx:118`, que era el marcador de este sprint.
- **Panel de asignación**, visible solo si `item.status === 'planned'`:
  - Dos `<select>` (vehículos activos por matrícula, conductores activos por nombre) y un botón "Asignar" / "Reasignar" según haya ya asignación, más "Quitar asignación" cuando la hay.
  - Los borradores se inicializan con `item.vehicleId ?? ''` / `item.driverId ?? ''` cuando llega el envío, para que reasignar solo el conductor no obligue a reelegir el vehículo.
  - **Trampa del recurso dado de baja**: igual que el `<select>` de clientes del formulario (`ShipmentPages.tsx:106`), si el envío apunta a un vehículo o conductor que ya no está en el listado de activos, hay que añadir una `<option>` extra `"{matrícula} (dado de baja)"` con su id, o el `<select>` se renderizaría vacío y una reasignación parcial perdería el recurso en silencio. El dato para el rótulo está en `item.vehiclePlate`/`item.driverName`, que el backend devuelve aunque el catálogo esté inactivo.
  - Botón "Asignar" deshabilitado si falta cualquiera de los dos (`!vehicleId || !driverId`), reflejando en cliente la regla de asignación conjunta.
- **Botones de transición**, derivados del estado actual y no de una lista fija:
  - `planned` → "Poner en curso" (deshabilitado, con `.hint` explicativo, si `!item.vehicleId || !item.driverId` — RN-07 en cliente, para que la acción imposible se vea imposible antes de pulsarla) y "Cancelar envío".
  - `in_progress` → "Marcar entregado" y "Cancelar envío".
  - `delivered` / `cancelled` → **ningún botón**, más un `.hint`: "El envío está en un estado final y ya no puede cambiar." Es RN-08 hecho visible.
  - "Cancelar envío" y "Marcar entregado" piden **confirmación** con `window.confirm` antes de llamar, igual que la baja de los catálogos ya hace en `VehiclePages.tsx`. Son irreversibles por RN-08 y el patrón ya existe en el proyecto; no se introduce un componente de diálogo nuevo.
- **Manejo de respuesta**: las tres acciones hacen `setItem(saved)` con lo que devuelve el backend, sin recargar. Tras asignar, `setWarning(saved.capacityWarning ?? '')`, pintado como aviso **no destructivo** (clase nueva `.notice`, ámbar, distinta de `ErrorAlert`) para que se lea como "hecho, pero ojo" y no como fallo. Es la razón de ser de la decisión 1 y tiene que verse en pantalla.
- Los errores de negocio (`shipment_vehicle_busy`, `shipment_assignment_required`, …) se pintan en el `ErrorAlert` existente con el mensaje del backend, que ya viene en español y nombra el envío en conflicto. **Cero mapeo de códigos en el front**: el backend es la fuente del texto, como en todo el proyecto desde S2.

**`ShipmentListPage`** — dos columnas nuevas, Vehículo y Conductor (`vehiclePlate ?? '—'`, `driverName ?? '—'`), entre Cliente y Acciones. Es lo que S3 dejó pendiente (`docs/Sprint3Plan.md:153`) y lo que da sentido a los filtros de vehículo y conductor que ya existen en la barra desde S3 pero no tenían nada que enseñar. Con siete columnas la tabla se estrecha; el `.table-wrap` ya scrollea en horizontal y el `@media` de `index.css` ya colapsa el resto, así que no hace falta CSS de tabla nuevo.

**`ShipmentFormPage`** — **no cambia**. La asignación no se hace desde el formulario (§Alcance) y las fechas reales no son editables (decisión 4).

### 3. Estilos — `index.css`

- Variantes de color por estado del chip. Hoy `.status-chip` es una sola regla gris (`index.css:64`) y las cuatro clases `.status-planned`/`.status-in_progress`/`.status-delivered`/`.status-cancelled` que el componente ya emite (`ShipmentPages.tsx:15`) **no existen**: todos los estados se ven iguales. Con el ciclo de vida como protagonista del sprint, distinguirlos deja de ser cosmético. Paleta existente del proyecto: azul `#175cd3`/`#c5dcff` para en curso, gris `#56627a` para planificado, verde para entregado, rojo `#a51414` para cancelado.
- `.notice` para el aviso de capacidad (fondo ámbar suave, borde a juego, radio `.75rem` como las tarjetas).
- `.operation-panel`: contenedor del panel de asignación y las acciones. `display: grid`, `gap`, borde `#d9e1ec` y radio `.75rem`, coherente con `.detail-list`.
- `.operation-actions`: `display: flex`, `gap: .75rem`, `flex-wrap: wrap`, y `button { margin: 0 }` para anular el `margin-top` global de `form button` (misma corrección que `.filter-actions` necesitó en S3). En el `@media`, `flex-direction: column`.

---

## Pruebas

### Backend — servicio (`TransitOps.Tests/Services/ShipmentServiceTests.cs`)

Se amplía el archivo existente, molde y helpers ya presentes (`CreateDatabase()` con prefijo `shipment-tests-`, siembra siempre con `DateTimeKind.Utc`). Los tests de S3 que construyan `ShipmentResponse` a mano hay que ajustarlos al nuevo record posicional.

Nueve tests nuevos, densos:

1. **Asignación válida** devuelve matrícula y nombre, persiste ambas FKs, y `capacityWarning` es `null` cuando la capacidad sobra.
2. **RN-02**: asignar sobre `in_progress`, `delivered` y `cancelled` → 409 `shipment_not_assignable` (`[Theory]` con los tres estados). Igual para `UnassignAsync`.
3. **RN-03**: vehículo inexistente, vehículo inactivo, conductor inexistente y conductor inactivo → 409 con el código propio de cada uno.
4. **RN-04**: vehículo ya asignado a un envío `planned` → 409 `shipment_vehicle_busy` con la referencia del otro envío en el mensaje; ídem con un envío `in_progress`; y **sí se permite** cuando el otro envío está `delivered` o `cancelled` (es el caso que valida que la regla mira estados y no la mera existencia de la FK). Mismo bloque para el conductor.
5. **RN-04, exclusión de sí mismo**: reasignar al mismo envío el vehículo que ya tiene, cambiando solo el conductor, funciona. Es el falso positivo que rompería la reasignación.
6. **RN-05**: capacidad menor que la carga → asignación **guardada** y `capacityWarning` no nulo; capacidad suficiente, capacidad `null` y carga `null` → sin aviso. Cuatro aserciones, un test.
7. **Asignación incompleta**: solo vehículo, solo conductor, ninguno → 400 `shipment_assignment_incomplete`.
8. **Máquina de estados**: `[Theory]` recorriendo la tabla de §5 completa — las cuatro transiciones válidas y las prohibidas (terminal→cualquiera, mismo estado, `planned`→`delivered`), comprobando el **código** de error y no solo que falle. Más el caso RN-07: `planned` sin asignación → 409 `shipment_assignment_required`; con asignación → 200.
9. **Fechas reales**: `in_progress` sella `ActualPickupAt` con `Kind == Utc` y deja `ActualDeliveryAt` nulo; `delivered` sella `ActualDeliveryAt` y **no altera** `ActualPickupAt`; `cancelled` desde `in_progress` no sella entrega y conserva la recogida.

Además: **`UnassignAsync` es idempotente** (envío planificado sin asignación → 200, sin cambios) y **404** en los tres métodos con un id inexistente.

### Backend — integración (`TransitOps.Tests/Controllers/ShipmentsControllerTests.cs`)

Molde ya presente (`Factory`, `Client`, `Json`, asserts sobre `JsonNode`). Cuatro tests nuevos:

1. **Los tres endpoints nuevos exigen token** — ampliar el `[Theory]` existente de `Endpoints_require_authentication` con `PUT /assignment`, `DELETE /assignment` y `POST /status`. La ruta ya no es fija, así que el `InlineData` pasa a llevar método **y** ruta; hay que retocar los dos casos actuales.
2. **Flujo 3 completo por HTTP**: crear catálogos y envío → `PUT /assignment` 200 con `vehiclePlate`/`driverName` → `POST /status` `in_progress` 200 con `actualPickupAt` no nulo → `POST /status` `delivered` 200 → `POST /status` `cancelled` 409 `shipment_status_terminal`. Un solo test que recorre el entregable del sprint.
3. **Validación del cuerpo**: `POST /status` con `{"status":"foo"}` y con `{}` → 400 `validation_error` con `details["Status"]`; `PUT /assignment` con un GUID mal formado → 400.
4. **Anti-doble-reserva por HTTP** → 409, y el mensaje contiene la referencia del envío en conflicto (`Assert.Contains`, no igualdad exacta).

Total esperado: **45 → en torno a 58 pruebas backend**.

### Frontend (`frontend/src/App.test.tsx`)

Se amplía el `describe('envíos')` existente. **Mock despachado por URL** (ya es el patrón del archivo): el detalle ahora hace tres peticiones al montar y encadenar `mockResolvedValueOnce` rompería tests ajenos. El despachador debe distinguir `/api/v1/shipments/{id}/assignment` de `/api/v1/shipments/{id}` — hacer coincidir por sufijo, o el detalle capturará las llamadas de asignación.

Cinco tests nuevos:

1. **Detalle de un envío planificado** muestra los `<select>`, el botón "Poner en curso" **deshabilitado** por falta de asignación, y el `.hint` que lo explica.
2. **Asignar** llama al endpoint con el cuerpo correcto (`{ vehicleId, driverId }`, método `PUT`) y repinta la matrícula y el nombre con la respuesta, sin volver a pedir el envío (aserción sobre el número de llamadas a `GET /shipments/{id}`).
3. **Aviso de capacidad**: la respuesta trae `capacityWarning` → el texto aparece en pantalla y **no** como `ErrorAlert` (comprobar la clase o el rol, no solo el texto, para que el test distinga aviso de error).
4. **Envío entregado**: sin botones de acción y con el mensaje de estado final. La ausencia se comprueba con `queryBy…` + `toBeNull()`.
5. **Transición con confirmación**: mockear `window.confirm` a `true`, pulsar "Marcar entregado", verificar el `POST` a `/status` con `{ status: 'delivered' }`; y con `confirm` a `false`, verificar que **no hay llamada**.

Regla transversal que sigue vigente desde S3: **ningún assert sobre fechas formateadas ni instantes hardcodeados** (`toLocaleString` depende de ICU y el CI corre en UTC mientras la máquina está en UTC+2).

Total esperado: **13 → en torno a 18 pruebas frontend**.

---

## Documentación y cierre

1. **`docs/Sprint4Plan.md`** — este documento.
2. **`docs/design/DataModel.md`**: añadir `datetime actual_pickup_at` y `datetime actual_delivery_at` al bloque `SHIPMENT` del diagrama Mermaid; actualizar el "Alcance" de la línea 5 (S4 completa `Shipment` con la operación); ampliar la viñeta de `Shipment` con las fechas reales sellladas automáticamente y la nota de que RN-04 se aplica en servicio y **no** con un índice único, con su justificación (§4).
3. **`docs/Roadmap.md`**: nota `**Cierre (YYYY-MM-DD)**` tras la línea 135, con duración real, entregables y recuento de pruebas, en el formato exacto de las de S2 y S3.
4. **`CONTEXT.md`**: entrada en el Recent Decision Log con las cuatro decisiones confirmadas; `Repository Snapshot` a "Sprints 1–4 implemented"; `Open Notes` apuntando a S5 y **registrando la limitación de concurrencia de RN-04** como candidata a revisión en S7. **`README.md`** (`## Current Status`) y **`AGENTS.md:52`** al mismo estado.
5. **`postman/`**: en la carpeta "Envíos", tres peticiones nuevas — "Asignar vehículo y conductor", "Quitar asignación", "Cambiar estado" — con la nomenclatura en español ya usada. Documentar en la descripción de la primera que `capacityWarning` solo viaja en esa respuesta (§6).
6. **Memoria LaTeX** (`tfg/memoria/`): sección de S4 en `contido/desarrollo_iterativo.tex`, filas en `contido/validacion.tex`, párrafo en `contido/resultados.tex`, y `anexos/trazabilidad.tex` con `RF-09, RF-10` pasando a evidencia real. Capturas nuevas en `imaxes/` tomadas de la verificación en navegador: panel de asignación, aviso de capacidad, envío en curso y envío entregado sin acciones. **La máquina de estados de §5 merece un diagrama** en la memoria: es la pieza de diseño más citable del sprint.

## Verificación (extremo a extremo)

1. **Backend**: `dotnet build TransitOps.slnx --configuration Release` y `dotnet test TransitOps.slnx` en verde.
2. **Migración**: generar `AddShipmentOperation`, comprobar la ruta del archivo y que solo añade las dos columnas más el `CHECK`, y aplicarla con `dotnet run --project TransitOps.Api -- --migrate-only`.
3. **Frontend**: `npm run lint`, `npm run build` (`tsc -b` con `noUnusedLocals` y `verbatimModuleSyntax` → imports de tipo con `type` inline) y `npm run test` en verde.
4. **PostgreSQL real** (`docker compose up --build`), los dos casos que InMemory no cubre:
   - Las **subconsultas correlacionadas** de matrícula/nombre (§8) se traducen a SQL válido: `GET /api/v1/shipments` y `GET /api/v1/shipments/{id}` responden 200 con `vehiclePlate` poblado. Es el riesgo real del sprint — InMemory las evalúa en LINQ-to-Objects y nunca falla.
   - El `CHECK` `ck_shipments_actual_dates` no salta en el flujo normal (las dos transiciones seguidas graban `ActualDeliveryAt >= ActualPickupAt` por construcción, pero conviene verlo).
5. **Flujo 3 en el navegador** (`http://localhost:5173`, a través de Nginx): login → crear vehículo, conductor y envío → asignar → poner en curso → marcar entregado → comprobar que ya no hay acciones. Capturas para la memoria en este paso.
6. **Casos de error visibles**: asignar a un segundo envío el vehículo ya ocupado (409 con la referencia del otro envío), "Poner en curso" deshabilitado sin asignación, asignar un vehículo con capacidad inferior a la carga (aviso ámbar, asignación guardada), e intentar operar un envío entregado (sin botones).
7. **CI**: `.github/workflows/ci.yml` valida ambos lados en el push.

## Archivos

**Crear** — migración `Persistence/Migrations/*_AddShipmentOperation.cs` (generada); `docs/Sprint4Plan.md` (este documento).

**Editar** — `TransitOps.Api/Domain/Shipment.cs`; `TransitOps.Api/Persistence/TransitOpsDbContext.cs`; `TransitOps.Api/Features/Shipments/{ShipmentContracts,ShipmentService}.cs`; `TransitOps.Api/Controllers/ShipmentsController.cs`; `TransitOps.Tests/Services/ShipmentServiceTests.cs`; `TransitOps.Tests/Controllers/ShipmentsControllerTests.cs`; `frontend/src/api/client.ts`; `frontend/src/pages/shipments/ShipmentPages.tsx`; `frontend/src/index.css`; `frontend/src/App.test.tsx`; `docs/design/DataModel.md`; `docs/Roadmap.md`; `CONTEXT.md`; `README.md`; `AGENTS.md`; `postman/TransitOps.Api.postman_collection.json`; `tfg/memoria/contido/{desarrollo_iterativo,validacion,resultados}.tex`; `tfg/memoria/anexos/trazabilidad.tex`.

**Sin tocar** — `Common/ApiContracts.cs` (el aviso viaja dentro de `data`, decisión 1); `Program.cs` (el servicio ya está registrado); `components/{CatalogUi,ErrorAlert}.tsx` y `form-errors.ts`; `ShipmentFormPage` (la asignación no se hace en el alta); `package.json` (cero dependencias nuevas); `archive/` (oráculo de consulta; su `SetNull` en las FKs y su enfoque de fechas **no** se copian).
