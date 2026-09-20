import fs from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { execFileSync } from 'node:child_process';
import { Presentation, PresentationFile } from '@oai/artifact-tool';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const repo = path.resolve(root, '../..');
const build = path.join(root, '.build');
const out = path.join(root, 'entrega');
const runtime = process.env.TRANSITOPS_RUNTIME || 'C:/Users/pey/.cache/codex-runtimes/codex-primary-runtime/dependencies';
process.env.RUNTIME_NODE_MODULES = path.join(runtime, 'node/node_modules');
const skill = process.env.TRANSITOPS_PRESENTATION_SKILL || 'C:/Users/pey/.codex/plugins/cache/openai-primary-runtime/presentations/26.909.12148/skills/presentations';
const { finalizePresentation } = await import(pathToFileURL(path.join(skill, 'container_tools/artifact_tool_utils.mjs')));

await fs.mkdir(build, { recursive: true });
await fs.mkdir(out, { recursive: true });
await fs.mkdir(path.join(root, '.codex-finalizer'), { recursive: true });

const content = JSON.parse(await fs.readFile(path.join(root, 'fuente/contenido.json'), 'utf8'));
const P = Presentation.create({ slideSize: { width: 1280, height: 720 } });
const C = {
  ink: '#14263D',
  muted: '#536174',
  accent: '#C0007F',
  blue: '#2156A5',
  teal: '#177668',
  paper: '#FFFFFF',
  pale: '#F1F4F8',
  line: '#D9DFE7',
  green: '#2E7D54',
  red: '#B3263C'
};
const font = 'Arial';
const images = path.join(repo, 'tfg/memoria/imaxes');

// Source figures are rendered from the submitted memory so the slide evidence
// remains identical to the document assessed by the panel.
const poppler = path.join(runtime, 'native/poppler/Library/bin/pdftoppm.exe');
const pdf = path.join(repo, 'tfg/memoria/memoria_tfg.pdf');
for (const [name, page, scale, x, y, w, h] of [
  ['arquitectura', 32, 2200, 287, 1220, 1093, 450],
  ['modelo', 33, 2200, 289, 1300, 1090, 352],
  ['estados', 40, 2800, 455, 1292, 1210, 425]
]) {
  execFileSync(poppler, [
    '-f', String(page), '-l', String(page), '-singlefile', '-scale-to', String(scale),
    '-x', String(x), '-y', String(y), '-W', String(w), '-H', String(h), '-png',
    pdf, path.join(build, name)
  ], { windowsHide: true });
}

function text(slide, value, x, y, width, height, size = 28, color = C.ink, bold = false, align = 'left') {
  const shape = slide.shapes.add({
    geometry: 'textbox',
    name: value.slice(0, 60),
    position: { left: x, top: y, width, height },
    fill: 'none',
    line: { fill: 'none', width: 0 }
  });
  shape.text = value;
  shape.text.style = {
    typeface: font,
    fontSize: size,
    bold,
    color,
    alignment: align,
    verticalAlignment: 'top',
    autoFit: 'none',
    wrap: 'word',
    insets: { left: 0, right: 0, top: 0, bottom: 0 }
  };
  return shape;
}

function rule(slide, x, y, width, color = C.line, height = 2) {
  return slide.shapes.add({
    geometry: 'rect',
    position: { left: x, top: y, width, height },
    fill: color,
    line: { fill: 'none', width: 0 }
  });
}

function base(index, dark = false) {
  const slide = P.slides.add();
  slide.background.fill = dark ? C.ink : C.paper;
  const entry = content[index];
  const fg = dark ? '#FFFFFF' : C.ink;
  const muted = dark ? '#D3DCE7' : C.muted;
  text(slide, entry.section.toUpperCase(), 64, 30, 950, 28, 17, dark ? '#F2A7D4' : C.accent, true);
  text(slide, entry.title, 64, 69, 1152, 64, 43, fg, true);
  rule(slide, 64, 142, 1152, dark ? '#42536A' : C.line, 2);
  text(slide, index < 18 ? String(index + 1).padStart(2, '0') : String.fromCharCode(65 + index - 18), 1160, 674, 56, 22, 16, muted, false, 'right');
  return slide;
}

async function pic(slide, file, x, y, width, height, alt, crop) {
  const image = slide.images.add({
    blob: new Uint8Array(await fs.readFile(file)),
    contentType: 'image/png',
    alt,
    fit: crop ? 'cover' : 'contain',
    position: { left: x, top: y, width, height }
  });
  if (crop) image.crop = crop;
  return image;
}

