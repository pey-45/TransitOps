# Plan · Sprint 7 — Endurecimiento, pruebas de sistema y despliegue

## Context

Sprints 1–6 están cerrados y RF-01…RF-14 implementados e integrados (árbol limpio en `3f877f4`, 127 pruebas backend y 30 frontend). El Sprint 7 es consolidación: dejar la aplicación endurecida, probada de extremo a extremo y desplegada de forma repetible.

Es el sprint más grande del proyecto con diferencia. Los seis anteriores se cerraron «en una sesión de trabajo» cada uno; este acumula cinco elementos de endurecimiento, una suite E2E desde cero, un entorno de despliegue nuevo, un job de CI nuevo y trabajo documental. De ahí la división en partes consecutivas.

**Decisiones ya fijadas** (tomadas en conversación, no reabrir):

| Tema | Decisión |
| --- | --- |
| Destino de despliegue | VM Ubuntu Server con Docker Compose en la máquina del autor (Hyper-V) |
| Evidencia de despliegue | **Vídeo**, no servicio permanentemente vivo. Solo tiene que funcionar el rato de grabarlo |
| Entrada pública y TLS | **Túnel de Cloudflare** (`cloudflared` como contenedor). Termina el TLS, así que **Caddy se elimina**: sin ACME, sin volumen de certificados y sin puertos que redirigir en el router |
| Superficie de red | **Ninguna. El compose de despliegue no publica ningún puerto**: el túnel es el único camino de acceso y el diagnóstico se hace desde dentro de la VM por SSH. Sustituye a la idea previa de modo puente para LAN, que el túnel cumple mejor (RNF-05 pide «desde cualquier ordenador de la oficina con conexión a internet», que es literalmente una URL HTTPS pública) |
| Cookie `Secure` | **Siempre activo** en el entorno desplegado, condicionado al **entorno** (desarrollo vs desplegado) y nunca al esquema de la petición. Un login por HTTP falla cerrado en lugar de crear una sesión insegura |
| CI | GitHub Actions, runners `ubuntu-latest`. Ya existe y está verde |
| CD | **Basado en pull**: Actions publica imágenes en GHCR; la VM hace `pull`. Actions no puede alcanzar la VM tras el NAT, y la existencia de la imagen *es* la prueba de que CI pasó |
| Repositorio y paquetes | **Ambos públicos.** La VM baja imágenes sin credenciales y no hay `docker login`. Descarta definitivamente los runners autoalojados |
| Timer de despliegue | `OnBootSec` + `OnUnitActiveSec=5min`. Existe para *demostrar* la automatización, no para servir tráfico |
| Sesión | **Migrar a cookie `HttpOnly`** (mismo origen lo hace barato) |
| Reglas en BD | **Testcontainers** para una clase de pruebas con PostgreSQL real; las 127 existentes siguen en memoria |
| Bootstrap en E2E | **Sin pantalla nueva.** Playwright usa la API para arranque y precondiciones; los flujos se ejercitan por interfaz |
| Aislamiento E2E | **Pruebas aisladas sin reiniciar la BD**: precondiciones por API y datos únicos por ejecución |
| Reparto de ejecución | Se prepara todo el material de despliegue; el autor crea la VM, sigue el documento y graba |

## Hallazgos de la exploración que condicionan el diseño

- **`localStorage` está en un solo sitio**: [AuthContext.tsx](../frontend/src/auth/AuthContext.tsx) (`STORAGE_KEY`, `storedSession`, `login`, `logout`), más la lectura del token en `frontend/src/api/client.ts`. Dos ficheros.
- **RN-04** son dos `AnyAsync` en `TransitOps.Api/Features/Shipments/ShipmentService.cs:205` y `:215`, filtrando `Status == Planned || Status == InProgress`. Dos comprobaciones → dos índices.
- **RN-12** es un único `EnsureNotLastAdmin` en `TransitOps.Api/Features/Users/UserService.cs:104`, invocado desde `:72` (desactivar) y `:92` (cambio de rol). Un solo punto donde poner el cerrojo.
- **El token se emite** en `CreateToken`, `TransitOps.Api/Features/Auth/AuthService.cs:82`, con claims `sub`/`unique_name`/`email`/`role`/`jti`. Añadir uno es trivial.
- **Las pruebas usan `UseInMemoryDatabase`** (`TransitOps.Tests/Support/TransitOpsApiFactory.cs`). Índice único filtrado y `pg_advisory_xact_lock` son exclusivos de PostgreSQL: hay que guardarlos con `Database.IsRelational()`, patrón que **ya existe** en `Program.cs:113`.
- **`JwtBearerEvents` ya tiene** `OnChallenge` y `OnForbidden` con el contrato de error uniforme (`WriteAuthError`). `OnTokenValidated` encaja en ese patrón y debe devolver el mismo 401.
- **No hay pantalla de bootstrap ni ruta para ello**: cero referencias en `frontend/src/`, y `/login` es la única ruta pública. Es deliberado, no un olvido.
- **El frontend es agnóstico del entorno**: `API_URL = import.meta.env.VITE_API_URL ?? ''` → rutas relativas → una imagen sirve en cualquier host, sin reconstruir.
- **CORS está configurado pero vacío** por defecto → operación en mismo origen confirmada.
- **No hay Playwright.** Dependencias de runtime: solo `react`, `react-dom`, `react-router-dom`. Los 2 avisos *high* de `npm audit` están, por tanto, casi con seguridad en el árbol de **devDependencies** — exposición de desarrollo, no de runtime, y eso cambia el triaje.

