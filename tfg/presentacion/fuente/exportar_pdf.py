"""Export the reviewed slide renders as a portable, full HD PDF."""
import json
from pathlib import Path
from reportlab.pdfgen import canvas
from pypdf import PdfReader

root = Path(__file__).resolve().parents[1]
content = json.loads((root / 'fuente/contenido.json').read_text(encoding='utf-8'))
target = root / 'entrega/TransitOps_Presentacion_TFG.pdf'
doc = canvas.Canvas(str(target), pagesize=(960, 540), pageCompression=1)
doc.setTitle('TransitOps - Defensa del Trabajo Fin de Grado')
doc.setAuthor('Pablo Manzanares López')
main_slide_count = 13
demo_seconds = 132
duration = sum(slide['seconds'] for slide in content[:main_slide_count]) + demo_seconds
doc.setSubject(
    f'{main_slide_count} diapositivas principales, una demo en vídeo y 4 diapositivas de apoyo. '
    f'Duración orientativa: {duration // 60} minutos y {duration % 60} segundos.'
)
for i, slide in enumerate(content, 1):
    key = f'slide-{i}'
    doc.bookmarkPage(key)
    doc.addOutlineEntry(f'{i:02d}. {slide["title"]}', key, level=0, closed=False)
    doc.drawImage(str(root / f'.build/slide-{i:02d}.png'), 0, 0, width=960, height=540)
    doc.showPage()
doc.save()
reader = PdfReader(target)
assert len(reader.pages) == 17
assert all(float(p.mediabox.width) == 960 and float(p.mediabox.height) == 540 for p in reader.pages)
print(target)
