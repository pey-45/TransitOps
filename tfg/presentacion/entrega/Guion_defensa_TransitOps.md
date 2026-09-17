# Guion de defensa de TransitOps

Duración objetivo: **16:50**. Diapositivas 1–18 para la exposición; 19–22 son apoyo y están ocultas en el PowerPoint. Los tiempos son una pauta de ensayo.

La defensa está organizada en siete secciones visibles. La diapositiva 2 presenta el recorrido y el rótulo superior identifica la sección actual en todo momento. Las capturas proceden de la memoria y usan datos de prueba o demostración.

| Diapositiva | Sección | Duración | Acumulado |
| --- | --- | ---: | ---: |
| 1. TransitOps | Portada | 0:25 | 0:25 |
| 2. Estructura de la presentación | Recorrido | 0:30 | 0:55 |
| 3. Problema de partida | 01 · Problema y objetivos | 0:50 | 1:45 |
| 4. Objetivos y criterios de éxito | 01 · Problema y objetivos | 0:55 | 2:40 |
| 5. Requisitos y alcance | 02 · Requisitos y alcance | 1:00 | 3:40 |
| 6. Casos de uso del sistema | 02 · Requisitos y alcance | 0:55 | 4:35 |
| 7. Metodología incremental | 03 · Metodología | 1:10 | 5:45 |
| 8. Arquitectura del sistema | 04 · Diseño y arquitectura | 1:05 | 6:50 |
| 9. Modelo y reglas del dominio | 04 · Diseño y arquitectura | 1:05 | 7:55 |
| 10. Flujo operativo completo | 05 · Demostración funcional | 1:00 | 8:55 |
| 11. Planificación y asignación | 05 · Demostración funcional | 1:05 | 10:00 |
| 12. Ejecución y trazabilidad | 05 · Demostración funcional | 1:05 | 11:05 |
| 13. Administración e indicadores | 05 · Demostración funcional | 0:50 | 11:55 |
| 14. Estrategia de validación | 06 · Validación y despliegue | 1:10 | 13:05 |
| 15. Despliegue reproducible | 06 · Validación y despliegue | 1:05 | 14:10 |
| 16. Evaluación del resultado | 07 · Conclusiones y trabajo futuro | 1:00 | 15:10 |
| 17. Límites y trabajo futuro | 07 · Conclusiones y trabajo futuro | 0:55 | 16:05 |
| 18. Conclusiones | 07 · Conclusiones y trabajo futuro | 0:45 | 16:50 |

## 1. TransitOps

**Portada**

Tiempo orientativo: 0:25. Tramo 0:00–0:25.

Buenos días. Soy Pablo Manzanares López y voy a presentar TransitOps, mi Trabajo Fin de Grado. El proyecto recorre el ciclo de vida completo de una aplicación de gestión de transportes, desde el análisis de necesidades hasta su validación y despliegue.

Fuente: Memoria, portada y capítulo 1, pp. 1–3.

## 2. Estructura de la presentación

**Recorrido**

Tiempo orientativo: 0:30. Tramo 0:25–0:55.

La exposición sigue el mismo razonamiento con el que se construyó el proyecto. Primero presentaré el problema, los objetivos y el alcance. Después explicaré la metodología y las decisiones de diseño. La parte central mostrará el producto mediante un flujo operativo. Terminaré con la validación, el despliegue y una evaluación crítica del resultado.

Fuente: Estructura de la defensa elaborada a partir de los capítulos 1–13 de la memoria.

## 3. Problema de partida

**01 · Problema y objetivos**

Tiempo orientativo: 0:50. Tramo 0:55–1:45.

El caso de partida es una pyme de transporte que coordina vehículos, conductores, clientes y envíos. Cuando la información queda repartida entre hojas de cálculo, mensajes y llamadas, responder qué recurso está disponible o qué ocurrió con una entrega exige reconstruir varias fuentes. Esto favorece dobles reservas, el uso de datos obsoletos y la pérdida de autoría de las incidencias. El caso de negocio se definió mediante una entrevista simulada, por lo que el trabajo no afirma mejoras medidas en una empresa real.