## Por qué cinco partes, y en este orden

La división no es por comodidad: cada parte cierra con su propia validación, igual que las rebanadas de S1–S6, y así la memoria gana evidencia granular en lugar de un único cierre monolítico.

El orden responde a una idea concreta: **las pruebas E2E van antes del cambio de sesión, para que hagan de red de seguridad en el refactor más arriesgado.** Los E2E actúan sobre la interfaz (rellenan el formulario de login), así que son indiferentes a si el token vive en `localStorage` o en una cookie. Escritas antes, protegen el cambio; escritas después, no protegen nada.

| Parte | Foco | Por qué aquí |
| --- | --- | --- |
| **S7.1** | Concurrencia y reglas en BD | Aislada y mecánica. Victoria rápida que además monta la infraestructura Testcontainers |
| **S7.2** | Pruebas de sistema E2E + dependencias | Construye la red de seguridad. Ya se toca `package.json`, así que el triaje de `npm audit` va aquí |
| **S7.3** | Seguridad de sesión | El cambio invasivo, ya protegido por los E2E |
| **S7.4** | Despliegue y entrega continua | Independiente de lo anterior salvo que el TLS es lo que hace comprobable la cookie `Secure` |
| **S7.5** | Cierre | Consolida evidencia y desbloquea los capítulos congelados de la memoria |

---

## S7.1 · Concurrencia y reglas en base de datos

**Objetivo.** Que RN-04 y RN-12 dejen de depender de una comprobación en servicio con ventana de carrera.

**Diseño — cinturón y tirantes, no sustitución.** La comprobación en servicio **se mantiene**: es la que produce el mensaje bueno, identificando el envío que ocupa el recurso. El índice se añade **debajo**, como garantía de corrección bajo concurrencia. Conviene decirlo así en la memoria: una da usabilidad, el otro da corrección.

- Migración nueva con dos índices únicos parciales sobre `Shipments`: uno por `VehicleId`, otro por `DriverId`, filtrados a los estados abiertos y a `IS NOT NULL`.
  - **Comprobar antes de escribir el filtro** cómo persiste EF el enum `Status` (entero o texto): el predicado del índice depende de ello.
  - Los envíos sin asignar no colisionan: PostgreSQL trata los `NULL` como distintos en índices únicos.
- Capturar la violación (SQLSTATE `23505`) y mapearla al **mismo contrato 409 que ya existe**, sin cambiar el código de error actual, para no romper pruebas ni contrato.
- RN-12: `pg_advisory_xact_lock` en el camino de `EnsureNotLastAdmin`, dentro de transacción, **guardado con `Database.IsRelational()`** o las 127 pruebas en memoria se rompen.

**Pruebas.** Añadir `Testcontainers.PostgreSql` y **una** clase de pruebas con PostgreSQL real: violación del índice bajo asignación concurrente (vehículo y conductor), y desactivación simultánea de los dos últimos administradores. Las 127 existentes no se tocan.

**Cierre.** Migración aplicada y listada contra base limpia; suite completa en verde; `docs/design/DataModel.md:95` actualizado, que es donde se dejó anotado *«queda registrada para revisión en el Sprint 7»*.

## S7.2 · Pruebas de sistema E2E y dependencias

**Objetivo.** Los cuatro flujos de negocio de `docs/Requirements.md` §Flujos de Negocio, automatizados sobre la aplicación integrada.

