# TransitOps

Backend de gestión de transportes como proyecto personal con foco en arquitectura cloud y prácticas DevOps.

## Estado actual

Fecha de referencia: 24 de marzo de 2026.

El repositorio contiene una solución inicial en ASP.NET Core con dos proyectos:

- `TransitOps.Api`: API base.
- `TransitOps.Tests`: proyecto de pruebas.

Con el estado actual del repositorio, el hito del 24 de marzo puede darse por cerrado: alcance, stack, base de solución y documentación inicial ya están definidos. La estructura de carpetas debe entenderse como orientativa, no como criterio rígido de aceptación.

El código sigue en fase de arranque respecto al MVP funcional. La documentación ya deja fijado el alcance del MVP y el roadmap completo hacia una entrega con AWS, Terraform y CI/CD.

## Objetivo del proyecto

Construir un backend pequeño en funcionalidad y sólido en operación:

- gestión de transportes;
- asignación de vehículos y conductores;
- trazabilidad mediante eventos logísticos;
- autenticación y autorización básicas;
- despliegue posterior en AWS con infraestructura como código;
- observabilidad, seguridad y documentación defendible.

## Alcance del MVP

El MVP cubre el backend funcional ejecutable en local con:

- API ASP.NET Core;
- persistencia en PostgreSQL;
- CRUD de transportes, vehículos y conductores;
- asignaciones y transiciones de estado;
- autenticación JWT con roles básicos;
- pruebas iniciales;
- empaquetado local con Docker.

Queda fuera del MVP la parte cloud avanzada: Terraform, ECS, ECR, RDS en AWS, CloudWatch, alarmas y despliegue automatizado completo.

El detalle cerrado del alcance está en [docs/MVP-Backlog.md](docs/MVP-Backlog.md).

## Stack objetivo

- ASP.NET Core
- PostgreSQL
- xUnit
- Docker
- Terraform
- GitHub Actions
- AWS ECS Fargate
- Amazon RDS
- Amazon ECR
- ALB
- CloudWatch

## Estructura de la solución

```text
TransitOps/
|-- TransitOps.slnx
|-- README.md
|-- docs/
|   |-- MVP-Backlog.md
|   |-- DailyRoadmap.pdf
|   `-- Roadmap.md
|-- TransitOps.Api/
|   |-- Controllers/
|   |-- Properties/
|   |-- Dockerfile
|   |-- Program.cs
|   `-- TransitOps.Api.csproj
`-- TransitOps.Tests/
    |-- UnitTest1.cs
    `-- TransitOps.Tests.csproj
```

La distribución exacta de carpetas puede evolucionar. Lo importante en esta fase es que la solución, la documentación base y los proyectos principales ya existen y son coherentes con el roadmap.

## Documentación disponible

- Roadmap completo en Markdown: [docs/Roadmap.md](docs/Roadmap.md)
- Backlog cerrado del MVP: [docs/MVP-Backlog.md](docs/MVP-Backlog.md)
- Fuente original del roadmap: [docs/DailyRoadmap.pdf](docs/DailyRoadmap.pdf)

## Requisitos locales

- .NET SDK 10
- Docker Desktop
- PostgreSQL 16 o superior, si se ejecuta sin contenedores

## Arranque local

### Estado actual del repositorio

El repositorio ya cubre la base esperada para el hito inicial: solución creada, API base, proyecto de tests, `README`, backlog del MVP y roadmap versionados en GitHub.

Todavía no existe integración con PostgreSQL, `docker-compose`, infraestructura como código ni automatización CI/CD. Eso forma parte de hitos posteriores.

### Comandos base

Restaurar dependencias:

```powershell
dotnet restore TransitOps.slnx
```

Compilar la solución:

```powershell
dotnet build TransitOps.slnx
```

Ejecutar la API:

```powershell
dotnet run --project .\TransitOps.Api\TransitOps.Api.csproj
```

Ejecutar tests:

```powershell
dotnet test .\TransitOps.Tests\TransitOps.Tests.csproj
```

## Próximos hitos

1. Cerrar el modelo de dominio y reglas de negocio.
2. Añadir persistencia real con PostgreSQL y migraciones.
3. Implementar el núcleo funcional del MVP.
4. Hacer el entorno local reproducible con Docker.
5. Preparar la transición a AWS, Terraform y CI/CD.

## Criterios de calidad del roadmap

- La solución debe compilar limpiamente.
- Los tests deben ser ejecutables en local y en CI.
- El entorno debe ser reproducible.
- La infraestructura cloud debe quedar versionada.
- Cada semana debe poder demostrarse un resultado verificable.

## Nota sobre verificación

La documentación se ha generado a partir del roadmap PDF existente y se ha ajustado al estado real del repositorio en esta fecha. El proyecto todavía está en fase inicial, así que algunos apartados del roadmap describen trabajo planificado y no funcionalidad ya implementada.
