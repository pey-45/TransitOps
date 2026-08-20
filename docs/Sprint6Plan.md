# Plan de implementación — Sprint 6 · Administración e Indicadores

## Contexto

Este plan asume los **Sprints 1–5 cerrados**: esqueleto autenticado, los tres catálogos, envíos con filtros, la operación del envío (asignación y ciclo de estados) y el historial de eventos con identidad del autor. Si S5 aún no está cerrado al empezar, este plan se ejecuta después: el recuento de incidencias de RF-14 lee `ShipmentEvent`, y `ICurrentUser` —que S5 introduce— es la pieza sobre la que se apoya el cambio de contraseña.

El **Sprint 6** (`docs/Roadmap.md:157-176`) cierra la funcionalidad: **RF-04** (administración de usuarios), **RF-03** (cambio de contraseña propia) y **RF-14** (resumen y estadísticas). Son los tres requisitos de prioridad media, los que la clienta situó explícitamente después de lo básico (`docs/ClientRequirements.md:99`). Al cerrarlo, **RF-01…RF-14 quedan implementados e integrados** — es la definición de hecho del sprint y el hito que abre S7.

Tres características lo distinguen de los anteriores:

- **Es el primer sprint con autorización diferenciada de verdad.** La política `Policies.Admin` existe desde S1 pero solo la usa un endpoint de prueba (`AuthController.cs:40`, `admin-check`). RF-04 es el primer módulo real tras ella, y RN-10 («solo un administrador puede dar de alta usuarios o cambiar permisos») es la primera regla de negocio que se cumple con una política y no con código.
- **Es el primer sprint que toca el slice de autenticación**, escrito en S1 y no revisitado desde entonces. `AuthService` gana la verificación de contraseña actual, y `AppUser` es la única entidad del proyecto con CRUD pendiente.
- **RF-14 es el primer requisito de solo-lectura con agregación.** No hay entidad nueva ni migración: son `GROUP BY` sobre datos que ya existen. El riesgo se desplaza del modelo a la consulta, y en particular a lo que InMemory traduce y PostgreSQL no (§7).

Entregable demostrable: un administrador da de alta un operador, cambia su propia contraseña, y todos ven el resumen operativo al entrar.

## Decisiones confirmadas con el usuario

1. **RF-14 completo con periodo configurable**: `GET /summary?from=&to=` devuelve envíos por estado (global), actividad por vehículo y por conductor en el periodo, e incidencias del periodo. Se descarta la «versión mínima» que el roadmap autorizaba (`docs/Roadmap.md:221`) porque dejaría fuera un criterio de aceptación de RF-14.
2. **El resumen vive en Inicio**, sustituyendo el saludo de esqueleto de S1. Es lo que pide el roadmap («todos ven el resumen operativo al entrar») y retira texto provisional que S8 tendría que limpiar de todos modos (`HomePage.tsx:8`: «Los módulos operativos se incorporarán en los siguientes sprints»).
3. **El resumen es visible para operadores y administradores** (`Policies.Operational`). El cliente lo describe como ahorro de trabajo diario, no como informe de dirección (`docs/ClientRequirements.md:73`).

## Alcance y no-objetivos

**Dentro**: CRUD de usuarios restringido a administradores con protección del último administrador; cambio de contraseña propia con confirmación de la actual; endpoint y pantalla de resumen; SPA con administración de usuarios oculta a operadores y protegida también por ruta; Inicio convertido en panel; pruebas backend y frontend; actualización de `DataModel.md`, Postman, Roadmap, CONTEXT, README y AGENTS.

**Fuera**: recuperación de contraseña olvidada — RF-03 la declara **explícitamente fuera de alcance** (`docs/Requirements.md:97`); que un administrador reasigne la contraseña de otro (mismo motivo: el requisito lo menciona como el camino manual, no como función); gráficos en el resumen (el cliente dijo que no hacen falta); exportación a CSV/PDF; auditoría de acciones administrativas — RNF-04 habla de trazabilidad **sobre el envío**, que S5 ya cubre.

**No-objetivo deliberado**: **no se borran usuarios**, solo se desactivan. RNF-03 y RN-13 lo exigen, y los eventos de S5 tienen FK `Restrict` a `AppUser` (§S5, §2), así que un borrado real fallaría contra la base de datos. El endpoint es un `DELETE` que desactiva, igual que en los catálogos.

---

## Backend — `TransitOps.Api/`

**Sin migración.** Es el primer sprint sin cambios de esquema: `AppUser` ya tiene todos los campos que RF-04 necesita (`Role`, `IsActive`, `UpdatedAt`) desde S1, y RF-14 solo agrega datos existentes. Conviene decirlo explícitamente porque rompe la cadencia de los cuatro sprints anteriores y es la prueba de que el modelo se diseñó completo al principio, como el roadmap defiende (`docs/Roadmap.md:10`).

### 1. Punto crítico — RN-12: no quedarse sin administrador activo

Es la regla más delicada del sprint. Enunciado: «debe existir siempre al menos un administrador activo». Tres operaciones distintas pueden violarla, y es fácil proteger una y olvidar las otras dos:

| Operación | Cómo viola RN-12 |
|---|---|
| Desactivar un usuario | Si es el último administrador activo |
| Cambiar el rol de administrador a operador | Deja de ser administrador: mismo efecto, distinta puerta |
| *(no aplica)* Borrar | No existe borrado (§Alcance) |

