#!/usr/bin/env python
"""Builds the CROMS data dictionary (Word) from the DEMO schema croms_demo.

Step 1 (PowerShell, read-only SELECT / information_schema, login read from CROMS\\App.config at run time
by Scripts\\Environment\\_Common.ps1 - nothing is stored here) exports three TSV files into a work folder:
    cols.tsv     table, ordinal, name, column_type, data_type, is_nullable, column_key, extra
    fks.tsv      table, column, referenced_table, referenced_column
    samples.tsv  table, column, "V:<first non-null, non-empty value>" or NULL   (blobs skipped)
Step 2 (this script) turns them into  <out>.docx : one 5-column table per database table
(Field | Type | Size | Attribute | Example), in the order of TABLES in gen_table_bubbles.py.

usage:  python build_data_dictionary.py export <workdir>            # runs the PowerShell export
        python build_data_dictionary.py build  <workdir> <out.docx>
        python build_data_dictionary.py all    <workdir> <out.docx>
"""
import ast
import datetime
import os
import re
import subprocess
import sys
import zipfile
from xml.sax.saxutils import escape

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, '..', '..'))
COMMON = os.path.join(REPO, 'Scripts', 'Environment', '_Common.ps1')
SCHEMA = 'croms_demo'
PINK = 'B5006E'
FONT = 'Times New Roman'
# widths in dxa (1 in = 1440): Field 1.35, Type 1.0, Size 0.85, Attribute 1.25, Example 2.05  = 6.5 in
WIDTHS = [1944, 1440, 1224, 1800, 2952]
SKIP_TABLES = {'_env_migrations'}          # demo-only migration ledger

# Columns that end in _id and clearly point at another table's key, but have NO foreign-key constraint
# in the database. Reviewed by hand against the code / migrations; marked FK in the dictionary and listed
# in the final report. (Polymorphic ids - record_id, owner_id, source_id, entity_id - point at different
# tables depending on another column, so they are deliberately NOT marked.)
SOFT_FK = {
    ('barangays', 'municipality_id'): 'municipalities',
    ('municipalities', 'province_id'): 'provinces',
    ('claimant_id_uploads', 'transaction_id'): 'transactions',
    ('claimant_id_uploads', 'queue_ticket_id'): 'queue_tickets',
    ('kiosk_ctc_intake', 'queue_ticket_id'): 'queue_tickets',
    ('kiosk_ctc_intake', 'transaction_id'): 'transactions',
    ('psa_copy_requests', 'queue_ticket_id'): 'queue_tickets',
    ('marriage_case_history', 'user_id'): 'users',
    ('psa_copy_history', 'user_id'): 'users',
    ('births', 'ocr_scan_id'): 'ocr_batch',
    ('deaths', 'ocr_scan_id'): 'ocr_batch',
    ('marriages', 'ocr_scan_id'): 'ocr_batch',
    ('ocr_field_audit', 'scan_id'): 'ocr_batch',
    ('psa_copy_requests', 'scan_id'): 'ocr_batch',
    ('office_assets', 'scan_id'): 'ocr_batch',
}

# Examples that must never come from a stored row, whatever the demo holds: text copied from a scanned
# certificate (OCR output) is personal data; the server beacon holds this PC's name and LAN addresses.
# These get a fictional value instead.
NEVER_FROM_ROW = {('ocr_batch', 'raw_text'), ('server_beacon', 'machine_name'), ('server_beacon', 'ip_list')}

# Default display widths MySQL 5.7 showed (MySQL 9 no longer stores them in column_type).
INT_WIDTH = {'tinyint': 4, 'smallint': 6, 'mediumint': 9, 'int': 11, 'bigint': 20}
INT_WIDTH_UNSIGNED = {'tinyint': 3, 'smallint': 5, 'mediumint': 8, 'int': 10, 'bigint': 20}