- `@playwright/test` como devDependency, especificaciones en `frontend/e2e/`, script `test:e2e`.
- Los cuatro flujos: arranque del primer administrador, alta de operador por administrador, **Flujo 3** (operador ejecuta un envío de principio a fin), y baja de usuario.
- Job nuevo en `.github/workflows/ci.yml`: levantar el stack con Compose, esperar `/api/v1/health`, correr Playwright, subir trazas como artefacto si falla.
- Escribirlos **contra el comportamiento actual** (sesión en `localStorage`), dirigiéndolos siempre por la interfaz y nunca por el mecanismo de almacenamiento, para que sobrevivan a S7.3 sin tocarlos.
- Triaje de los 2 avisos *high* de `npm audit`: confirmar si son de `devDependencies` —muy probable— y documentar la conclusión antes de aplicar cualquier arreglo automático que pueda romper el build.

**Bootstrap: por API, sin pantalla nueva.** La ausencia de interfaz para RF-02 es deliberada: es «creación *controlada*» protegida por `X-Bootstrap-Token`, un secreto de despliegue. Una página pública que crea administradores debilitaría el diseño, y añadir funcionalidad nueva en el sprint de consolidación es justo lo que S7 no debe hacer. El Flujo 1 es un procedimiento de **instalación**, no de interfaz: su E2E llama al endpoint (que *es* su primer paso), entra por la interfaz con esas credenciales y comprueba que un segundo bootstrap devuelve 409. Patrón estándar de Playwright: *arrange* por API, *act* por interfaz.

**Aislamiento: pruebas independientes sin reiniciar la base de datos.**

- Precondiciones creadas por API (fixture `request`), nunca reproduciendo por interfaz los flujos anteriores.
- **Datos únicos por ejecución** (usuario, matrícula, licencia, referencia de envío). No es comodidad: `Username`/`Email` son únicos globalmente **incluyendo filas inactivas**, y `Reference` es única global, así que datos fijos colisionarían en la segunda ejecución. Con datos únicos, el reinicio de BD deja de hacer falta.
- Se descarta reiniciar la base (frágil contra un stack Compose en marcha, que migra al arrancar) y se descarta el escenario serial compartido (un fallo en el flujo 2 arrastraría 3 y 4, no permitiría depurar uno aislado y el informe no diría qué regla se rompió).
- **Excepción, el bootstrap:** solo puede tener éxito una vez por base de datos. Va en `globalSetup`, que lo ejecuta si aún no está hecho; el test del Flujo 1 comprueba los efectos observables. En CI el stack siempre es nuevo, así que el camino de primera ejecución sí se ejercita de verdad allí.

**Cierre.** Los cuatro flujos en verde en CI; `npm audit` triado y documentado; RNF-01 (usabilidad, estados de carga y vacíos) repasado de paso al recorrer las pantallas.

## S7.3 · Seguridad de sesión

**Objetivo.** Cerrar las tres deudas de sesión que arrastra el Sprint 1.

**Invalidación del token.** Columna `TokenVersion` (entero) en `AppUser` + migración; el valor se emite como claim en `CreateToken`; se valida en un `OnTokenValidated` nuevo en `Program.cs`, reutilizando `WriteAuthError` para responder con el 401 del contrato. Se incrementa al cambiar contraseña, al desactivar usuario y al cambiar rol. Una lectura de BD por petición sobra para el volumen de RNF-06; documentar que se descartan *refresh tokens* por eso, con el mismo razonamiento que ya se usó en RN-04.

**Cookie `HttpOnly`.** Emitirla en login con `HttpOnly` + `Secure` + `SameSite=Strict`; limpiarla en logout. El flag `Secure` se condiciona al entorno, para que el desarrollo local por HTTP siga funcionando.

**Dos consecuencias que no son obvias y hay que resolver:**

1. **El cuerpo de la respuesta de login deja de llevar el token.** `LoginResponse` pasa a devolver solo usuario y caducidad → cambia el tipo `Session` del frontend y las pruebas que lo simulan.
2. **La SPA ya no puede leer su propia sesión al recargar**, porque una cookie `HttpOnly` es invisible a JavaScript. Hoy `storedSession()` la lee de `localStorage`. Hace falta un **`GET /api/v1/auth/me`** para rehidratar el estado al cargar la página. Sin esto, recargar echa al usuario.

**Frontend.** `AuthContext.tsx` deja de escribir `localStorage` y rehidrata contra `/auth/me`; `client.ts` deja de añadir `Authorization` y confía en la cookie de mismo origen.

