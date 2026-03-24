# TransitOps · Roadmap en Markdown

Fuente base: `TransitOps.Api/Docs/DailyRoadmap.pdf`

## Objetivo final

Construir y entregar un backend de gestión de transportes con despliegue en AWS, infraestructura como código, CI/CD, observabilidad, seguridad básica y memoria técnica defendible.

## Stack propuesto

- ASP.NET Core
- PostgreSQL
- Docker
- Terraform
- GitHub Actions
- AWS ECS Fargate
- Amazon ECR
- Amazon RDS
- Application Load Balancer
- CloudWatch
- Secrets Manager o SSM Parameter Store

## Criterio de ejecución

El proyecto debe mantenerse pequeño en funcionalidad y profundo en operación cloud. Cada día debe cerrar con un resultado verificable. Si un día se retrasa, se mueve el detalle accesorio, no el núcleo del roadmap.

## Semana 1 · 24/03 al 29/03

### Fase 1 · Definición y base

| Fecha | Foco | Tareas concretas | Resultado esperado |
| --- | --- | --- | --- |
| 24 mar | Definición de alcance y stack | Cerrar alcance funcional: transportes, vehículos, conductores, eventos y usuarios. Fijar stack definitivo: ASP.NET Core, PostgreSQL, Docker, Terraform, GitHub Actions y AWS ECS Fargate. Crear repositorio, ramas base y estructura de carpetas para `src`, `tests`, `infra` y `docs`. | README inicial, backlog cerrado de MVP y estructura de solución creada. |
| 25 mar | Modelado del dominio | Definir entidades principales y sus relaciones. Diseñar estados de transporte y reglas de transición. Escribir esquema inicial de base de datos. | Diagrama entidad-relación inicial y lista de reglas de negocio. |
| 26 mar | Arranque del backend | Crear proyecto Web API y proyectos auxiliares por capas. Configurar inyección de dependencias, `appsettings` y perfiles por entorno. Definir convenciones de rutas, DTOs y estructura de respuestas. | La solución compila y la API base arranca en local. |
| 27 mar | Persistencia inicial | Integrar PostgreSQL y configurar cadena de conexión por entorno. Crear `DbContext`, configuraciones de entidades y primera migración. Verificar que la base de datos se crea correctamente. | Primera migración aplicada y conexión local estable. |
| 28 mar | CRUD de transportes | Implementar `create`, `get by id`, `list`, `update` y `delete` lógico o físico de transportes. Separar capa de aplicación de persistencia. Probar manualmente todos los endpoints. | CRUD de `Transport` operativo y colección Postman o archivo `.http`. |
| 29 mar | Vehículos y conductores | Implementar entidades, endpoints y persistencia de `Vehicle` y `Driver`. Añadir validaciones de negocio básicas. Revisar relaciones y restricciones. | CRUD de `Vehicle` y `Driver` funcional y modelo relacional ajustado. |

## Semana 2 · 30/03 al 05/04

### Fase 2 · Lógica y calidad

| Fecha | Foco | Tareas concretas | Resultado esperado |
| --- | --- | --- | --- |
| 30 mar | Asignaciones | Implementar caso de uso de asignar vehículo y conductor a un transporte. Bloquear asignaciones inválidas según estado. Probar flujos completos de alta y asignación. | Caso de uso de asignación completo y pruebas manuales documentadas. |
| 31 mar | Estados de transporte | Implementar transición `planned -> in_transit -> delivered/cancelled`. Centralizar validación de transiciones. Añadir errores de dominio claros. | Máquina de estados básica y reglas documentadas. |
| 01 abr | Eventos logísticos | Crear `ShipmentEvent` para registrar incidencias y cambios. Permitir asociar eventos al transporte. Diseñar histórico cronológico consultable. | Endpoint de alta de eventos y consulta de histórico. |
| 02 abr | Listados y filtrado | Implementar filtros por estado, rango de fechas y asignación. Añadir paginación y ordenación básica. Optimizar el contrato de respuesta para listados. | Listado paginado y filtros útiles para demo. |
| 03 abr | Autenticación | Implementar autenticación con JWT. Definir roles mínimos: `admin` y `operator`. Proteger endpoints sensibles. | Login funcional y autorización por roles. |
| 04 abr | Errores y contrato API | Crear middleware global de excepciones. Normalizar códigos HTTP y `payloads` de error. Documentar respuestas de error más comunes. | Manejo de errores homogéneo y API más defendible. |
| 05 abr | Logging estructurado | Integrar logging estructurado con contexto por petición. Añadir `request id` o `correlation id`. Registrar operaciones críticas sin ruido excesivo. | Logs útiles para operación y trazabilidad por petición. |