# ----------------------------------------------------------------------------------------- export
def export(work):
    os.makedirs(work, exist_ok=True)
    ps = os.path.join(work, 'export_dictionary.ps1')
    open(ps, 'w', encoding='utf-8').write(r'''param([string]$Dir)
[Console]::OutputEncoding = [Text.Encoding]::UTF8
. "%COMMON%"
$S = '%SCHEMA%'
Assert-DisposableSchema $S
$o = Invoke-MySql -Batch -Sql "SELECT c.table_name, c.ordinal_position, c.column_name, c.column_type, c.data_type, c.is_nullable, c.column_key, c.extra FROM information_schema.columns c JOIN information_schema.tables t ON t.table_schema=c.table_schema AND t.table_name=c.table_name WHERE c.table_schema='$S' AND t.table_type='BASE TABLE' ORDER BY c.table_name, c.ordinal_position;"
[IO.File]::WriteAllLines("$Dir\cols.tsv", [string[]]$o, (New-Object Text.UTF8Encoding($false)))
$f = Invoke-MySql -Batch -Sql "SELECT table_name, column_name, referenced_table_name, referenced_column_name FROM information_schema.key_column_usage WHERE table_schema='$S' AND referenced_table_name IS NOT NULL ORDER BY table_name, column_name;"
[IO.File]::WriteAllLines("$Dir\fks.tsv", [string[]]$f, (New-Object Text.UTF8Encoding($false)))
$rows = $o | ForEach-Object { $p = $_ -split "`t"; [pscustomobject]@{T=$p[0];C=$p[2];D=$p[4]} }
$out = New-Object System.Collections.Generic.List[string]
foreach ($g in ($rows | Where-Object { $_.T -ne '_env_migrations' } | Group-Object T)) {
  $t = $g.Name; $parts = @()
  foreach ($c in $g.Group) {
    if ($c.D -match 'blob|binary') { continue }
    $cn = '`' + $c.C + '`'
    $parts += "SELECT '$t' AS t, '$($c.C)' AS c, (SELECT CONCAT('V:', CAST($cn AS CHAR)) FROM ``$t`` WHERE $cn IS NOT NULL AND CAST($cn AS CHAR) <> '' LIMIT 1) AS v"
  }
  if ($parts.Count -eq 0) { continue }
  foreach ($l in (Invoke-MySql -Schema $S -Batch -Sql (($parts -join ' UNION ALL ') + ';'))) { $out.Add([string]$l) }
}
[IO.File]::WriteAllLines("$Dir\samples.tsv", $out.ToArray(), (New-Object Text.UTF8Encoding($false)))
"cols $($o.Count) fks $($f.Count) samples $($out.Count)"
'''.replace('%COMMON%', COMMON).replace('%SCHEMA%', SCHEMA))
    r = subprocess.run(['powershell', '-NoProfile', '-File', ps, '-Dir', work], capture_output=True, text=True)
    print(r.stdout.strip())
    if r.returncode != 0:
        sys.exit('export failed: ' + (r.stderr or r.stdout)[-400:])


# ---------------------------------------------------------------------------------------- loading
def load_tables_order():
    """TABLES list (table, sentence) from gen_table_bubbles.py, read with ast (no PIL import needed)."""
    src = open(os.path.join(HERE, 'gen_table_bubbles.py'), encoding='utf-8').read()
    tree = ast.parse(src)
    for node in tree.body:
        if isinstance(node, ast.Assign) and any(getattr(t, 'id', '') == 'TABLES' for t in node.targets):
            return [(a, b) for a, b in ast.literal_eval(node.value)]
    sys.exit('TABLES not found in gen_table_bubbles.py')


def load(work):
    cols = {}
    for line in open(os.path.join(work, 'cols.tsv'), encoding='utf-8'):
        p = line.rstrip('\n').split('\t')
        if len(p) < 8 or p[0] in SKIP_TABLES:
            continue
        cols.setdefault(p[0], []).append(dict(name=p[2], ctype=p[3], dtype=p[4].lower(), nullable=p[5] == 'YES',
                                              key=p[6], extra=p[7]))
    fks = {}
    for line in open(os.path.join(work, 'fks.tsv'), encoding='utf-8'):
        p = line.rstrip('\n').split('\t')
        if len(p) >= 3:
            fks[(p[0], p[1])] = p[2]
    samples = {}
    for line in open(os.path.join(work, 'samples.tsv'), encoding='utf-8'):
        p = line.rstrip('\n').split('\t', 2)
        if len(p) == 3 and p[2].startswith('V:'):
            samples[(p[0], p[1])] = unescape_batch(p[2][2:])
    return cols, fks, samples