Un único helper privado cubre las tres:

```csharp
private async Task EnsureNotLastAdmin(AppUser user, CancellationToken cancellationToken)
{
    if (user.Role != UserRole.Admin || !user.IsActive) return;
    if (!await dbContext.AppUsers.AnyAsync(item =>
            item.Id != user.Id && item.IsActive && item.Role == UserRole.Admin, cancellationToken))
        throw new ApiException(409, "last_admin_protected",
            "No se puede dejar la aplicación sin ningún administrador activo.");
}
```

Cuatro detalles que importan:

- **La guarda de salida (`user.Role != Admin || !user.IsActive`) es lo que hace el helper reutilizable.** Desactivar un operador, o uno que ya estaba inactivo, no toca RN-12 y no debe consultar la base de datos.
- **`item.Id != user.Id` es imprescindible**, por la misma razón que en RN-04 de S4: sin la exclusión, el propio usuario que se está desactivando cuenta como administrador activo y la regla nunca salta. Es el fallo silencioso más probable de todo el sprint — y el más grave, porque solo se descubre cuando alguien se queda fuera de la aplicación.
- **Se comprueba *antes* de mutar, no después.** Si se cambiara `IsActive = false` y luego se consultara, la entidad rastreada ya estaría modificada y `AnyAsync` podría verla como inactiva según el proveedor. Comprobar primero, mutar después.
- **La misma limitación de concurrencia que RN-04**: dos administradores desactivándose mutuamente a la vez pueden pasar ambas comprobaciones. Se documenta como limitación conocida, coherente con la ya anotada en S4, y se anota para revisión en S7. Es aceptable para dos personas de administración (`docs/ClientRequirements.md:78`); fingir que un índice lo resuelve, no.

**Autodesactivación y autodegradación**: un administrador que no es el último **puede** desactivarse a sí mismo o bajarse a operador. RN-12 solo protege el colectivo, no al individuo, y añadir «no puedes modificarte a ti mismo» sería inventar un requisito. La consecuencia práctica —que su token sigue siendo válido hasta caducar— se trata en §4.

### 2. Contratos — `Features/Users/UserContracts.cs`

**Slice nuevo `Features/Users/`**, no una ampliación de `Features/Auth/`. Razón: `Auth` resuelve *quién eres* (login, bootstrap) y `Users` resuelve *quiénes hay* (CRUD administrativo). Son dos responsabilidades con políticas distintas (`AllowAnonymous` frente a `Admin`) y mezclarlas dejaría un `AuthService` con seis métodos y dos motivos de cambio. `UserResponse`, en cambio, **se reutiliza tal cual** desde `Features/Auth/AuthContracts.cs:18`: ya tiene la forma exacta que RF-04 necesita y duplicarla sería peor.

- **`CreateUserRequest(string Username, string Email, string Password, string? Role)`**: `Username` `[Required, StringLength(80, MinimumLength = 3)]`, `Email` `[Required, EmailAddress, StringLength(254)]`, `Password` `[Required, StringLength(128, MinimumLength = 10)]` — **exactamente las mismas anotaciones que `BootstrapAdminRequest`** (`AuthContracts.cs:5-8`), porque es la misma decisión de negocio sobre la misma entidad y divergir crearía dos políticas de contraseña. `Role` como **`string?` con `[Required, RegularExpression("^(admin|operator)$")]`** y mensaje en español: mismo criterio ya establecido en S3 y S5 (`ShipmentContracts.cs:32`) — con un enum, `?role=foo` daría el mensaje del binder **en inglés**.
- **`UpdateUserRoleRequest(string? Role)`** y **`UpdateUserActivationRequest(bool IsActive)`**: dos operaciones separadas, no un `PUT` que lo cambie todo. Razones concretas: los dos criterios de aceptación de RF-04 son independientes; cada uno tiene su propio choque con RN-12; y un `PUT` unificado obligaría a decidir qué pasa si llegan los dos cambios a la vez y uno es inválido. **`IsActive` es `bool` no anulable**, y por tanto un cuerpo vacío se interpreta como `false` — hay que documentarlo y validarlo, o desactivar por accidente será trivial. Alternativa aceptable: `bool?` con `[Required]`, que sí distingue ausencia. **Se elige `bool?` con `[Required]`** por eso.
- **`ChangePasswordRequest(string CurrentPassword, string NewPassword)`**: ambos `[Required, StringLength(128)]`, el nuevo con `MinimumLength = 10`. Vive en **`Features/Auth/`**, no en `Users`: es una acción sobre la propia sesión, no administración de terceros, y su endpoint cuelga de `/auth`.
- **`ListUsersQuery(bool? IncludeInactive)`**: por defecto solo activos, como los catálogos. Pero RF-04 pide poder **reactivar** a alguien, y un usuario inactivo que no se lista es imposible de reactivar desde la interfaz. Este es el punto donde el patrón de catálogos **no sirve** y hay que desviarse: sin `includeInactive`, la función de activación queda inalcanzable. Es una diferencia deliberada respecto a `CustomerService.GetAllAsync` (`CustomerService.cs:10`) y merece la línea de justificación.
- **`IUserService`**: `GetAllAsync(ListUsersQuery, ct)`, `GetByIdAsync`, `CreateAsync`, `ChangeRoleAsync`, `ChangeActivationAsync`. **Sin `UpdateAsync`**: RF-04 no pide editar nombre ni correo de otra persona, y añadirlo traería la unicidad de credenciales a un tercer sitio.

