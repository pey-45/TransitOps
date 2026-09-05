"""Set hidden-slide flags and native, reversible source-image crops."""
import re
import sys
import zipfile
from lxml import etree

source, target = sys.argv[1:]
ns = {'a': 'http://schemas.openxmlformats.org/drawingml/2006/main',
      'p': 'http://schemas.openxmlformats.org/presentationml/2006/main'}
# The exporter replaces manual image crops with automatic cover cropping.
# Set the authored crop in OOXML without changing the embedded source image.
crops = {
    9: (70/1265, 165/712, 85/1265, 39/712),
    10: (85/1265, 156/712, 348/1265, 62/712),
    15: (70/1264, 220/977, 69/1264, 241/977),
}
with zipfile.ZipFile(source) as src, zipfile.ZipFile(target, 'w', zipfile.ZIP_DEFLATED) as dst:
    for item in src.infolist():
        data = src.read(item.filename)
        match = re.fullmatch(r'ppt/slides/slide(\d+)\.xml', item.filename)
        if match:
            doc = etree.fromstring(data)
            number = int(match[1])
            if number > 18:
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
