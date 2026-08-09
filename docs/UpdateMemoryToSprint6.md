# Plan · Poner la memoria del TFG al día hasta el Sprint 6

## Context

El código está en un estado sano: Sprints 1–6 cerrados, RF-01…RF-14 implementados e integrados, árbol limpio en `3f877f4`. La memoria no lo está.

Al auditar `tfg/memoria/` aparecen tres problemas de distinta gravedad:

1. **Afirmaciones hoy falsas.** [arquitectura.tex](tfg/memoria/contido/arquitectura.tex) cierra diciendo que *"la migración inicial materializa únicamente `app_users`; las demás tablas se añadirán al implementar sus requisitos"* — cierto en S1, falso desde S2 (hay 5 migraciones y todas las tablas existen). [conclusions.tex](tfg/memoria/contido/conclusions.tex) abre con *"La primera iteración confirma la viabilidad"*, escrito con un sprint hecho cuando hay seis. El [README de la memoria](tfg/memoria/README.md) afirma que *"S2–S8 mantienen placeholders explícitos"*, cuando S2–S6 ya están redactados.
2. **Una deuda autodeclarada y nunca pagada.** [diseno_detallado.tex](tfg/memoria/contido/diseno_detallado.tex) termina con *"Los diseños de catálogos, envíos y operaciones se incorporarán en sus respectivos sprints."* No se incorporaron. El capítulo cubre solo autenticación, contrato de error y sesión, todo de S1. **El diseño detallado de S2–S6 —el núcleo de ingeniería del proyecto— no está en la memoria.**
3. **Figuras que son andamio, no diagramas.** Las dos figuras de `arquitectura.tex` son cajas de texto y una tabla `0..1 ← *` dentro de un `\fbox{minipage}`. No hay ningún diagrama real en todo el documento.

A esto se suma que el `memoria_tfg.pdf` versionado es el del commit de Sprint 3 (`9fbb3bb`): quien abra el PDF del repo ve el proyecto tres sprints por detrás.

**Resultado buscado:** una memoria que describa con fidelidad y a calidad de entrega el sistema realmente construido hasta S6, con diagramas propios, compilando sin errores y con el PDF regenerado. Queda deliberadamente fuera todo lo que dependa del Sprint 7.

**Por qué antes de S7:** el cuello de botella real del proyecto es documental, no de desarrollo (~7 semanas hasta la ventana de defensa, memoria a ~5.500 palabras). Además, redactar el diseño obliga a enunciar por escrito las limitaciones conocidas (invalidación del JWT, carreras RN-04/RN-12, `localStorage`), lo que alimenta el plan de S7 en lugar de duplicarlo.

## Material de partida ya disponible (reutilizar, no reinventar)

Esto es lo que hace el trabajo viable en un plazo corto: el razonamiento de diseño ya está escrito, solo en el formato equivocado.

| Fuente | Qué aporta |
| --- | --- |
| `docs/Sprint2Plan.md` … `docs/Sprint6Plan.md` (13–41 KB c/u) | Secciones `## Decisiones confirmadas con el usuario`, `## Backend`, `## Frontend`, `## Pruebas`. **Fuente primaria del capítulo de diseño detallado.** |
| `docs/design/DataModel.md` | `erDiagram` Mermaid completo, decisiones por entidad, restricciones RN. Fuente del diagrama ER y de la sección de modelo. |
| `docs/design/IntegrationArchitecture.md` | `flowchart` Mermaid, sesión/roles, contrato HTTP, arranque. Fuente del diagrama de arquitectura. |
| `docs/Requirements.md` | RF-01…RF-14, RN-01…RN-16, RNF-01…06, los 4 flujos de negocio. Fuente del Flujo 3. |
| `docs/Roadmap.md` | Cierres fechados por sprint, ya redactados. |
| `CONTEXT.md` (log de decisiones) | Justificaciones datadas de cada decisión de diseño. |

**Regla de oro durante la redacción:** los `SprintNPlan.md` describen lo *planificado*. La memoria debe describir lo *implementado*. Cada sección nueva se contrasta contra el servicio real antes de darla por buena (ver tabla de verificación en el Bloque 2).