### 3. Servicio — `Features/Users/UserService.cs`

`sealed class UserService(TransitOpsDbContext dbContext, IPasswordHasher<AppUser> passwordHasher)`. El hasher ya está registrado desde S1 (`Program.cs:58`).

- **`CreateAsync`**: normalizar (`Username.Trim()`, `Email.Trim().ToLowerInvariant()` — **igual que `BootstrapAsync`**, `AuthService.cs:32`) → comprobar unicidad → hashear → `Add`/`SaveChanges` → `Map`.
  - **Unicidad: se reutiliza el código `user_credentials_conflict`** (409) que `BootstrapAsync` ya usa (`AuthService.cs:34`), con el mismo mensaje. Es literalmente la misma condición sobre la misma tabla; inventar un código nuevo daría dos respuestas distintas al mismo problema.
  - **La unicidad es global, no «entre activos»**, a diferencia de vehículos y conductores. `AppUser` tiene índices únicos **sin filtro** sobre `Username` y `Email` (`TransitOpsDbContext.cs:23-24`), decididos así en S1. Consecuencia real y contraintuitiva: **el nombre de un usuario desactivado no se puede reutilizar**. Es coherente con la identidad (dos personas distintas con el mismo login serían indistinguibles en el historial de S5) y hay que documentarlo, porque contradice el «reuso de identificadores tras la baja» que S2 estableció para los catálogos.
  - **Un usuario nuevo se crea activo.** No hay criterio que diga lo contrario y es lo que espera quien lo da de alta.
- **`ChangeRoleAsync`**: `Existing(id)` → si el rol solicitado es el que ya tiene, **devolver sin más** (no es un error) → si baja de admin a operator, `EnsureNotLastAdmin` → asignar → `UpdatedAt` → `SaveChanges`.
- **`ChangeActivationAsync`**: `Existing(id)` → si `IsActive` ya coincide, devolver sin más → si desactiva, `EnsureNotLastAdmin` → asignar → `UpdatedAt` → `SaveChanges`. **Reactivar nunca choca con RN-12** (añadir un administrador no puede dejar la aplicación sin ninguno).
- **`Existing(id, ct)`** → 404 `user_not_found`, **sin filtrar por `IsActive`**: hay que poder direccionar a un usuario inactivo para reactivarlo. Es la segunda desviación del patrón de catálogos, por la misma causa que `IncludeInactive`, y por eso el helper se llama `Existing` y no `Active` — la misma distinción de nombre que S3 hizo para `Shipment` (`docs/Sprint3Plan.md:36`).
- **`GetAllAsync`**: `OrderBy(Role).ThenBy(Username)` — administradores primero, luego alfabético. Orden total y estable.
- **`Map`** delega en la forma de `UserResponse` ya existente. `PasswordHash` **nunca** sale del servicio; el record no tiene ese campo, así que el compilador lo garantiza.

### 4. Punto crítico — RF-03 y las contraseñas

`AuthService` gana `ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct)`, e `IAuthService` el método. Necesita saber **quién** pide el cambio: `AuthService` recibe `ICurrentUser` (introducido en S5) como quinto parámetro del constructor.

Flujo:
1. `currentUser.Id` → si es `null`, 401 (no debería ocurrir: el endpoint está tras `[Authorize]`).
2. Cargar el usuario; si no existe o está inactivo, 401 `invalid_credentials` — un token válido de un usuario desactivado entre tanto no debe poder cambiar nada.
3. **Verificar la contraseña actual** con `passwordHasher.VerifyHashedPassword`. Fallo → **401 `invalid_credentials`**, el mismo código y mensaje que el login (`AuthService.cs:52`). Es RN-14.
4. Hashear la nueva, asignar, `UpdatedAt`, `SaveChanges`.

Cinco puntos que hay que acertar:

- **`VerifyHashedPassword` puede devolver `SuccessRehashNeeded`, no solo `Success` o `Failed`.** El login existente compara `== PasswordVerificationResult.Failed` (`AuthService.cs:51`), que trata correctamente los tres casos. **Repetir ese patrón exacto**: comparar contra `Failed`, no contra `Success`. Escribir `== Success` rechazaría contraseñas correctas en cuanto el hasher cambie de parámetros, y sería un fallo intermitente sin causa aparente.
- **Rechazar que la nueva contraseña sea igual a la actual** → 400 `password_unchanged`. No está en RF-03 explícitamente, pero un cambio que no cambia nada es un error del usuario, no una operación válida, y avisar es más útil que un 200 mentiroso. Se compara **verificando el hash**, no comparando cadenas.
- **No se emite un token nuevo ni se invalidan los existentes.** El JWT es autocontenido y sin lista de revocación (decisión de S1), así que la sesión actual sigue viva y las demás también. Cerrar sesión en todos los dispositivos exigiría revocación, que S1 dejó fuera. **Se documenta como limitación conocida**, en la misma nota donde ya está el tradeoff de `localStorage` para revisar en S7 (`CONTEXT.md:77`).
- **La respuesta no lleva cuerpo útil**: 200 con `new { changed = true }`, siguiendo el precedente de `DeactivateAsync` en los catálogos (`CustomersController.cs:41`, `new { deactivated = true }`). Coherencia sobre elegancia.
- **El mismo razonamiento vale para desactivar a alguien desde RF-04**: su token sigue funcionando hasta caducar (máximo 60 minutos por defecto, `JwtOptions.ExpirationMinutes`). RN-13 dice que un usuario desactivado no puede acceder, y esto lo cumple **con retardo**. Merece quedar documentado: es una consecuencia real de la arquitectura sin estado elegida en S1, no un descuido, y la mitigación (validar `IsActive` en cada petición) es precisamente el tipo de endurecimiento que S7 puede evaluar.

