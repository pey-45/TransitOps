# Guion resumido de la defensa de TransitOps

**Autor:** Pablo Manzanares López  
**Duración orientativa:** 15 minutos y 7 segundos  
**Estructura:** 13 diapositivas principales + demo funcional de 2:12

> Guion de apoyo para la exposición oral. Las frases son ideas clave; no es necesario leerlas literalmente.

---

## Diapositiva 1 · TransitOps

- Buenos días. Soy Pablo Manzanares López y vengo a presentar TransitOps, mi Trabajo Fin de Grado.
- Es una aplicación para organizar el trabajo diario de una empresa de transporte.
- Además de enseñar qué hace, explicaré cómo pasé de una necesidad inicial a un sistema funcional y desplegado.

---

## Diapositiva 2 · Estructura de la presentación

- Primero explicaré el problema y el alcance que decidí darle al proyecto.
- Después presentaré la metodología, el diseño y la arquitectura.
- A continuación veremos en vídeo un envío de principio a fin.
- Para terminar, explicaré cómo validé y desplegué la aplicación y qué límites tiene todavía.

---

## Diapositiva 3 · Problema de partida

- El problema parte de una pequeña empresa de transporte que utiliza métodos de gestión manuales: los vehículos se controlan en una hoja de cálculo, los conductores se confirman por teléfono y las incidencias llegan por mensajes.
- Al preparar un envío, cuesta saber si un recurso está realmente libre.
- Cuando surge un problema durante el trayecto, también cuesta reconstruir qué ocurrió y quién lo registró.
- TransitOps reúne toda esa información en un mismo lugar.
- La empresa y la entrevista de partida son simuladas.

---

## Diapositiva 4 · Objetivos y criterios de éxito

- Mis objetivos son construir una aplicación útil para esas tareas diarias y mostrar el proceso completo de ingeniería que hay detrás.
- La aplicación permite preparar los datos de clientes y recursos, crear envíos, asignar vehículo y conductor, seguir su estado y consultar posteriormente lo ocurrido.
- Definí cuatro criterios de éxito:
  1. Que los casos de uso funcionaran de principio a fin.
  2. Que las reglas importantes tuvieran pruebas.
  3. Que otra persona pudiera poner el sistema en marcha siguiendo la documentación.
  4. Que existiera trazabilidad entre necesidades, decisiones y pruebas.

---

## Diapositiva 5 · Requisitos y alcance

- A partir de la entrevista redacté catorce funciones, diecisiete reglas de negocio y seis requisitos de calidad.
- La aplicación tiene dos roles:
  - **Operador:** gestiona vehículos, conductores, clientes y envíos.
  - **Administrador:** puede realizar el trabajo operativo y, además, gestionar cuentas y permisos.
- Los conductores y clientes son datos del dominio; no acceden a la aplicación con una cuenta propia.

---

## Diapositiva 6 · Casos de uso definidos en la memoria

- De los requisitos se extrajeron cuatro casos de uso:
  1. Crear la primera cuenta de administrador al poner en marcha el sistema.
  2. Crear una cuenta y permitir que la nueva persona acceda y cambie su contraseña.
  3. Llevar un envío desde su planificación hasta su cierre.
  4. Retirar el acceso a una persona sin borrar su actividad anterior.

---

## Diapositiva 7 · Metodología incremental

- Organicé el desarrollo en siete etapas.
- Cada etapa dejó una parte utilizable, con su interfaz, lógica de servidor, datos y pruebas.
- Empecé por el acceso al sistema.
- Después incorporé los catálogos, los envíos y los filtros.
- Más adelante añadí las asignaciones, el seguimiento y la administración.
- La última etapa reforzó la seguridad, comprobó el sistema completo y preparó el despliegue.
- Al cerrar cada etapa comprobé el recorrido completo, no solo una parte aislada.

---

## Diapositiva 8 · Arquitectura del sistema

- La imagen muestra el recorrido de una acción del usuario.
- La interfaz web está desarrollada con React y TypeScript.
- La interfaz se comunica con el servidor, desarrollado en .NET.
- El servidor comprueba los permisos y las reglas de negocio antes de guardar los cambios en PostgreSQL.
- Elegí un monolito en lugar de microservicios porque, para este tamaño de proyecto, permite separar responsabilidades y probar el sistema con menor complejidad.

---

## Diapositiva 9 · Modelo y reglas del dominio

- El envío es la pieza central del modelo.
- Puede estar vinculado a un cliente y recibe un vehículo y un conductor cuando se prepara para salir.
- Todo lo que sucede después queda asociado al envío.
- Para conservar la trazabilidad, los recursos se desactivan en lugar de borrarse y los eventos del trayecto no se editan ni se eliminan.
- Las reglas más delicadas también están reforzadas en la base de datos. Por ejemplo, dos envíos abiertos no pueden reservar el mismo recurso, incluso si llegan dos peticiones casi simultáneas.

---

# Demo funcional · Vídeo (2:12)

> **Reproducir `DEMO.mp4`.** Comentar en directo la creación y operación de un envío, la asignación de recursos, el aviso de capacidad, la incidencia y la entrega.

---

## Diapositiva 10 · Estrategia de validación

- Para comprobar el trabajo utilicé pruebas a distintos niveles:
  - 139 pruebas de backend.
  - 33 pruebas de frontend.
  - 4 pruebas de sistema, una por cada caso de uso.
- Las reglas expuestas a acciones simultáneas también se probaron con PostgreSQL real para verificar la concurrencia.

---

## Diapositiva 11 · Despliegue reproducible

- Cuando el proyecto supera la integración continua, se preparan imágenes listas para ejecutar de la web y del servidor.
- Una máquina Ubuntu descarga esas imágenes y pone en marcha la aplicación con la configuración documentada.
- Un procedimiento automático comprueba que el sistema responde y qué versión está instalada.
- Para la demostración, el acceso se realiza por HTTPS mediante un túnel saliente, sin publicar directamente los puertos de los contenedores.

---

## Diapositiva 12 · Evaluación del resultado

- El resultado cubre las catorce funciones previstas y los cuatro casos de uso.
- La calidad se comprueba mediante pruebas por capas y reglas de concurrencia.
- La ejecución es reproducible mediante Compose y un proceso de integración y despliegue documentado.
- No puedo afirmar que la aplicación ahorre tiempo o dinero a una empresa porque no se probó con usuarios reales.

---

## Diapositiva 13 · Conclusiones

- TransitOps demuestra un recorrido completo: partir de una necesidad, convertirla en requisitos, construir una aplicación funcional, ponerla a prueba y desplegarla.
- El valor del trabajo reside tanto en las funciones desarrolladas como en poder explicar y verificar las decisiones tomadas.

**Muchas gracias por vuestra atención. Quedo a vuestra disposición para las preguntas.**
