# Presentación de defensa de TransitOps

Material de defensa del Trabajo Fin de Grado de Pablo Manzanares López, dirigido por Paula María Castro Castro, preparado el 5 de septiembre de 2026.

## Entrega

- [PowerPoint con notas del ponente](entrega/TransitOps_Presentacion_TFG.pptx).
- [PDF para proyectar](entrega/TransitOps_Presentacion_TFG.pdf).
- [Guion de exposición y tiempos](entrega/Guion_defensa_TransitOps.md).

La exposición principal consta de **18 diapositivas** y tiene una pauta de **18 minutos y 20 segundos**. El guion incluye ajustes para una versión de 15 minutos. Los tiempos son orientativos y deben contrastarse en el ensayo.

Hay cuatro diapositivas de apoyo: trazabilidad, evolución de pruebas, estimación económica y alternativas de diseño. Están ocultas en el pase normal del PowerPoint y se pueden abrir desde la vista del moderador durante las preguntas. El PDF las conserva al final, tras las conclusiones.

El PowerPoint contiene texto, tablas y gráficos editables, notas de exposición y referencias por diapositiva. Los diagramas proceden de la memoria y son imágenes, al igual que las capturas reales. La presentación se puede exponer sin conexión a la aplicación. El PDF conserva el aspecto del pase a partir de las diapositivas renderizadas en Full HD y contiene marcadores de navegación.

## Fuentes y decisiones de contenido

La fuente principal es [memoria_tfg.pdf](../memoria/memoria_tfg.pdf), de 83 páginas. La ruta `tfg/memoria/memoria/_tfg.pdf` indicada en la petición no existe en este checkout. Las referencias de las notas y los pies emplean la **numeración impresa de la memoria**, no el número absoluto de página del archivo PDF.

La presentación se apoya también en `AGENTS.md`, `CONTEXT.md`, `README.md`, requisitos, documentación de diseño, despliegue y código del repositorio activo. Conserva los recuentos de pruebas documentados en la memoria como evidencia histórica de S7, sin afirmar una nueva ejecución de esas suites. Las cifras de esfuerzo y coste son estimaciones. Las capturas contienen datos de prueba y la URL del despliegue es temporal. La entrevista es simulada y no se atribuyen al proyecto resultados de productividad, estudios de usuarios ni pruebas de carga.

La narrativa prioriza problema, alcance, método, arquitectura, reglas críticas, evidencia y evaluación. Los documentos académicos y sus registros permanecen dentro de `tfg/`.

## Fuentes editables y regeneración

- `fuente/contenido.json`: guion, tiempos y referencias.
- `fuente/generar.mjs`: composición del PowerPoint mediante `@oai/artifact-tool`.
- `fuente/ocultar_apoyo.py`: indicadores nativos de diapositiva oculta y encuadres reversibles, sin modificar las imágenes originales.
- `fuente/exportar_pdf.py`: copia PDF de las diapositivas renderizadas.

La generación requiere el runtime de artefactos de Codex usado en esta sesión. `generar.mjs` admite `TRANSITOPS_RUNTIME` y `TRANSITOPS_PRESENTATION_SKILL` para resolver sus rutas. Para una revisión, establece `TRANSITOPS_PPTX_NAME` con un nombre nuevo: el finalizador conserva los archivos existentes. Las previsualizaciones, los borradores y los registros de validación permanecen en directorios locales ignorados por Git.