### 5. Contratos y servicio del resumen — `Features/Reporting/`

Slice nuevo `Features/Reporting/`, con `SummaryContracts.cs` y `SummaryService.cs`. No cuelga de `Shipments` porque cruza tres entidades (envíos, catálogos, eventos) y no es comportamiento del agregado envío.

- **`SummaryQuery(DateTime? From, DateTime? To)`**, con `IValidatableObject` que rechaza rango invertido con `details["To"]` — mismo patrón que `ListShipmentsQuery` (`ShipmentContracts.cs:41-45`), del que se puede copiar la forma.
- **`SummaryResponse`**:
  ```csharp
  public sealed record SummaryResponse(
      ShipmentStatusCounts Shipments,     // planned, inProgress, delivered, cancelled, total
      IReadOnlyList<ResourceActivity> Vehicles,
      IReadOnlyList<ResourceActivity> Drivers,
      int Incidents,
      DateTime? From, DateTime? To);
  public sealed record ShipmentStatusCounts(int Planned, int InProgress, int Delivered, int Cancelled, int Total);
  public sealed record ResourceActivity(Guid Id, string Label, int ShipmentCount);
  ```
  - **`ShipmentStatusCounts` como record de cuatro enteros, no un diccionario.** Los cuatro estados son fijos y conocidos; un `Dictionary<string,int>` obligaría al front a manejar claves ausentes cuando un estado tiene cero envíos, que es justo el caso normal al empezar.
  - **Los contadores de estado son globales, sin filtrar por periodo.** «Cuántos envíos hay **ahora mismo** en cada estado» (`docs/ClientRequirements.md:73`) es una foto del presente; filtrarla por fechas la haría incomparable con lo que el operador ve en el listado. La actividad por recurso y las incidencias **sí** usan el periodo. Esa asimetría es deliberada, es lo que pide el cliente, y **tiene que verse en la interfaz** o resultará confusa (§Frontend).
  - **`Label` en lugar de `LicensePlate`/`Name`**: permite un tipo único para vehículos y conductores y un solo componente de tabla en el front. Se rellena con la matrícula o el nombre.
  - **`From`/`To` se devuelven resueltos**, para que la pantalla muestre el periodo realmente aplicado y no lo que creía haber pedido.
- **Periodo por defecto**: si no llegan fechas, **los últimos 30 días** (`To = ahora`, `From = ahora - 30 días`). Un valor concreto y explicable; sin defecto, «actividad por vehículo» sobre todo el histórico crecería sin límite y perdería utilidad. Las fechas pasan por `ShipmentTime.Utc` (§S3) — es un helper de `Features/Shipments`, así que hay dos opciones honestas: **hacerlo `public` y reutilizarlo**, o duplicar tres líneas. **Reutilizarlo**, y anotar el cambio de visibilidad.

### 6. Servicio del resumen — cómo se calculan las cifras

- **Envíos por estado**: un `GROUP BY` y no cuatro `CountAsync`.
  ```csharp
  var counts = await dbContext.Shipments.AsNoTracking()
      .GroupBy(item => item.Status)
      .Select(group => new { Status = group.Key, Count = group.Count() })
      .ToListAsync(cancellationToken);
  ```
  Los estados sin envíos **no aparecen** en el resultado: hay que resolver a cero al construir el record. Es el error clásico de un `GROUP BY` leído como si fuera exhaustivo.
- **Actividad por recurso**: envíos del periodo agrupados por `VehicleId`, descartando `null`. La ventana se aplica sobre **`PlannedPickupAt`**, no sobre `CreatedAt` ni `ActualPickupAt`: es la fecha operativa que el usuario ve en el listado y filtra en la barra de S3, así que las cifras del resumen cuadran con lo que puede contar a mano. Elegir `ActualPickupAt` excluiría los planificados; elegir `CreatedAt` mezclaría el momento de teclear con el de operar. **Decisión que hay que escribir**, porque las tres son defendibles y solo una es coherente con el resto de la aplicación.
  - La etiqueta se resuelve con un `join` a `Vehicles`/`Drivers` **sin filtrar por `IsActive`**: un vehículo dado de baja que trabajó en el periodo debe aparecer con su actividad (RN-15, RNF-03). Filtrarlo haría desaparecer trabajo real del resumen.
  - `OrderByDescending(ShipmentCount).ThenBy(Label)` — el desempate alfabético evita que dos recursos empatados se reordenen entre peticiones.
- **Incidencias**: `ShipmentEvents.CountAsync(e => e.EventType == Incident && e.OccurredAt >= from && e.OccurredAt <= to)`. Se cuenta por **`OccurredAt`**, no `CreatedAt`: cuándo pasó, no cuándo se anotó. Es la razón por la que S5 guardó ambos campos, y aquí se cobra.

