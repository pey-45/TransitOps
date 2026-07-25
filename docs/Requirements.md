# TransitOps · Especificación de Requisitos

## Propósito

Este documento recoge los requisitos funcionales y no funcionales de TransitOps, derivados de la entrevista con la clienta recogida en [docs/ClientRequirements.md](ClientRequirements.md). Se mantiene deliberadamente a nivel funcional/de negocio, sin entrar en decisiones técnicas de implementación (arquitectura, tecnologías, contratos de API, modelo de datos técnico): esas decisiones pertenecen a la fase de diseño posterior, no a esta especificación.

Cada requisito indica su origen en la entrevista para mantener la trazabilidad.

## Alcance

### Dentro del alcance

- Acceso mediante usuario y contraseña, con dos niveles de permiso (operador y administrador).
- Arranque de la aplicación mediante la creación controlada del primer administrador.
- Que cada persona pueda cambiar su propia contraseña.
- Administración de usuarios (alta y gestión de permisos) por parte de administradores.
- Gestión de vehículos y conductores.
- Gestión de clientes y asociación de cada envío a un cliente.
- Gestión de transportes/envíos: creación, consulta, edición, asignación de vehículo y conductor, y seguimiento de su estado.
- Historial de eventos por envío (seguimiento e incidencias).
- Un resumen/estadísticas operativo básico.

### Fuera de alcance (por ahora)

- Cálculo de rutas óptimas, tiempos de viaje o integración con GPS de los vehículos.
- Facturación o presupuestos.
- Aplicación móvil.
- Acceso de clientes externos a la aplicación.
- Recuperación automática de contraseña (por ejemplo, por correo): de momento la reasigna un administrador.
- Acceso de los conductores a la aplicación: reciben la información por los medios que ya usan hoy (teléfono/mensajería); no son usuarios del sistema.

## Actores

- **Operador**: personal de operaciones que realiza el trabajo diario: gestiona vehículos, conductores, clientes, envíos, asignaciones y seguimiento.
- **Administrador**: además de todo lo anterior, es la única persona que puede dar de alta usuarios y decidir sus permisos.
- **Público no identificado**: solo puede llegar a la pantalla de inicio de sesión; el resto de la aplicación requiere haber iniciado sesión.
- **Instalador / puesta en marcha**: crea el primer administrador durante la instalación, fuera del uso normal de la aplicación.
- **Conductor** y **Cliente**: aparecen como información gestionada dentro del sistema (a un conductor se le asignan envíos, a un cliente pertenecen envíos), pero no son personas usuarias de la aplicación.

## Información que gestiona el sistema

Descrita en términos de negocio, sin especificar cómo se almacena técnicamente:

- **Vehículo**: matrícula, código interno (opcional), marca y modelo (opcional), capacidad de carga (opcional), si está activo o dado de baja.
- **Conductor**: nombre, número de carné, código de empleado (opcional), datos de contacto (opcional), si está activo o dado de baja.
- **Cliente**: nombre, datos de contacto (opcional), si está activo o dado de baja.
- **Transporte/envío**: origen, destino, fecha prevista de recogida, fecha prevista de entrega (opcional), cliente (opcional), carga estimada (opcional), descripción o notas (opcional), estado, vehículo asignado (opcional), conductor asignado (opcional).
- **Evento de seguimiento**: envío al que pertenece, tipo de evento, fecha, ubicación (opcional), notas (opcional), quién lo registró.
- **Usuario de la aplicación**: nombre de usuario, datos de contacto (opcional), tipo de acceso (operador o administrador), si está activo o inactivo.

## Requisitos Funcionales

