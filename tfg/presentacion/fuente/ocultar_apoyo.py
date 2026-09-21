"""Set hidden-slide flags and native, reversible source-image crops."""
import re
import sys
import zipfile
from lxml import etree

source, target = sys.argv[1:]
ns = {'a': 'http://schemas.openxmlformats.org/drawingml/2006/main',
      'p': 'http://schemas.openxmlformats.org/presentationml/2006/main'}
crops = {}
with zipfile.ZipFile(source) as src, zipfile.ZipFile(target, 'w', zipfile.ZIP_DEFLATED) as dst:
    for item in src.infolist():
        data = src.read(item.filename)
        match = re.fullmatch(r'ppt/slides/slide(\d+)\.xml', item.filename)
        if match:
            doc = etree.fromstring(data)
            number = int(match[1])
            if number > 13:
                doc.set('show', '0')
            if number in crops:
                blip = doc.find('.//p:pic/p:blipFill', ns)
                rect = blip.find('a:srcRect', ns)
                if rect is None:
                    rect = etree.Element('{'+ns['a']+'}srcRect')
                    blip.insert(1, rect)
                for attr, fraction in zip(('l','t','r','b'), crops[number]):
                    rect.set(attr, str(round(fraction*100000)))
            data = etree.tostring(doc, xml_declaration=True, encoding='UTF-8', standalone=True)
        dst.writestr(item, data)
