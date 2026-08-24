"""
Draws the Maltek symbol font and writes it in the format Space Engineers reads.

Run it from anywhere:  python art/symbols/build_font.py

A shared set of the symbols a user interface needs and the game does not provide - it has no
undo, no redo and no delete glyph, and its only cross means dismiss rather than destroy. A font
rather than a set of sprites, because anything that draws text can draw these, and a font is
resolved by name out of the same registry on a screen and on a text panel alike.

The output is a page PNG and an XNA bitmapfont XML. The PNG still has to be encoded to DDS
before the game will load it - see FontTool beside this file, which does that and knows the
format the game insists on.

Nothing here is clever. Each symbol is a handful of polygons in a unit square, drawn eight
times oversized and averaged down, which is the only way to get a clean diagonal out of a
raster at this size. Keeping the shapes as coordinates rather than as image files means the
whole set can be re-cut at a different weight by changing one number.
"""

import math
import os
import xml.sax.saxutils as esc

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
FONTS = os.path.join(ROOT, "src", "Reactor.Host.SpaceEngineers", "Content", "Fonts", "Maltek")

NAME = "MaltekSymbols"

# One glyph is a square of this many pixels on the page. Big enough that the screen never
# magnifies it - the biggest a symbol is drawn at is around a third of this - and small enough
# that the whole set fits one modest page.
CELL = 64

# How much of the cell a stroke takes. The game's own face is a hairline; these sit a little
# heavier because a symbol has to survive being read at a glance where a letter has its
# neighbours to lean on.
WEIGHT = 0.125

# Drawn this many times oversized and averaged down.
OVER = 8

# The metrics, in the same proportions the game's own face uses.
#
# The game's font is a 45 cell with a line height of 37, a baseline 30 down the line and 35 down the
# cell, and a left side bearing of -7. Nothing here can see what the renderer then does with those
# numbers - that drawing lives in the render assembly - so rather than guess the rule, this font is
# built to the same shape scaled to its own cell. Whatever the rule turns out to be it lands the same
# way on both, and a symbol sits on a line of text where a capital letter would.
#
# Getting it wrong is not subtle, but it is easy to misread: the first cut declared a baseline of 56
# in a 64 cell, and every symbol drew twelve pixels below the words beside it.
LINE = round(CELL * 37 / 45)
BASE = round(CELL * 30 / 45)
LSB = round(CELL * -7 / 45)


def poly(*points):
    return list(points)


def stroke(a, b, w=WEIGHT):
    """A line from a to b as a quad, with squared ends, in unit coordinates."""
    (x1, y1), (x2, y2) = a, b
    dx, dy = x2 - x1, y2 - y1
    length = math.hypot(dx, dy)
    if length == 0:
        return []
    nx, ny = -dy / length * w / 2, dx / length * w / 2
    return poly((x1 + nx, y1 + ny), (x2 + nx, y2 + ny), (x2 - nx, y2 - ny), (x1 - nx, y1 - ny))


def head(tip, back, width):
    """An arrowhead: a triangle pointing at tip, its base back along the given direction."""
    (tx, ty), (bx, by) = tip, back
    dx, dy = tx - bx, ty - by
    length = math.hypot(dx, dy)
    nx, ny = -dy / length * width / 2, dx / length * width / 2
    return poly((tx, ty), (bx + nx, by + ny), (bx - nx, by - ny))


def uturn(mirrored):
    """
    Undo: along the bottom, up, back along the top, into a head pointing where it came from.

    Deliberately square. The face this sits beside is all right angles and flat terminals, and
    a circular arrow would be the only curve on the screen. It is also the shape that cannot be
    confused with Back, which is a single open chevron and nothing else.
    """
    left, right = 0.22, 0.80
    top, bottom = 0.34, 0.70
    shapes = [
        stroke((left + 0.14, bottom), (right, bottom)),
        stroke((right, bottom), (right, top)),
        stroke((right, top), (left + 0.16, top)),
        head((left, top), (left + 0.26, top), 0.30),
    ]
    if mirrored:
        shapes = [[(1.0 - x, y) for (x, y) in s] for s in shapes]
    return shapes


def bin_():
    """
    Delete: a bin. It says destroy and nothing else, which is exactly what the game's close
    symbol does not - that one says dismiss, and borrowing it was the mistake this replaces.
    """
    top, bottom = 0.36, 0.80
    half, foot = 0.27, 0.21
    return [
        stroke((0.5 - 0.14, top - 0.12), (0.5 + 0.14, top - 0.12)),
        stroke((0.5 - half - 0.03, top), (0.5 + half + 0.03, top)),
        stroke((0.5 - half + 0.02, top), (0.5 - foot, bottom)),
        stroke((0.5 + half - 0.02, top), (0.5 + foot, bottom)),
        stroke((0.5 - foot, bottom), (0.5 + foot, bottom)),
    ]