| ID | Requisito | Prioridad |
| --- | --- | --- |
| RF-01 | Acceso a la aplicación | Alta |
| RF-02 | Arranque del primer administrador | Alta |
| RF-03 | Cambio de contraseña propia | Media |
| RF-04 | Administración de usuarios | Media |
| RF-05 | Gestión de vehículos | Alta |
| RF-06 | Gestión de conductores | Alta |
| RF-07 | Gestión de clientes | Media |
| RF-08 | Gestión de transportes/envíos | Alta |
| RF-09 | Asignación de vehículo y conductor | Alta |
| RF-10 | Estados del envío | Alta |
| RF-11 | Historial de eventos del envío | Alta |
| RF-12 | Visibilidad y filtros de envíos | Alta |
| RF-13 | Validación y avisos de error | Alta |
| RF-14 | Resumen y estadísticas operativas | Media |

### RF-01 · Acceso a la Aplicación

El sistema debe permitir el acceso mediante usuario y contraseña, y debe distinguir entre los dos tipos de acceso (operador y administrador) para mostrar solo las opciones que correspondan a cada persona.

**Criterios de aceptación:**
- Solo se puede entrar con un usuario y contraseña válidos.
- Una vez dentro, cada persona solo ve y puede usar las opciones que le corresponden según su tipo de acceso.
- Una persona desactivada no puede entrar aunque sus credenciales sean correctas.

**Origen:** "Sobre cómo quieren usarla", "Sobre quién usará la aplicación".

### RF-02 · Arranque del Primer Administrador

El sistema debe ofrecer un mecanismo controlado para crear el primer administrador durante la puesta en marcha, cuando todavía no existe ningún usuario dentro.

**Criterios de aceptación:**
- El mecanismo solo funciona mientras no exista ya un administrador activo.
- No forma parte del uso diario: una vez creado el primer administrador, el resto de usuarios se dan de alta desde dentro de la aplicación (RF-04).

**Origen:** "Sobre quién usará la aplicación" (creación del primer administrador en la instalación).

### RF-03 · Cambio de Contraseña Propia

El sistema debe permitir que cada persona usuaria cambie su propia contraseña.

**Criterios de aceptación:**
- Para cambiarla, la persona debe confirmar su contraseña actual.
- La recuperación de una contraseña olvidada no es automática: la reasigna un administrador (queda fuera de alcance por ahora).

**Origen:** "Sobre quién usará la aplicación" (gestión de contraseñas).

### RF-04 · Administración de Usuarios

El sistema debe permitir a un administrador dar de alta nuevas personas usuarias, asignarles un tipo de acceso (operador o administrador), y activarlas o desactivarlas.

**Criterios de aceptación:**
- Solo un administrador puede realizar estas acciones.
- El sistema no debe permitir quedarse sin ningún administrador activo.

**Origen:** "Sobre quién usará la aplicación".

### RF-05 · Gestión de Vehículos

El sistema debe permitir dar de alta, consultar, listar, editar y dar de baja vehículos. Además de la matrícula, debe permitir registrar (de forma opcional, pero disponible desde el principio) código interno, marca, modelo y capacidad de carga.

**Criterios de aceptación:**
- No puede haber dos vehículos activos con la misma matrícula.
- Si se indica un código interno, no puede repetirse entre vehículos activos.
- Dar de baja un vehículo no elimina su historial de envíos anteriores, y deja de aparecer en las listas de trabajo diario.

**Origen:** "Sobre los vehículos".

### RF-06 · Gestión de Conductores

El sistema debe permitir dar de alta, consultar, listar, editar y dar de baja conductores. Además del número de carné, debe permitir registrar (de forma opcional, pero disponible desde el principio) código de empleado y datos de contacto.

**Criterios de aceptación:**
- No puede haber dos conductores activos con el mismo número de carné.
- Dar de baja un conductor no elimina su historial de envíos anteriores, y deja de aparecer en las listas de trabajo diario.

**Origen:** "Sobre los conductores".

### RF-07 · Gestión de Clientes

El sistema debe permitir dar de alta, consultar, listar, editar y dar de baja clientes, con al menos su nombre y unos datos de contacto, para poder asociarlos a los envíos.

