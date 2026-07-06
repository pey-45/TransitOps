# Requisitos del Cliente · Entrevista Inicial

## Nota metodológica

Este TFG no tiene un cliente externo real. Para poder aplicar y documentar la fase de análisis de requisitos del ciclo de vida del software, este documento recoge una entrevista de recogida de requisitos con una clienta ficticia, una técnica habitual y aceptada en este tipo de proyecto académico.

- **Cliente (persona ficticia):** Marta Souto, responsable de operaciones en una empresa de transporte de mercancías por carretera, tamaño pequeño-mediano (una veintena de vehículos y conductores).
- **Siguiente paso:** a partir de este documento se derivan los requisitos funcionales formales (sin tecnicismos) y, después, la planificación en sprints.

No se menciona en este documento nada sobre cómo está construida la aplicación (tecnologías, arquitectura, etc.): esa información no la tendría un cliente real y no debe condicionar la entrevista.

---

## Contexto

Marta lleva la operación diaria de una empresa de transporte de mercancías. Hoy en día gestionan los envíos con una combinación de hojas de cálculo, whatsapp y llamadas de teléfono. A medida que ha crecido la flota, esto les está generando errores: vehículos asignados dos veces, conductores sin saber qué les toca hacer, y ningún sitio único donde ver "qué ha pasado" con un envío concreto si surge un problema o un cliente pregunta.

Buscan una aplicación sencilla, propia, donde todo el equipo trabaje sobre los mismos datos en tiempo real.

## Entrevista

### Sobre el negocio y el problema actual

**P: Cuéntame un poco sobre cómo gestionáis el trabajo hoy en día.**
R: Tenemos una hoja de Excel compartida con los envíos de la semana, y luego coordinamos por whatsapp quién lleva cada furgoneta y quién conduce. Cuando algo cambia sobre la marcha, alguien tiene que acordarse de avisar a los demás y actualizar la hoja.

**P: ¿Qué problemas os da esa forma de trabajar?**
R: Sobre todo tres cosas: se nos duplica alguna asignación de vehículo o conductor porque dos personas tocan la hoja a la vez; no queda constancia clara de qué ha pasado con un envío si hay una incidencia (¿cuándo salió?, ¿hubo algún problema por el camino?, ¿quién lo entregó?); y no hay forma de saber rápido qué transportes están en marcha ahora mismo sin preguntar a la gente.

### Sobre los vehículos

**P: ¿Qué necesitáis saber o controlar sobre los vehículos?**
R: Necesitamos tener la lista de vehículos que tenemos, con su matrícula, y poder distinguirlos fácilmente (algunos usamos un código interno además de la matrícula, tipo "Furgoneta 3"). También nos interesa saber marca/modelo y, si se puede, la capacidad de carga, para no asignar un vehículo pequeño a un envío grande. Ninguno de estos datos extra es imprescindible el primer día, pero como no deberían dar mucho trabajo, que se incluyan desde el principio. Cuando un vehículo se vende o se da de baja, no queremos verlo ya en las listas del día a día, pero tampoco queremos "perder" el historial de lo que hizo antes.

### Sobre los conductores

**P: ¿Y de los conductores?**
R: Igual que los vehículos: lista de conductores, su número de carné, algún dato de contacto y, si se puede, un código de empleado; tampoco cuesta mucho tenerlo desde el principio. También necesitamos poder marcar a alguien como de baja (vacaciones largas, baja médica, o que ya no trabaja con nosotros) sin borrar su historial de envíos ya hechos.

**P: ¿Los conductores necesitan entrar en la aplicación?**
R: No, y de momento que quede así: ellos reciben la información por teléfono o whatsapp como hasta ahora. La aplicación es para el personal de oficina/operaciones. Si en el futuro nos interesa darles acceso, ya lo pediremos como algo aparte; por ahora que se quede completamente fuera.

### Sobre los transportes/envíos (el trabajo del día a día)

**P: Cuando entra un envío nuevo, ¿qué información manejáis?**
R: Un envío tiene un origen y un destino, una fecha en la que está previsto recogerlo y, normalmente, una fecha en la que debería entregarse. A veces añadimos alguna nota o descripción (por ejemplo, "mercancía frágil" o el número de referencia que nos da el cliente).

**P: ¿Cómo pasa un envío de "pendiente" a "hecho"?**
R: Primero está "planificado" (lo tenemos apuntado, pero sin salir todavía). Cuando le asignamos un vehículo y un conductor, y sale, pasa a "en curso". Cuando llega a destino, lo marcamos como "entregado". Y si por lo que sea no se llega a hacer, "cancelado". Una vez entregado o cancelado, ya no se puede reabrir ni cambiar de estado: si hay que rehacerlo, se crea un envío nuevo.

**P: ¿Se puede asignar un vehículo o conductor a un envío que ya está en marcha?**
R: No debería hacerse; la asignación se decide antes de que salga, mientras está "planificado". Si hace falta cambiar el vehículo o el conductor, que sea también antes de que salga.

### Sobre el seguimiento / historial