def chevron(turns):
    """An open arrowhead pointing left, turned a quarter at a time clockwise."""
    tip, back, half = 0.30, 0.64, 0.22
    shapes = [
        stroke((back, 0.5 - half), (tip, 0.5)),
        stroke((tip, 0.5), (back, 0.5 + half)),
    ]
    for _ in range(turns % 4):
        shapes = [[(1.0 - y, x) for (x, y) in s] for s in shapes]
    return shapes


def circle(r):
    steps = 96
    return [(0.5 + r * math.cos(t * 2 * math.pi / steps),
             0.5 + r * math.sin(t * 2 * math.pi / steps)) for t in range(steps)]


def disc():
    """A filled circle. The mark on a chosen row where only one thing may be chosen."""
    return [circle(0.30)], []


def ring():
    """
    The same circle, hollow.

    The hole is a second shape taken back out rather than a reversed winding, because the thing
    drawing this fills each polygon on its own and knows nothing about even-odd rules. Drawing
    the inside as a hole is the only way to get a ring instead of a disc, which is exactly the
    mistake the first cut of this made.
    """
    return [circle(0.30)], [circle(0.30 - WEIGHT)]


def minus():
    return [stroke((0.24, 0.5), (0.76, 0.5))]


def plus():
    return [stroke((0.24, 0.5), (0.76, 0.5)), stroke((0.5, 0.24), (0.5, 0.76))]


def solid(shapes):
    """Most glyphs are just filled shapes with nothing taken back out."""
    return shapes, []


# Readable codes on purpose. Nothing but symbols is ever drawn with this font, so there is no
# collision to avoid, and a draw list that says "u" is one somebody can read.
GLYPHS = [
    ("u", "undo", solid(uturn(False))),
    ("r", "redo", solid(uturn(True))),
    ("d", "delete", solid(bin_())),
    ("<", "chevron left", solid(chevron(0))),
    ("^", "chevron up", solid(chevron(1))),
    (">", "chevron right", solid(chevron(2))),
    ("v", "chevron down", solid(chevron(3))),
    ("o", "ring", ring()),
    ("0", "disc", disc()),
    ("-", "minus", solid(minus())),
    ("+", "plus", solid(plus())),
]


def render(glyph):
    """One glyph as an alpha image, drawn oversized and averaged down."""
    from PIL import Image, ImageDraw

    shapes, holes = glyph
    size = CELL * OVER
    page = Image.new("L", (size, size), 0)
    draw = ImageDraw.Draw(page)

    for shape in shapes:
        draw.polygon([(x * size, y * size) for (x, y) in shape], fill=255)

    for hole in holes:
        draw.polygon([(x * size, y * size) for (x, y) in hole], fill=0)

    return page.resize((CELL, CELL), Image.LANCZOS)


def main():
    from PIL import Image

    across = 8
    down = (len(GLYPHS) + across - 1) // across
    width, height = across * CELL, down * CELL

    # A power of two, because a texture is happier that way and the cost is nothing.
    side = 1
    while side < max(width, height):
        side *= 2

    page = Image.new("RGBA", (side, side), (255, 255, 255, 0))
    entries = []

    for i, (ch, label, glyph) in enumerate(GLYPHS):
        x, y = (i % across) * CELL, (i // across) * CELL
        alpha = render(glyph)

        cell = Image.new("RGBA", (CELL, CELL), (255, 255, 255, 0))
        cell.putalpha(alpha)
        white = Image.new("RGBA", (CELL, CELL), (255, 255, 255, 255))
        white.putalpha(alpha)

        page.paste(white, (x, y))
        entries.append((ch, label, x, y))

    os.makedirs(FONTS, exist_ok=True)
    png = os.path.join(FONTS, NAME + "-0.png")
    page.save(png)

    # The metrics. A symbol is square and advances its own width, so there is no bearing and no
    # kerning - both of which the game's own face needs and this one does not.
    lines = [
        '<?xml version="1.0" encoding="UTF-8" ?>',
        '<font',
        '\txmlns="http://xna.microsoft.com/bitmapfont"',
        '\txmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"',
        '',
        '\tname="%s" base="%d" height="%d"' % (NAME, BASE, LINE),
        '\tface="Maltek Symbols" size="%d"' % (LINE,),
        '\t>',
        '<bitmaps>',
        '<bitmap id="0" name="%s-0.dds" size="%dx%d" />' % (NAME, side, side),
        '</bitmaps>',
        '<glyphs>',
    ]

    for ch, label, x, y in entries:
        lines.append(
            '<glyph ch=%s code="%04X" bm="0" origin="%d,%d" size="%dx%d" aw="%d" lsb="%d" />'
            % (esc.quoteattr(ch), ord(ch), x, y, CELL, CELL, CELL - 1, LSB))

    lines += ['</glyphs>', '<kernpairs>', '</kernpairs>', '</font>', '']

    xml = os.path.join(FONTS, NAME + ".xml")
    with open(xml, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))

    print("page  %s   %dx%d" % (png, side, side))
    print("xml   %s   %d glyphs" % (xml, len(entries)))


if __name__ == "__main__":
    main()
