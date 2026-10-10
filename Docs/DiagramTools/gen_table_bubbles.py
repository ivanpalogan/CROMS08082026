# CROMS data-entry table "bubble" diagrams -> .drawio (one page per table) + PIL preview PNGs.
#
#   python gen_table_bubbles.py --columns columns.tsv --out ..\CROMS_Data_Entry_Table_Diagrams.drawio --png out
#
# columns.tsv = tab separated: table_name, ordinal_position, column_name, column_type, column_key
# (made by export_columns.ps1 from information_schema of croms_demo; no login is stored here).
#
# Layout: big ringed circle = table, one small circle per column on K interleaved concentric rings
# (a, b, c, a, b, c ... around the circle, so an outer-ring circle sits between two inner ones).
# A solver grows the first-ring radius until ALL of these hold (checked numerically, not by eye):
#   - no two circles overlap (centre distance >= d + 12),
#   - no connector line (circle centre -> table centre) passes within d/2 + 7 of another circle,
#   - text fits inside its circle,
#   - legend box and page border clear of every circle.
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
CLEAR = 12            # min clear space between any two circles
LINE_PAD = 7          # min clear space between a connector line and a foreign circle

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
        for d in range(104, 262, 2):
            if all(fits(wrap(n, f, d * 0.82), f, fs, d) for n in names):
                if d <= 130 or fs == 9:
                    yield fs, d
                break


# ---- geometry / solver ------------------------------------------------------------------------
def seg_dist(px, py, ax, ay, bx, by):
    dx, dy = bx - ax, by - ay
    L = dx * dx + dy * dy
    t = 0 if L == 0 else max(0, min(1, ((px - ax) * dx + (py - ay) * dy) / L))
    return math.hypot(px - (ax + t * dx), py - (ay + t * dy))


def place(n, K, R1, pitch, off=-math.pi / 2):
    pts = []
    for i in range(n):
        a = off + 2 * math.pi * i / n
        R = R1 + (i % K) * pitch
        pts.append((R * math.cos(a), R * math.sin(a), R))
    return pts


def violations(pts, d, stop_first=False):
    """Return counts of (circle overlaps, line-through-circle) for circles at pts (origin = table centre)."""
    ov = li = 0
    n = len(pts)
    for i in range(n):
        xi, yi, _ = pts[i]
        for j in range(n):
            if i == j:
                continue
            xj, yj, _ = pts[j]
            if j > i and math.hypot(xi - xj, yi - yj) < d + CLEAR:
                ov += 1
                if stop_first: return ov, li
            if seg_dist(xj, yj, 0, 0, xi, yi) < d / 2 + LINE_PAD:
                li += 1
                if stop_first: return ov, li
    return ov, li


def solve(n, d, Rc):
    pitch = d + 28
    lb = Rc + d / 2 + 70                       # at least 70 px of visible spoke
    best = None
    for K in range(1 if n <= 16 else 2, 5):
        if K > n:
            break
        step = 2 * math.pi / n
        a = (d / 2 + LINE_PAD) / math.sin(min(step, math.pi / 2)) if n > 1 else 0
        c = (d + CLEAR) / (2 * math.sin(min(K * step / 2, math.pi / 2))) if n > 1 else 0
        R1 = max(lb, 0.92 * max(a, c))
        while R1 < 6000:
            pts = place(n, K, R1, pitch)
            if violations(pts, d, True) == (0, 0):
                break
            R1 += 6
        ext = R1 + (K - 1) * pitch + d / 2
        if best is None or ext < best[0] - 1:
            best = (ext, K, R1, pitch)
    return best


def estimate(n, d, names):
    sol = solve(n, d, 130)
    return 2 * (sol[0] + 70)


