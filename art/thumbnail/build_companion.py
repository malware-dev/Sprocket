"""
Draws the Sprocket Companion stamp, the plate every Sprocket Companion mod carries, as sprocket-companion.svg and
sprocket-companion.png.

The plate is the social card's tape, the icon stands at its left, and its two lines are Bahnschrift SemiBold set as
outlines, so the SVG needs no font. Chrome renders the PNG with a transparent ground, at SCALE times the SVG's size.
Run it again after changing anything here; the icon is read from sprocket-icon.svg.

    python art/thumbnail/build_companion.py
"""

import os
import re
import subprocess
import tempfile

from fontTools.pens.svgPathPen import SVGPathPen
from fontTools.pens.transformPen import TransformPen
from fontTools.ttLib import TTFont
from fontTools.varLib import instancer

HERE = os.path.dirname(os.path.abspath(__file__))
ICON = os.path.join(HERE, "sprocket-icon.svg")
SOURCE = os.path.join(HERE, "sprocket-companion.svg")
PICTURE = os.path.join(HERE, "sprocket-companion.png")
FONT = r"C:\Windows\Fonts\bahnschrift.ttf"
CHROME = r"C:\Program Files\Google\Chrome\Application\chrome.exe"

# The plate, in px at 1x: 340 by 84 with 6 px corners, framed with room for its shadow.
WIDTH, HEIGHT, CORNER = 340, 84, 6
MARGIN_X, MARGIN_TOP, MARGIN_BOTTOM = 16, 10, 22
SCALE = 4

# The icon: 68 px square, 12 px in from the plate's left and 8 down.
ICON_X, ICON_Y, ICON_SIZE = 12, 8, 68

# Each line: text, size, tracking after every letter, left edge, baseline. A baseline is the line's top plus
# Bahnschrift's ascent, since its ascent and descent add up to one em.
SMALL = ("SPROCKET AUTOMATION", 13, 3, 92, 20 + 13 * 1626 / 2048)
LARGE = ("COMPANION", 30, 3, 91, 39 + 30 * 1626 / 2048)


def semibold():
    font = TTFont(FONT)
    return instancer.instantiateVariableFont(font, {"wght": 600, "wdth": 100})


def kerning(font):
    """Pair adjustments of the kern feature, by glyph name pair, in font units."""
    pairs = {}
    gpos = font["GPOS"].table
    lookups = set()
    for record in gpos.FeatureList.FeatureRecord:
        if record.FeatureTag == "kern":
            lookups.update(record.Feature.LookupListIndex)
    for index in sorted(lookups):
        lookup = gpos.LookupList.Lookup[index]
        for sub in lookup.SubTable:
            if lookup.LookupType == 9:
                if sub.ExtensionLookupType != 2:
                    continue
                sub = sub.ExtSubTable
            elif lookup.LookupType != 2:
                continue
            coverage = sub.Coverage.glyphs
            if sub.Format == 1:
                for first, pairset in zip(coverage, sub.PairSet):
                    for record in pairset.PairValueRecord:
                        value = record.Value1
                        pairs.setdefault((first, record.SecondGlyph), getattr(value, "XAdvance", 0) or 0)
            elif sub.Format == 2:
                class1 = sub.ClassDef1.classDefs
                class2 = sub.ClassDef2.classDefs
                for first in coverage:
                    row = sub.Class1Record[class1.get(first, 0)]
                    for second in font.getGlyphOrder():
                        value = row.Class2Record[class2.get(second, 0)].Value1
                        advance = getattr(value, "XAdvance", 0) or 0 if value else 0
                        if advance:
                            pairs.setdefault((first, second), advance)
    return pairs


def outline(font, pairs, line):
    text, size, tracking, left, baseline = line
    cmap = font.getBestCmap()
    glyphs = font.getGlyphSet()
    upm = font["head"].unitsPerEm
    scale = size / upm
    pen = SVGPathPen(glyphs, lambda v: "%.2f" % v)
    x = left
    names = [cmap[ord(c)] for c in text]
    for i, name in enumerate(names):
        glyphs[name].draw(TransformPen(pen, (scale, 0, 0, -scale, x, baseline)))
        x += glyphs[name].width * scale + tracking
        if i + 1 < len(names):
            x += pairs.get((name, names[i + 1]), 0) * scale
    return pen.getCommands()