def unescape_batch(s):
    # mysql --batch escapes \t \n \\ ; keep the text readable on one line
    return s.replace('\\t', ' ').replace('\\n', ' ').replace('\\\\', '\\').strip()


# ------------------------------------------------------------------------------- column properties
def size_of(c):
    ct, dt = c['ctype'].lower(), c['dtype']
    if dt == 'enum' or dt == 'set':
        return ''
    m = re.match(r'^\w+\((\d+(?:,\d+)?)\)', ct)
    if m:
        return m.group(1)
    if dt in INT_WIDTH:
        return str('%d' % (INT_WIDTH_UNSIGNED if 'unsigned' in ct else INT_WIDTH)[dt])
    return ''


def attributes(t, c, fks):
    a = []
    if c['key'] == 'PRI':
        a.append('PK')
    a.append('AN' if c['nullable'] else 'NN')
    # order in the picture: PK, NN, AI, FK, AN  -> NN/AN is one or the other, placed by position below
    ai = 'auto_increment' in c['extra']
    is_fk = (t, c['name']) in fks or (t, c['name']) in SOFT_FK
    out = []
    if 'PK' in a:
        out.append('PK')
    if not c['nullable']:
        out.append('NN')
    if ai:
        out.append('AI')
    if is_fk:
        out.append('FK')
    if c['nullable']:
        out.append('AN')
    return ', '.join(out)


def enum_values(ctype):
    return re.findall(r"'((?:[^']|'')*)'", ctype[ctype.index('('):])


def invented(t, c):
    """Obviously fictional value of the right type, for a column that is NULL in every demo row."""
    dt, name = c['dtype'], c['name'].lower()
    sz = size_of(c)
    if dt in ('date',):
        return '2026-10-10'
    if dt in ('datetime', 'timestamp'):
        return '2026-10-10 09:30:00'
    if dt == 'time':
        return '09:30:00'
    if dt in ('decimal', 'float', 'double'):
        return '250.00' if dt == 'decimal' else '1.5'
    if dt in ('tinyint', 'smallint', 'mediumint', 'int', 'bigint'):
        return '1'
    if dt == 'year':
        return '2026'
    # text-like: pick by column name, then fit the declared length
    table = [
        ('email', 'sample.user@example.com'), ('contact', '09170000000'), ('phone', '09170000000'),
        ('mobile', '09170000000'), ('password', 'n/a'), ('first_name', 'Juan'), ('middle_name', 'Santos'),
        ('last_name', 'Dela Cruz'), ('full_name', 'Juan Santos Dela Cruz'), ('name', 'Sample Name'),
        ('address', '123 Sample St., Sample City'), ('remarks', 'Sample remarks'), ('reason', 'Sample reason'),
        ('note', 'Sample note'), ('purpose', 'Employment'), ('status', 'Pending'), ('username', 'sampleuser'),
        ('_no', 'SAMPLE-0001'), ('number', 'SAMPLE-0001'), ('code', 'SAMPLE'), ('title', 'Sample title'),
        ('position', 'Sample position'), ('relationship', 'Sample relationship'), ('place', 'Sample Place'),
        ('province', 'Sample Province'), ('municipality', 'Sample City'), ('barangay', 'Sample Barangay'),
        ('citizenship', 'Filipino'), ('religion', 'Sample religion'), ('occupation', 'Sample occupation'),
        ('type', 'Sample type'), ('label', 'Sample label'), ('description', 'Sample description'),
        ('text', 'Sample text'), ('url', 'https://example.com/sample'), ('path', 'C:\\Sample\\file.txt'),
    ]
    val = 'Sample text'
    for key, v in table:
        if key in name:
            val = v
            break
    if (t, c['name']) == ('server_beacon', 'machine_name'):
        val = 'OFFICE-PC-01'
    if (t, c['name']) == ('server_beacon', 'ip_list'):
        val = '192.168.0.10'
    if (t, c['name']) == ('ocr_batch', 'raw_text'):
        val = 'REPUBLIC OF THE PHILIPPINES  Certificate of Live Birth  (sample OCR text)'
    if sz.isdigit() and dt in ('varchar', 'char') and len(val) > int(sz):
        val = val[:int(sz)]
    return val


