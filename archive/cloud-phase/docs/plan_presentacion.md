# Guía de presentación para la defensa del TFG (TransitOps)

## Contexto

El usuario debe defender su TFG de Ingeniería Informática (UDC, mención Ingeniería
del Software, sello Euro-Inf): *"Diseño, despliegue y operación de una plataforma
cloud para la gestión de transportes en AWS mediante infraestructura como código y
prácticas DevOps"*. La memoria ya está corregida y compilada.

Necesita una **guía** (no la presentación en sí) que describa, diapositiva a
diapositiva, cómo estructurar una defensa de **15-17 minutos**. Preferencias
confirmadas:

- **Detalle**: guía completa → por diapositiva: objetivo, contenido en viñetas,
  descripción del esquema/tabla a dibujar, guion oral (qué decir) y tiempo asignado.
- **Herramienta**: PowerPoint / Google Slides / Impress (incluir consejos concretos:
  SmartArt, diagramas, plantillas).
- **Demo**: sin demo ni vídeo. Presentación 100 % conceptual, con esquemas y tablas,
  evitando capturas de pantalla concretas (difíciles de replicar; el entorno `dev`
  está destruido).
- **Idioma**: español (coherente con la memoria y el tribunal de la FIC).

El hilo conductor de toda la defensa es la tesis del proyecto:
**"pequeño en funcionalidad, completo en ingeniería"** — alcance funcional acotado
para profundizar en arquitectura cloud, IaC, DevOps, observabilidad, seguridad y
operación reales.

## Entregable

Un documento Markdown: **`memoria-tfg/presentacion/guion_presentacion.md`**
(carpeta nueva `presentacion/` junto a la memoria). Contendrá:

1. Resumen ejecutivo: nº de diapositivas, presupuesto de tiempo, tesis central y
   consejos de ensayo.
2. Convenciones visuales recomendadas (paleta coherente con la memoria —magenta UDC—,
   tipografía, uso de iconos AWS, plantilla maestra).
3. La guía diapositiva a diapositiva (el grueso), cada una con los 5 elementos.
4. Sección de consejos específicos de PowerPoint/Slides (SmartArt, diagramas,
   animaciones de aparición progresiva, tablas).
5. Anexo: banco de posibles preguntas del tribunal y respuestas breves.

> El guion oral de cada diapositiva se redacta como apoyo de ensayo, no para leerlo.

## Material de origen (qué alimenta cada diapositiva)

La guía se construye **solo** a partir de material ya existente y validado:

- Memoria LaTeX (`memoria-tfg/contido/*.tex`) — fuente principal de contenido.
- `docs/RequirementsTraceability.md` — matriz requisitos → implementación → evidencia
  (alimenta la diapositiva de validación/trazabilidad).
- `docs/FinalEvidence.md` — valores reales del último despliegue (87 tests, health 200,
  login 200, migración exit 0, 9 alarmas, 72 recursos destruidos, state vacío).
- `docs/CloudArchitecture.md` — topología AWS y decisiones (diapositiva de arquitectura).
- `docs/CloudOperations.md` / `docs/CloudReliability.md` — seguridad, coste, rollback,
  restore, runbooks.

No se inventan datos: todas las cifras provienen de estos documentos.

## Estructura propuesta (18 diapositivas, ≈16:30 de núcleo)

Bloques: **Apertura → Planteamiento → Diseño → DevOps y operación → Resultados y cierre**.

| # | Título | Propósito | Visual principal (conceptual) | Tiempo |
|---|--------|-----------|-------------------------------|--------|
| 1 | Portada | Identificación | Título, autor, directora, UDC, mención IS, sello Euro-Inf | 0:15 |
| 2 | Índice / hoja de ruta | Mapa de la charla | 5 bloques en barra horizontal o lista numerada | 0:20 |
| 3 | Motivación y problema | Por qué importa | Esquema del ciclo moderno: *compilar→probar→desplegar→observar→recuperar* | 0:55 |
| 4 | Objetivo y alcance (decisión clave) | La tesis del trabajo | Diagrama 2 ejes: funcionalidad (baja) vs profundidad técnica (alta) + tabla in/out scope | 1:25 |
| 5 | Metodología y planificación | Cómo se trabajó | Línea temporal de sprints/fases (1-9) + nota de coste/entorno efímero | 0:55 |
| 6 | Marco tecnológico | Stack y su justificación | Esquema del stack por capas + **tabla** "tecnología elegida vs alternativa vs razón" | 1:20 |
| 7 | Dominio y casos de uso | Qué modela el sistema | Modelo de dominio (5 entidades + relaciones) + máquina de estados del transporte | 0:55 |
| 8 | Arquitectura del sistema | Lógica + nube | (a) capas del backend monolito modular; (b) **diagrama AWS estrella**: VPC, subredes pub/priv, ALB→ECS→RDS, ECR, Route53/ACM, Secrets Manager, CloudWatch | 1:45 |
| 9 | Infraestructura como código | Reproducibilidad | Esquema de módulos Terraform + estado remoto S3/DynamoDB | 0:55 |
| 10 | CI/CD y entrega | Automatización segura | Diagrama de pipeline: GitHub Actions + OIDC → build→ECR→Terraform→migración→escalado→smoke | 0:55 |
| 11 | Decisiones de despliegue | Madurez operativa | Secuencia por fases: infra a 0 tareas → imagen → migración ECS puntual → escalar; secretos externos | 0:55 |
| 12 | Fiabilidad: rollback y restore | Recuperación | Esquema: rollback por tag vía Terraform + circuit breaker; restore RDS (snapshot→migrate-only); concepto de runbooks | 0:55 |
| 13 | Observabilidad | Operable y diagnosticable | Esquema: health live/ready, logs JSON + X-Correlation-ID, dashboard, alarmas | 0:45 |
| 14 | Seguridad | Línea base realista | **Tabla** por capas: aplicación / red / configuración + limitaciones asumidas | 0:45 |
| 15 | Coste y entorno efímero | Ingeniería responsable | Ciclo *recrear→validar→destruir* + tabla de coste por servicio + qué permanece | 0:45 |
| 16 | Validación y resultados | Qué se demostró | **Tabla** de resultados clave + idea de trazabilidad requisitos→evidencia | 1:10 |
| 17 | Conclusiones, competencias y lecciones | Cierre de valor | Objetivos cumplidos + competencias Euro-Inf + 3 incidencias reales resueltas (DNS, Secrets Manager, ECR) | 1:00 |
| 18 | Líneas futuras + gracias | Cierre y preguntas | Lista breve de evolución + pantalla de cierre/contacto | 0:35 |