function caption(slide, value, dark = false) {
  text(slide, value, 64, 674, 1068, 22, 14, dark ? '#D3DCE7' : C.muted);
}

function label(slide, value, x, y, width = 500, color = C.accent) {
  text(slide, value, x, y, width, 34, 22, color, true);
}

function para(slide, value, x, y, width, height = 120, size = 28, color = C.ink, bold = false) {
  text(slide, value, x, y, width, height, size, color, bold);
}

function metric(slide, value, description, x, y, width = 300, color = C.accent) {
  text(slide, value, x, y, width, 96, 70, color, true);
  text(slide, description, x, y + 94, width, 76, 24, C.ink);
}

function table(slide, values, x, y, width, height, widths, size = 23) {
  const t = slide.tables.add({
    rows: values.length,
    columns: values[0].length,
    left: x,
    top: y,
    width,
    height,
    values,
    columnWidths: widths
  });
  t.styleOptions = { headerRow: false, bandedRows: false };
  t.borders.assign({ fill: C.line, width: 1, style: 'solid' });
  t.cells.block({ row: 0, column: 0, rowCount: values.length, columnCount: values[0].length }).assign({
    fill: '#FFFFFF',
    textStyle: { typeface: font, fontSize: size, color: C.ink },
    margins: { left: 15, right: 15, top: 10, bottom: 9 },
    anchor: 'center'
  });
  for (let r = 0; r < values.length; r++) {
    t.rows[r].height = height / values.length;
    for (let c = 0; c < values[0].length; c++) {
      const cell = t.getCell(r, c);
      cell.text.style = {
        typeface: font,
        fontSize: size,
        color: r === 0 ? '#FFFFFF' : C.ink,
        bold: r === 0,
        autoFit: 'none'
      };
      if (r === 0) cell.fill = C.ink;
    }
  }
  return t;
}

// 01. Cover
{
  const slide = P.slides.add();
  slide.background.fill = C.paper;
  await pic(slide, path.join(images, 'udc-sanitized.png'), 64, 43, 430, 56, 'Universidade da Coruña');
  text(slide, 'TransitOps', 64, 170, 1110, 108, 88, C.ink, true);
  text(slide, 'Diseño y desarrollo de una aplicación\nde gestión de transportes', 67, 302, 1100, 105, 40, C.ink);
  text(slide, 'Ciclo de vida completo del software', 67, 429, 1100, 55, 34, C.accent, true);
  text(slide, 'Pablo Manzanares López', 67, 550, 580, 34, 26, C.ink, true);
  text(slide, 'Dirección: Paula María Castro Castro', 67, 593, 700, 32, 22, C.muted);
  text(slide, 'Grado en Ingeniería Informática\nMención en Ingeniería del Software', 825, 553, 392, 77, 21, C.muted, false, 'right');
  text(slide, 'Facultad de Informática · Septiembre de 2026', 67, 674, 1000, 25, 17, C.muted);
}

// 02. Agenda
{
  const slide = base(1);
  const items = [
    ['01', 'Problema y objetivos'],
    ['02', 'Requisitos y alcance'],
    ['03', 'Metodología'],
    ['04', 'Diseño y arquitectura'],
    ['05', 'Demostración funcional'],
    ['06', 'Validación y despliegue'],
    ['07', 'Conclusiones y trabajo futuro']
  ];
  items.forEach(([number, name], i) => {
    const left = i < 4 ? 64 : 681;
    const row = i < 4 ? i : i - 4;
    const top = 183 + row * 112;
    text(slide, number, left, top, 70, 44, 28, C.accent, true);
    text(slide, name, left + 88, top, 500, 50, 29, C.ink, true);
    rule(slide, left, top + 63, 535, C.line, 1);
  });
  caption(slide, 'Orden de la defensa: definición, construcción, demostración y evaluación.');
}