## Alcance

**Dentro:** coherencia factual, capítulo de diseño detallado S2–S6, cuatro diagramas, expansión de los capítulos de ingeniería, glosario/acrónimos/bibliografía de apoyo, compilación y PDF regenerado.

**Fuera (depende de S7, se deja explícitamente provisional):**

| Fichero / sección | Por qué espera |
| --- | --- |
| `diseno_detallado.tex` §"Sesión en frontend" | `localStorage` puede pasar a cookie `HttpOnly` en S7 |
| `despliegue.tex` | El destino accesible sigue sin decidir |
| `conclusions.tex` (cierre definitivo) | Necesita resultados de S7 |
| `validacion.tex` (pruebas de sistema) | Faltan los E2E de los 4 flujos |
| `desarrollo_iterativo.tex` §Sprints 7 y 8 | Los dos `% TODO` se rellenan al ejecutarlos |
| `anexos/trazabilidad.tex` fila RNF-01–06 | Ya dice "validación final pendiente en S7"; correcto así |

En estas secciones se mantiene la voz que la memoria ya usa bien (*"decisión pragmática para el Sprint 1, documentada con su riesgo… el endurecimiento del Sprint 7 evaluará…"*), que no habrá que reescribir después, solo continuar.

---

## Bloque 0 · Vía de compilación (bloqueante)

No hay `latexmk`, `xelatex`, `pdflatex`, `docker`, `dotnet` ni `node` en la máquina. Sin compilador, escribir varios capítulos seguidos acumula errores de LaTeX difíciles de localizar.

`winget install MiKTeX.MiKTeX` **falló** (exit 17): `Installer hash does not match` — el manifiesto de winget no cuadra con el instalador que sirve miktex.org (MiKTeX republica binarios sin subir versión).

**No voy a usar `--ignore-security-hash`**: salta la verificación de integridad de un ejecutable descargado, y eso no es algo que deba hacer por mi cuenta.

Vías, en orden de preferencia:

1. **MiKTeX desde la web oficial, instalado por ti** — `https://miktex.org/download` (Basic MiKTeX x64). Primera parte, integridad verificable por ti. Tras instalar, comprobar `latexmk --version` y `makeglossaries --version` (el `.latexmkrc` del proyecto depende de `makeglossaries`).
2. **TeX Live** vía `winget install TeXLive.TeXLive` — instalación mucho más grande y lenta, pero trae todo.
3. **Overleaf como plan B sin instalar nada** — subir `tfg/memoria/`, fijar el compilador a XeLaTeX. La plantilla UDC-FIC y `glossaries` funcionan ahí. Sirve para verificar compilación aunque el PDF final se genere luego en local.

**Criterio de salida del bloque:** `latexmk -xelatex memoria_tfg.tex` produce PDF sobre el estado *actual* del repo, antes de tocar nada. Esto establece la línea base: cualquier error posterior es atribuible a los cambios nuevos.

## Bloque 1 · Pasada de coherencia (corta, quita lo que hoy es falso)

Un commit. Sin contenido nuevo: solo dejar de afirmar cosas incorrectas.

| Fichero | Cambio |
| --- | --- |
| `contido/arquitectura.tex` | Sustituir la frase final falsa. Describir la cadena real de 5 migraciones (`InitialCreate` → `AddCatalogTables` → `AddShipments` → `AddShipmentOperation` → `AddShipmentEvents`) y que S6 no necesitó migración. Añadir sección corta de estructura del código (`Domain/`, `Features/`, `Controllers/`, `Persistence/`, `Security/`, `Middleware/`) explicando la organización por característica. |
| `contido/conclusions.tex` | Reescribir el arranque: seis incrementos cerrados y cobertura RF-01…RF-14, no "la primera iteración". Mantener el carácter provisional pendiente de S7. |
| `tfg/memoria/README.md` | Corregir "S2–S8 mantienen placeholders explícitos" → S1–S6 con contenido real, S7–S8 pendientes. |
| `memoria_tfg.tex` | Eliminar el macro `\fotoPlaceholder` (definido en la línea 13, sin usar en ningún sitio). |