## Semana 3 · 06/04 al 12/04

### Fase 3 · Producción local

| Fecha | Foco | Tareas concretas | Resultado esperado |
| --- | --- | --- | --- |
| 06 abr | Tests iniciales | Crear tests unitarios para reglas de estado y validaciones. Crear tests de integración para endpoints principales. Configurar proyecto de test y ejecución local estable. | Base de testing lista y cobertura inicial del núcleo. |
| 07 abr | Dockerfile | Crear `Dockerfile` multi-stage para la API. Reducir tamaño y simplificar runtime. Verificar build local de imagen. | Imagen Docker construida y `Dockerfile` reutilizable. |
| 08 abr | `docker-compose` local | Levantar API y PostgreSQL con `docker-compose`. Pasar variables por entorno. Asegurar que cualquier clon del repo arranca rápido. | Entorno local reproducible y onboarding técnico rápido. |
| 09 abr | Migraciones y arranque limpio | Decidir estrategia de migraciones en arranque o script separado. Verificar recreación completa desde cero. Documentar el flujo de bootstrap local. | Proyecto reiniciable sin pasos manuales ambiguos. |
| 10 abr | Health checks | Crear `/health/live` y `/health/ready`. Comprobar conectividad con base de datos en readiness. Preparar la app para despliegue detrás de balanceador. | Health checks útiles para ECS y ALB. |
| 11 abr | Rendimiento de consultas | Detectar consultas críticas y mejorar `includes` y `joins`. Crear índices iniciales en columnas de búsqueda. Medir tiempos antes y después. | Consultas principales optimizadas y notas para la memoria. |
| 12 abr | Validaciones de entrada | Refinar validaciones con FluentValidation o solución equivalente. Asegurar mensajes de error claros. Evitar estados inconsistentes desde la entrada. | Validación robusta y menor acoplamiento en controladores. |

## Semana 4 · 13/04 al 19/04

### Fase 4 · AWS + Terraform

| Fecha | Foco | Tareas concretas | Resultado esperado |
| --- | --- | --- | --- |
| 13 abr | Refactor y limpieza | Eliminar duplicidad. Revisar nombres, responsabilidades y separación de capas. Dejar el backend listo antes del salto a AWS. | Código más limpio y estable. |
| 14 abr | Preparación AWS y Terraform | Configurar credenciales AWS y estructura del proyecto Terraform. Definir módulos o carpetas base por recurso. Preparar variables, outputs y backend local inicial. | Terraform inicializado y base para IaC. |
| 15 abr | Red: VPC y subredes | Crear VPC, subredes públicas y privadas con Terraform. Diseñar un esquema mínimo pero correcto para app y RDS. Etiquetar recursos de forma consistente. | Topología de red creada. |
| 16 abr | Seguridad de red | Definir security groups para ALB, ECS y RDS. Permitir solo tráfico estrictamente necesario. Revisar exposición pública y segmentación. | Base de seguridad de red funcional. |
| 17 abr | Base de datos en AWS | Crear instancia RDS PostgreSQL. Configurar almacenamiento, credenciales y conectividad privada. Probar conexión desde entorno controlado. | RDS desplegado y persistencia cloud operativa. |
| 18 abr | Registro de imágenes | Crear repositorio ECR. Etiquetar imagen y subir primera versión manualmente. Validar autenticación desde local o CI. | Flujo de imagen hacia AWS funcionando. |
| 19 abr | ECS Cluster y Task Definition | Crear cluster ECS. Definir task definition con CPU, memoria, variables y logs. Preparar execution role y task role. | Aplicación lista para ejecutarse en Fargate. |

