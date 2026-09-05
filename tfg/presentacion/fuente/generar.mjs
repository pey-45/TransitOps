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
const skill = process.env.TRANSITOPS_PRESENTATION_SKILL || 'C:/Users/pey/.codex/plugins/cache/openai-primary-runtime/presentations/26.904.11930/skills/presentations';
const { finalizePresentation, applyPresentationChartFont } = await import(pathToFileURL(path.join(skill, 'container_tools/artifact_tool_utils.mjs')));
await fs.mkdir(build, {recursive:true});
await fs.mkdir(out, {recursive:true});
await fs.mkdir(path.join(root, '.codex-finalizer'), {recursive:true});
const content = JSON.parse(await fs.readFile(path.join(root, 'fuente/contenido.json'), 'utf8'));
const P = Presentation.create({slideSize:{width:1280,height:720}});
const C = {ink:'#14263D', muted:'#536174', accent:'#C0007F', blue:'#2156A5', teal:'#177668', paper:'#FFFFFF', pale:'#F1F4F8', line:'#D9DFE7'};
const font = 'Arial';
const images = path.join(repo, 'tfg/memoria/imaxes');

// Render bounded regions of the source PDF. These remain faithful source figures.
const poppler = path.join(runtime, 'native/poppler/Library/bin/pdftoppm.exe');
const pdf = path.join(repo,'tfg/memoria/memoria_tfg.pdf');
for(const [name, page, scale, x,y,w,h] of [
  ['arquitectura',32,2200,287,1220,1093,450],
  ['modelo',33,2200,289,1300,1090,352],
  ['estados',40,2800,455,1292,1210,425]
]) execFileSync(poppler,['-f',String(page),'-l',String(page),'-singlefile','-scale-to',String(scale),'-x',String(x),'-y',String(y),'-W',String(w),'-H',String(h),'-png',pdf,path.join(build,name)],{windowsHide:true});

function text(s,str,x,y,w,h,size=28,color=C.ink,bold=false,align='left'){
  const sh=s.shapes.add({geometry:'textbox',name:str.slice(0,60),position:{left:x,top:y,width:w,height:h},fill:'none',line:{fill:'none',width:0}});
  sh.text=str;
  sh.text.style={typeface:font,fontSize:size,bold,color,alignment:align,verticalAlignment:'top',autoFit:'none',wrap:'word',insets:{left:0,right:0,top:0,bottom:0}};
  return sh;
}
function base(i,dark=false){
  const s=P.slides.add(); s.background.fill=dark?C.ink:C.paper;
  text(s,content[i].title,64,46,1152,100,44,dark?'#FFFFFF':C.ink,true);
  text(s,i<18?String(i+1).padStart(2,'0'):String.fromCharCode(65+i-18),1160,674,56,22,16,dark?'#D3DCE7':C.muted,false,'right');
  return s;
}
async function pic(s,file,x,y,w,h,alt,crop){
  const img=s.images.add({blob:new Uint8Array(await fs.readFile(file)),contentType:'image/png',alt,fit:crop?'cover':'contain',position:{left:x,top:y,width:w,height:h}});
  if(crop) img.crop=crop;
}
function caption(s,str,dark=false){text(s,str,64,674,1070,24,15,dark?'#D3DCE7':C.muted);}
function label(s,t,x,y,w=500,color=C.accent){text(s,t,x,y,w,34,23,color,true);}
function para(s,t,x,y,w,h=120,size=28,color=C.ink){text(s,t,x,y,w,h,size,color);}
function table(s,values,x,y,w,h,widths,size=24){
  const t=s.tables.add({rows:values.length,columns:values[0].length,left:x,top:y,width:w,height:h,values,columnWidths:widths});
  t.styleOptions={headerRow:false,bandedRows:false};
  t.borders.assign({fill:C.line,width:1,style:'solid'});
  const block=t.cells.block({row:0,column:0,rowCount:values.length,columnCount:values[0].length});
  block.assign({fill:'#FFFFFF',textStyle:{typeface:font,fontSize:size,color:C.ink},margins:{left:16,right:16,top:12,bottom:10},anchor:'center'});
  for(let r=0;r<values.length;r++){
    t.rows[r].height=h/values.length;
    for(let c=0;c<values[0].length;c++){
      const cell=t.getCell(r,c);
      cell.text.style={typeface:font,fontSize:size,color:r===0?'#FFFFFF':C.ink,bold:r===0,autoFit:'none'};
      if(r===0)cell.fill=C.ink;
    }
  }
  return t;
}
function metric(s,n,desc,x,y,w=310,dark=false){
  text(s,n,x,y,w,105,76,dark?'#FFFFFF':C.accent,true);
  text(s,desc,x,y+109,w,76,25,dark?'#DFE7EF':C.ink);
}