`contido/resultados.tex` **no** se toca aquí: lo revisé y es correcto hasta S6, incluido su párrafo final que apunta a S7. Su única debilidad es que narra sprint a sprint y solapa con `desarrollo_iterativo.tex`; eso es una decisión de estructura, no un error, y se aborda en el Bloque 4 si sobra margen.

## Bloque 2 · `diseno_detallado.tex`: el diseño de S2–S6 (el grueso)

El capítulo pasa de 3 secciones (203 palabras, todas S1) a 8. Objetivo ~2.200–2.800 palabras. **Un commit por rebanada**, para que cada uno compile y sea revisable por separado.

Estructura propuesta:

| § | Contenido | Fuente de diseño | Contrastar contra |
| --- | --- | --- | --- |
| 1. Autenticación y autorización | Existente. Retoque menor. | — | `Security/`, `Features/Auth/AuthService.cs` |
| 2. Contrato uniforme de API | Existente. Mantener. | `IntegrationArchitecture.md` | `Middleware/` |
| 3. Sesión en el frontend | Existente. Reencuadrar como decisión de S1 + revisión S7. **No dar por final.** | — | `frontend/src/auth/` |
| 4. Catálogos: baja lógica y unicidad *(nueva)* | Unicidad funcional solo entre activos, reutilización de identificador tras baja, normalización, RN-15/RNF-03 | `Sprint2Plan.md`, `DataModel.md` | `Features/Vehicles/VehicleService.cs`, `Drivers/`, `Customers/` |
| 5. Envíos: identificación, fechas y consultas *(nueva)* | Referencia única global, FK `RESTRICT` para conservar historial, normalización UTC (`Z`/naive/offset), RN-06, filtros combinables y paginación estable | `Sprint3Plan.md` | `Features/Shipments/ShipmentService.cs` |
| 6. Operación: asignación y ciclo de vida *(nueva)* — **la más valiosa** | Las 4 decisiones confirmadas de S4: endpoints de acción explícitos, asignación conjunta nunca parcial, capacidad insuficiente como `capacityWarning` no bloqueante, sellado automático UTC de fechas reales. Máquina de estados RN-07/RN-08, recursos activos RN-03, anti-doble-reserva RN-04 **con su ventana de concurrencia declarada** y la justificación de no usar índice único filtrado | `Sprint4Plan.md`, `DataModel.md` §Restricciones | `Features/Shipments/ShipmentService.cs` |
| 7. Trazabilidad: historial inmutable *(nueva)* | Inmutabilidad (solo alta y consulta), `OccurredAt` de negocio vs `CreatedAt` de auditoría, tipos reservados al sistema vs manuales, `ICurrentUser` sobre el claim `sub`, eventos automáticos en el mismo `SaveChanges`, `CASCADE` como única excepción al criterio restrictivo | `Sprint5Plan.md`, `DataModel.md` | `Features/Shipments/ShipmentEventService.cs` |
| 8. Administración e indicadores *(nueva)* | RN-10/RN-12 con protección del último administrador **y su carrera declarada**, inactivos direccionables para reactivación, unicidad global de credenciales, RN-14 cambio de contraseña, agregados del resumen: estados globales vs periodo configurable de 30 días sobre `PlannedPickupAt`/`OccurredAt` | `Sprint6Plan.md` | `Features/Users/UserService.cs`, `Features/Reporting/SummaryService.cs` |

Criterios de redacción:

- **Diseño y por qué, no listado de código.** Cada sección responde "qué alternativas había y por qué esta". El `CONTEXT.md` tiene las justificaciones datadas.
- **No duplicar `desarrollo_iterativo.tex`.** Ese capítulo es la crónica temporal (qué pasó en cada sprint, con evidencia). Este es el diseño resultante, atemporal. Referencia cruzada con `\ref`, no repetición.
- **Las limitaciones conocidas se enuncian aquí, no se esconden.** Las dos carreras de concurrencia y la ventana de validez del JWT son decisiones de diseño documentadas con su motivo y su revisión prevista. Enunciarlas es más defendible que omitirlas, y es material directo para S7.
- Trazar cada sección a sus RF/RN con los identificadores, para que el anexo de trazabilidad siga cuadrando.