**Total núcleo ≈ 16 min 30 s** → encaja en 15-17 min con pausas naturales.

Flexibilidad incorporada en la guía:
- La diapositiva 8 puede partirse en dos (lógica + nube) si se prefiere; añade ~30 s.
- Diapositivas comprimibles si va sobrado de tiempo: 11, 13, 15.
- Diapositiva imprescindible y "estrella": **8 (arquitectura AWS)**.

## Contenido de cada diapositiva en el documento final

Por cada una de las 18 diapositivas, el fichero incluirá este formato fijo:

```
### Diapositiva N — Título
- Objetivo: (una frase: qué debe quedar claro al tribunal)
- Contenido (viñetas): los 3-5 puntos que aparecen en pantalla (texto breve)
- Visual: descripción del esquema/tabla a dibujar (cajas, flechas, columnas)
- Guion oral (~Xs): 3-5 frases de lo que decir, en lenguaje hablado
- Tiempo: 0:MM
- Transición: frase puente a la siguiente diapositiva
```

Decisiones de contenido relevantes para que la guía sea sólida en una defensa de la FIC:

- **Justificación tecnológica con tablas** (diap. 6): el tribunal valora el "por qué"
  frente a alternativas (ASP.NET Core vs Node/Spring; PostgreSQL vs MySQL/Mongo;
  Terraform vs CloudFormation/CDK; AWS vs Azure/GCP; CloudWatch vs Datadog). Esto
  además responde a una observación previa de la directora en la memoria.
- **Incidencias reales** (diap. 17): delegación DNS/ACM, secretos programados para
  borrado en Secrets Manager, `force_delete` de ECR. Demuestran operación real y
  madurez; son un punto fuerte ante el tribunal.
- **Resultados como tabla de evidencias** (diap. 16): cifras concretas de
  `FinalEvidence.md`, presentadas como tabla, no como capturas.
- **Mapa a competencias Euro-Inf / mención IS** (diap. 17): conecta el trabajo con
  las competencias del título.

## Consejos de PowerPoint/Slides (sección dedicada en el documento)

- **Plantilla maestra** con franja de color UDC (magenta) y pie con título + nº.
- **Diagramas**: usar formas + conectores agrupados; para el stack y el pipeline,
  SmartArt tipo "Proceso" / "Jerarquía". Iconos oficiales de AWS (Architecture Icons)
  para ALB/ECS/RDS/etc. — dan aspecto profesional sin capturas.
- **Tablas**: estilo limpio, encabezado con color de marca, máx. 4-5 columnas.
- **Aparición progresiva**: animar viñetas/cajas por clic para no saturar y marcar el
  ritmo del discurso (sin animaciones llamativas).
- **Regla 1 idea por diapositiva**; texto en frases cortas, no párrafos.
- **Contraste y tamaño**: fuente ≥ 24 pt en cuerpo; legible desde el fondo del aula.
- **Modo presentador**: usar las notas (el guion oral) en la vista de notas.

## Verificación (cómo comprobar que sirve)

1. **Ajuste temporal**: sumar la columna de tiempos = ~16:30; ensayar en voz alta
   cronometrando cada bloque. Margen objetivo 15-17 min.
2. **Cobertura**: comprobar que cada objetivo específico de la memoria (cap. 2) y cada
   bloque de resultados (cap. 13) aparece en al menos una diapositiva.
3. **Trazabilidad de cifras**: cada dato numérico de la guía debe existir en
   `docs/FinalEvidence.md` o en la memoria (sin inventar).
4. **Sin capturas**: revisar que ningún visual dependa de una captura de pantalla;
   todo son esquemas/tablas reproducibles.
5. **Coherencia con la directora**: la guía refuerza los puntos que la directora pidió
   en la memoria (justificación de tecnologías, explicación de figuras/conceptos).

## Notas

- Solo se crea **un fichero nuevo** (`memoria-tfg/presentacion/guion_presentacion.md`).
  No se modifica la memoria ni el código.
- La guía describe la presentación; no genera el `.pptx`. Si más adelante se quiere,
  puede derivarse a Beamer o a un `.pptx` a partir de esta guía.