def icon():
    svg = open(ICON, encoding="utf-8").read()
    defs = re.search(r"<defs>(.*?)</defs>", svg, re.S).group(1).replace('id="brass"', 'id="iconBrass"')
    body = re.search(r"</defs>(.*)</svg>", svg, re.S).group(1).replace("url(#brass)", "url(#iconBrass)")
    return defs, body


def draw():
    font = semibold()
    pairs = kerning(font)
    small = outline(font, pairs, SMALL)
    large = outline(font, pairs, LARGE)
    icon_defs, icon_body = icon()
    large_top = LARGE[4] - LARGE[1] * 1626 / 2048
    frame_w = WIDTH + 2 * MARGIN_X
    frame_h = HEIGHT + MARGIN_TOP + MARGIN_BOTTOM
    return f"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="{-MARGIN_X} {-MARGIN_TOP} {frame_w} {frame_h}" width="{frame_w}" height="{frame_h}" role="img" aria-label="Sprocket Automation Companion">
  <title>Sprocket Automation Companion</title>
  <defs>
    <linearGradient id="tape" gradientUnits="objectBoundingBox" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#4A3A21"/><stop offset="0.45" stop-color="#382B17"/><stop offset="1" stop-color="#241B0E"/></linearGradient>
    <linearGradient id="light" gradientUnits="userSpaceOnUse" x1="0" y1="{large_top:.2f}" x2="0" y2="{large_top + LARGE[1]:.2f}"><stop offset="0" stop-color="#D6E1EC"/><stop offset="0.4" stop-color="#F6FAFD"/><stop offset="1" stop-color="#C4D1DF"/></linearGradient>
    <filter id="stuck" x="-15%" y="-40%" width="130%" height="190%" color-interpolation-filters="sRGB"><feGaussianBlur in="SourceAlpha" stdDeviation="5" result="b"/><feOffset in="b" dy="5" result="o"/><feFlood flood-color="#000" flood-opacity="0.55" result="f"/><feComposite in="f" in2="o" operator="in" result="s"/><feMerge><feMergeNode in="s"/><feMergeNode in="SourceGraphic"/></feMerge></filter>
    {icon_defs}
  </defs>
  <g filter="url(#stuck)">
    <rect x="0" y="0" width="{WIDTH}" height="{HEIGHT}" rx="{CORNER}" fill="url(#tape)"/>
    <rect x="0.5" y="0.5" width="{WIDTH - 1}" height="{HEIGHT - 1}" rx="{CORNER - 0.5}" fill="none" stroke="#FFFFFF" stroke-opacity="0.07"/>
  </g>
  <svg x="{ICON_X}" y="{ICON_Y}" width="{ICON_SIZE}" height="{ICON_SIZE}" viewBox="-256 -256 512 512">{icon_body}</svg>
  <path fill="#C9B58C" d="{small}"/>
  <path fill="url(#light)" d="{large}"/>
</svg>
"""


def render():
    frame_w = (WIDTH + 2 * MARGIN_X) * SCALE
    frame_h = (HEIGHT + MARGIN_TOP + MARGIN_BOTTOM) * SCALE
    page = os.path.join(tempfile.mkdtemp(), "companion.html")
    with open(page, "w", encoding="utf-8") as out:
        out.write("<!doctype html><html><head><style>html,body{margin:0;padding:0;background:transparent;"
                  "overflow:hidden}img{display:block;width:%dpx;height:%dpx}</style></head><body>"
                  "<img src=\"file:///%s\"></body></html>" % (frame_w, frame_h, SOURCE.replace("\\", "/")))
    subprocess.run([CHROME, "--headless=new", "--disable-gpu", "--hide-scrollbars", "--allow-file-access-from-files",
                    "--default-background-color=00000000", "--window-size=%d,%d" % (frame_w, frame_h),
                    "--screenshot=" + PICTURE, "file:///" + page.replace("\\", "/")],
                   check=True, stderr=subprocess.DEVNULL)


with open(SOURCE, "w", encoding="utf-8", newline="\n") as out:
    out.write(draw())
render()
