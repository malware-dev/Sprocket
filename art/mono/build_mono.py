"""
Cuts the Maltek monospace face and writes it in the format Space Engineers reads.

Run it from anywhere:  python art/mono/build_mono.py

A fixed-width face for anything that is read as a listing rather than as prose - a saved
program above all, where a column of line numbers and a column of indentation only line up
if every character is the same width. The game ships a monospace face already and it is not
pleasant to read at this size.

The letterforms are Roboto Mono, by the Roboto Mono Project Authors, under the SIL Open Font
License 1.1. See OFL.txt beside this file, which ships with the mod because the licence says
it must: a bitmap cut from a font is a modified version of it, and every copy has to carry the
notice and the licence. The authors reserved no font name, so this could have kept theirs. It
does not, because the name in a font definition is a lookup key rather than a byline, and the
rasterising is ours to answer for rather than theirs.

The output is a page PNG and an XNA bitmapfont XML. The PNG still has to be encoded to DDS
before the game will load it - see art/symbols/FontTool, which does that and knows the format
the game insists on.

The font file is read from beside this script rather than from the machine's installed fonts,
so a clone of this repository can rebuild the face without installing anything. That is also
why the SDK's own BMFontGen is not used: it takes a font by family name and can only see what
Windows already has.
"""

import math
import os
import xml.sax.saxutils as esc

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
FONTS = os.path.join(ROOT, "src", "Sprocket.Host.SpaceEngineers", "Content", "Fonts", "Maltek")

SOURCE = os.path.join(HERE, "RobotoMono-Regular.ttf")

NAME = "MaltekMono"

# The size whose line height lands on the game's own.
#
# Both of the game's faces declare height="37" base="30" at size 23, and a row of the editor is
# built to that. Roboto Mono reaches a line of exactly 37 at 27, which is the whole reason for
# this number - matching the game's *size* instead would have matched a different typeface's
# metrics and put every line of a listing out of step with every other row on the screen.
SIZE = 29

# The line, the cell and where the baseline sits in it, all transcribed from the game's own face
# so a glyph of ours lands where a glyph of theirs would.
#
# The game draws a cell a fixed distance plus its lsb below the top of the line (see
# SeFontMetrics.CellTop), so where the ink sits in the cell decides where our baseline lands.
LINE = 37
BASE = 30
CELL_HEIGHT = 45

# Where the baseline sits in the cell.
#
# Read off a screenshot of both faces set side by side in the game. At 35, where the game's own
# face has its baseline, every row of ours sat exactly one pixel below the game's, on four rows
# measured, which is what 34 corrects.
BASELINE_IN_CELL = 34

# The advance and the room either side of it are measured from the face rather than declared, so
# that SIZE is the only number to change and the cell cannot be left too small for the ink.

# What the face covers, as the game's own default face declares it.
#
# Taken from the SDK's Options.txt, which is what Keen cut FontDataPA from. Whatever of these
# Roboto Mono has is included and the rest is simply absent - the point is to cover what the rest
# of the editor can draw, so that text reading correctly beside a listing reads correctly inside
# one.
RANGES = [
    (0x0020, 0x02FF),
    (0x0400, 0x04FF),
    (0x2000, 0x2043),
    (0x2045, 0x20FF),
    (0x2122, 0x2122),
    (0x2212, 0x2212),
]

# The private use area is left empty on purpose, and the space is left for it.
#
# Space Engineers keeps its own imagery there: the controller buttons at E001-E027 and the marks
# at E030-E053 in the default face, and a modder's much larger set in the game's monospace one,
# including the colour squares at E100-E2FF. None of it is letterforms and none of it can be cut
# from a typeface - it is drawn by hand, and fitting it to a fixed advance is drawing it again.
#
# So these codepoints stay unassigned rather than being designed away. Anything that lands on one
# draws U+25A1 instead, which is a box and reads as a gap rather than as a fault.
RESERVED = [
    (0xE001, 0xE027),
    (0xE030, 0xE058),
    (0xE070, 0xE078),
    (0xE100, 0xE2FF),
]


def room(face, codes):
    """
    How far a character advances, and how far the ink reaches past that either way.

    <b>The whole covered set, not the part a listing shows.</b> This face is shipped for anyone to
    use, so the cell has to hold its worst character rather than its typical one - which is an A
    carrying both a ring and an acute, and not any of the brackets a script is full of.
    """

    ascent, _ = face.getmetrics()
    advance = face.getlength("M")

    left = right = above = below = 0
    for code in codes:
        box = face.getbbox(chr(code))
        if box is None or box[3] <= box[1]:
            continue

        left = min(left, box[0])
        right = max(right, box[2] - advance)
        above = max(above, ascent - box[1])
        below = max(below, box[3] - ascent)

    # A pixel past the furthest ink, so antialiasing at the edge is never shaved.
    bearing = int(math.ceil(max(-left, right))) + 1

    return int(round(advance)), bearing, above, below


def wanted(face):
    """Every codepoint this face is asked for and actually has, in order."""

    from fontTools.ttLib import TTFont

    has = set()
    for table in TTFont(SOURCE)["cmap"].tables:
        has |= set(table.cmap.keys())

    return [c for first, last in RANGES for c in range(first, last + 1) if c in has]