**Riesgo técnico principal del sprint** (§Verificación): `GroupBy` con proyección es lo que EF traduce peor y donde InMemory y PostgreSQL más divergen. InMemory evalúa en LINQ-to-Objects y **acepta cualquier cosa**; Npgsql puede lanzar «could not be translated» en tiempo de ejecución para el mismo código. Con tests verdes y un 500 en Docker — el patrón exacto que S3 documentó con las fechas UTC. Mitigación: mantener las agrupaciones simples (clave escalar, `Count()`), resolver las etiquetas en una **segunda consulta** por ids en lugar de un `join` dentro del `GroupBy`, y **verificar contra PostgreSQL real antes de dar el sprint por hecho**.

### 7. Controladores

- **`Controllers/UsersController.cs`**: `[Route("api/v1/users")]`, **`[Authorize(Policy = Policies.Admin)]` a nivel de clase** — RN-10 resuelto con una línea, y la política ya existe desde S1. Cinco acciones: `GET` (lista, con `[FromQuery] ListUsersQuery`), `GET {id:guid}`, `POST` (201), `PUT {id:guid}/role`, `PUT {id:guid}/activation`. Los dos `PUT` devuelven el usuario actualizado.
  - **`[FromQuery]` explícito y obligatorio** en la lista: con `[ApiController]`, un tipo complejo se infiere como `[FromBody]` y el `GET` respondería 400 (lección de S3, `docs/Sprint3Plan.md:132`).
  - **`activation` como sub-recurso `PUT`, no `DELETE`**: a diferencia de los catálogos, aquí hace falta poder **reactivar**, y un `DELETE` solo expresa una dirección. Desviación deliberada del patrón, justificada por RF-04.
- **`AuthController`** gana `[HttpPost("password")]` con `[Authorize(Policy = Policies.Operational)]` — cualquiera cambia **su propia** contraseña, no solo administradores. La ruta cuelga de `/auth` porque es la propia sesión.
- **`Controllers/SummaryController.cs`**: `[Route("api/v1/summary")]`, `[Authorize(Policy = Policies.Operational)]` (decisión 3), una acción `GET` con `[FromQuery] SummaryQuery`.
- **`AuthController.AdminCheck` se retira.** Era el andamio de S1 para probar la política `Admin` (`AuthController.cs:40`); con `UsersController` detrás de esa política, ya no aporta nada y es exactamente el contenido provisional que S8 tendría que limpiar. **Comprobar antes qué test de S1 lo usa** (`AuthControllerTests`) y reapuntarlo a un endpoint real de usuarios: la cobertura de «un operador no alcanza la zona de administración» debe conservarse, no perderse.

`Program.cs`: `AddScoped<IUserService, UserService>()` y `AddScoped<ISummaryService, SummaryService>()`, más los dos `using`.

---

## Frontend — `frontend/src/`

Cero dependencias nuevas.

### 1. `api/client.ts`

- `UserInput`, `SummaryResponse` y tipos auxiliares (`ShipmentStatusCounts`, `ResourceActivity`). `User` **ya existe** (`client.ts:2`) con la forma exacta que devuelve el backend: no se duplica.
- Funciones: `listUsers(includeInactive?)`, `getUser(id)`, `createUser(input)`, `changeUserRole(id, role)`, `changeUserActivation(id, isActive)`, `changePassword(currentPassword, newPassword)`, `getSummary(from?, to?)`. Se reutiliza el helper `query(...)` de S3 (`client.ts:104`) — hoy está tipado como `ShipmentFilters`; hay que **generalizar su tipo** a un `Record<string, string | number | boolean | undefined>` para admitir los parámetros del resumen y de usuarios, sin cambiar su comportamiento.

### 2. `pages/HomePage.tsx` — el panel de resumen

Reescritura completa (decisión 2). Deja de ser el saludo de S1 y pasa a ser lo primero útil que se ve al entrar.

- Fila de tarjetas con los cuatro contadores de estado más el total, reutilizando `StatusChip`. **Ese componente vive hoy en `ShipmentPages.tsx`** (`ShipmentPages.tsx:15`) y no se exporta: hay que **moverlo a `components/CatalogUi.tsx`** junto con `statusLabel`, y actualizar los imports de `ShipmentPages`. Es refactor mecánico, pero conviene hacerlo antes de escribir la pantalla y no duplicar el chip.
- **Cada contador enlaza al listado filtrado** (`/envios?status=in_progress`). Los filtros de S3 viven en la URL precisamente para esto (`docs/Sprint3Plan.md:148`), y es la diferencia entre un panel decorativo y uno operativo. Cuesta un `<Link>`.
- Selector de periodo (dos `<input type="date">` y un botón) que **solo afecta a la actividad y a las incidencias**. La asimetría de la decisión de §5 tiene que ser visible: rótulos explícitos («Envíos por estado — situación actual» frente a «Actividad en el periodo»), o el usuario creerá que los contadores están filtrados. **Es el punto donde una interfaz descuidada convierte un dato correcto en un dato engañoso.**
- Dos tablas compactas (vehículos, conductores) con etiqueta y número de envíos, con `Empty` cuando no hay actividad en el periodo — el caso normal si el periodo es corto y hay que tratarlo.
- Se conserva el saludo con el nombre, ya reducido a una línea. Se retira la `role-card` y el texto «Los módulos operativos se incorporarán en los siguientes sprints», que ya no es cierto.
- **Trampa de fechas conocida**: `new Date('2026-08-01')` se parsea como **UTC** y `'2026-08-01T00:00'` como **local**. Los helpers `dayStart`/`dayEnd` de `ShipmentPages.tsx:16-17` ya resuelven esto; **moverlos también a un módulo compartido** y reutilizarlos, en lugar de reescribirlos.

