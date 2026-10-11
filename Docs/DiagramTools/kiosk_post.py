"""Post-process the raw kiosk captures (kshots/k*.png) into kshots/final/*.png.
 - masks the live webcam feed (a real room) with the flat dark preview colour
 - crops the white strip below the app
 - stitches the two Select Services captures (top and scrolled bottom) into one tall picture
"""
from PIL import Image, ImageDraw
import numpy as np, os
D = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'kshots')
O = os.path.join(D, 'final'); os.makedirs(O, exist_ok=True)
H = 988  # app area; below it is the host window's white strip
cam = {'k5_personal': (1005, 240, 1438, 657), 'k6_personal_marriage_reg': (1005, 240, 1438, 657),
       'k7_personal_marriage_app': (1008, 280, 1434, 640)}
for n in ['k1_welcome', 'k3_ctc', 'k4_psa', 'k5_personal', 'k6_personal_marriage_reg', 'k7_personal_marriage_app', 'k8_review']:
    im = Image.open(os.path.join(D, n + '.png')).convert('RGB')
    if n in cam:
        ImageDraw.Draw(im).rounded_rectangle(cam[n], radius=10, fill=(17, 24, 39))
    if n != 'k8_review':
        im = im.crop((0, 0, 1920, H))
    im.save(os.path.join(O, n + '.png'))
# stitch services
a = np.array(Image.open(os.path.join(D, 'k2_services.png')).convert('RGB').crop((0, 0, 1920, H))).astype(int)
b = np.array(Image.open(os.path.join(D, 'k2b_services_bottom.png')).convert('RGB').crop((0, 0, 1920, H))).astype(int)
best = None
for dy in range(100, 700):
    ov_a = a[300 + dy:H, 200:1750]; ov_b = b[300:H - dy, 200:1750]
    d = np.abs(ov_a - ov_b).mean()
    if best is None or d < best[0]: best = (d, dy)
print('services scroll shift', best)
dy = best[1]
# the title and step strip stay fixed; only the card area scrolls, so append the rows the first
# capture did not reach (bottom rows H-dy..H of the scrolled capture = top rows H..H+dy)
out = Image.new('RGB', (1920, H + dy), 'white')
out.paste(Image.fromarray(a.astype('uint8')), (0, 0))
out.paste(Image.fromarray(b.astype('uint8')).crop((0, H - dy, 1920, H)), (0, H))
out.save(os.path.join(O, 'k2_services.png')); print(out.size)
