DIAPOSITIVA 1 · TransitOps

Buenos días. Soy Pablo Manzanares López y vengo a presentar TransitOps, mi Trabajo Fin de Grado. Es una aplicación para organizar el trabajo diario de una empresa de transporte.

Además de enseñar lo que hace, quiero contaros cómo llegué hasta aquí. Es decir, cómo pasé de una necesidad inicial a un sistema que se puede probar y poner en marcha.


DIAPOSITIVA 2 · Estructura de la presentación

El recorrido va a ser sencillo. Primero explicaré el problema y qué decidí incluir en el proyecto. Después contaré cómo lo fui construyendo y por qué elegí esta arquitectura.

En la parte central veremos un envío de principio a fin. Y para terminar, enseñaré cómo comprobé el resultado, cómo lo desplegué y qué límites tiene todavía.


DIAPOSITIVA 3 · Problema de partida

Imaginemos una pequeña empresa de transporte. Una persona lleva los vehículos en una hoja de cálculo. Otra confirma conductores por teléfono. Y las incidencias llegan por mensajes.

Cuando hay que preparar un envío, cuesta saber si un recurso está libre de verdad. Y si surge un problema durante el trayecto, cuesta reconstruir qué pasó y quién lo registró.

TransitOps nace para reunir toda esa información en un mismo lugar. Conviene que aclare algo desde el principio: la empresa y la entrevista de partida son simuladas. El proyecto demuestra una solución técnica; no he medido mejoras en una empresa real.


DIAPOSITIVA 4 · Objetivos y criterios de éxito

Mi objetivo era doble. Por un lado, construir una aplicación útil para esas tareas diarias. Por otro, mostrar el proceso de ingeniería completo que hay detrás.

La aplicación debía permitir preparar los datos de clientes y recursos, crear envíos, asignar vehículo y conductor, seguir su estado y consultar después lo ocurrido.

Para saber si el trabajo estaba terminado, fijé cuatro señales concretas. Una: que los casos de uso funcionaran de principio a fin. Dos: que las reglas importantes tuvieran pruebas. Tres: que otra persona pudiera poner el sistema en marcha siguiendo la documentación. Y cuatro: que existiera un despliegue accesible. Así puedo evaluar el resultado con algo más que una lista de pantallas.


DIAPOSITIVA 5 · Requisitos y alcance

A partir de la entrevista redacté catorce funciones que debía ofrecer la aplicación, diecisiete reglas sobre cómo debe comportarse y seis requisitos de calidad.

Aquí me interesa aclarar sobre todo quién la usa. El operador se ocupa del trabajo diario: vehículos, conductores, clientes y envíos. El administrador también puede hacer ese trabajo, pero además gestiona las cuentas y sus permisos. Los conductores y los clientes aparecen como datos en la aplicación; no entran en ella con una cuenta propia.

Acoté el proyecto al núcleo de la operación. Por eso no incluí optimización de rutas, seguimiento por GPS ni facturación. Eran ampliaciones posibles, pero no hacían falta para demostrar el recorrido completo con calidad.


DIAPOSITIVA 6 · Casos de uso definidos en la memoria

La memoria concreta ese alcance en cuatro situaciones. La primera ocurre una sola vez: quien instala el sistema crea la primera cuenta de administrador. Después, esa vía de entrada ya no se puede repetir.

En la segunda, el administrador crea una cuenta y, más tarde, el operador que la recibe entra y cambia su contraseña inicial. Son dos roles distintos que intervienen en momentos diferentes.

La tercera es el trabajo principal: el operador lleva un envío desde su planificación hasta su cierre. Y la cuarta vuelve al administrador, que puede retirar el acceso a una persona sin borrar lo que hizo antes. Estos son los cuatro casos de uso que luego compruebo en las pruebas de sistema.


DIAPOSITIVA 7 · Metodología incremental

Organicé el desarrollo en siete etapas. En cada una intenté dejar una parte que ya se pudiera usar, con su pantalla, su lógica en el servidor, sus datos y sus pruebas.

Empecé por el acceso al sistema. Después llegaron las fichas de vehículos, conductores y clientes, seguidas de los envíos. Más adelante añadí las asignaciones, el seguimiento y la administración. La última etapa se dedicó a reforzar la seguridad, comprobar el sistema completo y desplegarlo.

Detrás de esta forma de trabajar hay una decisión importante. Diseñé desde el principio cómo se relacionaban las piezas principales, para evitar contradicciones. Pero fui creando las tablas y las funciones a medida que cada etapa las necesitaba. Y al cerrar una etapa, comprobaba el recorrido completo, no solo una parte aislada.


