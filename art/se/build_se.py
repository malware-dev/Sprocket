"""
Cuts bold, italic and bold italic styles of the game's own face and writes them in the format Space Engineers reads.

Run it from anywhere:  python art/se/build_se.py

Styles that give strong and emphasized words a shape of their own beside the game's face, in the same
letterforms.

The letterforms are Keen Software House's own, from space_engineers.ttf in the Space Engineers Mod
SDK's Tools/Fonts folder, read from where Steam installs the SDK rather than copied here: the Steam
library folder the STEAM_COMMON environment variable names, or E:/Steam/steamapps/common where it is
not set.

At SIZE the outline reproduces the game's FontDataPA glyphs to within a pixel, so every glyph is cut into a cell
laid out exactly as the game's own: the same height, the same baseline, the same left side bearing,
and the same advance as the regular glyph, widened by a pixel for bold. A word in one of these styles
stands where the same word in the game's face would.

Bold thickens every stroke by EMBOLDEN on each side; italic slants the regular by SLANT degrees about
the baseline. Both are drawn OVER times oversized and averaged down.

The output is a page PNG and an XNA bitmapfont XML for each style, encoded to DDS through
art/symbols/FontTool, which knows the format the game insists on.
"""

import math
import os
import subprocess
import sys
import xml.etree.ElementTree as ElementTree
import xml.sax.saxutils as esc

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, os.path.join(HERE, ".."))

import vanilla_characters

STEAM = os.environ.get("STEAM_COMMON", r"E:\Steam\steamapps\common")
SDK = os.path.join(STEAM, "SpaceEngineersModSDK", "Tools", "Fonts", "space_engineers.ttf")
GAME = os.path.join(STEAM, "SpaceEngineers", "Content", "Fonts", "white", "FontDataPA.xml")
FONTS = os.path.join(ROOT, "src", "SpaceEngineers.FontStyles", "Content", "Fonts", "SpaceEngineers")
TOOL = os.path.join(ROOT, "art", "symbols", "FontTool")

# Each style: whether it is bold, whether it is slanted, the name the game finds it by, and the face it declares.
STYLES = [
    (True, False, "SpaceEngineersBold", "Space Engineers Bold"),
    (False, True, "SpaceEngineersItalic", "Space Engineers Italic"),
    (True, True, "SpaceEngineersBoldItalic", "Space Engineers Bold Italic"),
]

# The pixel size at which the outline's glyphs match FontDataPA's: H, M, O and W land on the same ink
# width, height and advance, the rest within a pixel.
SIZE = 30.7

# FontDataPA's own layout: every cell this tall, its baseline this far down, its left edge this far left
# of the pen, on a line declared with this base and height.
CELL_HEIGHT = 45
BASELINE = 34
LSB = -7
BASE = 30
LINE = 37

# The face size FontDataPA declares, which the game reads from the table and these keep.
FACE_SIZE = 23

EMBOLDEN = 0.6
SLANT = 11.0
OVER = 8
PAGE = 1024


def advances():
    """FontDataPA's advance for each codepoint, which every style keeps."""
    ns = "{http://xna.microsoft.com/bitmapfont}"
    return {int(g.get("code"), 16): int(g.get("aw")) for g in ElementTree.parse(GAME).getroot().iter(ns + "glyph")}


def glyph(face, code, advance, bold, slanted):
    """One glyph's cell, and the advance it declares."""
    from PIL import Image, ImageDraw

    stroke = int(round(EMBOLDEN * OVER)) if bold else 0
    big = Image.new("L", ((-LSB + advance + 12) * OVER, CELL_HEIGHT * OVER), 0)
    ImageDraw.Draw(big).text(((-LSB) * OVER + stroke, BASELINE * OVER), chr(code), font=face, fill=255, anchor="ls",
                             stroke_width=stroke, stroke_fill=255)

    if slanted:
        k = math.tan(math.radians(SLANT))
        big = big.transform(big.size, Image.AFFINE, (1, k, -k * BASELINE * OVER, 0, 1, 0), resample=Image.BICUBIC)

    small = big.reduce(OVER)
    ink = small.getbbox()
    width = ink[2] + 1 if ink else 1
    return small.crop((0, 0, width, CELL_HEIGHT)), advance + (1 if bold else 0)


def cut(bold, slanted, name, face_name):
    """Cuts one style to a page and its table, and answers the page's path."""
    from fontTools.ttLib import TTFont
    from PIL import Image, ImageFont

    has = set()
    for table in TTFont(SDK)["cmap"].tables:
        has |= set(table.cmap.keys())

    face = ImageFont.truetype(SDK, SIZE * OVER)
    widths = advances()
    cells = []
    for code in vanilla_characters.read():
        if code != 0x20 and code not in has:
            raise SystemExit("space_engineers.ttf has nothing to draw U+%04X with" % code)
        image, advance = glyph(face, code, widths[code], bold, slanted)
        cells.append((code, image, advance))

    side = PAGE
    while True:
        x = y = 0
        places = []
        for code, image, advance in cells:
            if x + image.width > side:
                x, y = 0, y + CELL_HEIGHT
            places.append((x, y))
            x += image.width
        if y + CELL_HEIGHT <= side:
            break
        side *= 2

    page = Image.new("RGBA", (side, side), (255, 255, 255, 0))
    lines = [
        '<?xml version="1.0" encoding="UTF-8" ?>',
        '<font',
        '\txmlns="http://xna.microsoft.com/bitmapfont"',
        '\txmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"',
        '',
        '\tname="%s" base="%d" height="%d"' % (name, BASE, LINE),
        '\tface="%s" size="%d"' % (face_name, FACE_SIZE),
        '\t>',
        '<bitmaps>',
        '<bitmap id="0" name="%s-0.dds" size="%dx%d" />' % (name, side, side),
        '</bitmaps>',
        '<glyphs>',
    ]

    for (code, image, advance), (x, y) in zip(cells, places):
        white = Image.new("RGBA", image.size, (255, 255, 255, 255))
        white.putalpha(image)
        page.paste(white, (x, y))
        lines.append('<glyph ch=%s code="%04X" bm="0" origin="%d,%d" size="%dx%d" aw="%d" lsb="%d" />'
                     % (esc.quoteattr(chr(code)), code, x, y, image.width, CELL_HEIGHT, advance, LSB))

    lines += ['</glyphs>', '<kernpairs>', '</kernpairs>', '</font>', '']

    os.makedirs(FONTS, exist_ok=True)
    png = os.path.join(FONTS, name + "-0.png")
    page.save(png)
    with open(os.path.join(FONTS, name + ".xml"), "w", encoding="utf-8") as f:
        f.write("\n".join(lines))

    print("%s  %d glyphs on a %dx%d page" % (name, len(cells), side, side))
    return png


def main():
    for bold, slanted, name, face_name in STYLES:
        png = cut(bold, slanted, name, face_name)
        subprocess.run(["dotnet", "run", "--project", TOOL, "--", png, png[:-4] + ".dds"], check=True)


if __name__ == "__main__":
    main()
