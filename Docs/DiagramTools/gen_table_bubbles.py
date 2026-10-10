# CROMS data-entry table "bubble" diagrams -> .drawio (one page per table) + PIL preview PNGs.
#
#   python gen_table_bubbles.py --columns columns.tsv --out ..\CROMS_Data_Entry_Table_Diagrams.drawio --png out
#
# columns.tsv = tab separated: table_name, ordinal_position, column_name, column_type, column_key
# (made by export_columns.ps1 from information_schema of croms_demo; no login is stored here).
#
# Look (same as the reference picture): one big ringed circle = the table name, one small coloured-ring circle
# per column on a single ring around it, a short stub between each small circle and the big one.
# Nothing else on the page. The big circle grows with the number of columns so the small circles never
# touch; checked in code: centre distance >= d + 14, text inside its circle, page border clear.
import argparse, html, json, math, os, re, sys
import xml.etree.ElementTree as ET
from PIL import Image, ImageDraw, ImageFont

FONT_R = r'C:\Windows\Fonts\arial.ttf'
FONT_B = r'C:\Windows\Fonts\arialbd.ttf'

RING = ['#F39C12', '#8E44AD', '#2E86DE', '#27AE60', '#16A085', '#E74C3C', '#E5B800']   # orange purple blue green teal red yellow
INNER = '#EEF0F2'
NAVY = '#2C3E50'
LINE = '#9AA0A6'
TXT = '#222222'
RINGW = 5
CLEAR = 14            # min clear space between any two small circles
GAP = 34              # length of the stub between a small circle and the big circle

# ---- what is diagrammed: (table, screen sentence) in page order -------------------------------
TABLES = [
    ('births', 'Civil Registration > Birth Registration (Municipal Form 102); also saved by Document Processing > Commit'),
    ('deaths', 'Civil Registration > Death Registration (Municipal Form 103)'),
    ('marriage_licenses', 'Civil Registration > Marriage Registration > Applications & Licenses (Municipal Form 90)'),
    ('marriages', 'Civil Registration > Marriage Registration > Form 97 entry'),
    ('petitions', 'Petitions & Cases > Case Tracking'),
    ('certificate_requests', 'Transactions > Certificate Request'),
    ('transactions', 'Transactions > Certificate Request (the transaction row it opens)'),
    ('psa_copy_requests', 'Transactions > PSA Copies (BREQS); also the kiosk PSA Copy step'),
    ('payments', 'Transactions > Fees & Payments (walk-in payment, Official Receipt)'),
    ('payment_items', 'Transactions > Fees & Payments (one line per fee on a receipt)'),
    ('releases', 'Transactions > Release & Claim'),
    ('claimant_id_uploads', 'Transactions > Release & Claim (claimant ID and authorization letter)'),
    ('queue_tickets', 'Kiosk (client touch screen) and Transactions > Queue Management'),
    ('queue_ticket_services', 'Kiosk > Select Services (one row per service picked)'),
    ('kiosk_ctc_intake', 'Kiosk > Certified True Copy request step'),
    ('ocr_batch', 'Records & Documents > Document Processing'),
    ('marriage_copies', 'Civil Registration > Marriage Registration > Marriage record (copies)'),
    ('psa_transmittal_batches', 'Civil Registration > Marriage Registration > PSA Transmittal'),
    ('users', 'System > Settings > Users & Access'),
    ('staff_biodata', 'System > Settings > Users & Access > Staff Biodata'),
    ('windows', 'System > Settings > Window Management'),
    ('window_service_assignments', 'Sign-in window picker (services a window handles)'),
    ('fees', 'Transactions > Fees & Payments > Fee schedule'),
    ('office_profile', 'System > Settings > Forms & Templates > Office branding'),
    ('provinces', 'System > Settings > Master Files'),
    ('municipalities', 'System > Settings > Master Files'),
    ('barangays', 'System > Settings > Master Files'),
    ('hospitals', 'System > Settings > Master Files'),
    ('churches', 'System > Settings > Master Files'),
    ('occupations', 'System > Settings > Master Files'),
    ('religions', 'System > Settings > Master Files'),
    ('nationalities', 'System > Settings > Master Files'),
    ('relationships', 'System > Settings > Master Files'),
    ('causes_of_death', 'System > Settings > Master Files'),
    ('countries', 'System > Settings > Master Files'),
    ('birth_orders', 'System > Settings > Master Files'),
    ('type_of_births', 'System > Settings > Master Files'),
    ('civil_statuses', 'System > Settings > Master Files'),
    ('residences', 'System > Settings > Master Files'),
]