# ---- one diagram -------------------------------------------------------------------------------
def build(table, cols):
    names = [c['name'] for c in cols]
    n = len(names)
    opts = list(font_options(names))
    fs, d = opts[0]
    for fs, d in opts:                         # biggest readable text whose page stays <= ~3900 px
        if estimate(n, d, names) <= 3900:
            break
    f = font(fs)
    # centre circle
    cfs = 34
    while True:
        cf = font(cfs, True)
        lines = wrap(table, cf, 330)
        w, h = block(lines, cf, cfs)
        need = math.hypot(w, h) + 40
        if need <= 340 or cfs <= 26:
            break
        cfs -= 2
    D0 = max(260, math.ceil(need / 2) * 2)
    Rc = D0 / 2
    ext, K, R1, pitch = solve(n, d, Rc)
    pts = place(n, K, R1, pitch)
    H = ext + 70
    leg_w, leg_h = 300, 118
    while True:                                # legend (top-left) and border must clear every circle
        ok = True
        lx0, ly0 = -H + 24, -H + 24
        for (x, y, _) in pts:
            nx = max(lx0, min(x, lx0 + leg_w)); ny = max(ly0, min(y, ly0 + leg_h))
            if math.hypot(x - nx, y - ny) < d / 2 + 20:
                ok = False; break
        if ok: break
        H += 30
    H = max(H, 460)
    size = int(math.ceil(H * 2 / 10.0) * 10)
    ox = oy = size / 2.0
    circles = []
    for i, c in enumerate(cols):
        x, y, R = pts[i]
        nm = c['name']
        lines = wrap(nm, f, d * 0.82)
        circles.append(dict(i=i, name=nm, x=ox + x, y=oy + y, lines=lines, color=RING[i % len(RING)],
                            pk=(c['key'] == 'PRI' or nm == 'id'), fk=(nm.endswith('_id') and nm != 'id')))
    return dict(table=table, n=n, fs=fs, d=d, D0=D0, cfs=cfs, clines=lines if False else wrap(table, font(cfs, True), 330),
                K=K, R1=R1, pitch=pitch, size=size, cx=ox, cy=oy, circles=circles,
                legend=(24, 24, leg_w, leg_h))


def audit(g):
    """Independent re-check on final page coordinates. Returns dict of issue counts."""
    d, cx, cy, D0 = g['d'], g['cx'], g['cy'], g['D0']
    C = g['circles']
    n = len(C)
    res = dict(circle_overlap=0, line_through_circle=0, text_outside=0, legend_touch=0, border_touch=0, centre_touch=0)
    f = font(g['fs'])
    for i in range(n):
        a = C[i]
        for j in range(i + 1, n):
            if math.hypot(a['x'] - C[j]['x'], a['y'] - C[j]['y']) < d + CLEAR - 0.01:
                res['circle_overlap'] += 1
        for j in range(n):
            if i != j and seg_dist(C[j]['x'], C[j]['y'], cx, cy, a['x'], a['y']) < d / 2 + LINE_PAD - 0.01:
                res['line_through_circle'] += 1
        if not fits(a['lines'], f, g['fs'], d, pad=5):
            res['text_outside'] += 1
        if math.hypot(a['x'] - cx, a['y'] - cy) < D0 / 2 + d / 2 + 40:
            res['centre_touch'] += 1
        if min(a['x'], a['y'], g['size'] - a['x'], g['size'] - a['y']) < d / 2 + 24:
            res['border_touch'] += 1
        lx, ly, lw, lh = g['legend']
        nx = max(lx, min(a['x'], lx + lw)); ny = max(ly, min(a['y'], ly + lh))
        if math.hypot(a['x'] - nx, a['y'] - ny) < d / 2 + 10:
            res['legend_touch'] += 1
    # centre text
    cf = font(g['cfs'], True)
    w, h = block(g['clines'], cf, g['cfs'])
    if math.hypot(w / 2, h / 2) > D0 / 2 - 14:
        res['text_outside'] += 1
    return res


# ---- draw.io XML -------------------------------------------------------------------------------
def esc(lines):
    return '&lt;br&gt;'.join(html.escape(l, quote=False) for l in lines)


def style_circle(c, fs):
    base = ('ellipse;whiteSpace=nowrap;html=1;aspect=fixed;fontFamily=Arial;fontSize=%d;align=center;verticalAlign=middle;'
            'spacing=0;strokeWidth=%d;' % (fs, RINGW))
    if c['pk']:
        return base + 'fillColor=%s;strokeColor=%s;fontColor=#FFFFFF;fontStyle=1;' % (c['color'], c['color'])
    s = base + 'fillColor=%s;strokeColor=%s;fontColor=%s;' % (INNER, c['color'], TXT)
    if c['fk']:
        s += 'dashed=1;dashPattern=6 4;'
    return s


