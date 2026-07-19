# TransitOps · Roadmap de Sprints

## Propósito

Traducir la especificación de [docs/Requirements.md](Requirements.md) en un plan de sprints iterativos que recorra el ciclo de vida completo del software, en línea con la metodología del anteproyecto: enfoque iterativo e incremental, priorizando versiones funcionales desde fases tempranas, con cada incremento validado mediante pruebas funcionales y ejecución real del sistema.

## Modelo de Planificación

- **Rebanadas verticales, no fases horizontales.** Cada sprint de funcionalidad (S1–S6) añade una porción concreta de `Requirements.md` y la lleva de extremo a extremo: diseño → backend → frontend → pruebas. No se agrupa "todo el diseño", luego "todo el backend", luego "todo el frontend".
- **El modelo de datos se diseña completo al principio, no a trozos.** El dominio es pequeño y ya está definido en `Requirements.md`, así que el modelo de datos entero se diseña en el Sprint 1. Cada sprint posterior implementa (mediante migraciones incrementales) solo la parte del modelo que su funcionalidad necesita. Esto evita rediseñar el modelo sprint a sprint sin renunciar a la entrega incremental de funcionalidad.
- **Esqueleto ejecutable desde el primer sprint.** El Sprint 1 deja una aplicación mínima pero real (autenticación funcionando de punta a punta), de modo que a partir de ahí cada sprint amplía algo demostrable y ejecutable.
- **Sin fechas de calendario fijas.** A petición del autor, los sprints van numerados y secuenciales. La duración real de cada uno se registra al cerrarlo (una línea basta), en lugar de estimar horas por adelantado; si el ritmo observado lo exige, se replanifica lo pendiente.
- **Objetivo temporal.** Tener la ingeniería (S1–S7) terminada con holgura antes de la ventana de defensa de finales de septiembre de 2026, dejando el Sprint 8 (documentación y cierre) y el ensayo sin agobios.
- **La implementación anterior es un acelerador.** El código archivado en `archive/cloud-phase/` resuelve ya buena parte del backend del dominio (reglas, migraciones, casos de prueba). Se consulta como referencia al reconstruir cada rebanada, pero no se edita ni se parte de él.

## Convención de Sprint

Cada sprint de funcionalidad define:

- **Objetivo**: qué queda demostrable al cerrarlo.
- **Requisitos que cubre**: identificadores de `Requirements.md`.
- **Trabajo por capa**: diseño, backend, frontend y pruebas de esa rebanada.
- **Entregable demostrable**: qué se puede enseñar funcionando.
- **Definición de hecho**: criterios objetivos de cierre.

`RF-13` (validación y avisos de error) es transversal: su contrato base se establece en el Sprint 1 y se aplica a cada funcionalidad nueva en todos los sprints siguientes.

## Cadencia de Sprints

| Sprint | Foco | Requisitos |
| --- | --- | --- |
| Sprint 1 | Cimientos y esqueleto autenticado | RF-01, RF-02, RF-13 (base) |
| Sprint 2 | Catálogos: vehículos, conductores, clientes | RF-05, RF-06, RF-07 |
| Sprint 3 | Envíos: alta, edición, listado y filtros | RF-08, RF-12 |
| Sprint 4 | Operación del envío: asignación y ciclo de estados | RF-09, RF-10 |
| Sprint 5 | Trazabilidad: historial de eventos | RF-11 |
| Sprint 6 | Administración e indicadores | RF-03, RF-04, RF-14 |
| Sprint 7 | Endurecimiento, pruebas de sistema y despliegue | Transversal |
| Sprint 8 | Documentación y cierre | Transversal |

Los sprints 1–6 son rebanadas verticales de funcionalidad; los sprints 7–8 son consolidación y cierre, lo habitual al final de un proyecto iterativo.

## Sprint 1 · Cimientos y Esqueleto Autenticado