## Bloque 3 · Diagramas

**Formato: TikZ.** Razón: compila con XeLaTeX sin herramienta externa (no hay `node` para `mermaid-cli`), el fuente queda versionado junto al texto, y las figuras actuales ya son pseudo-TikZ en `\fbox{minipage}`, así que es una mejora natural y no una migración. Coste: más verboso que Mermaid.

| Diagrama | Reemplaza / ubicación | Fuente |
| --- | --- | --- |
| Arquitectura de integración y despliegue | `fig:architecture` en `arquitectura.tex` | `IntegrationArchitecture.md` flowchart + `docker-compose.yml` (3 servicios, Nginx proxy `/api`, volumen persistente) |
| Modelo de datos (ER) | `fig:data-model` en `arquitectura.tex` | `DataModel.md` `erDiagram`: 6 entidades, cardinalidades reales, atributos clave, PK/FK/UK |
| Máquina de estados del envío | Nuevo, en `diseno_detallado.tex` §6 | RN-07/RN-08: `Planned` → `InProgress` → `Delivered`/`Cancelled`, con guardas y terminalidad |
| Secuencia del Flujo 3 | Nuevo, en `diseno_detallado.tex` §6 o `requisitos.tex` | `Requirements.md` §Flujo 3 — operador ejecuta un envío de principio a fin |

El Roadmap S8 pide explícitamente "arquitectura, modelo de datos, flujos": los cuatro cubren esa exigencia. Opcional si sobra margen: diagrama de capas/carpetas del backend, aunque una tabla puede bastar.

Cada figura necesita `\caption` y `\label`, y quedar referenciada desde el texto (si no, `listoffigures` queda descolgado del discurso).

## Bloque 4 · Expansión de los capítulos de ingeniería

Son correctos pero de ~1 página. Ninguno depende de S7.

| Fichero | Ahora | Objetivo | Qué añadir |
| --- | --- | --- | --- |
| `contido/marco_tecnologico.tex` | 172 | ~1.100 | Justificar elecciones frente a alternativas descartadas, no solo enumerar. Añadir TypeScript, Vite, React Router, Vitest/RTL, xUnit, Docker Compose, GitHub Actions (hoy sin cita) |
| `contido/metodologia.tex` | 199 | ~1.000 | Desarrollar rebanadas verticales vs fases horizontales, definición de hecho, por qué el modelo completo se diseña en S1 pero se migra incrementalmente, cómo se registró el cierre de cada sprint |
| `contido/requisitos.tex` | 260 | ~900 | Método de elicitación (entrevista simulada → especificación no técnica → sprints), resumen de las 16 RN, los 4 flujos, y la trazabilidad entrevista→requisito |
| `contido/contexto_objetivos.tex` | 225 | ~700 | Contextualizar el cambio de dirección del 19-06-2026 y qué implicó; criterios de éxito medibles |
| `contido/introduccion.tex` | 180 | ~600 | Motivación del dominio y guía de lectura del documento |

Total estimado tras Bloques 1–4: **~5.500 → ~14.000–16.000 palabras**, cuerpo creíble para un TFG (~60–75 páginas con figuras y tablas), dejando S7 para completar despliegue, validación de sistema y conclusiones.

## Bloque 5 · Soporte: glosario, acrónimos, bibliografía

Hoy hay 7 acrónimos, 3 entradas de glosario y 6 referencias. El contenido nuevo se queda corto.

- `bibliografia/acronimos.tex`: añadir `utc`, `sql`, `json`, `http`, `dto`, `uuid`, `xss`, `csp`, `e2e`.
- `bibliografia/glosario.tex`: añadir `migración`, `rebanada vertical`, `máquina de estados`, `agregado`, `idempotencia`, `doble reserva`, `índice único filtrado`. Ya existen `bootstrap`, `walking-skeleton`, `baja-logica`.
- `bibliografia/bibliografia.bib`: añadir entradas para TypeScript, Vite, Vitest, React Router, xUnit, Docker, GitHub Actions, y al menos una referencia metodológica de ingeniería del software para sostener `metodologia.tex`.