DIAPOSITIVA 8 · Arquitectura del sistema

Esta imagen muestra el camino que sigue una acción del usuario. La persona utiliza la interfaz web, hecha con React y TypeScript. Cuando, por ejemplo, crea un envío, la interfaz se comunica con el servidor, desarrollado en punto NET. Ahí se comprueba si tiene permiso y si la operación respeta las reglas. Solo entonces se guarda el cambio en PostgreSQL.

Nginx entrega la interfaz y dirige esas peticiones al servidor desde una misma dirección.

Elegí una única aplicación de servidor, organizada por funciones. Para este tamaño de proyecto me permitió separar responsabilidades y probarlas con claridad, sin añadir la complejidad de varios servicios que tendrían que coordinarse entre sí.


DIAPOSITIVA 9 · Modelo y reglas del dominio

El envío es la pieza central del modelo. Puede estar vinculado a un cliente y, cuando se prepara para salir, recibe un vehículo y un conductor como pareja. Todo lo que ocurre después queda asociado a ese envío.

También necesitaba conservar la historia. Si un vehículo o una persona deja de estar disponible, desactivo su registro en vez de borrarlo, porque podría aparecer en envíos anteriores. Los hechos del trayecto tampoco se editan ni se eliminan.

Y las reglas más delicadas están reforzadas en la base de datos. Por ejemplo, impedir que dos envíos abiertos reserven el mismo recurso. Así se mantienen incluso si llegan dos peticiones casi a la vez.


DEMO FUNCIONAL · VÍDEO (2:12)

Reproducir DEMO.mp4 y comentar en directo la creación y operación de un envío, la asignación de recursos, el aviso de capacidad, la incidencia y la entrega.


DIAPOSITIVA 10 · Estrategia de validación

Para comprobar el trabajo usé pruebas a distintos niveles. Hay ciento treinta y nueve pruebas del servidor, centradas en las reglas, los permisos y la forma en que responde la aplicación. Otras treinta y tres comprueban las acciones que una persona realiza desde la interfaz. Y hay cuatro pruebas que recorren el sistema completo, una por cada caso de uso que vimos antes.

En las reglas donde dos personas podrían actuar al mismo tiempo, probé además con PostgreSQL real. Una simulación sencilla no bastaba para comprobar ese riesgo. Cada cambio pasa también por una comprobación automática antes de publicarse.

Un matiz importante: estos números son pruebas ejecutadas. No son un porcentaje de cobertura ni una medida de calidad por sí solos.


DIAPOSITIVA 11 · Despliegue reproducible

Quería que el resultado pudiera arrancarse fuera de mi ordenador. Cuando el proyecto supera las comprobaciones automáticas, se preparan versiones listas para ejecutar de la web y del servidor. Una máquina Ubuntu las descarga y pone en marcha la aplicación con la configuración descrita en el repositorio. Y un procedimiento automático comprueba después que responde y qué versión está instalada.

Para la demostración, el acceso se hace por HTTPS, mediante un túnel saliente. Eso permite abrir la aplicación desde fuera sin publicar directamente los puertos de los contenedores. Es una forma reproducible de enseñar el sistema, pero no la presento como un servicio listo para producción.


DIAPOSITIVA 12 · Evaluación del resultado

Si vuelvo a los criterios del principio, el resultado cubre las catorce funciones previstas y los cuatro casos de uso. También puedo seguir el rastro desde una necesidad hasta la parte de la aplicación que la resuelve y la prueba que la comprueba. Las distintas etapas terminaron integradas en una sola aplicación. Y el despliegue mostró que podía ponerla en marcha fuera del entorno de desarrollo.

Esta evaluación tiene un límite importante. Puedo afirmar que el sistema funciona según los requisitos y las pruebas realizadas. No puedo afirmar que ahorre tiempo o dinero a una empresa, porque no se probó con usuarios reales.


DIAPOSITIVA 13 · Conclusiones

Para terminar. TransitOps demuestra un recorrido completo: partir de una necesidad, convertirla en requisitos, construir una aplicación funcional, ponerla a prueba y desplegarla. El valor del trabajo está tanto en las funciones del sistema como en poder explicar y verificar las decisiones que las hicieron posibles.

El producto todavía necesita validación con personas reales, pero el núcleo operativo está construido y documentado.

Muchas gracias por vuestra atención. Quedo a vuestra disposición para las preguntas.
