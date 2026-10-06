"""
Cuts the Maltek sans faces, regular, bold, italic and bold italic, and writes them in the format Space Engineers reads.

Run it from anywhere:  python art/sans/build_sans.py

A proportional face for reading: the codex and anything else that is prose rather than a listing,
with bold and italic cuts that give strong and emphasized words a shape of their own.

The letterforms are Inter, by the Inter Project Authors, under the SIL Open Font License 1.1. See
OFL.txt beside this file, which ships with the mod because the license says it must: a bitmap cut
from a font is a modified version of it, and every copy has to carry the notice and the license.
It is named by what it is, as Maltek Mono is, because a subtype is a lookup key rather than a
byline.

The output is a page PNG and an XNA bitmapfont XML for each weight. The PNG still has to be encoded
to DDS before the game will load it, which this does through art/symbols/FontTool, and FontTool
knows the format the game insists on.

The fonts are read from beside this script rather than from the machine's installed fonts, so a
clone of this repository can rebuild the faces without installing anything.
"""

import math
import os
import subprocess
import sys
import xml.sax.saxutils as esc

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, os.path.join(HERE, ".."))

import vanilla_characters
FONTS = os.path.join(ROOT, "src", "Maltek.Sans", "Content", "Fonts", "Maltek")
TOOL = os.path.join(ROOT, "art", "symbols", "FontTool")

# Each style: the font file it is cut from, the name the game finds it by, and the face it declares.
WEIGHTS = [
    ("Inter-Regular.ttf", "MaltekSans", "Maltek Sans"),
    ("Inter-Bold.ttf", "MaltekSansBold", "Maltek Sans Bold"),
    ("Inter-Italic.ttf", "MaltekSansItalic", "Maltek Sans Italic"),
    ("Inter-BoldItalic.ttf", "MaltekSansBoldItalic", "Maltek Sans Bold Italic"),
]

# The size, the line, the cell and where the baseline sits in it: Maltek Mono's, so a line of either
# Maltek face occupies the height of a line of the game's own and sits on the same baseline. See
# art/mono/build_mono.py for where each of these was measured.
SIZE = 29
LINE = 37
BASE = 30
CELL_HEIGHT = 45
BASELINE_IN_CELL = 34

# How far down the game draws a glyph's cell: its left side bearing and its height offset, added. Maltek
# Mono's cells stand at -7, where its baseline was measured. A proportional face's bearing differs from
# glyph to glyph, so each one's height offset takes its own bearing back off and puts this in its place,
# and every glyph stands on Maltek Mono's line.
TOP = -7

# What the faces cover is exactly what the game's own Latin-script face can draw: the list in
# art/vanilla-characters.txt, read from FontDataPA, as Maltek Mono covers.

# What Inter lacks of that list, drawn from what it has: the bullet operator as its middle dot, the
# long-obsolete apostrophe-n as an apostrophe and an n, and the soft hyphen as the hyphen it shows as
# where a line breaks.
STAND_INS = {
    0x2219: "\u00B7",
    0x0149: "\u02BCn",
    0x00AD: "-",
}

# The smallest page tried. Rows of glyphs are packed across it one after another, each as tall as a
# cell, and where they do not fit the page is doubled and packed again.
PAGE = 1024


def wanted(source):
    """Every codepoint the faces are asked for, in order, with what is drawn for each."""

    from fontTools.ttLib import TTFont

    has = set()
    for table in TTFont(source)["cmap"].tables:
        has |= set(table.cmap.keys())

    drawn = []
    for code in vanilla_characters.read():
        text = STAND_INS.get(code, chr(code))
        lacking = [c for c in text if ord(c) not in has]
        if lacking:
            raise SystemExit("%s has nothing to draw U+%04X with" % (os.path.basename(source), code))
        drawn.append((code, text))

    return drawn