def diagram_xml(g):
    out = []
    t = g['table']
    S = g['size']
    out.append('<diagram name="%s" id="d_%s">' % (html.escape(t), html.escape(t)))
    out.append('<mxGraphModel dx="1200" dy="800" grid="0" gridSize="10" guides="1" tooltips="1" connect="1" arrows="1" '
               'fold="1" page="1" pageScale="1" pageWidth="%d" pageHeight="%d" math="0" shadow="0"><root>' % (S, S))
    out.append('<mxCell id="0"/><mxCell id="1" parent="0"/>')
    # edges first (behind)
    for c in g['circles']:
        out.append('<mxCell id="e%d" style="endArrow=none;startArrow=none;html=1;strokeColor=%s;strokeWidth=2;" edge="1" '
                   'parent="1" source="col%d" target="hub"><mxGeometry relative="1" as="geometry"/></mxCell>' % (c['i'], LINE, c['i']))
    D0 = g['D0']
    out.append('<mxCell id="hub" value="%s" style="ellipse;whiteSpace=nowrap;html=1;aspect=fixed;fontFamily=Arial;fontSize=%d;'
               'fontStyle=1;fontColor=#1B2A41;fillColor=#FFFFFF;strokeColor=%s;strokeWidth=10;align=center;verticalAlign=middle;" '
               'vertex="1" parent="1"><mxGeometry x="%.1f" y="%.1f" width="%d" height="%d" as="geometry"/></mxCell>'
               % (esc(g['clines']), g['cfs'], NAVY, g['cx'] - D0 / 2, g['cy'] - D0 / 2, D0, D0))
    d = g['d']
    for c in g['circles']:
        out.append('<mxCell id="col%d" value="%s" style="%s" vertex="1" parent="1"><mxGeometry x="%.1f" y="%.1f" width="%d" '
                   'height="%d" as="geometry"/></mxCell>' % (c['i'], esc(c['lines']), style_circle(c, g['fs']),
                                                               c['x'] - d / 2, c['y'] - d / 2, d, d))
    lx, ly, lw, lh = g['legend']
    out.append('<mxCell id="legbox" value="" style="rounded=1;whiteSpace=wrap;html=1;fillColor=#FFFFFF;strokeColor=#BBBBBB;" '
               'vertex="1" parent="1"><mxGeometry x="%d" y="%d" width="%d" height="%d" as="geometry"/></mxCell>' % (lx, ly, lw, lh))
    items = [('legsolid', 'solid ring = column', False, False), ('legpk', 'filled ring = primary key', True, False),
             ('legfk', 'dashed ring = foreign key (_id)', False, True)]
    for k, (iid, label, pk, fk) in enumerate(items):
        y = ly + 14 + k * 32
        col = NAVY
        st = ('ellipse;html=1;aspect=fixed;strokeWidth=3;strokeColor=%s;fillColor=%s;' % (col, col if pk else INNER)
              + ('dashed=1;dashPattern=4 3;' if fk else ''))
        out.append('<mxCell id="%s" value="" style="%s" vertex="1" parent="1"><mxGeometry x="%d" y="%d" width="24" height="24" '
                   'as="geometry"/></mxCell>' % (iid, st, lx + 16, y))
        out.append('<mxCell id="%s_t" value="%s" style="text;html=1;align=left;verticalAlign=middle;fontFamily=Arial;fontSize=13;'
                   'fontColor=#333333;" vertex="1" parent="1"><mxGeometry x="%d" y="%d" width="%d" height="24" as="geometry"/></mxCell>'
                   % (iid, html.escape(label), lx + 52, y, lw - 64))
    out.append('</root></mxGraphModel></diagram>')
    return ''.join(out)


# ---- PNG preview -------------------------------------------------------------------------------
def dashed_circle(dr, cx, cy, r, color, w, sc):
    segs = 28
    for k in range(segs):
        if k % 2: continue
        a0 = 360.0 * k / segs; a1 = 360.0 * (k + 1) / segs
        dr.arc([cx - r, cy - r, cx + r, cy + r], a0, a1, fill=color, width=w)