**Pruebas.** Backend: token invalidado tras cada uno de los tres disparadores. Frontend: rehidratación y logout. Y los E2E de S7.2 como red de regresión, que es la razón de haberlos escrito antes.

**Cierre.** Suite completa en verde; las tres deudas de sesión cerradas; el razonamiento XSS↔CSRF documentado (el intercambio no es gratis: se cambia exposición a XSS por exposición a CSRF, que `SameSite=Strict` sobre mismo origen cubre).

## S7.4 · Despliegue y entrega continua

**Objetivo.** La aplicación corriendo en la VM mediante un procedimiento repetible, con la entrega automatizada hasta el registro.

- **`docker-compose.deploy.yml`** nuevo y **autocontenido** (no un override del actual): **cuatro servicios** — `db`, `api`, `web` y `cloudflared` — con `image:` desde GHCR en lugar de `build:`. Mezclar `build:` e `image:` en Compose tiene semántica sorprendente; un fichero aparte se documenta y se defiende mejor.
  - **Ningún puerto publicado, tampoco el de PostgreSQL.** Hoy `docker-compose.yml` expone la base en `POSTGRES_PORT` para desarrollo; en la VM no se publica nada en absoluto: `db`, `api` y `web` hablan solo por la red interna de Compose y `cloudflared` conecta hacia fuera. Sin superficie HTTP externa que quede a medias.
  - **Diagnóstico desde dentro de la VM** por SSH: `docker compose exec -T web wget -qO- http://127.0.0.1/api/v1/health` y `docker compose ps`. Aísla el fallo — si el stack responde dentro y el túnel no, el problema está en el túnel, sin publicar un puerto ni siquiera en loopback.
- **`cloudflared` como terminador TLS y único camino de acceso.** Conecta hacia fuera, así que **no hay que redirigir ningún puerto en el router**. Un *quick tunnel* da un `*.trycloudflare.com` efímero sin cuenta ni dominio, suficiente porque el despliegue no necesita sobrevivir a la grabación. Sustituye a Caddy: sin ACME, sin volumen de certificados y sin límites de emisión.
  - Consecuencia para la cookie: como el TLS se termina en Cloudflare, la API recibe la petición **como HTTP** por la red interna. Por eso `Secure` se condiciona al **entorno** y no al esquema: un `Secure` dependiente del esquema dejaría de ponerse justo en el único camino funcional, y arreglarlo exigiría `X-Forwarded-Proto` (que `frontend/nginx.conf` hoy no propaga), `UseForwardedHeaders` y `KnownProxies`.
- **Job de publicación** en `ci.yml`, condicionado a que CI pase y a `main`/etiqueta: construye ambas imágenes y las sube a GHCR etiquetadas por **SHA de commit** más `latest`. Usa `GITHUB_TOKEN` con `permissions: packages: write` → **ni un secreto que gestionar**, coherente con la regla de `AGENTS.md`. Con repositorio y paquetes públicos, la VM baja sin `docker login`.
- **`scripts/deploy.sh`** en la VM: `pull`, `up -d`, verificar `/api/v1/health`, registrar el digest desplegado. Más una unidad y un *timer* de systemd (`OnBootSec` + `OnUnitActiveSec=5min`) que invocan **el mismo script**.
- **Monitorización mínima:** healthchecks de contenedor, políticas de reinicio, y el health check del script. Nada de escala CloudWatch.
- **`docs/Deployment.md`**: provisionar VM → instalar Docker → copiar `.env` y el compose de despliegue → `deploy.sh` → arrancar primer administrador con `X-Bootstrap-Token` → verificar salud.

**Reparto de ejecución.** Se entrega el material completo —compose de despliegue, `deploy.sh`, unidad y timer, job de CI y `docs/Deployment.md`—; el autor crea la VM en Hyper-V, sigue el documento y graba. Crear la VM requiere elevación de todos modos, y así `docs/Deployment.md` queda validado por alguien que lo sigue de cero, que es exactamente lo que exige la definición de hecho.

**Vídeo.** Grabar **por la URL del túnel**, que es el único camino de acceso. En una sola pieza: `deploy.sh` a mano (se ve el `pull`, los contenedores reemplazados y el health check pasando), luego `systemctl list-timers` y `journalctl -u transitops-deploy` para evidenciar la vía automática, y después los cuatro flujos en el navegador. En pantalla, el SHA desplegado, para que la evidencia quede atada a un commit.

