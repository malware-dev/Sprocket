"""
Draws the Workshop thumbnail of a Sprocket Companion mod: a mod that carries the Sprocket name. Every companion follows
the Sprocket Programmable Block's thumbnail and changes only its screenshot, its crop and the second line of its title.

The layout, 720 x 450, is the plain companion mods' (Door Auto-Close, Gravity Field Fit, Multi-Rename) in Sprocket's
dress, with the colors of the reproduction sheet:

- Rows 0 to 296: a screenshot of the mod in the game, cut to 720:296 about the middle of the crop, never stretched.
- Rows 296 to 300: a line in the brass gradient, where the plain companions draw it blue (78, 159, 216).
- Below: the band, #161B22.
- The title at x 48, top 333, Bahnschrift SemiBold in the silver gradient with a 0 4 4 50% black shadow, line height
  1.05: "Sprocket" at 32 px over the mod's short name at 40 px.
- The Companion stamp (sprocket-companion.png, from build_companion.py) placed by its plate: 228 px wide, its top left
  at (456, 347), so the title and the plate share a vertical center.

    python art/thumbnail/build_companion_thumb.py <screenshot> <name> <out.png> [left top right bottom]

The crop is four fractions of the screenshot's width and height; the whole screenshot without one. The Sprocket
Programmable Block's thumbnail is

    python art/thumbnail/build_companion_thumb.py <its 01-both-variants.jpg> "Programmable Block" thumb.png 0.07 0.19 0.93 0.83
"""

import base64
import io
import os
import subprocess
import sys
import tempfile

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
STAMP = os.path.join(HERE, "sprocket-companion.png")
CHROME = r"C:\Program Files\Google\Chrome\Application\chrome.exe"
WIDTH, HEIGHT = 720, 450
PICTURE_HEIGHT = 296

# sprocket-companion.png is a 340 x 84 plate in a 372 x 116 frame (16 px each side, 10 above, 22 below, holding its
# shadow). Placed by the plate: 228 px wide, its top left at (456, 347).
STAMP_SCALE = 228 / 340
STAMP_AT = (456 - 16 * STAMP_SCALE, 347 - 10 * STAMP_SCALE, 372 * STAMP_SCALE)

PAGE = """<!doctype html><html><head><style>
body {{ margin: 0; width: 720px; height: 450px; overflow: hidden; background: #161B22; font-family: Bahnschrift; }}
.picture {{ position: absolute; left: 0; top: 0; width: 720px; height: 296px; }}
.line {{ position: absolute; left: 0; top: 296px; width: 720px; height: 4px;
  background: linear-gradient(#8C6830 0%, #EED296 24%, #C69E50 44%, #7A5826 66%, #DCBC7A 82%, #86642E 100%); }}
.title {{ position: absolute; left: 48px; top: 333px; font-size: 40px; font-weight: 600; line-height: 1.05;
  background: linear-gradient(#D6E1EC 0%, #F6FAFD 40%, #C4D1DF 100%); -webkit-background-clip: text; color: transparent;
  filter: drop-shadow(0 4px 4px rgba(0,0,0,0.5)); }}
.stamp {{ position: absolute; left: {stamp_left:.2f}px; top: {stamp_top:.2f}px; width: {stamp_width:.2f}px; }}
</style></head><body>
<img class="picture" src="data:image/png;base64,{picture}">
<div class="line"></div>
<div class="title"><span style="font-size: 32px">Sprocket</span><br>{name}</div>
<img class="stamp" src="data:image/png;base64,{stamp}">
</body></html>"""


def picture(screenshot, crop):
    """The screenshot cut to the picture's proportions about the middle of the crop, so nothing stretches, as PNG data."""
    shot = Image.open(screenshot).convert("RGB")
    left, top, right, bottom = (round(f * s) for f, s in zip(crop, (shot.width, shot.height) * 2))
    want = WIDTH / PICTURE_HEIGHT
    if (right - left) / (bottom - top) > want:
        trim = round((right - left) - (bottom - top) * want)
        left, right = left + trim // 2, right - (trim - trim // 2)
    else:
        trim = round((bottom - top) - (right - left) / want)
        top, bottom = top + trim // 2, bottom - (trim - trim // 2)
    data = io.BytesIO()
    shot.crop((left, top, right, bottom)).resize((WIDTH, PICTURE_HEIGHT), Image.LANCZOS).save(data, "PNG")
    return base64.b64encode(data.getvalue()).decode("ascii")


def encoded(path):
    with open(path, "rb") as f:
        return base64.b64encode(f.read()).decode("ascii")


def main(arguments):
    if len(arguments) not in (3, 7):
        sys.exit(__doc__)
    screenshot, name, out = arguments[:3]
    crop = tuple(float(a) for a in arguments[3:]) or (0, 0, 1, 1)
    work = tempfile.mkdtemp()
    page = os.path.join(work, "thumb.html")
    left, top, width = STAMP_AT
    with open(page, "w", encoding="utf-8") as f:
        f.write(PAGE.format(picture=picture(screenshot, crop), stamp=encoded(STAMP), name=name,
                            stamp_left=left, stamp_top=top, stamp_width=width))
    shot = os.path.join(work, "thumb.png")
    subprocess.run([CHROME, "--headless=new", "--disable-gpu", "--hide-scrollbars", "--force-device-scale-factor=1",
                    "--window-size=%d,%d" % (WIDTH, HEIGHT), "--screenshot=" + shot, "file:///" + page.replace("\\", "/")],
                   check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    Image.open(shot).convert("RGB").crop((0, 0, WIDTH, HEIGHT)).save(out, optimize=True)
    print("wrote", os.path.abspath(out))


if __name__ == "__main__":
    main(sys.argv[1:])