**Objetivo**
Una aplicación mínima pero real: un usuario puede iniciar sesión desde el navegador y llegar a una zona autenticada vacía, con los roles diferenciados. Es el "esqueleto andante" sobre el que se cuelga todo lo demás.

**Requisitos que cubre**
RF-01 (acceso), RF-02 (arranque del primer administrador), RF-13 (contrato base de validación/errores).

**Trabajo por capa**
- **Diseño**: diseñar el modelo de datos **completo** a partir de `Requirements.md` (usuarios, vehículos, conductores, clientes, envíos, eventos, con sus relaciones, estados y borrado lógico); definir la arquitectura de integración front/back (SPA contra API REST, dónde vive el token, cómo se propagan roles y errores).
- **Backend**: esqueleto ASP.NET Core + EF Core/PostgreSQL; migración inicial del modelo; arranque del primer administrador (RF-02); autenticación con emisión de token (RF-01); contrato común de respuestas y errores (RF-13).
- **Frontend**: esqueleto React; pantalla de login; enrutado protegido (lo no autenticado redirige a login); estructura de navegación que se adapta al rol; patrón común de aviso de errores.
- **Pruebas**: pruebas de backend para arranque de admin, login válido/ inválido y protección de rutas; comprobación de que el esqueleto arranca de forma reproducible (Docker Compose) y de que la CI ejecuta build + pruebas.

**Entregable demostrable**
Login funcionando de punta a punta contra el backend real, con la aplicación contenerizada y la CI en verde.

**Definición de hecho**
- Se cumplen los criterios de aceptación de RF-01 y RF-02.
- El modelo de datos completo está diseñado y versionado (aunque solo esté implementada la parte de usuarios).
- La aplicación se levanta localmente sin pasos manuales ocultos y la CI valida build + pruebas.

**Cierre (2026-07-18)**
Sprint implementado en una sesión de trabajo. Se entregaron el modelo completo y la arquitectura de integración; API .NET 10 con migración de usuarios, bootstrap y JWT; SPA React/TypeScript con login, rutas protegidas y navegación por rol; 16 pruebas backend organizadas por servicio y controlador, y 4 frontend; Docker Compose y workflow de CI. La verificación local contenerizada completó salud, bootstrap, conflicto al repetir, login y sesión protegida a través del proxy. La ejecución remota del workflow queda pendiente del siguiente push.

## Sprint 2 · Catálogos (Vehículos, Conductores, Clientes)

**Objetivo**
Gestionar de extremo a extremo los tres recursos base que luego consumen los envíos.

**Requisitos que cubre**
RF-05 (vehículos), RF-06 (conductores), RF-07 (clientes).

**Trabajo por capa**
- **Diseño**: refinar las pantallas de listado/detalle/formulario reutilizables para las tres entidades; confirmar reglas de unicidad y baja lógica (RN-15).
- **Backend**: CRUD con baja lógica y validaciones de unicidad de identificadores de negocio para vehículos, conductores y clientes; campos opcionales ampliados (código interno, marca/modelo, capacidad; código de empleado, contacto).
- **Frontend**: vistas de listado, detalle, alta y edición para las tres entidades, con validación en cliente y avisos de conflicto (RF-13).
- **Pruebas**: backend para CRUD, unicidad, baja lógica; verificación funcional de las tres altas desde la interfaz.

**Entregable demostrable**
Alta, edición, consulta y baja de vehículos, conductores y clientes desde la interfaz.

**Definición de hecho**
- Se cumplen los criterios de aceptación de RF-05, RF-06 y RF-07.
- La baja lógica no borra historial y retira el elemento del uso diario.

## Sprint 3 · Envíos (Alta, Edición, Listado y Filtros)

**Objetivo**
Crear y consultar envíos, asociándolos a un cliente, con los filtros operativos que pidió la clienta.

**Requisitos que cubre**
RF-08 (gestión de envíos), RF-12 (visibilidad y filtros).