Fuente: Memoria, capítulos 1 y 4.1, pp. 1–3 y 15. docs/ClientRequirements.md.

## 4. Objetivos y criterios de éxito

**01 · Problema y objetivos**

Tiempo orientativo: 0:55. Tramo 1:45–2:40.

El objetivo principal fue construir una aplicación web mantenible para centralizar la operación diaria y demostrar el ciclo de vida completo que la produce. El resultado debía permitir gestionar datos maestros, planificar envíos, asignar recursos, controlar estados y consultar una cronología verificable. El proyecto se consideraría completo si cubría los cuatro casos de uso de extremo a extremo, mantenía trazabilidad entre requisitos y pruebas, podía ejecutarse de forma reproducible y quedaba accesible en un entorno desplegado.

Fuente: Memoria, apartados 1.2–1.3, pp. 2–3.

## 5. Requisitos y alcance

**02 · Requisitos y alcance**

Tiempo orientativo: 1:00. Tramo 2:40–3:40.

La especificación contiene catorce requisitos funcionales, diecisiete reglas de negocio y seis requisitos no funcionales. El operador gestiona catálogos, envíos, asignaciones, estados y eventos. El administrador añade la gestión de usuarios. Conductores y clientes son entidades del dominio, no usuarios del sistema. El alcance se limita al núcleo operativo. La optimización de rutas, el GPS, la facturación y el acceso de clientes externos quedan fuera para reservar esfuerzo a la calidad, la reproducibilidad y la evaluación del producto.

Fuente: Memoria, capítulo 4, pp. 15–21. docs/Requirements.md.

## 6. Casos de uso del sistema

**02 · Requisitos y alcance**

Tiempo orientativo: 0:55. Tramo 3:40–4:35.

Cuatro casos de uso organizan el alcance. El primero cubre el acceso y la sesión. El segundo mantiene vehículos, conductores y clientes. El tercero recorre la operación del envío, desde su creación y asignación hasta la entrega o cancelación, con eventos durante el trayecto. El cuarto reúne administración e indicadores. Esta división convierte una lista extensa de requisitos en recorridos demostrables y proporciona la base de las pruebas de sistema.

Fuente: Memoria, apartado 4.4, pp. 17–21. docs/Requirements.md.

## 7. Metodología incremental

**03 · Metodología**

Tiempo orientativo: 1:10. Tramo 4:35–5:45.

El desarrollo se organizó en incrementos verticales. Cada sprint añadió una capacidad completa, con datos, servicio, API, interfaz y pruebas. El primer sprint dejó un esqueleto autenticado que atravesaba todas las capas. Los siguientes añadieron catálogos, envíos, operación, historial y administración. El séptimo consolidó concurrencia, seguridad, pruebas de sistema y despliegue. El modelo conceptual se diseñó al principio para evitar contradicciones, mientras el esquema físico evolucionó mediante migraciones. Cada incremento se cerró solo cuando el flujo funcionaba integrado y sus comprobaciones quedaban documentadas.

Fuente: Memoria, capítulo 3 y tabla 3.1, pp. 9–14. docs/Roadmap.md.

## 8. Arquitectura del sistema

**04 · Diseño y arquitectura**

Tiempo orientativo: 1:05. Tramo 5:45–6:50.

La solución separa una SPA React con TypeScript, una API ASP.NET Core sobre .NET 10 y una base PostgreSQL mediante Entity Framework Core. Nginx sirve la interfaz y reenvía las peticiones bajo el mismo origen. La API concentra la autorización, la validación y las reglas de negocio. El backend se organiza por funcionalidades y los controladores delegan en servicios, lo que permite probar las reglas sin levantar todo el servidor. Una API modular única aporta separación suficiente para este alcance sin introducir coordinación distribuida.

Fuente: Memoria, capítulo 5 y figura 5.1, pp. 22–24. docs/design/IntegrationArchitecture.md.

## 9. Modelo y reglas del dominio

**04 · Diseño y arquitectura**

Tiempo orientativo: 1:05. Tramo 6:50–7:55.