def render_png(g, path):
    S = g['size']
    sc = 2 if S <= 2800 else 1
    im = Image.new('RGB', (S * sc, S * sc), 'white')
    dr = ImageDraw.Draw(im)
    cx, cy, D0, d = g['cx'] * sc, g['cy'] * sc, g['D0'] * sc, g['d'] * sc
    for c in g['circles']:
        dr.line([(c['x'] * sc, c['y'] * sc), (cx, cy)], fill=LINE, width=2 * sc)
    # hub
    w = 10 * sc
    dr.ellipse([cx - D0 / 2 - w / 2, cy - D0 / 2 - w / 2, cx + D0 / 2 + w / 2, cy + D0 / 2 + w / 2], fill=NAVY)
    dr.ellipse([cx - D0 / 2 + w / 2, cy - D0 / 2 + w / 2, cx + D0 / 2 - w / 2, cy + D0 / 2 - w / 2], fill='white')
    cf = font(g['cfs'] * sc, True)
    lh = g['cfs'] * sc * 1.2
    y0 = cy - lh * len(g['clines']) / 2
    for k, ln in enumerate(g['clines']):
        tw = cf.getlength(ln)
        dr.text((cx - tw / 2, y0 + k * lh), ln, font=cf, fill='#1B2A41')
    f = font(g['fs'] * sc)
    fb = font(g['fs'] * sc, True)
    for c in g['circles']:
        x, y = c['x'] * sc, c['y'] * sc
        rw = RINGW * sc
        r = d / 2
        if c['pk']:
            dr.ellipse([x - r - rw / 2, y - r - rw / 2, x + r + rw / 2, y + r + rw / 2], fill=c['color'])
            tc, ff = 'white', fb
        else:
            dr.ellipse([x - r - rw / 2, y - r - rw / 2, x + r + rw / 2, y + r + rw / 2], fill=INNER)
            if c['fk']:
                dashed_circle(dr, x, y, r, c['color'], int(rw), sc)
            else:
                dr.ellipse([x - r - rw / 2, y - r - rw / 2, x + r + rw / 2, y + r + rw / 2], outline=c['color'], width=int(rw))
            tc, ff = TXT, f
        lh2 = g['fs'] * sc * 1.2
        yy = y - lh2 * len(c['lines']) / 2
        for k, ln in enumerate(c['lines']):
            dr.text((x - ff.getlength(ln) / 2, yy + k * lh2), ln, font=ff, fill=tc)
    lx, ly, lw, lhh = [v * sc for v in g['legend']]
    dr.rounded_rectangle([lx, ly, lx + lw, ly + lhh], radius=10 * sc, outline='#BBBBBB', fill='white', width=sc)
    lf = font(13 * sc)
    for k, (label, pk, fk) in enumerate([('solid ring = column', False, False), ('filled ring = primary key', True, False),
                                         ('dashed ring = foreign key (_id)', False, True)]):
        yy = ly + (14 + k * 32) * sc
        col = NAVY
        bx = [lx + 16 * sc, yy, lx + 40 * sc, yy + 24 * sc]
        if pk:
            dr.ellipse(bx, fill=col, outline=col, width=3 * sc)
        else:
            dr.ellipse(bx, fill=INNER)
            if fk:
                dashed_circle(dr, (bx[0] + bx[2]) / 2, (bx[1] + bx[3]) / 2, 11 * sc, col, 3 * sc, sc)
            else:
                dr.ellipse(bx, outline=col, width=3 * sc)
        dr.text((lx + 52 * sc, yy + 3 * sc), label, font=lf, fill='#333333')
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
        print('%-28s cols=%3d rings=%d d=%3d fs=%2d hub=%3d page=%4dx%-4d overlaps=%d %s' %
              (t, g['n'], g['K'], g['d'], g['fs'], g['D0'], g['size'], g['size'], cnt, '' if not cnt else iss))
        root.append(diagram_xml(g))
        meta.append(dict(table=t, screen=screen, columns=g['n'], rings=g['K'], page=g['size']))
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