def shelves(cells, side):
    """Where each cell goes on a page <side> wide: across it in rows, each row a cell tall."""

    places = []
    x = y = 0
    for code, left, width, advance in cells:
        if x + width > side:
            x, y = 0, y + CELL_HEIGHT
        places.append((x, y))
        x += width

    return places


def cut(source, name, face_name):
    """Cuts one weight to a page and its table, and answers the page's path."""

    from PIL import Image, ImageDraw, ImageFont

    face = ImageFont.truetype(source, SIZE)
    ascent, _ = face.getmetrics()
    codes = wanted(source)

    # Each glyph's cell runs from a pixel left of its ink to a pixel right of it, so antialiasing at
    # the edge is never shaved; the left side bearing says where that is from the pen.
    cells = []
    texts = dict(codes)
    for code, text in codes:
        box = face.getbbox(text)
        advance = int(round(face.getlength(text)))
        if box is None or box[2] <= box[0]:
            left, width = 0, 1
        else:
            left = int(math.floor(box[0])) - 1
            width = int(math.ceil(box[2])) + 1 - left

            above = ascent - box[1]
            below = box[3] - ascent
            if above > BASELINE_IN_CELL or below > CELL_HEIGHT - BASELINE_IN_CELL:
                raise SystemExit("%s U+%04X needs %d above the baseline and %d below, and the cell holds %d and %d"
                                 % (name, code, above, below, BASELINE_IN_CELL, CELL_HEIGHT - BASELINE_IN_CELL))

        cells.append((code, left, width, advance))

    side = PAGE
    while True:
        places = shelves(cells, side)
        if places[-1][1] + CELL_HEIGHT <= side:
            break
        side *= 2

    page = Image.new("RGBA", (side, side), (255, 255, 255, 0))
    for (code, left, width, advance), (x, y) in zip(cells, places):
        cell = Image.new("L", (width, CELL_HEIGHT), 0)
        pen = ImageDraw.Draw(cell)

        # The pen goes where the cell's left edge is from it, and on the baseline the cell declares.
        # PIL draws from the top of the line, so the ascent is what turns one into the other.
        pen.text((-left, BASELINE_IN_CELL - ascent), texts[code], font=face, fill=255)

        white = Image.new("RGBA", (width, CELL_HEIGHT), (255, 255, 255, 255))
        white.putalpha(cell)
        page.paste(white, (x, y))

    os.makedirs(FONTS, exist_ok=True)
    png = os.path.join(FONTS, name + "-0.png")
    page.save(png)

    lines = [
        '<?xml version="1.0" encoding="UTF-8" ?>',
        '<font',
        '\txmlns="http://xna.microsoft.com/bitmapfont"',
        '\txmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"',
        '',
        '\tname="%s" base="%d" height="%d"' % (name, BASE, LINE),
        '\tface="%s" size="%d"' % (face_name, SIZE),
        '\t>',
        '<bitmaps>',
        '<bitmap id="0" name="%s-0.dds" size="%dx%d" />' % (name, side, side),
        '</bitmaps>',
        '<glyphs>',
    ]

    for (code, left, width, advance), (x, y) in zip(cells, places):
        lines.append('<glyph ch=%s code="%04X" bm="0" origin="%d,%d" size="%dx%d" aw="%d" lsb="%d" ho="%d" />'
                     % (esc.quoteattr(chr(code)), code, x, y, width, CELL_HEIGHT, advance, left, TOP - left))

    lines += ['</glyphs>', '<kernpairs>', '</kernpairs>', '</font>', '']

    with open(os.path.join(FONTS, name + ".xml"), "w", encoding="utf-8") as f:
        f.write("\n".join(lines))

    print("%s  %d glyphs on a %dx%d page" % (name, len(cells), side, side))
    return png


def main():
    for source, name, face_name in WEIGHTS:
        png = cut(os.path.join(HERE, source), name, face_name)
        subprocess.run(["dotnet", "run", "--project", TOOL, "--", png, png[:-4] + ".dds"], check=True)


if __name__ == "__main__":
    main()