### 3. `pages/users/UserPages.tsx`

Molde de los catálogos, pero con dos diferencias reales:

- **`UserListPage`**: tabla con usuario, correo, rol, estado y acciones. Casilla «Mostrar también los desactivados» que llama con `includeInactive` — sin ella, reactivar es imposible (§2 del backend). Acciones por fila: cambiar rol (un `<select>` que envía al cambiar, o un botón «Hacer administrador»/«Hacer operador») y activar/desactivar, ambas con `window.confirm` — el patrón ya usado en los catálogos.
- **`UserFormPage`**: solo alta (no hay edición de datos, §2). Usuario, correo, contraseña y `<select>` de rol. La contraseña con `type="password"` y `minLength={10}`, coherente con el backend.
- **El error de RN-12 se pinta con el mensaje del backend** en `ErrorAlert`. Cero mapeo de códigos en el front, como en todo el proyecto desde S2.
- **Deshabilitar en cliente las acciones que RN-12 prohíbe** sería adivinar: el front no sabe cuántos administradores activos hay sin contarlos, y contar en cliente duplica la regla. Se deja que el backend responda 409 y se muestra el mensaje. Es la decisión correcta y conviene justificarla, porque «deshabilitar el botón» parece mejor UX y aquí no lo es.

### 4. `pages/ChangePasswordPage.tsx`

Formulario propio, accesible desde el menú de cuenta del `AppLayout`. Tres campos (actual, nueva, repetir nueva), con la coincidencia de las dos nuevas comprobada **en cliente** —no se envía al servidor lo que se puede descartar aquí— y el 401 de contraseña actual incorrecta pintado bajo el campo correspondiente, no solo en la alerta general. Al terminar, mensaje de éxito y **no** cerrar sesión (§4: el token sigue válido).

### 5. Rutas, navegación y protección por rol

- `App.tsx`: `/usuarios`, `/usuarios/nuevo` y `/cambiar-contrasena`.
- **Las rutas de usuarios necesitan protección propia, no solo ocultar el enlace.** `ProtectedRoute` solo comprueba que haya sesión (`ProtectedRoute.tsx:8`). El criterio de aceptación de S6 es explícito: «un operador no alcanza la administración **ni por navegación directa**» (`docs/Roadmap.md:169`). Hace falta un `AdminRoute` análogo que redirija a `/` si `session.user.role !== 'admin'`, envolviendo esas dos rutas. Sin él, teclear `/usuarios` cargaría la pantalla (el backend devolvería 403 y se vería una alerta, pero la pantalla se habría montado) — y el criterio pide que no se alcance.
- `AppLayout.tsx`: sustituir `{session?.user.role === 'admin' && <span className="future-nav">Usuarios (próximamente)</span>}` (`AppLayout.tsx:15`) por un `NavLink` real a `/usuarios`, con la misma condición de rol. Y añadir el acceso a «Cambiar contraseña» en la zona de cuenta. Con esto **desaparece el último `future-nav`** del proyecto; comprobar si la clase queda sin uso en `index.css` y retirarla.

### 6. Estilos — `index.css`

- `.summary-cards`: grid `auto-fit minmax(9rem, 1fr)`, tarjetas con borde `#d9e1ec` y radio `.75rem`, cifra grande y rótulo pequeño en `#56627a`. Coherente con `.detail-list`.
- `.summary-section` con su encabezado, para que la separación entre «situación actual» y «periodo» sea visual y no solo textual.
- `.role-badge` reutilizando la forma de `.status-chip`, con variante para administrador; una variante apagada para usuario desactivado.
- Retirar `.future-nav` si queda sin uso (§5).
- En el `@media`, las tarjetas a una columna.

---

## Pruebas

### Backend — usuarios (`TransitOps.Tests/Services/UserServiceTests.cs`, nuevo)

Molde `CustomerServiceTests`, `CreateDatabase()` con prefijo `user-tests-`. Nueve tests:

1. **Alta** normaliza usuario y correo (minúsculas), crea activo, hashea la contraseña y **no la devuelve en claro** (afirmar sobre la entidad persistida que `PasswordHash != request.Password`).
2. **Unicidad**: usuario repetido y correo repetido → 409 `user_credentials_conflict`; y **repetido con un usuario desactivado** → también 409, documentando que el identificador no se reutiliza (§3).
3. **RN-12 al desactivar**: último administrador activo → 409 `last_admin_protected`; con dos administradores → permitido; desactivar un **operador** siendo el único administrador → permitido.
4. **RN-12 al cambiar de rol**: bajar el último administrador a operador → 409; con dos administradores → permitido.
5. **RN-12 no salta sobre un usuario ya inactivo** (la guarda de salida del helper).
6. **La autoexclusión funciona**: el usuario que se desactiva no se cuenta a sí mismo como administrador activo. Es el test que atrapa el fallo silencioso del §1 — con un único administrador debe dar 409, y sin `item.Id != user.Id` daría 200.
7. **Reactivar** nunca choca con RN-12.
8. **Operaciones idempotentes**: asignar el rol que ya tiene, o el `IsActive` que ya tiene, devuelve 200 sin cambios.
9. **`includeInactive`**: por defecto solo activos; con la bandera, todos. Más 404 `user_not_found` con un id inexistente, y que `Existing` **sí** encuentra a los inactivos.