def blob_example(name):
    return '(image)' if re.search(r'image|photo|scan|logo|stamp|picture|signature|letter|banner|attachment|asset', name) else '(binary)'


def shorten(s, n=90):
    s = re.sub(r'\s+', ' ', s)
    return s if len(s) <= n else s[:n - 3].rstrip() + '...'


def example(t, c, samples):
    name, dt = c['name'], c['dtype']
    n = name.lower()
    if 'blob' in dt or dt == 'binary' or dt == 'varbinary':
        return blob_example(n), False
    if 'password' in n or n.endswith('_hash') or n == 'hash':
        return '(hash, not shown)', False
    if dt in ('enum', 'set'):
        vals = enum_values(c['ctype'])
        shown = ', '.join(vals[:6]) + (', ...' if len(vals) > 6 else '')
        return 'one of: ' + shown, False
    raw = None if (t, name) in NEVER_FROM_ROW else samples.get((t, name))
    fake = raw is None
    v = raw if raw is not None else invented(t, c)
    if dt in ('varchar', 'char', 'text', 'tinytext', 'mediumtext', 'longtext', 'json'):
        return '"' + shorten(v) + '"', fake
    return v, fake


# ------------------------------------------------------------------------------------- docx writing
def rpr(bold=False, color=PINK, size=22):
    return ('<w:rPr><w:rFonts w:ascii="{f}" w:hAnsi="{f}" w:cs="{f}" w:eastAsia="{f}"/>{b}<w:color w:val="{c}"/>'
            '<w:sz w:val="{s}"/><w:szCs w:val="{s}"/></w:rPr>').format(
        f=FONT, b='<w:b/><w:bCs/>' if bold else '', c=color, s=size)


def para(text, bold=False, color=PINK, size=22, keep_next=False, space_after=0, jc=None, style=None, space_before=0):
    ppr = '<w:pPr>'
    if style:
        ppr += '<w:pStyle w:val="%s"/>' % style
    if keep_next:
        ppr += '<w:keepNext/>'
    ppr += '<w:spacing w:before="%d" w:after="%d" w:line="240" w:lineRule="auto"/>' % (space_before, space_after)
    if jc:
        ppr += '<w:jc w:val="%s"/>' % jc
    ppr += '</w:pPr>'
    if LB in text:
        runs = ''.join('<w:r>%s%s<w:t xml:space="preserve">%s</w:t></w:r>' % (rpr(bold, color, size), '<w:br/>' if i else '', escape(part))
                       for i, part in enumerate(text.split(LB)))
        return '<w:p>%s%s</w:p>' % (ppr, runs)
    return '<w:p>%s<w:r>%s<w:t xml:space="preserve">%s</w:t></w:r></w:p>' % (ppr, rpr(bold, color, size), escape(text))


def cell(width, text, bold=False, keep_next=False):
    return ('<w:tc><w:tcPr><w:tcW w:w="%d" w:type="dxa"/></w:tcPr>%s</w:tc>'
            % (width, para(text, bold=bold, keep_next=keep_next)))


_FONT_BOLD = None
LB = chr(11)          # vertical tab = marker for a line break inside a cell