**P: Decías que no queda constancia de las incidencias. ¿Qué te gustaría poder registrar?**
R: Me gustaría que, para cada envío, quedara un historial con fecha de cada cosa relevante que le pasa: que se creó, que se le asignó vehículo/conductor, que salió, algún punto de control por el camino si aplica, si hubo una incidencia, y cuándo se entregó o se canceló. Así, si un cliente llama preguntando, cualquiera del equipo puede mirar el historial y responder sin tener que localizar a la persona que lo llevó.

### Sobre estadísticas e informes

**P: ¿Te serviría de algo ver algún tipo de resumen o estadística del negocio?**
R: Sí, esto sí me interesa como algo real, no solo como capricho. Me gustaría un resumen donde ver de un vistazo cuántos envíos hay ahora mismo en cada estado (planificados, en curso, entregados, cancelados), cuántos envíos ha hecho cada vehículo y cada conductor en un periodo, y cuántas incidencias se han registrado. No hace falta que sea muy elaborado ni con gráficos complejos: con verlo de un vistazo al entrar ya nos ahorra tener que contar a mano en la hoja de cálculo.

### Sobre quién usará la aplicación

**P: ¿Quién va a usar esto en tu empresa?**
R: Por un lado, el personal de operaciones (turno de oficina), que es quien crea los envíos, asigna vehículos y conductores, y va actualizando el estado. Por otro lado, yo y una persona más llevamos la parte de administración: además de todo lo anterior, necesitamos poder dar de alta a la gente que puede entrar en la aplicación y decidir quién tiene permisos de "solo operación" y quién tiene permisos de "administración". No queremos que cualquiera pueda dar de alta usuarios nuevos.

### Sobre cómo quieren usarla

**P: ¿Cómo os imagináis usando la aplicación en el día a día?**
R: Algo sencillo, desde el navegador, sin instalar nada especial, a lo que se pueda entrar con un usuario y contraseña. Que se vea claro qué envíos hay, en qué estado están, y poder filtrar (por ejemplo, ver solo los que están "en curso", o los de una fecha). No hace falta que sea vistoso ni sofisticado, que sea claro y que no se rompa.

### Sobre lo que no necesitáis ahora

**P: ¿Hay cosas que no os hacen falta, al menos de momento?**
R: No necesitamos que la aplicación calcule rutas óptimas ni tiempos de viaje, ni que hable con GPS de los vehículos. Tampoco necesitamos facturación ni presupuestos: eso lo llevamos aparte con la gestoría. No necesitamos una app de móvil, con que funcione bien desde el navegador del ordenador nos vale. Y no necesitamos que los propios clientes finales entren a consultar nada; de cara al cliente externo seguimos respondiendo nosotros por teléfono.

### Prioridades

**P: Si tuvieras que elegir qué es imprescindible desde el primer día y qué puede esperar, ¿qué dirías?**
R: Lo imprescindible es: poder entrar con usuario/contraseña, tener la lista de vehículos y conductores, poder crear y ver los envíos, asignar vehículo/conductor, y cambiar el estado según va avanzando el envío. El historial de incidencias/eventos es muy importante también, casi tan imprescindible como lo anterior. El resumen de estadísticas también lo quiero de verdad, aunque puede llegar un poco después de tener lo básico funcionando. Lo de dar de alta usuarios y permisos de administración puede llegar también algo después, mientras alguien nos ayude a crear el primer usuario administrador para empezar.

## Resumen de necesidades (sin tecnicismos)

- Una aplicación web con usuario y contraseña para el personal de operaciones y administración (los conductores no la usan, y por ahora se quedan completamente fuera de la aplicación).
- Dos tipos de acceso: operación (trabajo diario) y administración (además, gestionar quién tiene acceso).
- Gestión de vehículos: alta, listado, ver detalle, editar datos (incluyendo marca/modelo/capacidad como datos adicionales), y retirarlos sin perder su historial.
- Gestión de conductores: alta, listado, ver detalle, editar datos (incluyendo código de empleado/contacto como datos adicionales), y darlos de baja sin perder su historial.
- Gestión de envíos/transportes: crear, listar (con filtros), ver detalle, editar datos generales.
- Asignar vehículo y conductor a un envío, solo mientras está planificado.
- Cambiar el estado de un envío siguiendo un orden lógico (planificado → en curso → entregado/cancelado), sin poder retroceder desde un estado final.
- Historial de eventos por envío: creación, asignación, salida, puntos de control, incidencias, entrega o cancelación, cada uno con su fecha.
- Un resumen/estadísticas real: envíos por estado, actividad por vehículo y por conductor, e incidencias registradas.
- Ver rápidamente qué envíos están en curso, filtrando por estado, fechas, vehículo o conductor.
- Nada de rutas óptimas, GPS, facturación, app de móvil, ni acceso para clientes externos, al menos por ahora.
- Prioridad alta: login, vehículos, conductores, envíos, asignación, estados, historial. Prioridad algo menor: estadísticas, alta de usuarios/permisos.