Cuidado: cada `\gls{}` y `\cite{}` nuevo debe resolver, o el PDF sale con `??` sin que LaTeX falle. Verificación explícita en el bloque siguiente.

## Bloque 6 · Cierre documental

- Regenerar `tfg/memoria/memoria_tfg.pdf` (versionado y hoy en estado S3).
- `CONTEXT.md`: entrada nueva en el log de decisiones con fecha, describiendo esta puesta al día y qué queda explícitamente pendiente de S7.
- `README.md` raíz: la sección "Current Status" dice "Reference date: July 31, 2026"; actualizar para reflejar el estado documental.
- Comprobar que `docs/Roadmap.md`, `CONTEXT.md` y `README.md` no contradigan lo que ahora afirma la memoria.
- Nota de convención: el repo tiene un `docs/SprintNPlan.md` por sprint. Este trabajo es S8 adelantado; si se quiere conservar la convención, volcar este plan a `docs/Sprint8Plan.md` al empezar.

---

## Verificación

**Compilación (tras cada bloque, no solo al final):**

```bash
cd tfg/memoria && latexmk -xelatex memoria_tfg.tex
```

Criterios objetivos:

1. Compila sin errores; `latexmk` llega a PDF.
2. **Cero** `LaTeX Warning: Reference ... undefined` y cero `Citation ... undefined` en el `.log`.
3. Cero `??` en el PDF (delata `\gls{}`/`\cite{}` sin resolver, que no rompen la compilación).
4. Los 12 capítulos + 2 anexos aparecen en el índice, con numeración correcta.
5. Las 4 figuras nuevas renderizan, tienen caption y están referenciadas desde el texto.
6. `listoffigures` y `listoftables` coherentes.
7. Revisar `Overfull \hbox` (el preámbulo ya pone `\emergencystretch{3em}` por problemas previos de desbordamiento).
8. Glosario y acrónimos se generan (`makeglossaries` corre vía `.latexmkrc`).

**Coherencia factual** — cada afirmación técnica nueva debe ser comprobable contra el repo:

```bash
grep -rn "app_users\|primera iteración\|placeholders" tfg/memoria
```

Y contraste sección a sección contra los servicios listados en la tabla del Bloque 2. Si la memoria afirma un comportamiento, debe existir en `TransitOps.Api/Features/` o en una prueba de `TransitOps.Tests/`.

**Nota sobre las suites:** no hay `dotnet` ni `node` en esta máquina, así que las cifras que la memoria cita (127 backend / 30 frontend) no son verificables aquí; provienen de los cierres de sprint. Si se quiere confirmarlas antes de fijarlas en la memoria, hace falta el toolchain o una ejecución de CI.

## Riesgos y decisiones abiertas

| Riesgo | Mitigación |
| --- | --- |
| Sin compilador, los errores de LaTeX se acumulan | Bloque 0 es bloqueante; compilar tras cada bloque |
| Solapamiento entre `diseno_detallado` y `desarrollo_iterativo` | División explícita: diseño atemporal vs crónica con evidencia; referencias cruzadas |
| Escribir el diseño de sesión como final y que S7 lo cambie | §3 se redacta como decisión de S1 con revisión declarada, no como estado final |
| TikZ consume más tiempo de lo previsto | Las figuras actuales siguen siendo válidas como respaldo; degradar a 2 diagramas (arquitectura + ER) antes que bloquear el bloque |
| Los `SprintNPlan.md` describen lo planificado, no lo implementado | Contraste obligatorio contra los servicios reales antes de dar por buena cada sección |

**Decisión que sigue abierta y no bloquea este trabajo:** el destino de despliegue. Condiciona `despliegue.tex`, CI/CD, HTTPS y secretos, y es lo primero que habrá que cerrar al entrar en S7.

## Orden de ejecución

0. Vía de compilación + línea base (bloqueante)
1. Coherencia — 1 commit
2. Diseño detallado S2–S6 — 5 commits, uno por rebanada
3. Diagramas TikZ — 1–2 commits
4. Expansión de capítulos — 1–2 commits
5. Glosario / acrónimos / bibliografía — junto con 2 y 4
6. PDF regenerado + coherencia de docs raíz — 1 commit