// 01. Academic cover, with the original institutional artwork.
{
const s=P.slides.add(); s.background.fill=C.paper;
await pic(s,path.join(images,'udc-sanitized.png'),64,43,430,56,'Universidade da Coruña');
text(s,'TransitOps',64,175,1110,106,88,C.ink,true);
text(s,'Diseño y desarrollo de una aplicación\nde gestión de transportes',67,306,1100,104,40,C.ink);
text(s,'Ciclo de vida completo del software',67,430,1100,56,34,C.accent,true);
text(s,'Pablo Manzanares López',67,550,580,34,26,C.ink,true);
text(s,'Dirección: Paula María Castro Castro',67,593,700,32,22,C.muted);
text(s,'Grado en Ingeniería Informática\nMención en Ingeniería del Software',825,553,392,77,21,C.muted,false,'right');
text(s,'Facultad de Informática · Septiembre de 2026',67,674,1000,25,17,C.muted);
}
// 02. Problem, clearly grounded in a simulated case.
{
const s=base(1); label(s,'CASO DE PARTIDA',64,171);
para(s,'Coordinar envíos con\ninformación repartida entre\nhojas, mensajes y llamadas',64,227,665,180,39);
label(s,'Disponibilidad incierta',795,184,420);
para(s,'Un vehículo o conductor puede\nquedar reservado dos veces.',795,228,420,105,27);
label(s,'Historia incompleta',795,385,420);
para(s,'Reconstruir una incidencia exige\nbuscar qué pasó y quién actuó.',795,429,420,112,27);
para(s,'Una aplicación común para la operación diaria',64,554,1090,68,31,C.blue);
caption(s,'Caso ficticio de pyme de transporte. Elicitación mediante entrevista simulada. Memoria, pp. 1–3 y 15.');
}
// 03. Purpose and bounded scope.
{
const s=base(2); para(s,'Construir una aplicación mantenible y demostrar su ciclo de vida completo',64,158,1120,92,34);
label(s,'Operación interna',64,295);
para(s,'Catálogos y envíos\nAsignación y estados\nEventos e incidencias\nUsuarios e indicadores',64,346,540,197,28);
metric(s,'14','requisitos funcionales',711,291,217);
metric(s,'17','reglas de negocio',987,291,230);
text(s,'Operador y administrador',64,571,590,40,26,C.blue,true);
para(s,'Fuera del alcance: rutas, GPS, facturación y acceso externo',711,528,495,97,25,C.muted);
caption(s,'6 requisitos no funcionales y 4 casos de uso completos. Memoria, pp. 2–3 y 15–21.');
}
// 04. Incremental plan as an editable evidence table.
{
const s=base(3); para(s,'Cada capacidad incorpora datos, API, interfaz y pruebas',64,151,1136,49,29,C.muted);
table(s,[['Sprint','Resultado integrado'],['S1','Esqueleto autenticado'],['S2–S3','Catálogos, envíos y filtros'],['S4–S5','Asignación, estados e historial'],['S6','Usuarios, contraseñas e indicadores'],['S7','Concurrencia, sesión, sistema y despliegue'],['S8','Cierre documental y defensa']],64,221,743,392,[163,580],24);
label(s,'Cierre de cada incremento',867,230,350);
para(s,'Flujo utilizable\nPruebas y compilación\nEvidencia documentada',867,289,347,160,27);
para(s,'Diseño conceptual completo.\nImplementación progresiva.',867,505,345,107,27,C.blue);
caption(s,'Proceso iterativo e incremental adaptado a un proyecto individual. Memoria, pp. 9–14.');
}
// 05. Editable chart; these are planned hours, never actual time measurements.
{
const s=base(4);
metric(s,'300 h','dedicación planificada',64,166,330);
label(s,'10 semanas efectivas',64,390,380,C.blue);
text(s,'6.137,50 €',64,467,388,73,49,C.ink,true);
para(s,'Valoración profesional estimada',64,548,395,60,24,C.muted);
const chart=s.charts.add('bar',{
 position:{left:461,top:174,width:755,height:435},
 categories:['Memoria','Pruebas y despliegue','Implementación','Análisis y diseño'],
 series:[{name:'Horas planificadas',values:[25,65,140,70],fill:C.blue}],
 barOptions:{direction:'bar',grouping:'clustered',gapWidth:90},hasLegend:false,
 xAxis:{visible:true,textStyle:{fontSize:22,typeface:font,fill:C.ink},line:{fill:'none',width:0},majorGridlines:null},
 yAxis:{visible:false,tickLabelPosition:'none',min:0,max:170,majorGridlines:null,line:{fill:'none',width:0}},
 dataLabels:{showValue:true,position:'outEnd',textStyle:{fontSize:25,typeface:font,fill:C.ink,bold:true}},
 chartFill:'#FFFFFF',plotAreaFill:'#FFFFFF',chartLine:{fill:'none',width:0}
});applyPresentationChartFont(chart,{fontFamily:font});
caption(s,'Estimaciones de la memoria, no mediciones de dedicación ni gasto real. Tablas 3.2–3.3, pp. 12–13.');
}
// 06. Original architecture figure.
{
const s=base(5); para(s,'React + TypeScript     ASP.NET Core / .NET 10     PostgreSQL + EF Core',64,151,1150,48,27,C.blue);
await pic(s,path.join(build,'arquitectura.png'),71,224,1138,365,'Arquitectura de integración de la memoria, figura 5.1');
para(s,'La API concentra autorización y reglas. La SPA presenta la operación.',64,599,1120,53,28);
caption(s,'Figura 5.1 de la memoria, p. 22. El JWT viaja en cookie HttpOnly desde el endurecimiento de S7.');
}
// 07. Source data model with explanatory conservation rule.
{
const s=base(6); para(s,'Seis entidades con relaciones históricas preservadas',64,152,1150,52,30,C.muted);
await pic(s,path.join(build,'modelo.png'),64,241,1152,369,'Modelo relacional resumido de la memoria, figura 5.2');
para(s,'Baja lógica en catálogos y usuarios. Los eventos pertenecen al envío.',64,605,1140,46,27,C.blue);
caption(s,'Figura 5.2, p. 23. Esquema resumido de relaciones, sin todos los atributos e índices finales.');
}
// 08. Original state machine and distinct business decisions.
{
const s=base(7);
await pic(s,path.join(build,'estados.png'),150,172,980,337,'Máquina de estados del envío, figura 6.1');
label(s,'Asignación conjunta',64,543,490);
para(s,'Vehículo y conductor, solo en planificado.',64,587,560,60,25);
label(s,'Fechas reales del servidor',687,543,520);
para(s,'Recogida al iniciar y entrega al finalizar, en UTC.',687,587,522,60,25);
caption(s,'RF-09 y RF-10. Figura 6.1 y apartado 6.6 de la memoria, pp. 29–30.');
}
// 09. A real screenshot; original evidence remains unaltered.
{
const s=base(8); para(s,'La capacidad insuficiente genera una advertencia',64,151,1150,51,30,C.muted);
await pic(s,path.join(images,'aviso-capacidad-sprint4.png'),64,231,828,379,'Advertencia de capacidad en el envío S4-UI-001',{left:.054,top:.215,right:.056,bottom:.219});
label(s,'RN-05',945,243,260);
text(s,'4.500 kg',945,302,270,64,42,C.ink,true);
para(s,'carga estimada',945,365,265,44,24,C.muted);
text(s,'3.000 kg',945,429,270,62,42,C.ink,true);
para(s,'capacidad registrada',945,489,265,69,24,C.muted);
para(s,'Asignación confirmada\ncon aviso no bloqueante',945,573,270,76,24,C.blue);
caption(s,'Captura con datos de prueba del Sprint 4. Figura 7.5, p. 39.');
}
// 10. Real timeline and atomicity.
{
const s=base(9);
await pic(s,path.join(images,'historial-eventos-sprint5.png'),64,171,775,460,'Historial de eventos automáticos y manuales del Sprint 5',{left:.062,top:.216,right:.3,bottom:0});
label(s,'Dos tiempos',892,176,322);
para(s,'Cuándo ocurrió\ny cuándo se registró',892,220,321,102,28);
label(s,'Identidad autenticada',892,353,323);
para(s,'Autor obtenido del usuario\nque ejecuta la acción',892,397,324,102,26);
label(s,'Una transacción',892,530,325);
para(s,'Operación y evento\nse confirman juntos',892,574,323,78,26);
caption(s,'Solo alta y consulta de eventos. Captura con datos de prueba del Sprint 5. Memoria, pp. 30–32 y 42.');
}
// 11. Technical invariants, compared in a native table.
{
const s=base(10); para(s,'Dos peticiones pueden observar a la vez un recurso libre',64,151,1140,74,31,C.muted);
table(s,[['Invariante','Garantía en PostgreSQL','Resultado'],['Un recurso en un solo\nenvío abierto','Índices únicos parciales\npor vehículo y conductor','La segunda reserva\nrecibe 409'],['Al menos un\nadministrador activo','Bloqueo transaccional\ny reevaluación de la regla','Una baja incompatible\nrecibe 409']],64,266,1152,268,[326,452,374],26);
para(s,'Pruebas simultáneas contra PostgreSQL real',64,584,1140,54,32,C.blue);
caption(s,'RN-04 y RN-12. Memoria, pp. 29–30, 32–33 y 52–54.');
}
// 12. No credential screenshot: explain session properties directly.
{
const s=base(11);
label(s,'Protección en el navegador',64,170,590);
para(s,'Cookie HttpOnly\nSameSite=Strict\nSecure fuera de desarrollo',64,228,550,174,33);
para(s,'La SPA recupera su identidad\nmediante /auth/me.',64,487,540,117,28,C.muted);
label(s,'Revocación en la siguiente petición',682,170,535);
para(s,'Contraseña, rol o activación\ncambian TokenVersion.',682,228,534,108,32);
para(s,'La API comprueba cuenta activa\ny versión en cada petición\nprotegida.',682,380,534,140,29);
text(s,'Coste: una lectura de usuario por petición',682,569,531,77,25,C.blue);
caption(s,'La autorización efectiva reside en la API. Mismo origen y SameSite mitigan CSRF. Memoria, pp. 25–27.');
}
// 13. Counts and precise evidence boundaries.
{
const s=base(12);
metric(s,'139','pruebas backend\nxUnit',64,152,306);
metric(s,'33','pruebas frontend\nVitest + RTL',463,152,306);
metric(s,'4','flujos de sistema\nPlaywright',862,152,354);
table(s,[['Evidencia','Qué comprueba'],['PostgreSQL real','Restricciones y carreras de concurrencia'],['CI sobre entorno limpio','Compilación, suites y migraciones'],['Inspección sobre HTTPS','Cookie Secure y configuración desplegada']],64,396,1152,211,[350,802],24);
caption(s,'Recuentos documentados en S7, no porcentajes de cobertura. Secure se verificó manualmente. Memoria, pp. 48–55.');
}
// 14. Deployment sequence is evidence in native rows, with real deployed screenshot.
{
const s=base(13);
table(s,[['Entrega','Responsabilidad'],['GitHub Actions','Valida y publica API/web en GHCR'],['VM Ubuntu','Descarga imágenes y aplica Compose'],['Script + systemd','Comprueba salud, revisión y digest']],64,190,705,266,[225,480],25);
label(s,'Acceso HTTPS temporal',64,514,680);
para(s,'Cuatro servicios. Sin puertos de contenedores\npublicados en el host. Túnel saliente.',64,558,719,81,26);
await pic(s,path.join(images,'despliegue-tunel-sprint7.png'),839,193,370,399,'Captura de acceso público al despliegue documentado');
caption(s,'Despliegue verificado en la memoria. E2E y publicación construyen imágenes distintas del mismo commit. Cap. 9.');
}
// 15. Application result, with actual demonstration data labeled.
{
const s=base(14);
await pic(s,path.join(images,'resumen-sprint6.png'),64,224,820,376,'Panel operativo con datos de demostración',{left:.042,top:.104,right:.039,bottom:.239});
label(s,'RF-01 a RF-14',934,195,280);
para(s,'Integrados de\nextremo a extremo',934,245,280,105,31);
label(s,'Indicadores con contexto',934,408,280);
para(s,'Estado global de envíos.\nActividad e incidencias\npor periodo.',934,456,280,139,27);
caption(s,'Captura con datos de demostración del Sprint 6. Sin medición de impacto empresarial. Memoria, pp. 33, 45 y 66–67.');
}
// 16. A concrete lesson, rather than a generic list of lessons.
{
const s=base(15);
label(s,'Necesidad en la entrevista',64,169,545);
para(s,'Reasignar una contraseña\ncuando alguien la olvida',64,224,547,115,35);
label(s,'Carencia detectada en el manual',64,395,545);
para(s,'El autoservicio exigía\nla contraseña anterior.',64,450,547,108,32);
label(s,'Corrección integrada',706,169,510);
text(s,'RN-17',706,225,510,104,76,C.accent,true);
para(s,'El administrador asigna una\ncontraseña a otra cuenta\ne invalida sus sesiones.',706,368,511,154,31);
para(s,'Requisitos, API, interfaz y pruebas actualizados',706,572,506,79,25,C.blue);
caption(s,'La documentación actúa también como verificación del producto. Memoria, p. 33 y matriz de trazabilidad.');
}
// 17. Limits are explicit, with prioritized future work.
{
const s=base(16);
label(s,'Límites del resultado',64,175,565);
para(s,'Caso de negocio simulado\nSin evaluación con usuarios reales\nSin pruebas de carga\nTúnel temporal, sin copias ni métricas',64,240,584,237,28);
label(s,'Continuación propuesta',731,175,487);
para(s,'Validar el núcleo con usuarios\nEstabilizar alojamiento y recuperación\nPublicar el mismo artefacto probado',731,240,483,234,28);
para(s,'Rutas, GPS y facturación se valorarían después de validar el uso real',64,562,1138,73,31,C.blue);
caption(s,'Entorno de demostración reproducible. Memoria, pp. 57, 60 y 66–69.');
}
// 18. Main presentation ends here. Backup slides are hidden in the PPTX.
{
const s=base(17,true);
text(s,'Un núcleo operativo completo\ncon un proceso de ingeniería verificable',64,190,1115,147,46,'#FFFFFF',true);
para(s,'Requisitos vinculados a decisiones y pruebas.\nIntegración progresiva y deudas cerradas con evidencia.\nDespliegue reproducible con límites explícitos.',64,397,1120,160,29,'#DFE7EF');
text(s,'Gracias',64,609,660,56,34,'#FFFFFF',true);
text(s,'Preguntas',930,609,286,56,28,'#DFE7EF',false,'right');
caption(s,'Pablo Manzanares López · TransitOps',true);
}
// 19–22. Backup slides.
{
const s=base(18);
table(s,[['Requisitos','Incremento','Evidencia principal'],['RF-01, RF-02, RF-13','S1','Acceso, bootstrap y contrato de error'],['RF-05–RF-07','S2','Catálogos y conservación histórica'],['RF-08, RF-12','S3','Envíos, filtros y fechas'],['RF-09, RF-10','S4 + S7','Operación y concurrencia real'],['RF-11','S5','Historial, autoría y atomicidad'],['RF-03, RF-04, RF-14','S6 + cierre S7','Usuarios, contraseña e indicadores']],64,170,1152,454,[337,204,611],24);
caption(s,'RF-04 incorpora RN-17 en S7. Los RNF se validan de forma transversal. Memoria, pp. 61–62.');
}
{
const s=base(19);
const ch=s.charts.add('line',{position:{left:64,top:153,width:1152,height:461},categories:['S1','S2','S3','S4','S5','S6','S7'],series:[{name:'Backend',values:[16,29,45,74,96,127,139],line:{fill:C.blue,width:4},marker:{symbol:'circle',size:7}},{name:'Frontend',values:[4,7,13,19,24,30,33],line:{fill:C.accent,width:4},marker:{symbol:'circle',size:7}}],hasLegend:true,legend:{position:'bottom',textStyle:{typeface:font,fontSize:23,fill:C.ink}},xAxis:{textStyle:{typeface:font,fontSize:23,fill:C.ink},majorGridlines:null,line:{fill:C.line,width:1}},yAxis:{min:0,max:160,majorUnit:40,textStyle:{typeface:font,fontSize:21,fill:C.muted},majorGridlines:{fill:C.line,width:1}},dataLabels:{showValue:true,position:'outEnd',textStyle:{typeface:font,fontSize:22,fill:C.ink}},chartFill:'#FFFFFF',plotAreaFill:'#FFFFFF',chartLine:{fill:'none',width:0}});applyPresentationChartFont(ch,{fontFamily:font});
caption(s,'Número de pruebas documentado al cierre de cada sprint. S7 añade 4 flujos E2E. No es cobertura porcentual. Cap. 8.');
}
{
const s=base(20);
table(s,[['Concepto','Hipótesis de cálculo','Importe'],['Personal','300 h × 20 €/h','6.000,00 €'],['Amortización del equipo','1.200 € × 2,5 / 48 meses','62,50 €'],['Licencias','Uso libre o gratuito en el proyecto','0,00 €'],['Infraestructura y servicios','Máquina local y cuotas gratuitas','0,00 €'],['Costes indirectos','Energía y conectividad, estimados','75,00 €'],['Total','Valoración profesional hipotética','6.137,50 €']],64,177,1152,447,[341,531,280],24);
caption(s,'Estimación de la memoria, tabla 3.3, p. 13. No constituye desembolso real ni presupuesto de operación sostenida.');
}
{
const s=base(21);
table(s,[['Decisión','Alternativa considerada','Razón en este proyecto'],['API modular única','Servicios distribuidos','Alcance acotado y menor coordinación'],['EF Core y PostgreSQL','SQL manual en cada acceso','Esquema evolutivo con integridad relacional'],['Evento en la transacción','Bus de eventos','Consistencia local sin infraestructura extra'],['TokenVersion','Revocación por lista o refresh tokens','Invalidación simple con lectura por petición']],64,192,1152,392,[307,369,476],24);
caption(s,'Decisiones proporcionales al dominio y al volumen esperado. Memoria, capítulos 2, 5 y 6.');
}