**Cierre.** Los cuatro flujos funcionando en el entorno desplegado; despliegue reproducible siguiendo `docs/Deployment.md` desde una VM recién creada.

## S7.5 · Cierre

- Triaje y corrección de defectos encontrados en 7.1–7.4; reverificación de la suite completa.
- **Enmendar `docs/Roadmap.md`**: la única línea del repo que dice «accesible por internet» (entregable de S7) pasa a describir lo realmente entregado. Todo lo demás —RNF-05, la definición de hecho, `contexto_objetivos.tex`— ya dice «entorno accesible» y no necesita tocarse.
- Cierre fechado del Sprint 7 en el Roadmap, con el estilo de los seis anteriores.
- Entrada nueva en el log de decisiones de `CONTEXT.md`.
- **Capítulos de memoria que S7 desbloquea:** `desarrollo_iterativo.tex` §Sprint 7 (el `% TODO` de la línea 152), la sección de pruebas de sistema en `validacion.tex`, `despliegue.tex` completo, `resultados.tex`, `conclusions.tex` definitivo, la fila RNF-01–06 de `anexos/trazabilidad.tex`, y §3 de `diseno_detallado.tex` ya como decisión final y no provisional.

---

## Verificación

Por parte, no solo al final:

```bash
dotnet test TransitOps.slnx
```

```bash
cd frontend && npm run lint && npm run test && npm run build
```

```bash
cd frontend && npm run test:e2e
```

Criterios objetivos de cierre de sprint:

1. Suite completa en verde: 127 pruebas backend existentes + las nuevas de Testcontainers + 30 frontend + 4 flujos E2E.
2. Migraciones aplicadas y listadas contra una base limpia; `--migrate-only` en verde en CI.
3. Los índices únicos parciales **rechazan** de verdad la doble reserva concurrente (probado contra PostgreSQL real, no en memoria).
4. Un token emitido antes de un cambio de contraseña, una desactivación o un cambio de rol deja de ser válido.
5. Recargar la página mantiene la sesión mediante `/auth/me`, y la cookie no es legible desde `document.cookie`.
6. `docs/Deployment.md` reproducible desde una VM recién creada, sin pasos ocultos.
7. Los cuatro flujos ejecutados en el entorno desplegado, grabados por la URL del túnel, único camino de acceso.
8. El compose de despliegue no publica ningún puerto, y un intento de login por HTTP falla cerrado.
9. Ningún requisito de prioridad alta roto.

**Prerrequisitos de máquina que faltan hoy:** no hay **Node** instalado (solo copias empotradas en Visual Studio y otro runtime) y `frontend/node_modules` no existe, así que el frontend no se puede construir ni probar. `dotnet` 10.0.302 y Docker Desktop sí están (Docker en `%LOCALAPPDATA%\Programs\DockerDesktop`, fuera del PATH de la shell). Hyper-V no se pudo consultar sin elevación.

## Riesgos

| Riesgo | Mitigación |
| --- | --- |
| El refactor de sesión rompe muchas pruebas de frontend | Es la razón de que los E2E vayan antes; ejecutarlos entre cada paso de S7.3 |
| El filtro del índice depende de cómo persista el enum `Status` | Verificarlo en el snapshot del modelo antes de escribir la migración |
| El advisory lock rompe las pruebas en memoria | Guardarlo con `Database.IsRelational()`, patrón ya existente en `Program.cs:113` |
| `Secure` condicionado al esquema se caería tras el túnel | Condicionarlo al entorno, no al esquema. Evita tener que propagar `X-Forwarded-Proto` y configurar `KnownProxies` |
| Si el túnel cae, no hay camino alternativo de acceso | El diagnóstico desde el contenedor `web` demuestra que el stack está sano con independencia del túnel; reiniciar `cloudflared` da una URL nueva sin debilitar la superficie de red |
| El hostname del *quick tunnel* es efímero | Irrelevante: el despliegue no necesita sobrevivir a la grabación. Un hostname estable exigiría cuenta de Cloudflare y dominio propio |
| Repositorio público | Ningún secreto en ficheros versionados (ya es regla de `AGENTS.md`); runners autoalojados descartados por seguridad |
| `npm audit --fix` rompe el build | Triar primero; son casi seguro devDependencies, así que la urgencia es baja |
| Testcontainers necesita Docker en local y en CI | Ambos lo tienen; los runners de Actions traen Docker |
| S7 y la memoria compiten por el mismo calendario | El cierre de S7.5 alimenta justo los capítulos congelados de la memoria |
