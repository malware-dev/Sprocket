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
FONTS = os.path.join(ROOT, "src", "Sprocket.Host.SpaceEngineers", "Content", "Fonts", "Maltek")

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

# The baseline where our own artwork's baseline falls, not where the game's does.
#
# A renderer puts a glyph's cell at (base - baseline in cell) from the top of the line. Declaring the
# game's 30/45 left ours seven units adrift of its own ink, so the symbol hung below the box it had
# just been centred in. Declaring the same proportion the cell is actually built to makes that
# difference zero and the cell start exactly at the top of the line.
BASE = round(CELL * 35 / 45)

# And the ink is centred on the LINE rather than on the cell.
#
# The cell is square and the line is shorter, because the game's face spends the difference on
# descenders and ours copies its proportions. Nothing here has a descender, so ink centred in the cell
# sits low in the line by half that difference - which is exactly how far the tick sat off the middle
# of its checkbox.
#
# The factor is empirical. Half the cell/line difference was the arithmetic answer and still drew low
# in the game, so it is a whole difference now. Nothing outside the game can referee this: the
# simulator's vertical rule is its own reconstruction and it drew both acceptably.
INK = (CELL - LINE) / CELL

# And a bearing of zero, which is the one metric NOT copied from the game's face.
#
# Its -7 is there to tuck letters together as a pen walks along a word. A symbol is never in a word:
# it is drawn on its own and centred in whatever room it was given, using the advance width to work
# out the middle. A negative bearing then drags it left of that middle by a tenth of its cell, which
# looks exactly like a glyph that was drawn wrong - it sat touching the left wall of its checkbox.
LSB = 0


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


def cycle():
    """
    Refresh: a square loop, open at the top right, with the head running into the gap.

    A loop rather than a u-turn, because this is the same thing again rather than a step back, and
    the break is what makes it read as going round rather than as a box. Square for the same reason
    undo is: the face beside it has no curves.
    """
    left, right = 0.24, 0.76
    top, bottom = 0.30, 0.72

    return [
        stroke((left, top), (right - 0.30, top)),
        stroke((left, top), (left, bottom)),
        stroke((left, bottom), (right, bottom)),
        stroke((right, bottom), (right, top + 0.16)),
        head((right - 0.02, top), (right - 0.32, top), 0.30),
    ]


def arrow():
    """
    A shaft with a head on it: "leads to".

    Distinct from the chevron on purpose. A chevron is a direction to travel and belongs on a
    control somebody presses; this is a consequence, and belongs in a sentence between what
    happened and what it caused. Same head as the undo arrow, so the two read as one family.
    """
    y = 0.52
    return [
        stroke((0.16, y), (0.60, y)),
        head((0.84, y), (0.58, y), 0.30),
    ]


def check():
    """
    A tick. Yes, as opposed to the game's own filled square.

    Space Engineers draws a checked box as a smaller box inside a bigger one, and its indeterminate
    state as a bar - which is faithful to itself and reads as "partly" to anybody who has used any
    other interface in their life. This audience has used other interfaces and not this one, so the
    tick wins over the convention. Flat terminals like everything else here; no taper.
    """
    return [
        stroke((0.20, 0.50), (0.41, 0.71)),
        stroke((0.38, 0.71), (0.80, 0.29)),
    ]


def bang():
    """
    An exclamation. Says something is wrong without saying what.

    Plain, rather than inside a triangle. A triangle with a bar and a dot inside it is three
    shapes competing for about twenty pixels once it is drawn at the size a row heading uses,
    and the outline is the first thing to turn to mush - where the bar alone stays legible.

    The gap between bar and dot is wider than a letter's would be, because at this size a
    tight one closes up and reads as a single stroke.
    """
    return [
        stroke((0.50, 0.17), (0.50, 0.56)),
        stroke((0.50, 0.71), (0.50, 0.79)),
    ]


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


