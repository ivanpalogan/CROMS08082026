# Appends the section "Data-entry tables and screens" to the existing Word report.
#
#   python build_doc_section.py --doc <report.docx> --out <new.docx> --diagrams out --shots shots --meta out\meta.json
#                               --columns columns.tsv
#
# The existing body is left exactly as it is (its last section keeps its page setup); the new section is
# spliced in after it as raw WordprocessingML that uses the report's own Heading1 / Heading2 styles and
# Calibri. Images are real PNG parts with alt text. Big diagrams get a landscape page of their own.
import argparse, json, os, re, shutil, struct, sys, zipfile
from xml.sax.saxutils import escape

EMU = 914400


def png_size(path):
    with open(path, 'rb') as f:
        h = f.read(24)
    return struct.unpack('>II', h[16:24])


class Pkg:
    def __init__(self, docx):
        self.z = zipfile.ZipFile(docx)
        self.files = {n: self.z.read(n) for n in self.z.namelist()}
        self.media = {}      # basename -> (rId, partname)
        self.next_rid = 1000
        self.docpr = 5000

    def rid_for(self, path):
        key = os.path.basename(path)
        if key in self.media:
            return self.media[key][0]
        rid = 'rIdImg%d' % self.next_rid; self.next_rid += 1
        part = 'word/media/' + key
        self.files[part] = open(path, 'rb').read()
        self.media[key] = (rid, part)
        return rid


def run(text, bold=False, italic=False, size=22, color=None):
    rp = ''
    if bold: rp += '<w:b/>'
    if italic: rp += '<w:i/>'
    if color: rp += '<w:color w:val="%s"/>' % color
    rp += '<w:sz w:val="%d"/><w:szCs w:val="%d"/>' % (size, size)
    return '<w:r><w:rPr>%s</w:rPr><w:t xml:space="preserve">%s</w:t></w:r>' % (rp, escape(text))


def para(runs, style=None, after=100, keep_next=False, jc=None, sect=None):
    pp = ''
    if style: pp += '<w:pStyle w:val="%s"/>' % style
    if keep_next: pp += '<w:keepNext/>'
    pp += '<w:spacing w:after="%d"/>' % after
    if jc: pp += '<w:jc w:val="%s"/>' % jc
    if sect: pp += sect
    return '<w:p><w:pPr>%s</w:pPr>%s</w:p>' % (pp, runs)


def heading(text, level):
    return para(run(text, size=32 if level == 1 else 26, color='2E74B5'), style='Heading%d' % level, after=120, keep_next=True)


def image(pkg, path, width_in, alt, max_h_in=None):
    w, h = png_size(path)
    cx = width_in * EMU
    cy = cx * h / w
    if max_h_in and cy > max_h_in * EMU:
        cy = max_h_in * EMU; cx = cy * w / h
    rid = pkg.rid_for(path)
    pkg.docpr += 1
    return ('<w:p><w:pPr><w:keepNext/><w:spacing w:after="60"/><w:jc w:val="center"/></w:pPr><w:r><w:drawing>'
            '<wp:inline distT="0" distB="0" distL="0" distR="0"><wp:extent cx="%d" cy="%d"/>'
            '<wp:docPr id="%d" name="Picture %d" descr="%s"/>'
            '<wp:cNvGraphicFramePr><a:graphicFrameLocks xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" noChangeAspect="1"/></wp:cNvGraphicFramePr>'
            '<a:graphic xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main"><a:graphicData uri="http://schemas.openxmlformats.org/drawingml/2006/picture">'
            '<pic:pic xmlns:pic="http://schemas.openxmlformats.org/drawingml/2006/picture"><pic:nvPicPr><pic:cNvPr id="%d" name="%s" descr="%s"/><pic:cNvPicPr/></pic:nvPicPr>'
            '<pic:blipFill><a:blip r:embed="%s"/><a:stretch><a:fillRect/></a:stretch></pic:blipFill>'
            '<pic:spPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="%d" cy="%d"/></a:xfrm><a:prstGeom prst="rect"/></pic:spPr></pic:pic>'
            '</a:graphicData></a:graphic></wp:inline></w:drawing></w:r></w:p>'
            % (cx, cy, pkg.docpr, pkg.docpr, escape(alt, {'"': '&quot;'}), pkg.docpr, escape(os.path.basename(path)), escape(alt, {'"': '&quot;'}),
               rid, cx, cy))


