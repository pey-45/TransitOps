# Archive

## cloud-phase

Contenido del TFG de cuando el enfoque era "Diseño, despliegue y operación de
una plataforma cloud para la gestión de transportes en AWS mediante
infraestructura como código y prácticas DevOps".

El TFG cambió de dirección hacia "Diseño y desarrollo de una aplicación de
gestión de transportes: ciclo de vida completo del software" (solicitud de
modificación firmada el 19/06/2026). Esta carpeta conserva ese trabajo previo;
no forma parte del proyecto activo.

Se decidió **reimplementar la aplicación desde cero** siguiendo la metodología
iterativa de ciclo completo del nuevo enfoque, en lugar de continuar sobre el
código anterior. Ese código se conserva aquí como **referencia** (oráculo) para
consultarlo durante la reimplementación: reglas de negocio ya resueltas,
migraciones, casos de prueba y decisiones técnicas. El stack se mantiene
(.NET + PostgreSQL en backend, React en frontend), por lo que buena parte es
directamente consultable.

### Documentación y planificación de la fase cloud (referencia histórica)

- `README.md`, `CONTEXT.md`, `AGENTS.md`: documentación de repositorio de la fase cloud.
- `docs/`: requisitos, roadmap de sprints, arquitectura/despliegue/operación/fiabilidad cloud, trazabilidad, evidencia final, guion de presentación y guía de verificación local (`LocalVerification.md`) de la fase cloud.
- `infra/terraform/`: infraestructura como código (bootstrap, entornos dev/prod, módulos).
- `scripts/cloud/`: scripts de validación y auditoría AWS.
- `github-workflows/`: workflows de CI, Terraform, despliegue y rollback (fuera de `.github/workflows`, por lo que ya no se ejecutan).
- `tfg/memoria/`: memoria LaTeX completa de la fase cloud.
- `tfg/presentacion/`: diapositivas y guion de defensa de la fase cloud.

### Implementación anterior (oráculo de referencia para el rediseño)

Solución .NET autocontenida y todavía compilable/ejecutable desde esta carpeta:

- `TransitOps.slnx`, `TransitOps.Api/`, `TransitOps.Tests/`: backend ASP.NET Core (.NET 10) con persistencia EF Core/PostgreSQL, autenticación JWT, CRUD de transportes/vehículos/conductores, asignación, ciclo de vida, eventos, administración de usuarios, y pruebas de integración xUnit.
- `docker-compose.yml`, `.dockerignore`, `.env` / `.env.example`, `dotnet-tools.json`: reproducibilidad local del backend anterior.
- `scripts/database/`, `scripts/testing/`: seed de datos y flujo de smoke Postman/Newman.
- `docs/LocalVerification.md`: cómo arrancar y verificar la implementación anterior.

Nada de esta carpeta debe editarse ni usarse como base directa del proyecto
activo: es solo consulta.