**Criterios de aceptación:**
- Dar de baja un cliente no elimina los envíos que ya se le hicieron, y deja de aparecer en las listas de trabajo diario.
- Un cliente dado de baja no puede asociarse a nuevos envíos.

**Origen:** "Sobre los clientes de los envíos".

### RF-08 · Gestión de Transportes/Envíos

El sistema debe permitir crear, listar, consultar y editar transportes/envíos, con origen, destino, fecha prevista de recogida, fecha prevista de entrega, cliente, carga estimada y notas.

**Criterios de aceptación:**
- La fecha prevista de entrega no puede ser anterior a la fecha prevista de recogida.
- Cada envío debe poder identificarse de forma inequívoca dentro de la aplicación.
- El cliente y la carga estimada son opcionales; si se indica un cliente, debe ser un cliente activo.

**Origen:** "Sobre los transportes/envíos".

### RF-09 · Asignación de Vehículo y Conductor

El sistema debe permitir asignar un vehículo y un conductor a un envío, y cambiar esa asignación, solo mientras el envío esté planificado, evitando duplicar recursos ya ocupados.

**Criterios de aceptación:**
- No se puede asignar un vehículo o conductor que esté dado de baja.
- No se puede asignar un vehículo o conductor que ya esté asignado a otro envío sin terminar (planificado o en curso): el sistema lo impide y lo avisa.
- Si la capacidad del vehículo es conocida y es menor que la carga estimada del envío, el sistema avisa antes de confirmar la asignación (aviso, no bloqueo).
- No se puede modificar la asignación una vez que el envío ha salido (está en curso o finalizado).

**Origen:** "Sobre el negocio y el problema actual" (evitar duplicar asignaciones), "Sobre los vehículos" (capacidad), "Sobre los transportes/envíos".

### RF-10 · Estados del Envío

El sistema debe llevar el envío a través de una secuencia de estados: planificado, en curso, y finalmente entregado o cancelado.

**Criterios de aceptación:**
- Un envío nuevo empieza siempre en estado planificado.
- Pasar a "en curso" solo es posible si el envío ya tiene vehículo y conductor asignados.
- Una vez entregado o cancelado, el envío no puede volver a cambiar de estado; si hace falta rehacerlo, se crea un envío nuevo.

**Origen:** "Sobre los transportes/envíos".

### RF-11 · Historial de Eventos del Envío

El sistema debe permitir registrar y consultar, para cada envío, un historial de sucesos con su fecha: creación, asignación, salida, puntos de control, incidencias, entrega o cancelación.

**Criterios de aceptación:**
- El historial se muestra ordenado por fecha.
- Cada suceso registrado queda asociado a la persona que lo registró.

**Origen:** "Sobre el seguimiento / historial".

### RF-12 · Visibilidad y Filtros de Envíos

El sistema debe permitir ver rápidamente el estado de los envíos, filtrando por estado, fechas, vehículo o conductor.

**Criterios de aceptación:**
- Es posible ver solo los envíos en un estado concreto (por ejemplo, "en curso").
- Es posible acotar por fechas, por vehículo o por conductor.

**Origen:** "Sobre cómo quieren usarla", "Sobre el negocio y el problema actual".

### RF-13 · Validación y Avisos de Error

El sistema debe validar los datos que introduce la persona usuaria y avisar con claridad cuando algo es incorrecto, en lugar de fallar de forma confusa o quedarse a medias.

**Criterios de aceptación:**
- Los datos obligatorios ausentes o incorrectos se señalan antes de guardar.
- Cuando una acción no es válida por una regla de negocio (por ejemplo, un duplicado o una transición de estado no permitida), el sistema lo explica de forma comprensible.

**Origen:** "Sobre cómo quieren usarla" (que avise con claridad de los errores).

### RF-14 · Resumen y Estadísticas Operativas

El sistema debe ofrecer un resumen con el número de envíos en cada estado, la actividad de cada vehículo y cada conductor en un periodo, y el número de incidencias registradas.