def drawn(face, advance, bearing, cell_width):
    """
    The two the game's face has and Roboto Mono does not, drawn here.

    A bullet operator and an empty square, neither of which is a letter. The square earns its
    place twice over: it is also what a renderer shows for a character no glyph exists for, so
    having it is the difference between a gap that looks deliberate and one that looks broken.

    Both are sized from the face rather than chosen: the dot is the full stop's, on the axis
    halfway up the lowercase, and the square is the lowercase height and sits on the baseline.
    Numbers picked by eye would be a second designer's opinion inside somebody else's typeface.
    """

    from fontTools.ttLib import TTFont
    from PIL import Image, ImageDraw

    metrics = TTFont(SOURCE)
    per_em = metrics["head"].unitsPerEm
    lowercase = metrics["OS/2"].sxHeight * SIZE / float(per_em)

    stop = face.getbbox(".")
    dot = (stop[3] - stop[1]) / 2.0

    ink = {}
    middle = bearing + advance / 2.0

    for code in (0x2219, 0x25A1):
        cell = Image.new("L", (cell_width, CELL_HEIGHT), 0)
        draw = ImageDraw.Draw(cell)

        if code == 0x2219:
            axis = BASELINE_IN_CELL - lowercase / 2.0
            draw.ellipse([middle - dot, axis - dot, middle + dot, axis + dot], fill=255)
        else:
            half = round(lowercase) / 2.0
            draw.rectangle(
                [middle - half, BASELINE_IN_CELL - half * 2, middle + half, BASELINE_IN_CELL],
                outline=255, width=1)

        ink[code] = cell

    return ink


def main():
    from PIL import Image, ImageDraw, ImageFont

    face = ImageFont.truetype(SOURCE, SIZE)
    ascent, _ = face.getmetrics()

    codes = wanted(face)
    advance, bearing, above, below = room(face, codes)
    cell_width = advance + bearing * 2

    if above > BASELINE_IN_CELL or below > CELL_HEIGHT - BASELINE_IN_CELL:
        raise SystemExit(
            "the ink needs %d above the baseline and %d below, and the cell holds %d and %d"
            % (above, below, BASELINE_IN_CELL, CELL_HEIGHT - BASELINE_IN_CELL))

    custom = drawn(face, advance, bearing, cell_width)
    codes += [c for c in sorted(custom) if c not in codes]

    across = 1024 // cell_width
    down = (len(codes) + across - 1) // across

    side = 1
    while side < max(across * cell_width, down * CELL_HEIGHT):
        side *= 2

    page = Image.new("RGBA", (side, side), (255, 255, 255, 0))
    entries = []

    for i, code in enumerate(codes):
        x, y = (i % across) * cell_width, (i // across) * CELL_HEIGHT

        if code in custom:
            alpha = custom[code]
        else:
            cell = Image.new("L", (cell_width, CELL_HEIGHT), 0)
            pen = ImageDraw.Draw(cell)

            # The pen goes at the bearing, and on the baseline the cell declares. PIL draws from
            # the top of the line, so the ascent is what turns one into the other.
            pen.text((bearing, BASELINE_IN_CELL - ascent), chr(code), font=face, fill=255)
            alpha = cell

        white = Image.new("RGBA", (cell_width, CELL_HEIGHT), (255, 255, 255, 255))
        white.putalpha(alpha)

        page.paste(white, (x, y))
        entries.append((code, x, y))

    os.makedirs(FONTS, exist_ok=True)
    png = os.path.join(FONTS, NAME + "-0.png")
    page.save(png)

    lines = [
        '<?xml version="1.0" encoding="UTF-8" ?>',
        '<font',
        '\txmlns="http://xna.microsoft.com/bitmapfont"',
        '\txmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"',
        '',
        '\tname="%s" base="%d" height="%d"' % (NAME, BASE, LINE),
        '\tface="Maltek Mono" size="%d"' % (SIZE,),
        '\t>',
        '<bitmaps>',
        '<bitmap id="0" name="%s-0.dds" size="%dx%d" />' % (NAME, side, side),
        '</bitmaps>',
        '<glyphs>',
    ]

    for code, x, y in entries:
        lines.append(
            '<glyph ch=%s code="%04X" bm="0" origin="%d,%d" size="%dx%d" aw="%d" lsb="%d" />'
            % (esc.quoteattr(chr(code)), code, x, y, cell_width, CELL_HEIGHT, advance, -bearing))

    lines += ['</glyphs>', '<kernpairs>', '</kernpairs>', '</font>', '']

    xml = os.path.join(FONTS, NAME + ".xml")
    with open(xml, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))

    print("page  %s   %dx%d" % (png, side, side))
    print("xml   %s   %d glyphs, %d of them drawn here" % (xml, len(entries), len(custom)))
    print("line  %d declared, ink %d (%d above the baseline, %d below), %d to spare"
          % (LINE, above + below, above, below, LINE - above - below))
    print("cut   em %d, advance %d, cell %dx%d, bearing %d"
          % (SIZE, advance, cell_width, CELL_HEIGHT, bearing))


if __name__ == "__main__":
    main()