**Trabajo por capa**
- **Diseño**: pantalla de listado con filtros (estado, fechas, vehículo, conductor) y paginación; formulario de envío con cliente y carga estimada.
- **Backend**: CRUD de envíos con validación de fechas (RN-06), identificación inequívoca, enlace opcional a cliente activo, carga estimada; listado filtrado y paginado.
- **Frontend**: listado con filtros y paginación; detalle de envío; formularios de alta/edición.
- **Pruebas**: backend para creación/edición, validación de fechas y filtros; verificación funcional del alta y del filtrado desde la interfaz.

**Entregable demostrable**
Crear un envío, verlo en el listado, filtrarlo por estado/fecha/vehículo/conductor y editarlo.

**Definición de hecho**
- Se cumplen los criterios de aceptación de RF-08 y RF-12.
- El envío arranca siempre en estado "planificado".

## Sprint 4 · Operación del Envío (Asignación y Ciclo de Estados)

**Objetivo**
La lógica de negocio central: asignar recursos sin duplicarlos y mover el envío por su ciclo de vida.

**Requisitos que cubre**
RF-09 (asignación), RF-10 (estados).

**Trabajo por capa**
- **Diseño**: interacción de asignación (vehículo + conductor juntos) y de transición de estado desde el detalle del envío, deshabilitando lo no válido según el estado actual.
- **Backend**: asignación solo en "planificado" (RN-02); rechazo de recursos inactivos o ya ocupados en otro envío sin terminar (RN-03, RN-04); aviso de capacidad insuficiente sin bloquear (RN-05); transiciones de estado con prerequisitos y estados terminales (RN-07, RN-08); captura de fechas reales de recogida/entrega.
- **Frontend**: acciones de asignar/reasignar y de transición de estado, con avisos claros cuando una acción no es válida o cuando la capacidad se queda corta.
- **Pruebas**: backend para asignación válida/ inválida, anti-doble-reserva, aviso de capacidad, transiciones válidas e inválidas; verificación funcional del flujo completo desde la interfaz.

**Entregable demostrable**
El "Flujo 3" de `Requirements.md` completo desde la interfaz: crear envío, asignar, pasar a en curso, entregar/cancelar; y comprobación de que no deja duplicar un vehículo o conductor ocupado.

**Definición de hecho**
- Se cumplen los criterios de aceptación de RF-09 y RF-10.
- Un envío entregado o cancelado no puede volver a cambiar de estado.

## Sprint 5 · Trazabilidad (Historial de Eventos)

**Objetivo**
Que cada envío tenga un historial consultable de lo que le ha pasado, con autor y fecha.

**Requisitos que cubre**
RF-11 (historial de eventos del envío).

**Trabajo por capa**
- **Diseño**: sección de historial cronológico en el detalle del envío y formulario de registro de evento.
- **Backend**: registro y consulta de eventos por envío, ordenados por fecha, con autor tomado del usuario autenticado (RN-09); tipos de evento definidos.
- **Frontend**: línea de tiempo del envío y alta de nuevos eventos, reflejada sin recargar.
- **Pruebas**: backend para creación/orden/actor y validación contra envíos inexistentes; verificación funcional del registro desde la interfaz.

**Entregable demostrable**
Registrar eventos (salida, punto de control, incidencia, etc.) sobre un envío y ver su historial ordenado con quién lo registró.

**Definición de hecho**
- Se cumplen los criterios de aceptación de RF-11.

## Sprint 6 · Administración e Indicadores

**Objetivo**
Cerrar la funcionalidad de prioridad media: gestión de usuarios, contraseña propia y el resumen operativo.

**Requisitos que cubre**
RF-04 (administración de usuarios), RF-03 (cambio de contraseña), RF-14 (resumen y estadísticas).

