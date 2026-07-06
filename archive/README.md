# Archive

## cloud-phase

Contenido del TFG de cuando el enfoque era "Diseño, despliegue y operación de
una plataforma cloud para la gestión de transportes en AWS mediante
infraestructura como código y prácticas DevOps".

El TFG cambió de dirección hacia "Diseño y desarrollo de una aplicación de
gestión de transportes: ciclo de vida completo del software" (solicitud de
modificación firmada el 19/06/2026). Esta carpeta conserva ese trabajo previo
como referencia; no forma parte del proyecto activo.

Contenido movido tal cual, sin reescribir:

- `README.md`, `CONTEXT.md`, `AGENTS.md`: documentación de repositorio de la fase cloud.
- `docs/`: requisitos, roadmap de sprints, arquitectura/despliegue/operación/fiabilidad cloud, trazabilidad, evidencia final y guion de presentación de la fase cloud.
- `infra/terraform/`: infraestructura como código (bootstrap, entornos dev/prod, módulos).
- `scripts/cloud/`: scripts de validación y auditoría AWS.
- `github-workflows/`: workflows de Terraform, despliegue y rollback en AWS (fuera de `.github/workflows`, por lo que ya no se ejecutan).
- `tfg/memoria/`: memoria LaTeX completa de la fase cloud.
- `tfg/presentacion/`: diapositivas y guion de defensa de la fase cloud.