**Criterios de aceptación:**
- El resumen se puede consultar sin necesidad de contar manualmente los envíos uno a uno.
- No es necesario que incluya gráficos elaborados; basta con verlo de un vistazo.

**Origen:** "Sobre estadísticas e informes".

## Reglas de Negocio

- **RN-01:** Un envío solo puede tener un vehículo y un conductor asignados a la vez.
- **RN-02:** La asignación (o su cambio) solo es válida mientras el envío está planificado.
- **RN-03:** Solo pueden participar en un envío vehículos, conductores y clientes que estén activos.
- **RN-04:** Un vehículo o un conductor no puede estar asignado simultáneamente a más de un envío sin terminar (planificado o en curso).
- **RN-05:** Al asignar, si la capacidad conocida del vehículo es menor que la carga estimada del envío, el sistema avisa, pero no impide la asignación.
- **RN-06:** La fecha prevista de entrega no puede ser anterior a la fecha prevista de recogida.
- **RN-07:** Pasar a "en curso" requiere tener vehículo y conductor asignados.
- **RN-08:** Un envío entregado o cancelado no puede volver a cambiar de estado.
- **RN-09:** Cada evento de seguimiento pertenece a un único envío y queda vinculado a quien lo registró.
- **RN-10:** Solo un administrador puede dar de alta usuarios o cambiar sus permisos.
- **RN-11:** El mecanismo de arranque solo puede crear el primer administrador cuando no existe ya un administrador activo.
- **RN-12:** Debe existir siempre al menos un administrador activo.
- **RN-13:** Una persona usuaria desactivada no puede acceder a la aplicación.
- **RN-14:** Cambiar la contraseña propia requiere confirmar la contraseña actual.
- **RN-15:** Dar de baja un vehículo, conductor o cliente no elimina los envíos ni el historial asociados; solo lo retira del uso diario.
- **RN-16:** Los conductores y los clientes no acceden a la aplicación; su información se gestiona, pero no son personas usuarias del sistema.

## Flujos de Negocio

Los siguientes flujos describen los recorridos de extremo a extremo más representativos del sistema. Sirven de guía para las pruebas funcionales de sistema (ver `Roadmap.md`, Sprint 7) y no introducen requisitos nuevos: cada paso se apoya en requisitos funcionales (RF) y reglas de negocio (RN) ya definidos.

### Flujo 1 · Puesta en Marcha del Primer Administrador

1. Al instalar la aplicación todavía no existe ninguna persona usuaria.
2. El mecanismo de arranque crea el primer administrador (RF-02, RN-11).
3. Una vez creado, el arranque queda inhabilitado mientras exista un administrador activo (RN-11); a partir de ahí, el resto de usuarios se dan de alta desde dentro de la aplicación (RF-04).

**Origen:** "Sobre quién usará la aplicación".

### Flujo 2 · Un Administrador Da de Alta a un Operador

1. El administrador inicia sesión (RF-01).
2. Da de alta una nueva persona usuaria con acceso de operador (RF-04, RN-10).
3. El operador ya puede iniciar sesión con sus credenciales y, la primera vez, cambiar su contraseña (RF-03, RN-14).

**Origen:** "Sobre quién usará la aplicación".

### Flujo 3 · Un Operador Ejecuta un Envío de Principio a Fin

1. El operador inicia sesión (RF-01).
2. Da de alta o reutiliza el vehículo, el conductor y el cliente que necesita (RF-05, RF-06, RF-07).
3. Crea un envío, que nace en estado planificado, con origen, destino, fechas previstas, cliente y carga estimada (RF-08, RN-06).
4. Asigna vehículo y conductor mientras el envío está planificado; el sistema impide reutilizar recursos ya ocupados o dados de baja y avisa si la capacidad del vehículo se queda corta (RF-09, RN-02, RN-03, RN-04, RN-05).
5. Pasa el envío a en curso, lo que exige tener vehículo y conductor asignados (RF-10, RN-07).
6. Registra los eventos de seguimiento que correspondan: salida, puntos de control, incidencias, etc. (RF-11, RN-09).
7. Marca el envío como entregado o, si no llega a hacerse, cancelado; desde un estado final ya no puede volver a cambiar (RF-10, RN-08).