PORTRAIT = '<w:sectPr><w:pgSz w:w="12240" w:h="15840" w:orient="portrait"/><w:pgMar w:top="1440" w:right="1440" w:bottom="1440" w:left="1440" w:header="708" w:footer="708" w:gutter="0"/><w:pgNumType/><w:docGrid w:linePitch="360"/></w:sectPr>'
LAND = '<w:sectPr><w:pgSz w:w="15840" w:h="12240" w:orient="landscape"/><w:pgMar w:top="720" w:right="720" w:bottom="720" w:left="720" w:header="708" w:footer="708" w:gutter="0"/><w:pgNumType/><w:docGrid w:linePitch="360"/></w:sectPr>'


def crop_white(src, dst, margin=24):
    from PIL import Image, ImageChops
    im = Image.open(src).convert('RGB')
    bg = Image.new('RGB', im.size, 'white')
    box = ImageChops.difference(im, bg).getbbox()
    if not box:
        return src
    box = (max(0, box[0] - margin), max(0, box[1] - margin), min(im.width, box[2] + margin), min(im.height, box[3] + margin))
    im.crop(box).save(dst)
    return dst


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--doc', required=True); ap.add_argument('--out', required=True)
    ap.add_argument('--diagrams', required=True); ap.add_argument('--shots', required=True)
    ap.add_argument('--meta', required=True); ap.add_argument('--columns', required=True)
    a = ap.parse_args()
    meta = json.load(open(a.meta, encoding='utf-8'))
    shots = json.load(open(os.path.join(a.shots, 'shots.json'), encoding='utf-8'))
    by_table = {}
    for s in shots:
        for t in s['tables']:
            by_table.setdefault(t, []).append(s)
    schema_tables = sorted(set(l.split('\t')[0] for l in open(a.columns, encoding='utf-8') if l.strip()))
    diagrammed = [m['table'] for m in meta]
    system_tables = [t for t in schema_tables if t not in diagrammed and not t.startswith('v_')]

    pkg = Pkg(a.doc)
    body = pkg.files['word/document.xml'].decode('utf-8')
    m = re.search(r'<w:sectPr>.*?</w:sectPr></w:body>', body, re.S)
    assert m, 'no final sectPr'
    old_sect = m.group(0)[:-len('</w:body>')]
    head = body[:m.start()]

    out = []
    # the original content ends here, in its own (portrait) section
    out.append('<w:p><w:pPr>%s</w:pPr></w:p>' % old_sect)
    out.append(heading('Data-entry tables and screens', 1))
    total_cols = sum(m_['columns'] for m_ in meta)
    out.append(para(run('This section shows every table that a person types into through a CROMS screen, as a hub-and-spoke diagram '
                        '(%d tables, %d columns), followed by the screen that writes it. The big ringed circle is the table; each small circle around it is '
                        'one column, written exactly as the database column name. Column names and order are read from information_schema of the demo database; screens are the real forms '
                        'running against the demo environment (fictional sample data, orange DEMO ENVIRONMENT badge).' % (len(meta), total_cols))))
    out.append(para(run('The diagrams are full-resolution images: on a big table (births, marriages, marriage licences) zoom in to read the column names. '
                        'The same pages, one per table, are in Docs\\CROMS_Data_Entry_Table_Diagrams.drawio (open it in diagrams.net).')))
    out.append(para(run('Tables that CROMS writes by itself (audit trail, histories, generated requirement rows, deleted-record snapshots, capture tokens, '
                        'OCR audit, templates, settings and similar) are not diagrammed here: ' + ', '.join(system_tables) + '.', italic=True, size=20)))

    first = True
    for mt in meta:
        t = mt['table']
        big = mt['columns'] > 40
        small = mt['page'] <= 1000
        # --- diagram section
        if big:
            out.append('<w:p><w:pPr>%s</w:pPr></w:p>' % PORTRAIT)      # close the previous portrait section
            out.append(heading(t, 2))
            out.append(para(run('Screen: ' + mt['screen'] + '.  %d columns.' % mt['columns'], size=20), after=60, keep_next=True))
            out.append(image(pkg, os.path.join(a.diagrams, t + '.png'), 6.7, 'Bubble diagram of table %s with its %d columns' % (t, mt['columns']), max_h_in=6.7))
            out.append('<w:p><w:pPr>%s</w:pPr></w:p>' % LAND)           # end of the landscape section
        else:
            out.append(heading(t, 2))
            out.append(para(run('Screen: ' + mt['screen'] + '.  %d columns.' % mt['columns'], size=20), after=60, keep_next=True))
            dp = os.path.join(a.diagrams, t + '.png')
            if mt['page'] <= 1100:
                dp = crop_white(dp, os.path.join(a.diagrams, 'doc_' + t + '.png'))
            out.append(image(pkg, dp, 4.4 if small else 5.6, 'Bubble diagram of table %s with its %d columns' % (t, mt['columns']), max_h_in=4.4))
        # --- screenshots
        for s in by_table.get(t, []):
            p = os.path.join(a.shots, s['file'])
            if not os.path.exists(p):
                continue
            out.append(image(pkg, p, 6.5, 'Screenshot: ' + s['caption'], max_h_in=4.6))
            out.append(para(run(s['caption'], italic=True, size=18, color='555555'), after=160, jc='center'))
        if not by_table.get(t):
            out.append(para(run('(no screenshot captured for this table)', italic=True, size=18)))
    tail = ''.join(out)
    new_body = head + tail + PORTRAIT + '</w:body>'
    # the closing </w:document>
    new_body += body[m.end():]
    pkg.files['word/document.xml'] = new_body.encode('utf-8')

    # relationships
    rels = pkg.files['word/_rels/document.xml.rels'].decode('utf-8')
    add = ''.join('<Relationship Id="%s" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/image" Target="media/%s"/>' % (rid, os.path.basename(part))
                  for (rid, part) in pkg.media.values())
    rels = rels.replace('</Relationships>', add + '</Relationships>')
    pkg.files['word/_rels/document.xml.rels'] = rels.encode('utf-8')
    ct = pkg.files['[Content_Types].xml'].decode('utf-8')
    if 'Extension="png"' not in ct:
        ct = ct.replace('<Override', '<Default Extension="png" ContentType="image/png"/><Override', 1)
    pkg.files['[Content_Types].xml'] = ct.encode('utf-8')
    # declare namespaces used by the drawing XML on the root element if missing
    doc = pkg.files['word/document.xml'].decode('utf-8')
    root_end = doc.index('>', doc.index('<w:document'))
    root = doc[:root_end]
    for pre, uri in (('wp', 'http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing'),
                     ('r', 'http://schemas.openxmlformats.org/officeDocument/2006/relationships')):
        if 'xmlns:%s=' % pre not in root:
            root += ' xmlns:%s="%s"' % (pre, uri)
    pkg.files['word/document.xml'] = (root + doc[root_end:]).encode('utf-8')

    with zipfile.ZipFile(a.out, 'w', zipfile.ZIP_DEFLATED) as zo:
        names = ['[Content_Types].xml'] + [n for n in pkg.files if n != '[Content_Types].xml']
        for n in names:
            zo.writestr(n, pkg.files[n])
    print('wrote', a.out, 'tables', len(meta), 'images', len(pkg.media))


if __name__ == '__main__':
    main()
