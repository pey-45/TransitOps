# TransitOps · Backlog cerrado del MVP

## Propósito

Definir un alcance fijo, pequeño y defendible para el MVP de TransitOps. Este backlog queda cerrado: cualquier incorporación nueva debe sustituir a otra ya aprobada.

## Definición operativa del MVP

El MVP es un backend funcional de gestión de transportes, ejecutable en local, con persistencia en PostgreSQL, autenticación básica, reglas de negocio principales, pruebas iniciales y empaquetado reproducible.

El MVP no incluye todavía despliegue en AWS, Terraform, observabilidad en CloudWatch, alarmas, dashboards ni automatización completa de CI/CD cloud.

## Objetivos del MVP

- Gestionar transportes, vehículos y conductores.
- Permitir asignaciones válidas entre transporte, vehículo y conductor.
- Controlar el ciclo de vida del transporte mediante estados.
- Registrar eventos logísticos asociados al transporte.
- Ejecutar el sistema en local con una base de datos real y arranque repetible.
- Dejar una base técnica apta para la fase cloud posterior.

## Definition of Done

Un ítem del MVP se considera cerrado cuando:

- Tiene código integrado en la solución.
- Dispone de criterio de aceptación verificable.
- Puede demostrarse manualmente o mediante test.
- No introduce deuda estructural que bloquee la siguiente fase.
- Está reflejado en documentación mínima del repositorio.

## Alcance incluido

### Épica 1 · Base de solución

| ID | Ítem | Prioridad | Criterios de aceptación |
| --- | --- | --- | --- |
| MVP-01 | Estructurar la solución por proyectos y carpetas base | Must | Existen proyecto API y proyecto de tests. La solución compila. La estructura admite evolución hacia capas sin rehacer el arranque. |
| MVP-02 | Definir configuración por entorno | Must | Existen `appsettings` y perfiles de ejecución locales. La API arranca en desarrollo sin cambios manuales ambiguos. |
| MVP-03 | Documentar arranque y alcance inicial | Must | Existe `README.md` de solución y backlog cerrado del MVP enlazado desde documentación. |

### Épica 2 · Dominio y persistencia

| ID | Ítem | Prioridad | Criterios de aceptación |
| --- | --- | --- | --- |
| MVP-04 | Modelar `Transport` con estados de negocio | Must | La entidad existe con identificador, datos operativos y estado inicial. Las transiciones válidas quedan definidas y documentadas. |
| MVP-05 | Modelar `Vehicle` y `Driver` | Must | Existen entidades persistibles con restricciones básicas y relación utilizable desde `Transport`. |
| MVP-06 | Modelar `ShipmentEvent` | Should | Se pueden registrar eventos cronológicos asociados a un transporte. |
| MVP-07 | Integrar PostgreSQL y migraciones iniciales | Must | La solución crea el esquema mediante migraciones. La conexión local a PostgreSQL es estable y reproducible. |

### Épica 3 · Casos de uso API

| ID | Ítem | Prioridad | Criterios de aceptación |
| --- | --- | --- | --- |
| MVP-08 | CRUD de transportes | Must | Existen endpoints de alta, detalle, listado, actualización y baja lógica o física. Los contratos devuelven códigos HTTP coherentes. |
| MVP-09 | CRUD de vehículos | Must | Existen endpoints operativos para alta, consulta, edición y baja. |
| MVP-10 | CRUD de conductores | Must | Existen endpoints operativos para alta, consulta, edición y baja. |
| MVP-11 | Asignar vehículo y conductor a transporte | Must | Solo se permiten asignaciones coherentes con el estado del transporte. Los errores de negocio son claros. |
| MVP-12 | Transiciones de estado del transporte | Must | El flujo `planned -> in_transit -> delivered/cancelled` está implementado y protegido contra transiciones inválidas. |
| MVP-13 | Registro y consulta de eventos logísticos | Should | Se puede registrar un evento y recuperar el histórico ordenado de un transporte. |
| MVP-14 | Listados con filtros mínimos | Should | Se puede listar por estado, rango de fechas y asignación con paginación básica. |

### Épica 4 · Seguridad y contrato

| ID | Ítem | Prioridad | Criterios de aceptación |
| --- | --- | --- | --- |
| MVP-15 | Autenticación JWT con roles `admin` y `operator` | Must | Existe login funcional. Los endpoints sensibles quedan protegidos por rol. |
| MVP-16 | Manejo homogéneo de errores | Must | Existe middleware global o equivalente. Las respuestas de error siguen un contrato consistente. |
| MVP-17 | Validación de entrada | Must | La API rechaza entradas inválidas con mensajes claros y sin dejar estados inconsistentes. |
| MVP-18 | Logging estructurado básico | Should | Cada petición relevante deja trazabilidad mínima con identificador de correlación o equivalente. |

### Épica 5 · Calidad y operación local

| ID | Ítem | Prioridad | Criterios de aceptación |
| --- | --- | --- | --- |
| MVP-19 | Tests unitarios del núcleo | Must | Existen tests para reglas de estado y validaciones críticas. |
| MVP-20 | Tests de integración de endpoints clave | Should | Existen tests sobre al menos los flujos principales de transportes y autenticación. |
| MVP-21 | Dockerfile multi-stage | Must | La API puede construirse en imagen Docker de forma repetible. |
| MVP-22 | Entorno local reproducible con API y PostgreSQL | Must | Existe composición local con contenedores o procedimiento equivalente claramente documentado. |
| MVP-23 | Health checks | Should | Existen endpoints de salud separados para vida y disponibilidad. |

## Alcance excluido del MVP

- Terraform y definición de infraestructura AWS.
- Despliegue en ECS Fargate.
- ECR, ALB y RDS en cloud.
- GitHub Actions con despliegue automático.
- CloudWatch, dashboards, métricas y alarmas.
- Secrets Manager o SSM.
- OpenTelemetry, rate limiting y resiliencia avanzada.
- Pruebas de carga, costes, ADRs y memoria técnica final.
- Frontend o panel visual.

## Dependencias principales

- SDK de .NET 10.
- PostgreSQL local o contenedor Docker.
- Credenciales y configuración por entorno separadas del código.

## Riesgos a vigilar dentro del MVP

- Sobredimensionar el modelo funcional y perder tiempo antes de llegar a la parte cloud.
- Acoplar controladores, lógica y persistencia en exceso.
- Posponer pruebas y validación hasta el final.
- Introducir autenticación demasiado tarde y romper contratos ya usados.

## Criterio de cierre del MVP

El MVP queda cerrado cuando el sistema puede arrancarse desde cero en local, autenticar usuarios, gestionar transportes con sus asignaciones y estados, persistir datos en PostgreSQL, ejecutar tests mínimos y exponerse de forma empaquetable para la fase de despliegue posterior.