// 03. Problem
{
  const slide = base(2);
  para(slide, 'La información operativa está repartida entre hojas, mensajes y llamadas', 64, 180, 675, 150, 39, C.ink, true);
  label(slide, 'Disponibilidad incierta', 795, 181, 420);
  para(slide, 'Un vehículo o conductor puede quedar reservado dos veces.', 795, 225, 420, 84, 26);
  label(slide, 'Historia incompleta', 795, 345, 420);
  para(slide, 'Reconstruir una incidencia exige buscar qué pasó y quién actuó.', 795, 389, 420, 93, 26);
  label(slide, 'Datos sin vigencia clara', 795, 518, 420);
  para(slide, 'Los registros obsoletos pueden volver a utilizarse por error.', 795, 562, 420, 72, 26);
  para(slide, 'Caso ficticio de pyme de transporte definido mediante una entrevista simulada', 64, 514, 650, 94, 29, C.blue);
  caption(slide, 'Memoria, pp. 1–3 y 15. El trabajo no mide impacto en una empresa real.');
}

// 04. Goals
{
  const slide = base(3);
  para(slide, 'Centralizar la operación diaria y demostrar el ciclo de vida completo del producto', 64, 169, 1150, 78, 34, C.ink, true);
  table(slide, [
    ['Criterio', 'Evidencia esperada'],
    ['Funcionalidad', 'Cuatro casos de uso completos'],
    ['Calidad', 'Reglas verificadas en varias capas'],
    ['Reproducibilidad', 'Ejecución y despliegue documentados'],
    ['Trazabilidad', 'Necesidad, decisión y prueba relacionadas']
  ], 64, 280, 1152, 340, [330, 822], 25);
  caption(slide, 'Objetivo general y criterios de éxito de la memoria, pp. 2–3.');
}

// 05. Requirements and scope
{
  const slide = base(4);
  metric(slide, '14', 'requisitos\nfuncionales', 64, 170, 220);
  metric(slide, '17', 'reglas de\nnegocio', 326, 170, 220);
  metric(slide, '6', 'requisitos no\nfuncionales', 588, 170, 250);
  label(slide, 'Incluido', 888, 171, 320, C.green);
  para(slide, 'Catálogos y envíos\nAsignación y estados\nEventos e incidencias\nUsuarios e indicadores', 888, 221, 330, 185, 26);
  label(slide, 'Fuera del TFG', 888, 447, 330, C.red);
  para(slide, 'Optimización de rutas\nGPS y facturación\nAcceso de clientes externos', 888, 497, 330, 130, 26);
  para(slide, 'Actores del sistema', 64, 462, 300, 42, 25, C.accent, true);
  para(slide, 'Operador: trabajo diario\nAdministrador: cuentas y permisos', 64, 510, 700, 92, 28);
  caption(slide, 'Conductores y clientes son datos del dominio. Memoria, pp. 15–21.');
}

// 06. Use cases
{
  const slide = base(5);
  table(slide, [
    ['Caso de uso', 'Actor y responsabilidad', 'Resultado observable'],
    ['CU-1 · Poner en marcha una instalación vacía', 'Instalador', 'Primer administrador creado; arranque bloqueado'],
    ['CU-2 · Incorporar a una persona al sistema', 'Administrador: crea la cuenta\nOperador: realiza el primer acceso', 'Cuenta activa con contraseña propia'],
    ['CU-3 · Ejecutar un envío de principio a fin', 'Operador', 'Envío terminal con cronología verificable'],
    ['CU-4 · Retirar el acceso sin perder historial', 'Administrador', 'Cuenta desactivada; historial conservado']
  ], 64, 170, 1152, 446, [375, 350, 427], 19);
  caption(slide, 'Operador y administrador son roles separados; en CU-2 intervienen en momentos distintos. Memoria, pp. 17–21.');
}

// 07. Methodology
{
  const slide = base(6);
  table(slide, [
    ['Incremento', 'Resultado integrado'],
    ['S1', 'Esqueleto autenticado'],
    ['S2–S3', 'Catálogos, envíos y filtros'],
    ['S4–S5', 'Asignación, estados e historial'],
    ['S6', 'Usuarios e indicadores'],
    ['S7', 'Endurecimiento, sistema y despliegue']
  ], 64, 180, 735, 421, [175, 560], 24);
  label(slide, 'Definición de hecho', 856, 183, 350);
  para(slide, 'Flujo utilizable\nPruebas y compilación\nEvidencia documentada', 856, 232, 350, 155, 27);
  label(slide, 'Decisión de diseño', 856, 442, 350, C.blue);
  para(slide, 'Modelo conceptual completo\ny esquema físico incremental', 856, 492, 350, 100, 27);
  caption(slide, 'Rebanadas verticales adaptadas a un proyecto individual. Memoria, pp. 9–14.');
}