// Notes and rehearsal text share the same content source.
let cumulative=0;
const mmss=n=>`${Math.floor(n/60)}:${String(n%60).padStart(2,'0')}`;
let guide='# Guion de defensa de TransitOps\n\n';
const total=content.reduce((a,c)=>a+c.seconds,0);
guide+=`Duración objetivo: **${mmss(total)}**. Diapositivas 1–18 para la exposición; 19–22 son apoyo y están ocultas en el PowerPoint. Los tiempos son una pauta de ensayo, no una duración automática.\n\n`;
guide+='La presentación se sostiene con sus capturas y no necesita conexión ni demostración en vivo. Las capturas son evidencias de la memoria con datos de prueba. Ensaya con vista del moderador. Para una versión de 15 minutos, resume las diapositivas 4, 6, 11, 12 y 13 en 50 segundos cada una y ajusta las diapositivas 3 y 10 a 55 segundos, la 7 a 45 segundos y las 9, 14 y 16 a 50 segundos. Mantén las conclusiones y los límites.\n\n';
guide+='| Diapositiva | Duración | Acumulado |\n| --- | ---: | ---: |\n';
for(let i=0;i<18;i++){cumulative+=content[i].seconds;guide+=`| ${i+1}. ${content[i].title} | ${mmss(content[i].seconds)} | ${mmss(cumulative)} |\n`;}
cumulative=0;
for(let i=0;i<content.length;i++){
const c=content[i],start=cumulative;cumulative+=c.seconds;
const timing=i<18?`Tiempo orientativo: ${mmss(c.seconds)}. Tramo ${mmss(start)}–${mmss(cumulative)}.`:'Apoyo para preguntas. Fuera del tiempo principal.';
P.slides.items[i].speakerNotes.textFrame.setText(`${timing}\n\n${c.notes}\n\nFUENTES\n${c.source}`);
guide+=`\n## ${i+1}. ${c.title}\n\n${timing}\n\n${c.notes}\n\nFuente: ${c.source}\n`;
}
await fs.writeFile(path.join(out,'Guion_defensa_TransitOps.md'),guide);
await fs.writeFile(path.join(build,'presentation.json'),JSON.stringify(P.toProto()));
const candidate=path.join(build,'candidate.pptx');
await (await PresentationFile.exportPptx(P)).save(candidate);
// OOXML visibility is applied to the authored candidate, before validation.
const hiddenCandidate=path.join(build,'candidate-with-backup.pptx');
execFileSync(path.join(runtime,'python/python.exe'),[path.join(root,'fuente/ocultar_apoyo.py'),candidate,hiddenCandidate],{windowsHide:true});
const final=path.join(out,process.env.TRANSITOPS_PPTX_NAME||'TransitOps_Presentacion_TFG.pptx');
const result=await finalizePresentation({workspaceDir:root,candidatePath:hiddenCandidate,finalPath:final,pythonExecutable:path.join(runtime,'python/python.exe'),integrityValidatorPath:path.join(skill,'container_tools/inspect_presentation_package_integrity.py'),layoutValidatorPath:path.join(skill,'container_tools/inspect_presentation_layout_geometry.py'),layoutArgs:['--expected-slide-size-emu','12192000,6858000','--validate-bullet-geometry','--validate-heading-fit',...[4,11,13,14,19,21,22].flatMap(n=>['--require-native-table-slide',String(n)])],fontPolicy:{basis:'design',families:[font]},requiredNativeTableOwnerSlides:[4,11,13,14,19,21,22],requiredNativeChartOwnerSlides:[5,20],materializeLiteralChartWorkbooks:true,verifyArtifactToolImport:true,receiptPath:path.join(root,'.codex-finalizer',path.basename(final)+'.validation.json')});
console.log(JSON.stringify(result));
// Render each final slide, including backups, from the finalized package.
const { FileBlob } = await import('@oai/artifact-tool');
const reviewed=await PresentationFile.importPptx(await FileBlob.load(final));
for(let i=0;i<reviewed.slides.items.length;i++){
 const s=reviewed.slides.items[i];
 const img=await reviewed.export({slide:s,format:'png',scale:1.5});
 await fs.writeFile(path.join(build,`slide-${String(i+1).padStart(2,'0')}.png`),new Uint8Array(await img.arrayBuffer()));
 const layout=await s.export({format:'layout'});
 await fs.writeFile(path.join(build,`slide-${String(i+1).padStart(2,'0')}.json`),await layout.text());
 console.log(`Rendered ${i+1}/${reviewed.slides.items.length}`);
}
console.log(`Final presentation: ${final}; target duration ${mmss(total)}`);