El envío es el centro del modelo. Se vincula con un cliente y puede recibir una pareja de vehículo y conductor. Los eventos pertenecen al envío y conservan la identidad del usuario que actúa. Los catálogos y usuarios emplean baja lógica para preservar referencias históricas. El ciclo de estados se expresa mediante acciones explícitas y cerradas: un envío nace planificado, puede iniciarse si tiene recursos y termina entregado o cancelado. PostgreSQL refuerza las invariantes que deben mantenerse incluso cuando llegan peticiones simultáneas.

Fuente: Memoria, apartados 5.2 y 6.4–6.7, pp. 23 y 27–32. Figura 5.2.

## 10. Flujo operativo completo

**05 · Demostración funcional**

Tiempo orientativo: 1:00. Tramo 7:55–8:55.

La demostración funcional sigue un único envío. Primero se preparan los catálogos y se crea el envío. Después se asignan conjuntamente un vehículo y un conductor. El operador inicia el trayecto, registra puntos de control o incidencias y termina la operación con la entrega o la cancelación. El resumen refleja el resultado. Este recorrido es útil porque conecta la mayor parte del dominio y permite observar las reglas en la interfaz sin separar artificialmente frontend y backend.

Fuente: Memoria, capítulos 6–7 y casos de uso 2–3, pp. 18–20 y 27–46.

## 11. Planificación y asignación

**05 · Demostración funcional**

Tiempo orientativo: 1:05. Tramo 8:55–10:00.

El envío se crea en estado planificado y la asignación recibe siempre el vehículo y el conductor juntos. La aplicación impide utilizar recursos inactivos o ya ocupados en otro envío abierto. La capacidad insuficiente se trata de forma distinta: la asignación se confirma y el operador recibe una advertencia. En el ejemplo, la carga estimada es de cuatro mil quinientos kilos y el vehículo registra una capacidad de tres mil. La regla de negocio pedía informar, no bloquear.

Fuente: Memoria, apartado 6.6 y figura 7.5, pp. 29–30 y 39. Datos de prueba del Sprint 4.

## 12. Ejecución y trazabilidad

**05 · Demostración funcional**

Tiempo orientativo: 1:05. Tramo 10:00–11:05.

Durante el trayecto, el sistema combina eventos automáticos con puntos de control e incidencias introducidos por el operador. El historial solo permite añadir y consultar. Distingue cuándo ocurrió un hecho de cuándo se registró y obtiene el autor de la identidad autenticada. Los cambios de estado y su evento automático se confirman en la misma transacción, de forma que el estado actual y la explicación de cómo se alcanzó no puedan divergir.

Fuente: Memoria, apartado 6.7 y figura 7.8, pp. 30–32 y 42. Datos de prueba del Sprint 5.

## 13. Administración e indicadores

**05 · Demostración funcional**

Tiempo orientativo: 0:50. Tramo 11:05–11:55.

El administrador puede crear cuentas, cambiar roles, activar o desactivar usuarios y reasignar contraseñas. La API protege que siempre permanezca al menos un administrador activo. El panel resume los envíos por estado, la actividad de vehículos y conductores y las incidencias del periodo seleccionado. Los indicadores sirven para orientar la operación y enlazan con los listados filtrados. La captura usa datos de demostración y no representa actividad empresarial real.

Fuente: Memoria, apartados 6.8 y 7.6, pp. 32–33 y 44–46. Datos de demostración del Sprint 6.

## 14. Estrategia de validación

**06 · Validación y despliegue**

Tiempo orientativo: 1:10. Tramo 11:55–13:05.

La validación se reparte según el tipo de riesgo. Ciento treinta y nueve pruebas de backend comprueban reglas, contratos HTTP, persistencia y concurrencia. Treinta y tres pruebas de frontend verifican los flujos de interacción. Cuatro pruebas de sistema con Playwright recorren los casos de uso sobre la composición completa. Las restricciones de concurrencia se prueban contra PostgreSQL real, y la integración continua repite compilación, suites y migraciones sobre un entorno limpio. Los recuentos indican casos ejecutados, no porcentaje de cobertura.

Fuente: Memoria, capítulo 8, pp. 48–55. Recuentos documentados al cierre del Sprint 7.

## 15. Despliegue reproducible

**06 · Validación y despliegue**