// 08. Architecture
{
  const slide = base(7);
  para(slide, 'React + TypeScript     ASP.NET Core / .NET 10     PostgreSQL + EF Core', 64, 161, 1150, 42, 27, C.blue);
  await pic(slide, path.join(build, 'arquitectura.png'), 71, 218, 1138, 365, 'Arquitectura de integración de la memoria, figura 5.1');
  para(slide, 'La API concentra autorización, validación y reglas de negocio', 64, 600, 1120, 44, 28, C.ink, true);
  caption(slide, 'Figura 5.1 de la memoria, p. 22. Nginx mantiene un único origen para la SPA y /api.');
}

// 09. Domain model
{
  const slide = base(8);
  await pic(slide, path.join(build, 'modelo.png'), 64, 184, 760, 330, 'Modelo relacional resumido de la memoria, figura 5.2');
  label(slide, 'Envío como núcleo', 868, 181, 340);
  para(slide, 'Cliente opcional y pareja de vehículo y conductor.', 868, 228, 340, 90, 25);
  label(slide, 'Historia preservada', 868, 345, 340);
  para(slide, 'Baja lógica en catálogos y usuarios. Eventos sin edición ni borrado.', 868, 392, 340, 125, 25);
  para(slide, 'Las invariantes críticas también se refuerzan en PostgreSQL', 64, 561, 1140, 58, 29, C.blue, true);
  caption(slide, 'Figura 5.2, p. 23. Esquema resumido, sin todos los campos e índices finales.');
}

// 10. End-to-end flow
{
  const slide = base(9);
  await pic(slide, path.join(build, 'estados.png'), 191, 171, 898, 309, 'Máquina de estados del envío, figura 6.1');
  const steps = [
    ['1', 'Preparar catálogos'],
    ['2', 'Crear el envío'],
    ['3', 'Asignar recursos'],
    ['4', 'Registrar el trayecto'],
    ['5', 'Cerrar y resumir']
  ];
  steps.forEach(([n, name], i) => {
    const x = 64 + i * 230;
    text(slide, n, x, 527, 42, 45, 27, C.accent, true);
    text(slide, name, x + 44, 527, 180, 70, 23, C.ink, true);
  });
  caption(slide, 'El flujo reúne los casos de uso 2 y 3. Estado terminal tras entrega o cancelación. Memoria, pp. 18–20 y 29–30.');
}

// 11. Planning and assignment
{
  const slide = base(10);
  await pic(slide, path.join(images, 'aviso-capacidad-sprint4.png'), 64, 186, 826, 433, 'Advertencia de capacidad durante la asignación', { left: 0.054, top: 0.215, right: 0.056, bottom: 0.219 });
  label(slide, 'Asignación conjunta', 932, 188, 284);
  para(slide, 'Vehículo y conductor solo mientras el envío está planificado.', 932, 234, 284, 112, 25);
  label(slide, 'Dos respuestas', 932, 388, 284);
  para(slide, 'Recurso ocupado: bloqueo\nCapacidad insuficiente: aviso', 932, 434, 284, 105, 25);
  para(slide, '4.500 kg sobre 3.000 kg', 932, 574, 284, 45, 24, C.blue, true);
  caption(slide, 'Captura con datos de prueba del Sprint 4. Figura 7.5 de la memoria, p. 39.');
}

// 12. Execution and traceability
{
  const slide = base(11);
  await pic(slide, path.join(images, 'historial-eventos-sprint5.png'), 64, 181, 780, 449, 'Historial de eventos automáticos y manuales', { left: 0.062, top: 0.216, right: 0.3, bottom: 0 });
  label(slide, 'Cronología verificable', 888, 184, 328);
  para(slide, 'Eventos automáticos y manuales ordenados por ocurrencia.', 888, 230, 328, 104, 25);
  label(slide, 'Autoría', 888, 372, 328);
  para(slide, 'El usuario procede de la sesión, no de un dato libre del cliente.', 888, 418, 328, 104, 25);
  label(slide, 'Consistencia', 888, 558, 328);
  para(slide, 'Operación y evento se confirman juntos.', 888, 602, 328, 54, 24);
  caption(slide, 'Captura con datos de prueba del Sprint 5. Figura 7.8 de la memoria, p. 42.');
}

