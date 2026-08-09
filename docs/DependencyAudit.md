# Triaje de dependencias del frontend

## Auditoría del Sprint 7

Fecha: 2026-08-09.

Antes de aplicar correcciones, `npm audit` notificó cinco vulnerabilidades: cuatro de severidad alta y una moderada. La cifra es superior a los dos avisos anotados al planificar el sprint porque la base de avisos cambió entre ambos momentos.

| Cadena | Ámbito | Evaluación | Tratamiento |
| --- | --- | --- | --- |
| `react-router-dom` → `react-router` | Producción | Dos entradas del informe representan la misma vulnerabilidad de CSRF en el modo RSC. TransitOps usa rutas SPA y no usa acciones RSC, pero la dependencia forma parte de la imagen servida. | Actualización compatible a `react-router-dom` 7.18.2. |
| `vite` → `postcss` → `nanoid` | Desarrollo y compilación | No se incluye en la imagen final de Nginx. Afecta al procesamiento durante el build y a generadores personalizados que TransitOps no invoca directamente. | Actualización compatible de Vite y sus dependencias transitivas. |
| `jsdom` → `undici` | Pruebas | Solo se ejecuta en Vitest; no forma parte del artefacto desplegado. | Se mantiene JSDOM 29 y se actualiza su dependencia transitiva compatible, evitando elevar innecesariamente el requisito mínimo de Node. |

No se ejecuta una actualización mayor automática sin validar. Tras actualizar se repiten `npm audit`, lint, pruebas unitarias y build; Playwright se incorpora después como dependencia exclusivamente de desarrollo.