## Semana 5 · 20/04 al 26/04

### Fase 5 · CI/CD

| Fecha | Foco | Tareas concretas | Resultado esperado |
| --- | --- | --- | --- |
| 20 abr | Servicio ECS y ALB | Crear Application Load Balancer y target group. Desplegar ECS Service en Fargate. Verificar acceso externo a la API y health checks. | Primera versión en AWS accesible. |
| 21 abr | CI básica | Crear workflow de GitHub Actions para `restore`, `build` y `test`. Fallar pipeline ante errores de compilación o tests. Publicar badges opcionalmente. | CI mínima operativa. |
| 22 abr | Build Docker y push a ECR | Automatizar build de imagen en GitHub Actions. Autenticarse en AWS y subir imagen a ECR. Versionar tags de imagen de forma coherente. | Pipeline de artefactos completado. |
| 23 abr | Terraform plan en CI | Ejecutar `terraform fmt`, `validate` y `plan` desde pipeline. Separar variables sensibles. Guardar plan o resumen para revisión. | Control de cambios infraestructurales automatizado. |
| 24 abr | Terraform apply controlado | Definir cómo aplicar cambios: aprobación manual o rama concreta. Evitar despliegues accidentales. Comprobar idempotencia. | Proceso de despliegue más seguro. |
| 25 abr | Despliegue automático de ECS | Actualizar task definition con nueva imagen. Forzar nuevo deployment tras merge. Confirmar que la nueva versión queda activa. | CD funcional. |
| 26 abr | Smoke tests post-deploy | Añadir comprobaciones básicas después del despliegue. Verificar login, health y caso mínimo de negocio. Fallar el pipeline si el despliegue no es usable. | Pipeline end-to-end más fiable. |

## Semana 6 · 27/04 al 03/05

### Fase 6 · Observabilidad y seguridad

| Fecha | Foco | Tareas concretas | Resultado esperado |
| --- | --- | --- | --- |
| 27 abr | Consolidación CI/CD | Revisar pipelines completos y tiempos. Limpiar pasos redundantes. Documentar el flujo de integración y entrega. | CI/CD listo para presentar. |
| 28 abr | Logs en CloudWatch | Enviar logs del contenedor a CloudWatch. Verificar formato, filtros y búsqueda. Asegurar que los logs sirven para diagnosticar fallos reales. | Observabilidad básica en producción. |
| 29 abr | Métricas operativas | Revisar métricas de ECS, ALB y RDS. Elegir las que aparecerán en demo y memoria. Anotar indicadores relevantes: CPU, memoria, errores y latencia. | Set mínimo de métricas definido. |
| 30 abr | Dashboard | Crear dashboard en CloudWatch. Agrupar métricas de infraestructura y aplicación. Preparar visualización clara para defensa. | Dashboard de operación disponible. |
| 01 may | Alarmas | Crear alarmas de CPU alta, errores o indisponibilidad. Definir umbrales razonables para un entorno académico. Probar que se disparan ante un escenario controlado si es posible. | Base de alertado implementada. |
| 02 may | Gestión de secretos | Mover credenciales y secretos a AWS Secrets Manager o SSM. Eliminar cualquier secreto del código y del pipeline. Documentar estrategia de configuración segura. | Configuración segura externalizada. |
| 03 may | IAM mínimo privilegio | Revisar roles de ECS, GitHub Actions y acceso Terraform. Reducir permisos innecesarios. Documentar el principio de mínimo privilegio. | IAM más defendible. |

## Semana 7 · 04/05 al 10/05

### Fase 7 · Mejora técnica