def breakable(name, width_dxa=WIDTHS[0] - 180):
    """Field names are one long word: Word would cut them mid-word at the cell edge. A name wider than the
    cell is wrapped after an underscore instead (line break marker LB, drawn as <w:br/>); names that fit
    stay byte-exact."""
    global _FONT_BOLD
    try:
        if _FONT_BOLD is None:
            from PIL import ImageFont
            _FONT_BOLD = ImageFont.truetype(os.path.join(os.environ.get('WINDIR', r'C:\Windows'), 'Fonts', 'timesbd.ttf'), 11)  # 11 px == 11 pt
        limit = width_dxa / 20.0 - 3
        if _FONT_BOLD.getlength(name) <= limit:
            return name
        lines, cur = [], ''
        for tok in re.findall(r'[^_]+_?|_', name):
            if cur and _FONT_BOLD.getlength(cur + tok) > limit:
                lines.append(cur)
                cur = tok
            else:
                cur += tok
        lines.append(cur)
        return LB.join(lines)
    except Exception:
        return name


def row(vals, header=False, bold_first=False, widths=WIDTHS, keep_next=False):
    trpr = '<w:trPr><w:cantSplit/>%s</w:trPr>' % ('<w:tblHeader/>' if header else '')
    cells = ''.join(cell(w, v, bold=header or (bold_first and i == 0), keep_next=keep_next)
                    for i, (w, v) in enumerate(zip(widths, vals)))
    return '<w:tr>%s%s</w:tr>' % (trpr, cells)


def table(rows_xml, widths=WIDTHS):
    b = ''.join('<w:%s w:val="single" w:sz="8" w:space="0" w:color="000000"/>' % s
                for s in ('top', 'left', 'bottom', 'right', 'insideH', 'insideV'))
    return ('<w:tbl><w:tblPr><w:tblW w:w="%d" w:type="dxa"/><w:tblBorders>%s</w:tblBorders>'
            '<w:tblLayout w:type="fixed"/><w:tblCellMar><w:top w:w="40" w:type="dxa"/><w:left w:w="90" w:type="dxa"/>'
            '<w:bottom w:w="40" w:type="dxa"/><w:right w:w="90" w:type="dxa"/></w:tblCellMar></w:tblPr>'
            '<w:tblGrid>%s</w:tblGrid>%s</w:tbl>') % (
        sum(widths), b, ''.join('<w:gridCol w:w="%d"/>' % w for w in widths), ''.join(rows_xml))