### Backend — contraseña (`TransitOps.Tests/Services/AuthServiceTests.cs`, se amplía)

El constructor de `AuthService` gana `ICurrentUser`, así que **todas** las construcciones del archivo dejan de compilar. Extraer un helper `CreateService(...)` primero (misma maniobra que S5 necesitó en `ShipmentServiceTests`). Cuatro tests:

1. **Cambio correcto**: se puede iniciar sesión con la contraseña nueva y **no** con la antigua. Verificarlo vía `LoginAsync` y no solo comparando hashes es lo que prueba que el cambio es efectivo de verdad.
2. **Contraseña actual incorrecta** → 401 `invalid_credentials`, y la contraseña **no cambia** (comprobar que el login antiguo sigue funcionando).
3. **Nueva igual a la actual** → 400 `password_unchanged`.
4. **Usuario desactivado entre tanto** → 401, aunque la contraseña actual sea correcta.

### Backend — resumen (`TransitOps.Tests/Services/SummaryServiceTests.cs`, nuevo)

Seis tests. Siembra con `DateTimeKind.Utc` siempre.

1. **Contadores por estado**, incluidos **estados con cero envíos** → devuelven `0`, no se omiten. Es el fallo del `GROUP BY` no exhaustivo (§6).
2. **Los contadores de estado ignoran el periodo** (sembrar envíos fuera de la ventana y comprobar que siguen contando). Fija la asimetría deliberada.
3. **Actividad por vehículo y conductor** dentro del periodo, con envíos fuera de la ventana excluidos, y **límites inclusivos**.
4. **Un recurso dado de baja con actividad en el periodo aparece** con su etiqueta (RN-15).
5. **Envíos sin asignación no aparecen** en la actividad (el `null` descartado).
6. **Incidencias** se cuentan por `OccurredAt` y solo del tipo `incident` — sembrar un `checkpoint` en el mismo periodo y comprobar que **no** suma. Más: rango invertido → error, y periodo por defecto de 30 días cuando no llegan fechas.

### Backend — integración (`TransitOps.Tests/Controllers/`)

`UsersControllerTests.cs` y `SummaryControllerTests.cs` nuevos; `AuthControllerTests.cs` se amplía. Molde `CatalogControllerTests` (`FactoryWithOperator`, `AuthenticatedClient`, `ReadJson`). Seis tests:

1. **`/users` exige rol admin**: sin token → 401; **con token de operador → 403** con el código `authorization_forbidden` que `Program.cs:93` ya produce. `[Theory]` sobre los cinco métodos/rutas. Es RN-10 verificado de punta a punta y **reemplaza la cobertura que `admin-check` daba** (§7).
2. **Alta y listado** por HTTP con el sobre común, 201, y `passwordHash` **ausente de la respuesta** (`Assert.Null(json["data"]!["passwordHash"])`) — la garantía de que RNF-02 no se rompe por un cambio de contrato.
3. **RN-12 por HTTP** → 409 `last_admin_protected` al desactivar el único administrador.
4. **`POST /auth/password`** con la contraseña correcta → 200, y **el login posterior con la nueva funciona** por HTTP; con la actual incorrecta → 401.
5. **`/auth/password` es accesible a un operador** (no solo a admin) — el matiz de política que se olvida.
6. **`GET /summary`** devuelve la forma esperada con un envío sembrado, y `?from=…&to=…` con rango invertido → 400 con `details["To"]`.

Total esperado, partiendo de ~75 tras S5: **en torno a 100 pruebas backend**.

### Frontend (`frontend/src/App.test.tsx`)

Seis tests nuevos. El despachador por URL ya es el patrón del archivo; añadir `/api/v1/users`, `/api/v1/summary` y `/api/v1/auth/password`.

1. **Inicio pinta el resumen** con los contadores del mock, y los rótulos distinguen «situación actual» de «periodo».
2. **Un contador enlaza al listado filtrado** (`href` con `status=in_progress`).
3. **La navegación oculta «Usuarios» a un operador** y la muestra a un administrador.
4. **Un operador que teclea `/usuarios` es redirigido** y no ve la pantalla. Es el criterio de aceptación explícito del sprint; comprobar la ausencia con `queryBy… + toBeNull()`.
5. **Alta de usuario** envía el cuerpo correcto y, ante un 409, muestra el mensaje del backend.
6. **Cambio de contraseña**: las dos nuevas que no coinciden **no llaman al servidor**; con datos válidos, se llama a `/auth/password` con el cuerpo correcto.

Total esperado, partiendo de ~23 tras S5: **en torno a 29 pruebas frontend**.

---

## Documentación y cierre

1. **`docs/Sprint6Plan.md`** — este documento.
2. **`docs/design/DataModel.md`**: **sin cambios de esquema** (§Backend), pero conviene una nota en la viñeta de `AppUser` sobre la unicidad **global** de usuario y correo (a diferencia de los catálogos) y su consecuencia: el identificador de un usuario desactivado no se reutiliza.
3. **`docs/Roadmap.md`**: nota `**Cierre (YYYY-MM-DD)**` tras la línea 176, en el formato de S2–S5. Debe decir explícitamente que **RF-01…RF-14 quedan implementados e integrados** — es la definición de hecho del sprint y el hito que habilita S7.
4. **`CONTEXT.md`**: entrada en el Recent Decision Log con las tres decisiones; `Repository Snapshot` a "Sprints 1–6 implemented / all RF implemented"; `Open Notes` con S7 como prioridad y **tres limitaciones conocidas agrupadas para revisión allí**: el token que sobrevive a la desactivación (§4), la ausencia de revocación al cambiar la contraseña (§4) y las dos carreras de concurrencia (RN-04 de S4 y RN-12 de este sprint). Tenerlas juntas y por escrito es lo que convierte S7 en un sprint con agenda. **`README.md`** y **`AGENTS.md:52`** al mismo estado.
5. **`postman/`**: carpeta «Usuarios» (listar con y sin inactivos, crear, cambiar rol, cambiar activación), «Cambiar contraseña» en la carpeta de autenticación, y «Resumen» con el query de periodo.