| Fecha | Foco | Tareas concretas | Resultado esperado |
| --- | --- | --- | --- |
| 04 may | Revisión general de seguridad | Comprobar puertos expuestos, logs sensibles y configuración pública. Revisar autenticación y autorización. Cerrar deuda de seguridad detectada. | Baseline de seguridad aceptable. |
| 05 may | OpenTelemetry o trazas básicas | Instrumentar la aplicación con trazas o telemetría equivalente. Propagar identificadores entre logs y peticiones. Preparar historia clara de observabilidad para la memoria. | Telemetría superior al mínimo. |
| 06 may | Mejora del logging | Añadir contexto de negocio: `transport id`, estado y usuario. Evitar loggear datos innecesarios. Hacer que un fallo real pueda seguirse mejor. | Logs más útiles para soporte. |
| 07 may | Rate limiting o protección básica | Aplicar limitación de peticiones o protección equivalente. Restringir abusos sencillos. Documentar por qué se incluye como medida operativa. | API más robusta frente a abuso básico. |
| 08 may | Resiliencia | Introducir `retry` controlado donde tenga sentido. Revisar timeouts y manejo de dependencias. Evitar fallos silenciosos o bloqueos innecesarios. | Comportamiento más estable ante errores transitorios. |
| 09 may | Estrategia de migraciones | Definir cómo evolucionará el esquema en despliegues futuros. Separar migración de arranque de aplicación si es necesario. Redactar justificación técnica. | Historia clara de cambios de base de datos. |
| 10 may | Backups y recuperación | Revisar snapshots o backups de RDS. Documentar RPO y RTO aproximados a nivel académico. Explicar qué se recupera y qué no. | Sección de continuidad operativa lista. |

## Semana 8 · 11/05 al 17/05

### Fase 8 · Diagramas y documentación

| Fecha | Foco | Tareas concretas | Resultado esperado |
| --- | --- | --- | --- |
| 11 may | Pruebas de carga simples | Ejecutar una carga ligera con `k6`, `hey` o herramienta similar. Medir tiempos y errores en escenario básico. Extraer 2 o 3 conclusiones útiles, no solo números. | Resultados medibles para la memoria. |
| 12 may | Diagrama de contexto | Crear C4 nivel 1 con actores y sistema. Representar empresa operadora, API y servicios externos relevantes. Alinear diagrama con el alcance real. | Diagrama de contexto listo. |
| 13 may | Diagrama de contenedores | Crear C4 nivel 2. Reflejar API, base de datos, CI/CD y componentes AWS clave. Evitar incluir elementos no implementados. | Diagrama de contenedores consistente. |
| 14 may | Diagrama de despliegue AWS | Representar VPC, subredes, ALB, ECS, RDS y flujos. Indicar qué está en red pública y qué en privada. Usar el diagrama para explicar seguridad y operación. | Diagrama de despliegue claro. |
| 15 may | Diagrama de pipeline | Dibujar CI/CD desde commit hasta despliegue. Incluir build, test, imagen, ECR, Terraform y ECS. Dejar clara la automatización. | Pipeline visual defendible. |
| 16 may | Modelo de datos | Generar modelo de datos final. Revisar nombres, claves e índices. Ajustar documentación a lo realmente desplegado. | Modelo relacional final. |
| 17 may | Redacción de arquitectura | Escribir sección de arquitectura backend y razones del monolito modular. Explicar capas y responsabilidades. Justificar por qué no se usaron microservicios. | Sección de arquitectura casi cerrada. |

## Semana 9 · 18/05 al 24/05

### Fase 9 · Memoria técnica

