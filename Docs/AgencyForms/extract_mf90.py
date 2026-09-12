import re, zlib, base64, sys

import os
P = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'Application-for-Marriage-License_Municipal-Form-90_BLANK.pdf')
d = open(P, 'rb').read()
m = re.search(rb'stream\r?\n', d)
s = m.end(); e = d.find(b'endstream', s)
raw = d[s:e].strip()
if raw.endswith(b'~>'):
    raw = raw[:-2]
dec = zlib.decompress(base64.a85decode(raw, adobe=False)).decode('latin-1')

text = []
for mm in re.finditer(r'BT 1 0 0 1 ([\d.]+) ([\d.]+) Tm \((.*?)\) Tj', dec):
    text.append((float(mm.group(2)), float(mm.group(1)), mm.group(3)))

# Ruled lines: "n x1 y1 m x2 y2 l S" - these are the blanks a value is written ON.
rules = []
for mm in re.finditer(r'n ([\d.]+) ([\d.]+) m ([\d.]+) ([\d.]+) l S', dec):
    x1, y1, x2, y2 = (float(g) for g in mm.groups())
    if abs(y1 - y2) < 0.5:
        rules.append((y1, min(x1, x2), max(x1, x2)))

text.sort(key=lambda t: (-t[0], t[1]))
rules.sort(key=lambda r: (-r[0], r[1]))

what = sys.argv[1] if len(sys.argv) > 1 else 'text'
if what == 'text':
    print("TEXT %d items  (y, x, string)" % len(text))
    for y, x, t in text:
        print('%7.1f %7.1f  %s' % (y, x, t))
else:
    print("RULES %d  (y, x1, x2, width)" % len(rules))
    for y, x1, x2 in rules:
        print('%7.1f  %7.1f -> %7.1f   w=%6.1f' % (y, x1, x2, x2 - x1))