def load_columns(path):
    cols = {}
    for line in open(path, encoding='utf-8'):
        p = line.rstrip('\n').split('\t')
        if len(p) < 5:
            continue
        cols.setdefault(p[0], []).append(dict(name=p[2], type=p[3], key=p[4]))
    return cols


def font(sz, bold=False):
    return ImageFont.truetype(FONT_B if bold else FONT_R, sz)


# ---- text wrapping / fitting ------------------------------------------------------------------
def wrap(name, f, maxw):
    toks = re.findall(r'[^_]+_?', name) or [name]
    lines, cur = [], ''
    for t in toks:
        trial = cur + t
        if cur and f.getlength(trial) > maxw:
            lines.append(cur)
            cur = t
        else:
            cur = trial
    lines.append(cur)
    out = []
    for ln in lines:                       # hard split a single over-long token
        while f.getlength(ln) > maxw and len(ln) > 4:
            k = len(ln)
            while k > 3 and f.getlength(ln[:k]) > maxw:
                k -= 1
            out.append(ln[:k]); ln = ln[k:]
        out.append(ln)
    return out


def block(lines, f, fs):
    w = max(f.getlength(l) for l in lines)
    h = len(lines) * fs * 1.2
    return w, h


def fits(lines, f, fs, d, pad=7):
    w, h = block(lines, f, fs)
    return math.hypot(w / 2, h / 2) <= d / 2 - pad


def font_options(names):
    """Yield (fs, d) from the largest readable font down: a circle diameter <= 130 px, else smaller text."""
    for fs in (13, 12, 11, 10, 9):
        f = font(fs)
        for d in range(76, 262, 2):
            if all(fits(wrap(n, f, d * 0.82), f, fs, d) for n in names):
                if d <= 130 or fs == 9:
                    yield fs, d
                break


# ---- geometry ----------------------------------------------------------------------------------
def build(table, cols):
    names = [c['name'] for c in cols]
    n = len(names)
    opts = list(font_options(names))
    fs, d = opts[0]
    if n > 60:                                 # a one-ring diagram of 80-128 circles: smaller text keeps the page manageable
        fs, d = next((o for o in opts if o[0] <= 10), opts[-1])
    f = font(fs)
    # ring radius from the circumference, so the small circles never touch
    R_ring = (d + CLEAR) / (2 * math.sin(math.pi / n)) if n >= 3 else 0
    D0 = max(300, 2 * (R_ring - d / 2 - GAP))
    cfs = int(max(30, min(150, D0 * 0.11)))
    while True:                                # table name: bold, wrapped on '_', inside the white face
        cf = font(cfs, True)
        clines = wrap(table, cf, D0 * 0.62)
        w, h = block(clines, cf, cfs)
        if math.hypot(w / 2, h / 2) <= D0 / 2 * 0.80 or cfs <= 26:
            break
        cfs -= 2
    R = D0 / 2 + GAP + d / 2                   # centre of the small circles
    if n > 1 and R < R_ring:
        R = R_ring
        D0 = 2 * (R - d / 2 - GAP)
    margin = 60
    size = int(math.ceil((2 * (R + d / 2 + margin)) / 10.0) * 10)
    ox = oy = size / 2.0
    circles = []
    for i, c in enumerate(cols):
        a = -math.pi / 2 + 2 * math.pi * i / n
        circles.append(dict(i=i, name=c['name'], x=ox + R * math.cos(a), y=oy + R * math.sin(a),
                            lines=wrap(c['name'], f, d * 0.82), color=RING[i % len(RING)]))
    return dict(table=table, n=n, fs=fs, d=d, D0=D0, cfs=cfs, clines=clines, R=R, size=size, cx=ox, cy=oy, circles=circles)