## Verificación (extremo a extremo)

1. **Backend**: `dotnet build TransitOps.slnx --configuration Release` y `dotnet test TransitOps.slnx` en verde. El primer intento fallará a compilar por el constructor de `AuthService` (§Pruebas) y por la retirada de `admin-check`; ambos son esperados.
2. **Sin migración**: confirmar con `dotnet ef migrations list` que no hay ninguna pendiente y que el snapshot no ha cambiado. Si `dotnet ef migrations add` generase algo, es que se tocó el modelo sin querer.
3. **Frontend**: `npm run lint`, `npm run build` y `npm run test` en verde. Atención a `noUnusedLocals` tras mover `StatusChip` y los helpers de fecha.
4. **PostgreSQL real** (`docker compose up --build`) — **el paso imprescindible de este sprint**:
   - `GET /api/v1/summary` responde **200 y no 500**. Es el riesgo principal (§6): los `GroupBy` que InMemory acepta pueden no traducirse en Npgsql. Probar además con periodo explícito, sin periodo, y con la base **vacía** (el caso en que todos los grupos están ausentes).
   - `GET /api/v1/summary?from=2026-08-01` con **date-only**, que llega `Unspecified` y reventaría contra `timestamptz` sin la normalización (§5). Es el mismo fallo que S3 documentó.
5. **Flujo funcional en el navegador**: login como administrador → Inicio muestra el resumen → dar de alta un operador → cerrar sesión → entrar como el operador → **«Usuarios» no aparece y `/usuarios` redirige** → cambiar su propia contraseña → volver a entrar con la nueva.
6. **Casos de error visibles**: intentar desactivar al único administrador (409 con mensaje claro), contraseña actual incorrecta (aviso bajo el campo), usuario repetido (409).
7. **Comprobación de coherencia de cifras** (no la cubren los tests): crear un envío, verlo sumar en el contador de «planificados», asignarlo y ponerlo en curso, y comprobar que el contador se mueve de columna. Registrar una incidencia y ver crecer el recuento. Es la verificación de que el resumen refleja la realidad y no una consulta plausible.
8. **CI**: `.github/workflows/ci.yml` valida ambos lados en el push.

## Archivos

**Crear** — `TransitOps.Api/Features/Users/{UserContracts,UserService}.cs`; `TransitOps.Api/Features/Reporting/{SummaryContracts,SummaryService}.cs`; `TransitOps.Api/Controllers/{UsersController,SummaryController}.cs`; `TransitOps.Tests/Services/{UserServiceTests,SummaryServiceTests}.cs`; `TransitOps.Tests/Controllers/{UsersControllerTests,SummaryControllerTests}.cs`; `frontend/src/pages/users/UserPages.tsx`; `frontend/src/pages/ChangePasswordPage.tsx`; `frontend/src/routes/AdminRoute.tsx`; `docs/Sprint6Plan.md` (este documento).

**Editar** — `TransitOps.Api/Features/Auth/{AuthContracts,AuthService}.cs` (cambio de contraseña + `ICurrentUser`); `TransitOps.Api/Controllers/AuthController.cs` (nuevo endpoint, retirada de `admin-check`); `TransitOps.Api/Features/Shipments/ShipmentContracts.cs` (`ShipmentTime` a `public`); `TransitOps.Api/Program.cs`; `TransitOps.Tests/Services/AuthServiceTests.cs`; `TransitOps.Tests/Controllers/AuthControllerTests.cs`; `frontend/src/api/client.ts`; `frontend/src/App.tsx`; `frontend/src/components/{AppLayout,CatalogUi}.tsx`; `frontend/src/pages/HomePage.tsx`; `frontend/src/pages/shipments/ShipmentPages.tsx` (imports tras mover `StatusChip` y los helpers de fecha); `frontend/src/index.css`; `frontend/src/App.test.tsx`; `docs/design/DataModel.md`; `docs/Roadmap.md`; `CONTEXT.md`; `README.md`; `AGENTS.md`; `postman/TransitOps.Api.postman_collection.json`.

**Sin tocar** — `Common/ApiContracts.cs`; `Domain/AppUser.cs` (ya tiene todo lo necesario desde S1; **ninguna migración**); `Persistence/` (sin cambios de esquema); `Features/{Vehicles,Drivers,Customers}/`; `Features/Shipments/ShipmentService.cs`; `components/{ErrorAlert}.tsx` y `form-errors.ts`; `routes/ProtectedRoute.tsx` (el control de rol va en un `AdminRoute` aparte); `package.json` (cero dependencias nuevas); `archive/` (su administración de usuarios es oráculo de consulta: tiene precedente de RF-04, **no** de RF-03 ni de RF-14).