// 13. Administration and summary
{
  const slide = base(12);
  await pic(slide, path.join(images, 'resumen-sprint6.png'), 64, 188, 826, 425, 'Panel operativo con datos de demostración', { left: 0.042, top: 0.104, right: 0.039, bottom: 0.239 });
  label(slide, 'Administración', 932, 190, 284);
  para(slide, 'Cuentas, roles, activación y recuperación de contraseña.', 932, 236, 284, 121, 25);
  label(slide, 'Resumen operativo', 932, 400, 284);
  para(slide, 'Estados globales, actividad por recurso e incidencias por periodo.', 932, 446, 284, 130, 25);
  para(slide, 'RF-01 a RF-14 integrados', 932, 603, 284, 40, 23, C.blue, true);
  caption(slide, 'Captura con datos de demostración del Sprint 6. Memoria, pp. 32–33 y 45.');
}

// 14. Validation
{
  const slide = base(13);
  metric(slide, '139', 'pruebas backend\nxUnit', 64, 161, 280);
  metric(slide, '33', 'pruebas frontend\nVitest + RTL', 450, 161, 280);
  metric(slide, '4', 'flujos de sistema\nPlaywright', 836, 161, 330);
  table(slide, [
    ['Nivel', 'Riesgo comprobado'],
    ['Servicios y API', 'Reglas, contratos y autorización'],
    ['PostgreSQL real', 'Restricciones y carreras de concurrencia'],
    ['Sistema integrado', 'Casos de uso sobre Compose'],
    ['Entorno desplegado', 'HTTPS, cookie y configuración efectiva']
  ], 64, 394, 1152, 237, [350, 802], 22);
  caption(slide, 'Recuentos al cierre del Sprint 7. Son casos ejecutados, no porcentaje de cobertura. Memoria, pp. 48–55.');
}

// 15. Deployment
{
  const slide = base(14);
  table(slide, [
    ['Etapa', 'Responsabilidad'],
    ['GitHub Actions', 'Valida y publica API y web en GHCR'],
    ['VM Ubuntu', 'Descarga imágenes y aplica Compose'],
    ['Script + systemd', 'Comprueba salud, revisión y digests']
  ], 64, 184, 705, 292, [225, 480], 23);
  label(slide, 'Acceso HTTPS temporal', 64, 519, 680);
  para(slide, 'Cuatro servicios, sin puertos de contenedores publicados en el host', 64, 565, 705, 70, 26);
  await pic(slide, path.join(images, 'despliegue-tunel-sprint7.png'), 839, 184, 370, 431, 'Aplicación desplegada y accesible mediante túnel HTTPS');
  caption(slide, 'Entorno de demostración reproducible. El túnel saliente evita abrir puertos de entrada. Memoria, cap. 9.');
}

// 16. Evaluation
{
  const slide = base(15);
  table(slide, [
    ['Criterio inicial', 'Evidencia final', 'Evaluación'],
    ['Funcionalidad', 'RF-01 a RF-14 y cuatro casos de uso', 'Cumplido'],
    ['Calidad', 'Pruebas por capas y reglas de concurrencia', 'Cumplido'],
    ['Reproducibilidad', 'Compose, CI y despliegue documentado', 'Cumplido'],
    ['Impacto empresarial', 'Sin evaluación con usuarios reales', 'No evaluado']
  ], 64, 180, 1152, 399, [330, 584, 238], 23);
  para(slide, 'El resultado cumple los criterios técnicos definidos al inicio sin atribuir beneficios no medidos', 64, 609, 1152, 44, 27, C.blue, true);
  caption(slide, 'Evaluación basada en la trazabilidad, los resultados y las conclusiones de la memoria, pp. 61–69.');
}

// 17. Limits and future work
{
  const slide = base(16);
  label(slide, 'Límites del resultado', 64, 179, 530);
  para(slide, 'Caso de negocio simulado\nSin evaluación con usuarios reales\nSin pruebas de carga\nTúnel temporal, sin copias ni métricas', 64, 232, 545, 224, 28);
  label(slide, 'Prioridades de continuación', 688, 179, 528, C.blue);
  para(slide, 'Validar el núcleo con usuarios\nEstabilizar alojamiento y recuperación\nPublicar el mismo artefacto probado', 688, 232, 528, 190, 28);
  rule(slide, 64, 500, 1152, C.line, 2);
  para(slide, 'Rutas, GPS, facturación y acceso externo se valorarían después de validar el uso real', 64, 539, 1152, 76, 30, C.ink, true);
  caption(slide, 'Límites y líneas futuras de la memoria, pp. 60 y 66–69.');
}