def audit(g):
    d, cx, cy, D0 = g['d'], g['cx'], g['cy'], g['D0']
    C = g['circles']; n = len(C)
    res = dict(circle_overlap=0, text_outside=0, border_touch=0, hub_touch=0)
    f = font(g['fs'])
    for i in range(n):
        a = C[i]
        for j in range(i + 1, n):
            if math.hypot(a['x'] - C[j]['x'], a['y'] - C[j]['y']) < d + CLEAR - 0.01:
                res['circle_overlap'] += 1
        if not fits(a['lines'], f, g['fs'], d, pad=5):
            res['text_outside'] += 1
        if math.hypot(a['x'] - cx, a['y'] - cy) - d / 2 - D0 / 2 < GAP - 1:
            res['hub_touch'] += 1
        if min(a['x'], a['y'], g['size'] - a['x'], g['size'] - a['y']) < d / 2 + 24:
            res['border_touch'] += 1
    cf = font(g['cfs'], True)
    w, h = block(g['clines'], cf, g['cfs'])
    if math.hypot(w / 2, h / 2) > D0 / 2 - 20:
        res['text_outside'] += 1
    return res


# ---- draw.io XML -------------------------------------------------------------------------------
def esc(lines):
    return '&lt;br&gt;'.join(html.escape(l, quote=False) for l in lines)


def diagram_xml(g):
    out = []
    t = g['table']; S = g['size']; d = g['d']; D0 = g['D0']
    out.append('<diagram name="%s" id="d_%s">' % (html.escape(t), html.escape(t)))
    out.append('<mxGraphModel dx="1200" dy="800" grid="0" gridSize="10" guides="1" tooltips="1" connect="1" arrows="1" '
               'fold="1" page="1" pageScale="1" pageWidth="%d" pageHeight="%d" math="0" shadow="0"><root>' % (S, S))
    out.append('<mxCell id="0"/><mxCell id="1" parent="0"/>')
    for c in g['circles']:                     # stubs first, so they sit behind the circles
        out.append('<mxCell id="e%d" style="endArrow=none;startArrow=none;html=1;strokeColor=#5B6B7B;strokeWidth=5;" edge="1" '
                   'parent="1" source="col%d" target="hub"><mxGeometry relative="1" as="geometry"/></mxCell>' % (c['i'], c['i']))

    def ell(i, dia, style, val=''):
        return ('<mxCell id="%s" value="%s" style="ellipse;whiteSpace=nowrap;html=1;aspect=fixed;%s" vertex="1" parent="1">'
                '<mxGeometry x="%.1f" y="%.1f" width="%.1f" height="%.1f" as="geometry"/></mxCell>'
                % (i, val, style, g['cx'] - dia / 2, g['cy'] - dia / 2, dia, dia))
    # big circle: dark ring, pale ring, white face (as in the reference picture)
    out.append(ell('hub', D0, 'fillColor=%s;strokeColor=#1F2D3D;strokeWidth=2;' % NAVY))
    out.append(ell('hubring', D0 * (1 - 0.09), 'fillColor=#DDE5EC;strokeColor=none;'))
    out.append(ell('hubface', D0 * (1 - 0.19),
                   'fontFamily=Arial;fontSize=%d;fontStyle=1;fontColor=#000000;fillColor=#FFFFFF;gradientColor=#ECECEC;'
                   'gradientDirection=radial;strokeColor=#C9D1D8;strokeWidth=2;align=center;verticalAlign=middle;' % g['cfs'],
                   esc(g['clines'])))
    for c in g['circles']:
        st = ('fontFamily=Arial;fontSize=%d;align=center;verticalAlign=middle;spacing=0;strokeWidth=%d;fontColor=#222222;'
              'fillColor=#FFFFFF;gradientColor=#E3E3E3;gradientDirection=radial;strokeColor=%s;' % (g['fs'], RINGW, c['color']))
        out.append('<mxCell id="col%d" value="%s" style="ellipse;whiteSpace=nowrap;html=1;aspect=fixed;%s" vertex="1" parent="1">'
                   '<mxGeometry x="%.1f" y="%.1f" width="%d" height="%d" as="geometry"/></mxCell>'
                   % (c['i'], esc(c['lines']), st, c['x'] - d / 2, c['y'] - d / 2, d, d))
    out.append('</root></mxGraphModel></diagram>')
    return ''.join(out)