def sheets():
    """
    Duplicate: two sheets, the one behind showing only the edges the one in front does not cover.

    Everything here is one flat colour, so nothing can be drawn as being behind anything - an
    occluded edge has to simply not be drawn. The sheet behind is therefore its left and top edges
    in full and the two stubs of its other sides that stand clear of the sheet in front, each
    stopping exactly on that sheet's outline so the two read as joined.

    Two complete outlines overlapping was the first attempt at this and it loses the gap between
    them first, which at twenty pixels is the whole of what makes it read as two.
    """
    near, far = 0.34, 0.82
    back, over = 0.18, 0.66
    return [
        poly((near, near), (far, near), (far, far), (near, far)),
        stroke((back, back), (back, over)),
        stroke((back, back), (over, back)),
        stroke((over, back), (over, near)),
        stroke((back, over), (near, over)),
    ], [
        poly((near + WEIGHT, near + WEIGHT), (far - WEIGHT, near + WEIGHT),
             (far - WEIGHT, far - WEIGHT), (near + WEIGHT, far - WEIGHT)),
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


def pencil():
    """
    Rename: a pencil, lying at a diagonal with its point to the lower left.

    The one symbol here that stands for an act on words rather than on a thing, and the reason it
    exists is that the word "rename" beside a heading read as a second heading. A picture beside a
    name is furniture; a word beside a name is a name.

    Barrel and nib as one line, with the ferrule taken back out of it as a hole rather than drawn
    as a gap - a gap would need the barrel in two pieces and they would not stay aligned when the
    weight changes. Nothing else about it: at the size this is drawn, an eraser and a wood grain
    are three grey pixels that make the shape harder to read rather than easier.
    """
    # Sized to the extent the rest of the set occupies rather than to the cell. A stroke squares its
    # ends, so at forty-five degrees the barrel's corners stand half its width out in both directions
    # past where the line stops - which is why the far end sits short of where it looks like it should.
    tip = (0.25, 0.75)
    neck = (0.38, 0.62)
    butt = (0.665, 0.335)
    barrel = 0.24

    # Across the barrel rather than along it, so the cut stays square to the pencil at any weight.
    cut = 0.085
    return [stroke(neck, butt, barrel), head(tip, neck, barrel)],            [stroke((neck[0] - cut, neck[1] - cut), (neck[0] + cut, neck[1] + cut), 0.05)]


def cross():
    """
    Dismiss: putting something away, which is never destroying what is on it. That is the bin, and
    the two are deliberately nothing alike.

    The one shape here the game also has, kept anyway because its copy cannot be borrowed. The
    game's cross belongs to a control on the host screen, and the harness draws its controls before
    it hands over to the mod - so the first opaque thing a mod paints buries it. Anything that
    draws its own panel has to carry its own way off it.

    Not drawn by Sprocket today: the chevron in the header does that job, on every screen including
    the front door. It is here for the dialogs that do not exist yet, where a corner cross is the
    only thing anybody will look for.

    Same extent as the chevrons, so a row of controls in the chrome reads as one set.
    """
    near, far = 0.30, 0.70
    return [
        stroke((near, near), (far, far)),
        stroke((near, far), (far, near)),
    ]


def minus():
    return [stroke((0.24, 0.5), (0.76, 0.5))]


def plus():
    return [stroke((0.24, 0.5), (0.76, 0.5)), stroke((0.5, 0.24), (0.5, 0.76))]


def play():
    """
    A triangle pointing the way a thing runs. Doing now, by hand, what something else would do.

    The one symbol here that everybody already knows, which is the whole reason to spend a glyph
    on it rather than two words: it is on every machine that has ever played anything. Squared
    off like the rest of the set, and sitting a little left of centre so the mass of it reads as
    centred - a triangle's weight is not where its bounding box is.
    """
    left, right, half = 0.32, 0.70, 0.24
    return [poly((left, 0.5 - half), (right, 0.5), (left, 0.5 + half))]


def stop():
    """
    A square. Stopping by hand what is running, which is the other half of play.

    Smaller than the triangle it stands beside, and centred where that one sits left: a filled
    square reads heavier than a triangle of the same height, so matching their boxes would make
    this one look like the louder of the two. They are the same act at the same weight.
    """
    half = 0.21
    return [poly((0.5 - half, 0.5 - half), (0.5 + half, 0.5 - half),
                 (0.5 + half, 0.5 + half), (0.5 - half, 0.5 + half))]


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
    ("a", "arrow", solid(arrow())),
    ("c", "check", solid(check())),
    ("p", "pencil", pencil()),
    ("x", "cross", solid(cross())),
    ("!", "warning", solid(bang())),
    ("s", "refresh", solid(cycle())),
    ("2", "duplicate", sheets()),
    ("g", "play", solid(play())),
    # Not "s", which refresh already has. The letter is in the word and nothing else wanted it.
    ("t", "stop", solid(stop())),
]


def render(glyph):
    """One glyph as an alpha image, drawn oversized and averaged down."""
    from PIL import Image, ImageDraw

    shapes, holes = glyph
    size = CELL * OVER
    page = Image.new("L", (size, size), 0)
    draw = ImageDraw.Draw(page)

    for shape in shapes:
        draw.polygon([(x * size, (y - INK) * size) for (x, y) in shape], fill=255)

    for hole in holes:
        draw.polygon([(x * size, (y - INK) * size) for (x, y) in hole], fill=0)

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