**Origen:** "Sobre los transportes/envíos", "Sobre el seguimiento / historial".

### Flujo 4 · Un Administrador Da de Baja a una Persona Usuaria

1. El administrador inicia sesión (RF-01).
2. Marca como inactiva a una persona usuaria, siempre que con ello no se quede el sistema sin ningún administrador activo (RF-04, RN-10, RN-12).
3. La persona desactivada deja de poder iniciar sesión (RN-13).
4. Su historial (envíos gestionados, eventos registrados) se conserva íntegro (RNF-03).

**Origen:** "Sobre quién usará la aplicación".

## Requisitos No Funcionales

| ID | Requisito | Descripción |
| --- | --- | --- |
| RNF-01 | Facilidad de uso | Interfaz clara y sencilla, sin necesitar formación previa, accesible desde el navegador sin instalar nada. |
| RNF-02 | Acceso seguro | Solo entra quien tenga usuario y contraseña válidos; las credenciales se custodian de forma segura (nunca en claro); cada persona ve y hace solo lo que su tipo de acceso permite. |
| RNF-03 | Fiabilidad de los datos | La información no se pierde; dar de baja un vehículo, conductor, cliente o usuario no borra su historial. |
| RNF-04 | Trazabilidad | Las acciones relevantes sobre un envío quedan registradas con su fecha y con quién las realizó, de forma que se pueda reconstruir qué ha pasado. |
| RNF-05 | Disponibilidad | La aplicación debe poder usarse desde cualquier ordenador de la oficina con conexión a internet. |
| RNF-06 | Rendimiento adecuado | La aplicación debe responder con fluidez para el volumen de una empresa pequeña-mediana (una veintena de vehículos/conductores), sin necesitar infraestructura compleja. |

## Prioridades

- **Alta (imprescindible desde el principio):** RF-01, RF-02, RF-05, RF-06, RF-08, RF-09, RF-10, RF-11, RF-12, RF-13.
- **Media (puede llegar una vez lo anterior funcione):** RF-03, RF-04, RF-07, RF-14.

Esta priorización refleja lo que indicó la clienta: el acceso (con su arranque inicial), la gestión de vehículos/conductores/envíos, la asignación sin duplicados, los estados, el historial y unos avisos de error claros son imprescindibles desde el principio; los clientes, las estadísticas, el cambio de contraseña y la administración de usuarios son igualmente reales, pero pueden llegar un poco después.

## Trazabilidad con la Entrevista

| Sección de la entrevista | Requisitos derivados |
| --- | --- |
| Sobre el negocio y el problema actual | RF-09 (evitar duplicados), RF-12 (visibilidad) |
| Sobre los vehículos | RF-05, RF-09 (capacidad) |
| Sobre los conductores | RF-06 |
| Sobre los clientes de los envíos | RF-07, RF-08 (asociar cliente) |
| Sobre los transportes/envíos | RF-08, RF-09, RF-10 |
| Sobre el seguimiento / historial | RF-11 |
| Sobre estadísticas e informes | RF-14 |
| Sobre quién usará la aplicación | RF-01, RF-02, RF-03, RF-04 |
| Sobre cómo quieren usarla | RF-01, RF-12, RF-13, RNF-01, RNF-05 |
| Sobre lo que no necesitáis ahora | Sección "Fuera de alcance" |
| Prioridades | Sección "Prioridades" |

## Siguiente Paso

A partir de esta especificación, el siguiente paso es dividir el desarrollo en sprints iterativos: cada sprint debe añadir funcionalidad concreta de esta lista y recorrer el ciclo completo de desarrollo (diseño, implementación, pruebas) para esa funcionalidad, en lugar de agrupar el trabajo por fases técnicas.