// 18. Conclusions
{
  const slide = base(17, true);
  text(slide, 'Un núcleo operativo completo\ncon un ciclo de ingeniería verificable', 64, 184, 1120, 130, 45, '#FFFFFF', true);
  para(slide, 'Las necesidades se relacionan con el diseño y las pruebas.\nCada incremento entrega una capacidad utilizable.\nEl sistema puede reproducirse y desplegarse siguiendo la documentación.', 64, 374, 1120, 178, 29, '#DFE7EF');
  text(slide, 'Gracias', 64, 612, 600, 47, 34, '#FFFFFF', true);
  text(slide, 'Preguntas', 930, 612, 286, 47, 28, '#DFE7EF', false, 'right');
  caption(slide, 'Pablo Manzanares López · TransitOps', true);
}

// 19. Backup: traceability
{
  const slide = base(18);
  table(slide, [
    ['Requisitos', 'Incremento', 'Evidencia principal'],
    ['RF-01, RF-02, RF-13', 'S1', 'Acceso, bootstrap y contrato de error'],
    ['RF-05–RF-07', 'S2', 'Catálogos y conservación histórica'],
    ['RF-08, RF-12', 'S3', 'Envíos, filtros y fechas'],
    ['RF-09, RF-10', 'S4 + S7', 'Operación y concurrencia real'],
    ['RF-11', 'S5', 'Historial, autoría y atomicidad'],
    ['RF-03, RF-04, RF-14', 'S6 + S7', 'Usuarios, contraseña e indicadores']
  ], 64, 174, 1152, 452, [337, 204, 611], 23);
  caption(slide, 'RF-04 incorpora la recuperación de contraseña administrativa en S7. Memoria, pp. 61–62.');
}

// 20. Backup: concurrency and security
{
  const slide = base(19);
  table(slide, [
    ['Riesgo', 'Garantía aplicada', 'Resultado'],
    ['Doble reserva', 'Índices únicos parciales en PostgreSQL', 'La segunda operación recibe 409'],
    ['Último administrador', 'Bloqueo transaccional y reevaluación', 'La baja incompatible recibe 409'],
    ['Token accesible a JavaScript', 'Cookie HttpOnly, Secure y SameSite=Strict', 'Menor exposición del token'],
    ['Permisos obsoletos', 'TokenVersion comprobado por petición', 'Revocación tras cambio sensible']
  ], 64, 178, 1152, 401, [310, 504, 338], 22);
  caption(slide, 'Memoria, pp. 25–33 y 52–54.');
}

// 21. Backup: plan and cost
{
  const slide = base(20);
  table(slide, [
    ['Concepto', 'Hipótesis de cálculo', 'Importe'],
    ['Personal', '300 h × 20 €/h', '6.000,00 €'],
    ['Amortización del equipo', '1.200 € × 2,5 / 48 meses', '62,50 €'],
    ['Licencias', 'Uso libre o gratuito en el proyecto', '0,00 €'],
    ['Infraestructura y servicios', 'Máquina local y cuotas gratuitas', '0,00 €'],
    ['Costes indirectos', 'Energía y conectividad estimadas', '75,00 €'],
    ['Total', 'Valoración profesional hipotética', '6.137,50 €']
  ], 64, 173, 1152, 451, [341, 531, 280], 23);
  caption(slide, 'Estimación académica de la memoria, tabla 3.3, p. 13. No representa dedicación ni gasto reales.');
}

// 22. Backup: test evolution
{
  const slide = base(21);
  table(slide, [
    ['Sprint', 'Backend', 'Frontend', 'Sistema'],
    ['S1', '16', '4', '—'],
    ['S2', '29', '7', '—'],
    ['S3', '45', '13', '—'],
    ['S4', '74', '19', '—'],
    ['S5', '96', '24', '—'],
    ['S6', '127', '30', '—'],
    ['S7', '139', '33', '4 flujos E2E']
  ], 170, 170, 940, 456, [230, 220, 220, 270], 24);
  caption(slide, 'Recuentos acumulados documentados al cierre de cada sprint. No representan porcentaje de cobertura. Memoria, pp. 49–52.');
}