STYLES = '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
<w:docDefaults><w:rPrDefault><w:rPr><w:rFonts w:ascii="{f}" w:hAnsi="{f}" w:cs="{f}" w:eastAsia="{f}"/><w:sz w:val="22"/><w:szCs w:val="22"/><w:lang w:val="en-US"/></w:rPr></w:rPrDefault>
<w:pPrDefault><w:pPr><w:spacing w:after="0" w:line="240" w:lineRule="auto"/></w:pPr></w:pPrDefault></w:docDefaults>
<w:style w:type="paragraph" w:default="1" w:styleId="Normal"><w:name w:val="Normal"/><w:qFormat/></w:style>
<w:style w:type="paragraph" w:styleId="Heading1"><w:name w:val="heading 1"/><w:basedOn w:val="Normal"/><w:next w:val="Normal"/><w:qFormat/>
<w:pPr><w:keepNext/><w:spacing w:before="0" w:after="160"/><w:outlineLvl w:val="0"/></w:pPr><w:rPr><w:b/><w:bCs/><w:sz w:val="32"/><w:szCs w:val="32"/></w:rPr></w:style>
<w:style w:type="paragraph" w:styleId="Heading2"><w:name w:val="heading 2"/><w:basedOn w:val="Normal"/><w:next w:val="Normal"/><w:qFormat/>
<w:pPr><w:keepNext/><w:spacing w:before="320" w:after="40"/><w:outlineLvl w:val="1"/></w:pPr><w:rPr><w:b/><w:bCs/><w:sz w:val="24"/><w:szCs w:val="24"/></w:rPr></w:style>
<w:style w:type="paragraph" w:styleId="Footer"><w:name w:val="footer"/><w:basedOn w:val="Normal"/></w:style>
</w:styles>'''.format(f=FONT)

FOOTER = ('<?xml version="1.0" encoding="UTF-8" standalone="yes"?><w:ftr xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">'
          '<w:p><w:pPr><w:pStyle w:val="Footer"/><w:jc w:val="center"/></w:pPr>'
          '<w:r><w:rPr><w:sz w:val="18"/></w:rPr><w:t xml:space="preserve">CROMS data dictionary - page </w:t></w:r>'
          '<w:r><w:rPr><w:sz w:val="18"/></w:rPr><w:fldChar w:fldCharType="begin"/></w:r>'
          '<w:r><w:rPr><w:sz w:val="18"/></w:rPr><w:instrText xml:space="preserve"> PAGE </w:instrText></w:r>'
          '<w:r><w:rPr><w:sz w:val="18"/></w:rPr><w:fldChar w:fldCharType="separate"/></w:r>'
          '<w:r><w:rPr><w:sz w:val="18"/></w:rPr><w:t>1</w:t></w:r>'
          '<w:r><w:rPr><w:sz w:val="18"/></w:rPr><w:fldChar w:fldCharType="end"/></w:r></w:p></w:ftr>')


def write_docx(path, body):
    ns = 'xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"'
    doc = ('<?xml version="1.0" encoding="UTF-8" standalone="yes"?><w:document %s><w:body>%s'
           '<w:sectPr><w:footerReference w:type="default" r:id="rId2"/><w:pgSz w:w="12240" w:h="15840"/>'
           '<w:pgMar w:top="1440" w:right="1440" w:bottom="1440" w:left="1440" w:header="720" w:footer="720" w:gutter="0"/></w:sectPr>'
           '</w:body></w:document>') % (ns, body)
    ct = ('<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">'
          '<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/>'
          '<Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>'
          '<Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/>'
          '<Override PartName="/word/footer1.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.footer+xml"/>'
          '<Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/></Types>')
    rels = ('<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">'
            '<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>'
            '<Relationship Id="rId2" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="docProps/core.xml"/></Relationships>')
    drels = ('<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">'
             '<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>'
             '<Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/footer" Target="footer1.xml"/></Relationships>')
    now = datetime.datetime.now(datetime.timezone.utc).strftime('%Y-%m-%dT%H:%M:%SZ')
    core = ('<?xml version="1.0" encoding="UTF-8" standalone="yes"?><cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties" '
            'xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:dcterms="http://purl.org/dc/terms/" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">'
            '<dc:title>Data Dictionary - CROMS database</dc:title><dc:creator>CROMS</dc:creator>'
            '<dcterms:created xsi:type="dcterms:W3CDTF">%s</dcterms:created></cp:coreProperties>') % now
    with zipfile.ZipFile(path, 'w', zipfile.ZIP_DEFLATED) as z:
        z.writestr('[Content_Types].xml', ct)
        z.writestr('_rels/.rels', rels)
        z.writestr('word/document.xml', doc)
        z.writestr('word/styles.xml', STYLES)
        z.writestr('word/footer1.xml', FOOTER)
        z.writestr('word/_rels/document.xml.rels', drels)
        z.writestr('docProps/core.xml', core)


# ------------------------------------------------------------------------------------------- build
def build(work, out):
    order = load_tables_order()
    cols, fks, samples = load(work)
    listed = [t for t, _ in order]
    missing = sorted(set(cols) - set(listed))
    extra = sorted(set(listed) - set(cols))
    if missing or extra:
        sys.exit('table list mismatch: not in TABLES=%s, in TABLES but not in DB=%s' % (missing, extra))

    soft_found, invented_cols, body = [], [], []
    counts = {}
    body.append(para('Data Dictionary - CROMS database', size=32, color='000000', bold=True, style='Heading1'))
    body.append(para(
        'This dictionary describes every table of the CROMS database (Civil Registry Operations Management System, '
        'LGU Penablanca LCRO): each table lists its fields with the data type, the size, the attributes and one example value. '
        'The structure was read from the database itself; the example values come from the fictional sample data of the demo '
        'database, never from real civil registry records. Tables a person types into come first, then the tables the system '
        'writes by itself.', color='000000', space_after=160))
    body.append(para('Attribute codes', bold=True, color='000000', keep_next=True, space_after=60))
    legend = [('PK', 'Primary key'), ('NN', 'Not null (a value is required)'), ('AI', 'Auto increment'),
              ('FK', 'Foreign key (points at the key of another table)'), ('AN', 'Allows null (may be left empty)')]
    lw = [1200, 8160]
    body.append(table([row(['Code', 'Meaning'], header=True, widths=lw)] +
                      [row([a, b], bold_first=True, widths=lw) for a, b in legend], lw))
    body.append(para('', color='000000', space_after=80))
    body.append(para('Size is the length of a text field, or the digits of a number (decimal: digits, decimals). '
                     'Date, time, text and image fields have no size; a list field shows its allowed values in the Example column.',
                     color='000000', size=20, space_after=0))

    for n, (t, sentence) in enumerate(order, 1):
        body.append(para('Table %d. %s' % (n, t), bold=True, color='000000', keep_next=True, space_before=300, space_after=40, size=24))
        body.append(para(describe(t, sentence), color='000000', keep_next=True, space_after=100))
        rows_xml = [row(['Field', 'Type', 'Size', 'Attribute', 'Example'], header=True, keep_next=True)]
        for c in cols[t]:
            ex, fake = example(t, c, samples)
            if fake:
                invented_cols.append('%s.%s' % (t, c['name']))
            if (t, c['name']) in SOFT_FK:
                soft_found.append('%s.%s -> %s' % (t, c['name'], SOFT_FK[(t, c['name'])]))
            rows_xml.append(row([breakable(c['name']), c['dtype'], size_of(c), attributes(t, c, fks), ex], bold_first=True))
        body.append(table(rows_xml))
        counts[t] = len(cols[t])

    write_docx(out, ''.join(body))
    verify(out, counts, order)
    return counts, soft_found, invented_cols


def describe(t, sentence):
    """One plain sentence from the screen / purpose text of gen_table_bubbles.py."""
    s = sentence.strip()
    if s.startswith('System-written:'):
        s = s[len('System-written:'):].strip()
        s = s[0].upper() + s[1:]
        return 'Filled in by the system, not typed in. ' + s.rstrip('.') + '.'
    if ' > ' in s or s.startswith(('Kiosk', 'Sign-in')):
        return 'Filled in on: %s.' % s.rstrip('.')
    return s.rstrip('.') + '.'


def verify(out, counts, order):
    import xml.etree.ElementTree as ET
    W = '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}'
    root = ET.fromstring(zipfile.ZipFile(out).read('word/document.xml'))
    tbls = root.findall('.//%stbl' % W)
    data_tbls = tbls[1:]                     # first = legend
    assert len(data_tbls) == len(order), (len(data_tbls), len(order))
    total = 0
    for tb, (t, _) in zip(data_tbls, order):
        rws = tb.findall('%str' % W)
        assert len(rws) - 1 == counts[t], (t, len(rws) - 1, counts[t])
        # every row has exactly 5 cells
        assert all(len(r.findall('%stc' % W)) == 5 for r in rws), t
        total += len(rws) - 1
        print('  %-30s %3d columns' % (t, counts[t]))
    print('TABLES %d  COLUMNS %d  (every Word table row count == information_schema column count)' % (len(data_tbls), total))


if __name__ == '__main__':
    if len(sys.argv) < 3:
        sys.exit(__doc__)
    cmd, work = sys.argv[1], sys.argv[2]
    if cmd in ('export', 'all'):
        export(work)
    if cmd in ('build', 'all'):
        out = sys.argv[3]
        counts, soft, inv = build(work, out)
        print('\nSOFT FOREIGN KEYS (%d):' % len(soft))
        print('\n'.join('  ' + s for s in soft))
        print('\nCOLUMNS WITH NO DEMO VALUE, EXAMPLE INVENTED (%d):' % len(inv))
        print('\n'.join('  ' + s for s in inv))
