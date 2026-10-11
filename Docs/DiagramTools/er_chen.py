"""Chen-style entity-attribute diagrams drawn with PIL, in the look of the "Data Environment" pictures of the
user's-view document: entity = rectangle, attribute = ellipse, relationship = diamond with 1 / N, primary key
underlined, foreign key under a dotted line, Times New Roman, black on white.

draw_er(entities, relation) -> PIL image
  entities : list of (name, [(label, kind), ...]) with kind '' | 'pk' | 'fk'   (one or two entities)
  relation : (name, card_left, card_right) or None   (needed when there are two entities)
"""
import math
from PIL import Image, ImageDraw, ImageFont

S = 2  # supersampling
FONT = r'C:\Windows\Fonts\times.ttf'
BOLD = r'C:\Windows\Fonts\timesbd.ttf'


def _f(size, bold=False):
    return ImageFont.truetype(BOLD if bold else FONT, size * S)


class _Box:
    def __init__(self, cx, cy, w, h):
        self.cx, self.cy, self.w, self.h = cx, cy, w, h

    def hit(self, o, pad=0):
        return abs(self.cx - o.cx) < (self.w + o.w) / 2 + pad and abs(self.cy - o.cy) < (self.h + o.h) / 2 + pad


def _tw(draw, text, font):
    b = draw.textbbox((0, 0), text, font=font)
    return (b[2] - b[0]) / S


def _ring(draw, attrs, cx, cy, rect_w, rect_h, excl_deg=None, excl_half=38, afont=None):
    """Place the attribute ellipses around an entity; returns [(label, kind, _Box)]."""
    n = len(attrs)
    sizes = [(_tw(draw, a[0], afont) + 44, 58) for a in attrs]
    if excl_deg is None:
        angles = [(-90 + 360.0 * i / n) for i in range(n)]
    else:
        start = excl_deg + excl_half
        span = 360 - 2 * excl_half
        angles = [(start + span * (i + 0.5) / n) for i in range(n)]
    wmax = max(s[0] for s in sizes)
    rx = rect_w / 2 + wmax / 2 + 70
    ry = rect_h / 2 + 29 + 70
    ent = _Box(cx, cy, rect_w, rect_h)
    for _ in range(80):
        boxes = []
        for (w, h), a in zip(sizes, angles):
            boxes.append(_Box(cx + rx * math.cos(math.radians(a)), cy + ry * math.sin(math.radians(a)), w, h))
        bad = any(boxes[i].hit(boxes[j], 16) for i in range(n) for j in range(i + 1, n)) or any(b.hit(ent, 40) for b in boxes)
        if not bad:
            break
        rx *= 1.05
        ry *= 1.05
    return [(a[0], a[1], b) for a, b in zip(attrs, boxes)]


