# Presentación de defensa de TransitOps

Material de defensa del Trabajo Fin de Grado de Pablo Manzanares López, dirigido por Paula María Castro Castro, revisado el 17 de septiembre de 2026.

## Entrega

- [PowerPoint con notas del ponente](entrega/TransitOps_Presentacion_TFG.pptx).
- [PDF para proyectar](entrega/TransitOps_Presentacion_TFG.pdf).
- [Guion de exposición y tiempos](entrega/Guion_defensa_TransitOps.md).
- [Guion resumido para lectura o exportación a PDF](entrega/GUION.md).
- [Demo funcional en vídeo](entrega/DEMO.mp4).

La exposición principal consta de **13 diapositivas** y una demo funcional en vídeo de **2 minutos y 12 segundos**. La pauta completa es de **15 minutos y 7 segundos**. Los tiempos son orientativos y deben contrastarse en el ensayo.

Hay cuatro diapositivas de apoyo: trazabilidad de requisitos, concurrencia y seguridad, planificación y coste, y evolución de las pruebas. Están ocultas en el pase normal del PowerPoint y se pueden abrir desde la vista del moderador durante las preguntas. El PDF las conserva al final, tras las conclusiones.

El PowerPoint contiene texto, tablas y gráficos editables, notas de exposición y referencias por diapositiva. Los diagramas proceden de la memoria y son imágenes, al igual que las capturas reales. La presentación se puede exponer sin conexión a la aplicación. El PDF conserva el aspecto del pase a partir de las diapositivas renderizadas en Full HD y contiene marcadores de navegación.

## Fuentes y decisiones de contenido

La fuente principal es [memoria_tfg.pdf](../memoria/memoria_tfg.pdf), de 83 páginas. La ruta `tfg/memoria/memoria/_tfg.pdf` indicada en la petición no existe en este checkout. Las referencias de las notas y los pies emplean la **numeración impresa de la memoria**, no el número absoluto de página del archivo PDF.

La presentación se apoya también en `AGENTS.md`, `CONTEXT.md`, `README.md`, requisitos, documentación de diseño, despliegue y código del repositorio activo. Conserva los recuentos de pruebas documentados en la memoria como evidencia histórica de S7, sin afirmar una nueva ejecución de esas suites. Las cifras de esfuerzo y coste son estimaciones. Las capturas contienen datos de prueba y la URL del despliegue es temporal. La entrevista es simulada y no se atribuyen al proyecto resultados de productividad, estudios de usuarios ni pruebas de carga.

La defensa sigue seis secciones visibles y ordenadas: problema y objetivos, requisitos y alcance, metodología, diseño y arquitectura, validación y despliegue, y conclusiones y trabajo futuro. La demostración funcional se realiza con `DEMO.mp4` después de explicar el modelo y las reglas del dominio. La estimación económica y los detalles técnicos que interrumpen ese recorrido quedan en el anexo. Los documentos académicos y sus registros permanecen dentro de `tfg/`.

## Fuentes editables y regeneración

- `fuente/contenido.json`: guion, tiempos y referencias.
- `fuente/generar.mjs`: composición del PowerPoint mediante `@oai/artifact-tool`.
- `fuente/ocultar_apoyo.py`: indicadores nativos de diapositiva oculta y encuadres reversibles, sin modificar las imágenes originales.
- `fuente/exportar_pdf.py`: copia PDF de las diapositivas renderizadas.

La generación requiere el runtime de artefactos de Codex usado en esta sesión. `generar.mjs` admite `TRANSITOPS_RUNTIME` y `TRANSITOPS_PRESENTATION_SKILL` para resolver sus rutas. Para una revisión, establece `TRANSITOPS_PPTX_NAME` con un nombre nuevo: el finalizador conserva los archivos existentes. Las previsualizaciones, los borradores y los registros de validación permanecen en directorios locales ignorados por Git.