// Notes and rehearsal guide use the same source as the slide titles.
let cumulative = 0;
const mmss = n => `${Math.floor(n / 60)}:${String(n % 60).padStart(2, '0')}`;
const total = content.slice(0, 18).reduce((sum, item) => sum + item.seconds, 0);
let guide = '# Guion de defensa de TransitOps\n\n';
guide += `Duración objetivo: **${mmss(total)}**. Diapositivas 1–18 para la exposición; 19–22 son apoyo y están ocultas en el PowerPoint. Los tiempos son una pauta de ensayo.\n\n`;
guide += 'Este texto está escrito para decirlo en voz alta. Úsalo como apoyo y adáptalo a tu forma de hablar: no hace falta memorizarlo palabra por palabra. Las referencias son para preparar posibles preguntas, no para leerlas durante la exposición. Las capturas usan datos de prueba o demostración.\n\n';
guide += '| Diapositiva | Sección | Duración | Acumulado |\n| --- | --- | ---: | ---: |\n';
for (let i = 0; i < 18; i++) {
  cumulative += content[i].seconds;
  guide += `| ${i + 1}. ${content[i].title} | ${content[i].section} | ${mmss(content[i].seconds)} | ${mmss(cumulative)} |\n`;
}

cumulative = 0;
for (let i = 0; i < content.length; i++) {
  const entry = content[i];
  const start = cumulative;
  if (i < 18) cumulative += entry.seconds;
  const timing = i < 18
    ? `Tiempo orientativo: ${mmss(entry.seconds)}. Tramo ${mmss(start)}–${mmss(cumulative)}.`
    : 'Apoyo para preguntas. Fuera del tiempo principal.';
  P.slides.items[i].speakerNotes.textFrame.setText(`${timing}\n\n${entry.notes}\n\nFUENTES\n${entry.source}`);
  guide += `\n## ${i + 1}. ${entry.title}\n\n**${entry.section}**\n\n${timing}\n\n${entry.notes}\n\nFuente: ${entry.source}\n`;
}

await fs.writeFile(path.join(out, 'Guion_defensa_TransitOps.md'), guide);
await fs.writeFile(path.join(build, 'presentation.json'), JSON.stringify(P.toProto()));

const candidate = path.join(build, 'candidate-reorganized.pptx');
await (await PresentationFile.exportPptx(P)).save(candidate);
const hiddenCandidate = path.join(build, 'candidate-reorganized-with-backup.pptx');
execFileSync(path.join(runtime, 'python/python.exe'), [
  path.join(root, 'fuente/ocultar_apoyo.py'), candidate, hiddenCandidate
], { windowsHide: true });

const final = path.join(out, process.env.TRANSITOPS_PPTX_NAME || 'TransitOps_Presentacion_TFG_Reorganizada.pptx');
const tableOwners = [4, 6, 7, 14, 15, 16, 19, 20, 21, 22];
const result = await finalizePresentation({
  workspaceDir: root,
  candidatePath: hiddenCandidate,
  finalPath: final,
  pythonExecutable: path.join(runtime, 'python/python.exe'),
  integrityValidatorPath: path.join(skill, 'container_tools/inspect_presentation_package_integrity.py'),
  layoutValidatorPath: path.join(skill, 'container_tools/inspect_presentation_layout_geometry.py'),
  layoutArgs: [
    '--expected-slide-size-emu', '12192000,6858000',
    '--validate-bullet-geometry',
    '--validate-heading-fit',
    ...tableOwners.flatMap(number => ['--require-native-table-slide', String(number)])
  ],
  requiredNativeTableOwnerSlides: tableOwners,
  requiredNativeChartOwnerSlides: [],
  fontPolicy: { basis: 'design', families: [font] },
  verifyArtifactToolImport: true,
  receiptPath: path.join(root, '.codex-finalizer', `${path.basename(final)}.validation.json`)
});
console.log(JSON.stringify(result));

const { FileBlob } = await import('@oai/artifact-tool');
const reviewed = await PresentationFile.importPptx(await FileBlob.load(final));
for (let i = 0; i < reviewed.slides.items.length; i++) {
  const slide = reviewed.slides.items[i];
  const image = await reviewed.export({ slide, format: 'png', scale: 1.5 });
  await fs.writeFile(path.join(build, `slide-${String(i + 1).padStart(2, '0')}.png`), new Uint8Array(await image.arrayBuffer()));
  const layout = await slide.export({ format: 'layout' });
  await fs.writeFile(path.join(build, `slide-${String(i + 1).padStart(2, '0')}.json`), await layout.text());
  console.log(`Rendered ${i + 1}/${reviewed.slides.items.length}`);
}
console.log(`Final presentation: ${final}; target duration ${mmss(total)}`);