def draw_er(entities, relation=None):
    probe = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    afont, efont, rfont, cfont = _f(27), _f(36), _f(27), _f(32, True)
    rects = []
    for name, _ in entities:
        rects.append((_tw(probe, name, efont) + 64, 66))

    placed = []   # per entity: (cx, cy, [(label, kind, box)])
    if len(entities) == 1:
        rw, rh = rects[0]
        ring = _ring(probe, entities[0][1], 0, 0, rw, rh, None, afont=afont)
        placed.append((0, 0, ring))
        diamond = None
    else:
        (rw1, rh1), (rw2, rh2) = rects
        dw, dh = 200, 116
        gap = 110
        ring1 = _ring(probe, entities[0][1], 0, 0, rw1, rh1, 0, afont=afont)
        D = rw1 / 2 + gap + dw + gap + rw2 / 2
        while True:
            ring2 = _ring(probe, entities[1][1], D, 0, rw2, rh2, 180, afont=afont)
            dia = _Box(rw1 / 2 + gap + dw / 2, 0, dw, dh)
            ents = [_Box(0, 0, rw1, rh1), _Box(D, 0, rw2, rh2)]
            clash = any(a[2].hit(b[2], 18) for a in ring1 for b in ring2) \
                or any(a[2].hit(dia, 22) for a in ring1 + ring2) \
                or any(a[2].hit(ents[1], 30) for a in ring1) or any(b[2].hit(ents[0], 30) for b in ring2)
            if not clash:
                break
            D += 30
        placed = [(0, 0, ring1), (D, 0, ring2)]
        diamond = dia

    # bounds
    xs, ys = [], []
    for (cx, cy, ring), (rw, rh) in zip(placed, rects):
        xs += [cx - rw / 2, cx + rw / 2]; ys += [cy - rh / 2, cy + rh / 2]
        for _, _, b in ring:
            xs += [b.cx - b.w / 2, b.cx + b.w / 2]; ys += [b.cy - b.h / 2, b.cy + b.h / 2]
    pad = 36
    ox, oy = -min(xs) + pad, -min(ys) + pad
    W, H = int(max(xs) - min(xs) + 2 * pad), int(max(ys) - min(ys) + 2 * pad)
    im = Image.new('RGB', (W * S, H * S), 'white')
    d = ImageDraw.Draw(im)
    lw = 2 * S

    def P(x, y):
        return ((x + ox) * S, (y + oy) * S)

    # lines first (shapes are painted over them, so lines stop at the shape edge)
    for (cx, cy, ring), (rw, rh) in zip(placed, rects):
        for _, _, b in ring:
            d.line([P(cx, cy), P(b.cx, b.cy)], fill='black', width=lw)
    if diamond is not None:
        (cx1, _, _), (cx2, _, _) = placed
        d.line([P(cx1, 0), P(cx2, 0)], fill='black', width=lw)

    # entities
    for (cx, cy, ring), (rw, rh), (name, _) in zip(placed, rects, entities):
        d.rectangle([P(cx - rw / 2, cy - rh / 2), P(cx + rw / 2, cy + rh / 2)], fill='white', outline='black', width=lw)
        tw = _tw(d, name, efont)
        d.text(P(cx - tw / 2, cy - 24), name, font=efont, fill='black')
    # attributes
    for (cx, cy, ring) in placed:
        for label, kind, b in ring:
            d.ellipse([P(b.cx - b.w / 2, b.cy - b.h / 2), P(b.cx + b.w / 2, b.cy + b.h / 2)], fill='white', outline='black', width=lw)
            tw = _tw(d, label, afont)
            tx, ty = b.cx - tw / 2, b.cy - 17
            d.text(P(tx, ty), label, font=afont, fill='black')
            if kind:
                uy = ty + 36
                if kind == 'pk':
                    d.line([P(tx, uy), P(tx + tw, uy)], fill='black', width=S * 2)
                else:   # foreign key: dotted underline
                    x = tx
                    while x < tx + tw:
                        d.line([P(x, uy), P(min(x + 4, tx + tw), uy)], fill='black', width=S * 2)
                        x += 8
    # relationship
    if diamond is not None and relation:
        rname, c1, c2 = relation
        cx, cy, w, h = diamond.cx, 0, diamond.w, diamond.h
        d.polygon([P(cx, cy - h / 2), P(cx + w / 2, cy), P(cx, cy + h / 2), P(cx - w / 2, cy)], fill='white', outline='black')
        d.line([P(cx, cy - h / 2), P(cx + w / 2, cy), P(cx, cy + h / 2), P(cx - w / 2, cy), P(cx, cy - h / 2)], fill='black', width=lw)
        tw = _tw(d, rname, rfont)
        d.text(P(cx - tw / 2, cy - 17), rname, font=rfont, fill='black')
        d.text(P(rects[0][0] / 2 + 14, -rects[0][1] / 2 - 18), c1, font=cfont, fill='black')
        tw2 = _tw(d, c2, cfont)
        d.text(P(placed[1][0] - rects[1][0] / 2 - 14 - tw2, -rects[1][1] / 2 - 18), c2, font=cfont, fill='black')

    return im.resize((W, H), Image.LANCZOS) if S > 1 else im