# ---- PNG preview -------------------------------------------------------------------------------
def render_png(g, path):
    S = g['size']
    sc = 2 if S <= 2800 else 1
    im = Image.new('RGB', (S * sc, S * sc), 'white')
    dr = ImageDraw.Draw(im)
    cx, cy, D0, d = g['cx'] * sc, g['cy'] * sc, g['D0'] * sc, g['d'] * sc
    for c in g['circles']:
        dr.line([(c['x'] * sc, c['y'] * sc), (cx, cy)], fill='#5B6B7B', width=5 * sc)

    def disc(r, fill):
        dr.ellipse([cx - r, cy - r, cx + r, cy + r], fill=fill)
    disc(D0 / 2, NAVY); disc(D0 / 2 * (1 - 0.09), '#DDE5EC'); disc(D0 / 2 * (1 - 0.19), '#F6F6F6')
    cf = font(int(g['cfs'] * sc), True)
    lh = g['cfs'] * sc * 1.2
    y0 = cy - lh * len(g['clines']) / 2
    for k, ln in enumerate(g['clines']):
        dr.text((cx - cf.getlength(ln) / 2, y0 + k * lh), ln, font=cf, fill='black')
    f = font(g['fs'] * sc)
    for c in g['circles']:
        x, y = c['x'] * sc, c['y'] * sc
        r = d / 2; rw = RINGW * sc
        dr.ellipse([x - r - rw / 2, y - r - rw / 2, x + r + rw / 2, y + r + rw / 2], fill=c['color'])
        dr.ellipse([x - r + rw / 2, y - r + rw / 2, x + r - rw / 2, y + r - rw / 2], fill='#F4F4F4')
        lh2 = g['fs'] * sc * 1.2
        yy = y - lh2 * len(c['lines']) / 2
        for k, ln in enumerate(c['lines']):
            dr.text((x - f.getlength(ln) / 2, yy + k * lh2), ln, font=f, fill='#222222')
    if sc > 1:
        im = im.resize((S, S), Image.LANCZOS)
    im.save(path)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--columns', required=True)
    ap.add_argument('--out', required=True)
    ap.add_argument('--png', default='')
    ap.add_argument('--only', default='')
    ap.add_argument('--meta', default='')
    a = ap.parse_args()
    cols = load_columns(a.columns)
    only = set(x for x in a.only.split(',') if x)
    root = ['<?xml version="1.0" encoding="UTF-8"?><mxfile host="app.diagrams.net" agent="gen_table_bubbles.py" version="22.0.0">']
    total_cols, bad, meta = 0, 0, []
    for t, screen in TABLES:
        if only and t not in only:
            continue
        if t not in cols:
            print('MISSING TABLE', t); bad += 1; continue
        g = build(t, cols[t])
        iss = audit(g)
        cnt = sum(iss.values())
        bad += cnt
        total_cols += g['n']
        print('%-28s cols=%3d d=%3d fs=%2d hub=%4d page=%4dx%-4d overlaps=%d %s' %
              (t, g['n'], g['d'], g['fs'], g['D0'], g['size'], g['size'], cnt, '' if not cnt else iss))
        root.append(diagram_xml(g))
        meta.append(dict(table=t, screen=screen, columns=g['n'], rings=1, page=g['size']))
        if a.png:
            os.makedirs(a.png, exist_ok=True)
            render_png(g, os.path.join(a.png, t + '.png'))
    root.append('</mxfile>')
    xml = ''.join(root)
    open(a.out, 'w', encoding='utf-8').write(xml)
    if a.meta:
        json.dump(meta, open(a.meta, 'w', encoding='utf-8'), indent=1)
    # validate: parses, unique ids per page, column counts
    tree = ET.fromstring(xml.encode('utf-8'))
    pages = tree.findall('diagram')
    for p in pages:
        ids = [c.get('id') for c in p.iter('mxCell')]
        assert len(ids) == len(set(ids)), 'duplicate ids in ' + p.get('name')
        ncol = sum(1 for c in p.iter('mxCell') if (c.get('id') or '').startswith('col'))
        assert ncol == len(cols[p.get('name')]), 'column count mismatch ' + p.get('name')
    print('pages=%d columns=%d total_issues=%d' % (len(pages), total_cols, bad))
    sys.exit(1 if bad else 0)


if __name__ == '__main__':
    main()