Tiempo orientativo: 1:05. Tramo 13:05–14:10.

Tras superar la validación, GitHub Actions publica las imágenes de la API y la web en GHCR. Una máquina Ubuntu descarga esas imágenes y aplica la misma composición de contenedores descrita en la documentación. Un script comprueba la configuración, la salud, la revisión y los digests desplegados. Systemd ejecuta periódicamente ese procedimiento. El acceso público usa un túnel saliente de Cloudflare, de modo que ningún puerto de los contenedores queda publicado en el host. Es un entorno de demostración reproducible, no un servicio en producción.

Fuente: Memoria, capítulo 9, pp. 56–60. docs/Deployment.md.

## 16. Evaluación del resultado

**07 · Conclusiones y trabajo futuro**

Tiempo orientativo: 1:00. Tramo 14:10–15:10.

El producto cubre RF-01 a RF-14 y los cuatro casos de uso completos. La trazabilidad relaciona cada necesidad con su diseño, su implementación y su evidencia. La arquitectura demostró que podía crecer por incrementos sin abandonar la integración de extremo a extremo. El despliegue verificó además que los procedimientos podían reproducirse fuera de la máquina de desarrollo. El resultado cumple los criterios técnicos definidos al inicio, aunque no permite afirmar impacto empresarial porque no se evaluó con usuarios reales.

Fuente: Memoria, capítulos 10, 12 y 13, pp. 61–62 y 66–69.

## 17. Límites y trabajo futuro

**07 · Conclusiones y trabajo futuro**

Tiempo orientativo: 0:55. Tramo 15:10–16:05.

La evaluación tiene límites claros. El caso de negocio es simulado, no hubo pruebas con usuarios ni pruebas de carga, y el despliegue carece de alojamiento estable, copias y métricas. Además, las imágenes publicadas se construyen de nuevo después de las pruebas de sistema, aunque parten del mismo commit. La continuación prioritaria sería validar el núcleo con usuarios y estabilizar la operación. Solo después tendría sentido ampliar el producto con rutas, GPS, facturación o acceso externo.

Fuente: Memoria, apartados 9.5, 12.2 y capítulo 13, pp. 60 y 66–69.

## 18. Conclusiones

**07 · Conclusiones y trabajo futuro**

Tiempo orientativo: 0:45. Tramo 16:05–16:50.

TransitOps entrega un núcleo operativo completo y demostrable. El proyecto vincula las necesidades iniciales con decisiones de diseño, reglas de negocio, pruebas y un despliegue reproducible. La principal aportación del trabajo está en esa continuidad: cada incremento produjo una capacidad utilizable y dejó evidencia suficiente para evaluar el resultado. Muchas gracias. Quedo a vuestra disposición para las preguntas.

Fuente: Memoria, capítulo 13, pp. 68–69.

## 19. Trazabilidad de requisitos

**Anexo**

Apoyo para preguntas. Fuera del tiempo principal.

Diapositiva de apoyo para preguntas sobre la relación entre requisitos, incrementos y evidencias.

Fuente: Memoria, capítulo 10, pp. 61–62. docs/Requirements.md y docs/Roadmap.md.

## 20. Concurrencia y seguridad

**Anexo**

Apoyo para preguntas. Fuera del tiempo principal.

Diapositiva de apoyo para explicar las garantías de doble reserva, último administrador y revocación de sesiones.

Fuente: Memoria, apartados 6.1–6.3, 6.6, 6.8 y 8.3, pp. 25–33 y 52–54.

## 21. Planificación y coste

**Anexo**

Apoyo para preguntas. Fuera del tiempo principal.

Diapositiva de apoyo. Las 300 horas y los 6.137,50 euros son estimaciones académicas, no registros reales de dedicación o gasto.

Fuente: Memoria, apartados 3.3–3.4 y tablas 3.2–3.3, pp. 11–13.

## 22. Evolución de las pruebas

**Anexo**

Apoyo para preguntas. Fuera del tiempo principal.

Diapositiva de apoyo con los recuentos acumulados de pruebas al cierre de cada sprint. No representan porcentaje de cobertura.

Fuente: Memoria, apartado 8.2, pp. 49–52.