| Fecha | Foco | Tareas concretas | Resultado esperado |
| --- | --- | --- | --- |
| 18 may | Decisiones técnicas | Redactar ADRs resumidas o tabla de decisiones. Justificar ECS frente a EKS, Terraform frente a consola y RDS frente a alternativa local. Anotar trade-offs reales. | Sección de decisiones lista. |
| 19 may | Introducción y objetivos | Redactar motivación del TFG. Describir problema, objetivo general y objetivos específicos. Alinear texto con la transición a cloud y DevOps. | Apertura de memoria preparada. |
| 20 may | Requisitos funcionales | Documentar casos de uso implementados. Separar claramente lo realizado de lo descartado. Añadir criterios de aceptación resumidos. | Requisitos funcionales cerrados. |
| 21 may | Requisitos no funcionales | Redactar disponibilidad, seguridad, mantenibilidad, despliegue y observabilidad. Relacionarlos con decisiones implementadas. Evitar requisitos que no se puedan demostrar. | Requisitos no funcionales defendibles. |
| 22 may | Sección backend | Explicar estructura de la API, capas, endpoints y persistencia. Añadir ejemplos de flujo. Insertar capturas o fragmentos pequeños si aportan valor. | Capítulo backend avanzado. |
| 23 may | Sección cloud | Explicar AWS, red, despliegue y operación básica. Incluir Terraform como eje de reproducibilidad. Relacionar arquitectura con seguridad. | Capítulo cloud avanzado. |
| 24 may | CI/CD y observabilidad | Redactar pipeline, estrategia de despliegue, logging, métricas y alarmas. Incluir capturas del dashboard si conviene. Destacar automatización y operación. | Capítulo DevOps casi listo. |

## Semana 10 · 25/05 al 31/05

### Fase 10 · Cierre

| Fecha | Foco | Tareas concretas | Resultado esperado |
| --- | --- | --- | --- |
| 25 may | Seguridad y pruebas | Redactar autenticación, IAM, secretos, pruebas unitarias, integración y carga. Resumir resultados de forma concreta. Evitar promesas excesivas. | Capítulo de calidad y seguridad completado. |
| 26 may | Resultados | Redactar qué se consiguió exactamente. Comparar objetivo inicial y alcance final. Añadir métricas o hitos verificables. | Sección de resultados terminada. |
| 27 may | Costes | Estimar coste mensual aproximado del despliegue. Identificar qué servicios impactan más. Explicar que el entorno es académico y acotado. | Análisis de costes incluido. |
| 28 may | Limitaciones | Indicar qué no se implementó: frontend, escalado avanzado, multi-región y similares. Convertir limitaciones en honestidad técnica. Evitar que parezcan fallos conceptuales. | Limitaciones bien argumentadas. |
| 29 may | Trabajo futuro | Proponer mejoras realistas: colas, eventos, más observabilidad, autoscaling o frontend. Relacionar con una evolución profesional hacia Cloud Engineer o DevOps. Mantener foco en continuidad del sistema. | Trabajo futuro coherente. |
| 30 may | Revisión integral | Revisar memoria, diagramas, ortografía, enlaces y consistencia técnica. Comprobar que el repositorio está limpio y presentable. Ejecutar demo completa una última vez. | Versión candidata final. |
| 31 may | Entrega final | Preparar ZIP o repositorio, memoria, presentación y guion de demo. Verificar capturas, comandos, credenciales y materiales de defensa. Cerrar checklist final de entrega. | Proyecto listo para entregar y defender. |

## Checklist de control semanal

| # | Control | Qué validar |
| --- | --- | --- |
| 1 | Compilación limpia | La solución compila sin pasos manuales extraños. |
| 2 | Tests ejecutables | Los tests existentes corren localmente y en CI. |
| 3 | Entorno reproducible | Otro equipo podría levantar el proyecto con instrucciones claras. |
| 4 | Infraestructura versionada | Los cambios de AWS pasan por Terraform. |
| 5 | Demo operativa | La funcionalidad implementada esa semana se puede enseñar en 5 minutos. |
| 6 | Documentación al día | README, diagramas y notas no quedan desplazados al final. |

## Resultado esperado al 31 de mayo

Backend funcional, desplegado en AWS, infraestructura definida con Terraform, pipeline CI/CD operativo, logs y métricas visibles, memoria técnica escrita y material de defensa preparado.