**Trabajo por capa**
- **Diseño**: pantallas de administración de usuarios (solo administrador), formulario de cambio de contraseña, y pantalla de resumen.
- **Backend**: alta de usuarios, cambio de rol y activación/desactivación con protección de último administrador (RN-10, RN-12); cambio de contraseña con confirmación de la actual (RN-14); agregados para el resumen (envíos por estado, actividad por vehículo/conductor, incidencias).
- **Frontend**: administración de usuarios oculta a operadores; cambio de contraseña; panel de resumen visible de un vistazo.
- **Pruebas**: backend para permisos, protección de último admin, cambio de contraseña y cálculo del resumen; verificación funcional de que un operador no alcanza la administración ni por navegación directa.

**Entregable demostrable**
Un administrador da de alta un operador, cambia su propia contraseña, y todos ven el resumen operativo al entrar.

**Definición de hecho**
- Se cumplen los criterios de aceptación de RF-03, RF-04 y RF-14.
- Toda la lista de `Requirements.md` (RF-01…RF-14) queda implementada e integrada.

## Sprint 7 · Endurecimiento, Pruebas de Sistema y Despliegue

**Objetivo**
Dejar la aplicación completa, probada de punta a punta y accesible en un entorno real.

**Trabajo**
- Prueba funcional de extremo a extremo de los cuatro flujos de negocio de `Requirements.md` sobre la aplicación integrada.
- Repaso transversal de usabilidad, manejo de errores, estados de carga y vacíos, y seguridad básica (RNF-01, RNF-02).
- Despliegue a un entorno accesible sencillo (decidido en este sprint; deliberadamente no una plataforma cloud gestionada con Terraform) y documentación repetible del proceso; CI/CD ligero de build y despliegue; monitorización mínima (RNF-04, y observabilidad básica).
- Triaje y corrección de defectos encontrados; reverificación.

**Entregable demostrable**
La aplicación completa, accesible por internet, con los cuatro flujos funcionando y un procedimiento de despliegue documentado.

**Definición de hecho**
- Los cuatro flujos de negocio funcionan de extremo a extremo en el entorno desplegado.
- Ningún requisito de prioridad alta queda roto.
- El despliegue es repetible siguiendo la documentación.

## Sprint 8 · Documentación y Cierre

**Objetivo**
La memoria del TFG y el material de defensa para la nueva dirección.

**Trabajo**
- Redactar la memoria: introducción, objetivos, metodología, requisitos, arquitectura, implementación de backend y frontend, pruebas, despliegue, resultados y conclusiones.
- Producir los diagramas (arquitectura, modelo de datos, flujos) que la memoria referencia.
- Preparar la presentación de defensa y un guion de ensayo.
- Repaso final de `README.md`, `CONTEXT.md`, `Requirements.md` y este roadmap para que describan el sistema realmente entregado.

**Entregable demostrable**
Memoria compilada, presentación y documentación alineada con lo entregado.

**Definición de hecho**
- El proyecto queda listo para defender antes de la ventana de finales de septiembre de 2026, con tiempo de ensayo.
- No queda contenido provisional ni funcionalidad descrita como hecha sin estarlo.

## Nota de Despliegue

El despliegue no es una fase final aislada. La aplicación se mantiene contenerizada y con CI (build + pruebas) desde el Sprint 1, de modo que cada sprint entrega algo ejecutable. El despliegue a un entorno accesible por internet se aborda en el Sprint 7, una vez la funcionalidad está completa; si conviene, puede adelantarse un esqueleto desplegado antes, pero no se retrasa más allá de ese punto.

## Nota de Ritmo

No se asignan fechas a los sprints. Al cerrar cada uno, anota aquí (o en `CONTEXT.md`) cuánto duró de verdad, para dimensionar los siguientes con datos en vez de estimaciones. Como referencia orientativa: ocho sprints a un ritmo aproximado de una a dos semanas cada uno encajan con holgura antes de finales de septiembre. Si el ritmo real se alarga, comprime alcance (por ejemplo, uniendo los sprints 5 y 6, o acotando RF-14 a su versión mínima) antes de dejar que el Sprint 8 quede apretado.
