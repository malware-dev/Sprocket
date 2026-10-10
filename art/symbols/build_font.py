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
FONTS = os.path.join(ROOT, "src", "Maltek.Symbols", "Content", "Fonts", "Maltek")

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

# The line is the cell, and that is the whole of the vertical design.
#
# A symbol has no baseline, no ascender and no descender. It is a mark, and what it owes anybody who
# draws it is to sit in the middle of the line it declares. The only way to promise that without
# knowing how a renderer places a cell against a line is to leave no room between them: a cell the
# height of its own line has no slack for the ink to be lost in, so ink centred in the cell is ink
# centred in the line, by construction rather than by arithmetic.
#
# <b>This font is not cut to match Space Engineers' text.</b> The game's face sits slightly high in
# its own line, and correcting for that here would bake one consumer's quirk into a font meant to be
# used by anyone. Where a symbol has to line up with the game's letters, the code that puts it there
# corrects it - see Widgets.
#
# The record of getting this wrong is worth keeping: the first cut declared a baseline of 56 in a 64
# cell and drew twelve pixels low; the second copied the game's cell-to-line proportions and left the
# ink adrift in the slack, which was then chased with a shift that was doubled by eye when the
# arithmetic answer still looked wrong. All of that was the slack.
LINE = CELL

# Where a renderer is told the baseline falls.
#
# It has no meaning for a mark, but the format has the field and a renderer places the cell by it.
# The game's proportion of its own cell is the one answer that is not invented, and with the cell and
# the line the same height it puts the cell exactly on the line.
BASE = round(CELL * 35 / 45)

# And a bearing of zero, which is the one metric NOT copied from the game's face.
#
# Its -7 is there to tuck letters together as a pen walks along a word. A symbol is never in a word:
# it is drawn on its own and centred in whatever room it was given, using the advance width to work
# out the middle. A negative bearing then drags it left of that middle by a tenth of its cell, which
# looks exactly like a glyph that was drawn wrong - it sat touching the left wall of its checkbox.
LSB = 0


# How far the glyph being drawn is scaled, set by fitted() while it works out the size.
#
# It is applied to the points a shape is written from and never to the width a stroke is drawn
# at, which is the whole point: every mark in the set is one weight, and scaling finished artwork
# would thin the ones that shrink and fatten the ones that grow.
_fit = 1.0


def poly(*points):
    """A shape, in unit coordinates, at whatever size the set has settled on."""
    return [(0.5 + (x - 0.5) * _fit, 0.5 + (y - 0.5) * _fit) for (x, y) in points]


def raw(*points):
    """The same, already placed - for the primitives that work out their own offsets."""
    return list(points)


def placed(p):
    """One point, at whatever size the set has settled on."""
    return 0.5 + (p[0] - 0.5) * _fit, 0.5 + (p[1] - 0.5) * _fit


def stroke(a, b, w=WEIGHT):
    """A line from a to b as a quad, with squared ends, in unit coordinates."""
    (x1, y1), (x2, y2) = placed(a), placed(b)
    dx, dy = x2 - x1, y2 - y1
    length = math.hypot(dx, dy)
    if length == 0:
        return []
    nx, ny = -dy / length * w / 2, dx / length * w / 2
    return raw((x1 + nx, y1 + ny), (x2 + nx, y2 + ny), (x2 - nx, y2 - ny), (x1 - nx, y1 - ny))


def path(points, w=WEIGHT):
    """
    A polyline of the given width, mitred at every turn, as one polygon.

    Two strokes meeting at a corner do not make a corner: each ends in an edge square to its own
    direction, so their union is notched on the outside and doubled on the inside. At the size
    these are drawn that reads as a jog of a pixel or two at every turn, which is most of what
    makes a mark look coarse beside a shape that has no turns in it.

    So the whole path is offset at once and each corner carried out to where its two edges
    actually cross. The ends stay square, which is the house style.
    """

    def unit(p, q):
        dx, dy = q[0] - p[0], q[1] - p[1]
        length = math.hypot(dx, dy)
        return (dx / length, dy / length) if length else (0.0, 0.0)

    points = [placed(p) for p in points]
    count = len(points)
    ways = [unit(points[i], points[i + 1]) for i in range(count - 1)]
    edges = [(-d[1], d[0]) for d in ways]

    def along(sign):
        half = w / 2.0 * sign
        out = [(points[0][0] + edges[0][0] * half, points[0][1] + edges[0][1] * half)]

        for i in range(1, count - 1):
            before, after = edges[i - 1], edges[i]
            mx, my = before[0] + after[0], before[1] + after[1]
            length = math.hypot(mx, my)

            if length == 0:
                out.append((points[i][0] + before[0] * half, points[i][1] + before[1] * half))
                continue

            mx, my = mx / length, my / length

            # How far along the bisector the two offset edges cross. It runs away as a turn
            # approaches a fold, so it is held to something a glyph can contain.
            reach = min((w / 2.0) / max(mx * before[0] + my * before[1], 0.2), w * 3) * sign
            out.append((points[i][0] + mx * reach, points[i][1] + my * reach))

        out.append((points[-1][0] + edges[-1][0] * half, points[-1][1] + edges[-1][1] * half))
        return out

    return raw(*(along(1) + list(reversed(along(-1)))))


def head(tip, back, width):
    """An arrowhead: a triangle pointing at tip, its base back along the given direction."""
    (tx, ty), (bx, by) = placed(tip), placed(back)
    dx, dy = tx - bx, ty - by
    length = math.hypot(dx, dy)
    nx, ny = -dy / length * width / 2, dx / length * width / 2
    return raw((tx, ty), (bx + nx, by + ny), (bx - nx, by - ny))


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
        path([(left + 0.14, bottom), (right, bottom), (right, top), (left + 0.16, top)]),
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
        path([(right - 0.30, top), (left, top), (left, bottom), (right, bottom),
              (right, top + 0.16)]),
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


def finish():
    """
    An arrow meeting a bar: runs until it is finished.

    The arrow's shaft and head, stopped by an upright where it arrives, so it reads beside play and
    stop without repeating either: this is neither starting nor stopping but going on to an end.
    """
    y = 0.5
    return [
        stroke((0.14, y), (0.52, y)),
        head((0.68, y), (0.46, y), 0.30),
        stroke((0.80, 0.26), (0.80, 0.74)),
    ]


def check():
    """
    A tick. Yes, as opposed to the game's own filled square.

    Space Engineers draws a checked box as a smaller box inside a bigger one, and its indeterminate
    state as a bar - which is faithful to itself and reads as "partly" to anybody who has used any
    other interface in their life. This audience has used other interfaces and not this one, so the
    tick wins over the convention. Flat terminals like everything else here; no taper.
    """
    return [path([(0.20, 0.50), (0.395, 0.705), (0.80, 0.29)])]


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
    symbol does not - that one says dismiss, and borrowing it was the mistake this replaced.

    Solid, with the slots taken back out. The first cut drew the whole thing in strokes and read
    as a tub with a plank over it: at twenty pixels an outline that thin loses its corners, and
    what survives is the silhouette. So the silhouette is what is drawn, and the two slots are
    the only detail kept, because a body with no slots is a plant pot.

    It is also cut to the height of the tick and the cross rather than to the room available. The
    stroked one stood a third taller than the marks it sits beside in a row, which is not a thing
    a reader forgives in a set of symbols.
    """
    lift, foot = 0.34, 0.74

    handle = poly((0.435, 0.26), (0.565, 0.26), (0.565, 0.315), (0.435, 0.315))
    lid = poly((0.285, 0.325), (0.715, 0.325), (0.715, 0.385), (0.285, 0.385))
    body = poly((0.335, lift), (0.665, lift), (0.625, foot), (0.375, foot))

    # Taken back out of the body, and tapering with it so they read as cut into the same object.
    slots = [
        poly((0.435, 0.415), (0.475, 0.415), (0.468, 0.675), (0.428, 0.675)),
        poly((0.525, 0.415), (0.565, 0.415), (0.572, 0.675), (0.532, 0.675)),
    ]

    return [handle, lid, body], slots


def sheets():
    """
    Duplicate: two sheets, the one behind showing only the edges the one in front does not cover.

    Everything here is one flat colour, so nothing can be drawn as being behind anything - an
    occluded edge has to simply not be drawn. The sheet behind is therefore its left and top edges
    in full and the two stubs of its other sides that stand clear of the sheet in front. The stubs
    stop a little short of that sheet rather than running into it: two white shapes that touch are
    one white shape, and the dark between them is the whole of what says there are two.

    <b>Drawn at six units where the set is drawn at eight, and that is the correction, not a
    lapse.</b> Every other mark here is an open stroke with dark on both sides of it. This one is a
    frame inside a frame, and the set's stroke leaves an opening no wider than the border round it
    - which reads as a filled square with a window punched in it, and reads at the small sizes as
    a blob. A nested shape needs a lighter line than an open one to carry the same weight beside
    it, which is the ordinary optical correction and is why the width is a number here rather than
    the set's.
    """
    line = 0.094
    gap = 0.07
    near, far = 0.37, 0.85
    back, over = 0.15, 0.63

    # The opening is set out from the sheet after it has been placed, not written as coordinates
    # beside it. Written that way the inset is scaled along with the points, so the front sheet's
    # border came out thinner than the back sheet's outline - one icon drawn at two weights.
    front = poly((near, near), (far, near), (far, far), (near, far))
    (left, top), (right, bottom) = front[0], front[2]

    return [
        front,

        # One path rather than four strokes. Its three turns are corners, and two quads meeting at
        # one only make a notch - which is what this glyph had at every turn of it.
        path([(over, near - gap), (over, back), (back, back), (back, over), (near - gap, over)],
             line),
    ], [
        raw((left + line, top + line), (right - line, top + line),
            (right - line, bottom - line), (left + line, bottom - line)),
    ]


def chevron(turns):
    """An open arrowhead pointing left, turned a quarter at a time clockwise."""
    tip, back, half = 0.30, 0.64, 0.22
    shapes = [path([(back, 0.5 - half), (tip, 0.5), (back, 0.5 + half)])]
    for _ in range(turns % 4):
        shapes = [[(1.0 - y, x) for (x, y) in s] for s in shapes]
    return shapes


def circle(r):
    steps = 96
    return poly(*[(0.5 + r * math.cos(t * 2 * math.pi / steps),
                   0.5 + r * math.sin(t * 2 * math.pi / steps)) for t in range(steps)])


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


def pause():
    """
    Two bars. Held where it is, kept as it was, and passed over until it is let go again.

    In the same box as stop, so the two read at one weight: each bar a third of the box across,
    with a third between them.
    """
    half = 0.21
    bar = 2 * half / 3
    return [poly((0.5 - half, 0.5 - half), (0.5 - half + bar, 0.5 - half),
                 (0.5 - half + bar, 0.5 + half), (0.5 - half, 0.5 + half)),
            poly((0.5 + half - bar, 0.5 - half), (0.5 + half, 0.5 - half),
                 (0.5 + half, 0.5 + half), (0.5 + half - bar, 0.5 + half))]


def chip(pin_width=0.065):
    """
    The pieces of a two-sided chip: a body and three pins along its left side and three along its right.

    Two sides rather than four, because the gaps between the pins are what make it a chip at row size, and on four sides
    they close up into a gear.
    """
    half_wide, half_tall = 0.17, 0.27
    pins, pin_length = 3, 0.12
    shapes = [poly((0.5 - half_wide, 0.5 - half_tall), (0.5 + half_wide, 0.5 - half_tall),
                   (0.5 + half_wide, 0.5 + half_tall), (0.5 - half_wide, 0.5 + half_tall))]
    step = 2 * half_tall / (pins + 1)
    for i in range(1, pins + 1):
        y = 0.5 - half_tall + step * i
        shapes.append(stroke((0.5 - half_wide - pin_length, y), (0.5 - half_wide + 0.01, y), pin_width))
        shapes.append(stroke((0.5 + half_wide - 0.01, y), (0.5 + half_wide + pin_length, y), pin_width))
    return shapes


def chip_at(cx, cy, grow=0.0):
    """The logic system's chip, smaller, about (cx, cy), every part grown by <grow> on all sides."""
    half_wide, half_tall, pin, pin_width = 0.15, 0.23, 0.10, 0.065
    parts = [poly((cx - half_wide - grow, cy - half_tall - grow), (cx + half_wide + grow, cy - half_tall - grow),
                  (cx + half_wide + grow, cy + half_tall + grow), (cx - half_wide - grow, cy + half_tall + grow))]
    step = 2 * half_tall / 4
    for i in range(1, 4):
        y = cy - half_tall + step * i
        parts.append(stroke((cx - half_wide - pin - grow, y), (cx - half_wide + 0.01, y), pin_width + 2 * grow))
        parts.append(stroke((cx + half_wide - 0.01, y), (cx + half_wide + pin + grow, y), pin_width + 2 * grow))
    return parts


def installed():
    """
    Installation: the logic system's chip in front of another of the same, one of however many.

    The chip behind is drawn darker rather than fainter, so it is solid and never reads as a shadow, and only a little
    darker, so the two read as one kind. The gap between them follows the front chip's own outline, pins and all.
    """
    offset, gap = 0.16, 0.05
    cx, cy = 0.5 - offset / 2, 0.5 + offset / 2
    return [], chip_at(cx, cy, gap), chip_at(cx, cy), chip_at(cx + offset, cy - offset)


def logic():
    """Logic system: a chip, solid. The working thing a blueprint is the mould of."""
    return chip()


def blueprint():
    """
    Blueprint: the logic system's chip cut out of a solid square, the mould the chip is made from.

    The chip is cut at its full size, inside a thin rim, and with broader pins than the logic system's own: the cut-out is
    what has to read at row size, and a chip shrunk to leave a wide rim closes up into a dot.
    """
    half = 0.34
    square = poly((0.5 - half, 0.5 - half), (0.5 + half, 0.5 - half), (0.5 + half, 0.5 + half), (0.5 - half, 0.5 + half))
    return [square], chip(0.085)


def steps():
    """
    Sequence: three steps, taken in order down the arrow beside them.

    The steps are the bars, one under another like the rows of a sequence's screen; the arrow says
    they are gone through from the top, which is what tells a sequence from a plain list. Drawn at the
    blueprint's lighter line, because three bars at the set's stroke close their gaps into a block at row size.
    """
    left, right = 0.44, 0.84
    line = 0.094
    return [
        stroke((left, 0.24), (right, 0.24), line),
        stroke((left, 0.50), (right, 0.50), line),
        stroke((left, 0.76), (right, 0.76), line),
        stroke((0.22, 0.18), (0.22, 0.60), line),
        head((0.22, 0.82), (0.22, 0.58), 0.24),
    ]


def pulse():
    """
    Signal: one square pulse, low, high and low again.

    The shape a signal has on any trace of one: nothing, then something for a moment, then nothing. Square
    like the rest of the set, and wider than it is tall, because a pulse is read along its line. Drawn at the
    sequence's lighter line, which it sits beside on a trigger's row.
    """
    low, high = 0.66, 0.34
    return [path([(0.12, low), (0.36, low), (0.36, high), (0.64, high), (0.64, low), (0.88, low)], 0.094)]


def bolt():
    """
    Action: a lightning bolt, something a block is told to do.

    The mark every interface already uses for a quick action, and one shape: a thing done rather than a thing
    read. An action that can be waited on shows the clock in its place, so a row carries one mark.
    """
    return [poly((0.60, 0.10), (0.26, 0.56), (0.47, 0.56), (0.38, 0.90), (0.74, 0.42), (0.53, 0.42), (0.62, 0.10))]


def query():
    """
    Value: "=?", what something on a block is equal to.

    A value is what a block can be asked and what it can be set to. Every picture of one tried before this -
    a slider, a gauge, a field, a scale - read as something else first; an equals sign asking a question
    reads as nothing but itself. The question mark is squared like the rest of the set, at the lighter line.
    """
    line = 0.094
    return [
        stroke((0.10, 0.42), (0.40, 0.42), line),
        stroke((0.10, 0.58), (0.40, 0.58), line),
        path([(0.54, 0.34), (0.54, 0.22), (0.86, 0.22), (0.86, 0.48), (0.70, 0.48), (0.70, 0.64)], line),
        poly((0.65, 0.72), (0.75, 0.72), (0.75, 0.82), (0.65, 0.82)),
    ]


def clock():
    """
    Awaitable: a clock face, its hands at three.

    Something that takes time, which is what a wait on it is for. A ring, with the hands put back inside the
    hole once it is cut, so they are the same ink as the rim. Drawn at the blueprint's lighter line, with the
    hands short of the rim: at the set's stroke the hands fill the face and it reads as a chart, not a clock.
    """
    line = 0.094
    hands = [path([(0.5, 0.33), (0.5, 0.5), (0.63, 0.5)], line)]
    return [circle(0.34)], [circle(0.34 - line)], hands


def hexagon(r):
    """A regular hexagon standing on a corner, in unit coordinates before any fitting."""
    return [(0.5 + r * math.cos(math.radians(a)), 0.5 + r * math.sin(math.radians(a))) for a in (-90, -30, 30, 90, 150, 210)]


def cube():
    """
    Block: a cube seen corner on, at the lighter line.

    The kind of thing a block slot holds and a watched moment is about, where no block in particular is meant;
    a block that is named keeps the game's own picture of it. The rim is one shape with its inside cut out, so
    it has no joins, and the three edges inside are put back over the hole, ending at the rim.
    """
    line = 0.094
    r = 0.36
    inner = (r * math.cos(math.radians(30)) - line) / math.cos(math.radians(30))
    top, right, _, bottom, _, left = hexagon(inner)
    middle = (0.5, 0.5)
    return [poly(*hexagon(r))], [poly(*hexagon(inner))], [path([left, middle, right], line), stroke(middle, bottom, line)]


# The toolbar an eye looks over: where its eye sits and how wide it opens, the bar under it and the gap between.
EYE_Y, EYE_RISE, EYE_HALF = 0.38, 0.20, 0.38
BAR_TOP, BAR_BOTTOM, BAR_GAP, BAR_DOT = 0.64, 0.86, 0.06, 0.04


def lid(rise, half=EYE_HALF, steps=48):
    """An eyelid from corner to corner: bowed up for a positive rise, down for a negative one."""
    return [(0.5 - half + 2 * half * i / steps, EYE_Y - math.sin(math.pi * i / steps) * rise) for i in range(steps + 1)]


def toolbar():
    """A toolbar: a bar with three slots cut out of it, and the band of gap above it cleared of whatever is there."""
    line = 0.094
    bar = poly((0.10, BAR_TOP), (0.90, BAR_TOP), (0.90, BAR_BOTTOM), (0.10, BAR_BOTTOM))
    middle = (BAR_TOP + BAR_BOTTOM) / 2
    dots = [poly((x - BAR_DOT, middle - BAR_DOT), (x + BAR_DOT, middle - BAR_DOT), (x + BAR_DOT, middle + BAR_DOT),
                 (x - BAR_DOT, middle + BAR_DOT)) for x in (0.30, 0.5, 0.70)]
    band = poly((0.0, BAR_TOP - BAR_GAP), (1.0, BAR_TOP - BAR_GAP), (1.0, BAR_TOP), (0.0, BAR_TOP))
    return bar, dots, band, line


def toolbar_eye():
    """
    Hidden from toolbars, shown: an open eye over a toolbar.

    A block can be kept out of the toolbar's list, and this is the switch that shows such blocks after all. The eye is
    the game's own picture for showing what is hidden, and the toolbar says which list it means. Its partner, the same
    eye shut, is drawn on this one's frame, so one replaces the other without the bar moving.
    """
    bar, dots, band, line = toolbar()
    outer = poly(*(lid(EYE_RISE) + lid(-EYE_RISE)[::-1][1:-1]))
    inside = lid(EYE_RISE - line, EYE_HALF - line * 1.4) + lid(-(EYE_RISE - line), EYE_HALF - line * 1.4)[::-1][1:-1]
    inner = poly(*[(x, min(y, BAR_TOP - BAR_GAP)) for (x, y) in inside])
    pupil = [(x, y - (0.5 - EYE_Y) * _fit) for (x, y) in circle(EYE_RISE * 0.5)]
    return [outer, bar], [inner, band] + dots, [pupil]


def toolbar_eye_shut():
    """
    Hidden from toolbars, hidden: the same eye, shut, over the same toolbar.

    The lid comes down between the open eye's own two corners with its lashes below it, so the eye is seen to close
    rather than give way to another mark; drawn on the open eye's frame, see FRAMED.
    """
    bar, dots, band, line = toolbar()
    depth = 0.07
    lashes = []
    for t, lean in ((0.25, -0.035), (0.5, 0.0), (0.75, 0.035)):
        x = 0.5 - EYE_HALF + 2 * EYE_HALF * t
        y = EYE_Y + math.sin(math.pi * t) * depth
        lashes.append(stroke((x, y), (x + lean, y + 0.10), line * 0.75))
    return [bar], [band] + dots, [path(lid(-depth, EYE_HALF - line * 0.3), line)] + lashes


def solid(shapes):
    """Most glyphs are just filled shapes with nothing taken back out."""
    return shapes, []


# Each entry says how to draw its glyph rather than holding one already drawn, because the size
# the set settles on is not known until the glyph has been built once and measured - see fitted().
#
# Readable codes on purpose. Nothing but symbols is ever drawn with this font, so there is no
# collision to avoid, and a draw list that says "u" is one somebody can read.
GLYPHS = [
    ("u", "undo", lambda: solid(uturn(False))),
    ("r", "redo", lambda: solid(uturn(True))),
    ("d", "delete", lambda: bin_()),
    ("<", "chevron left", lambda: solid(chevron(0))),
    ("^", "chevron up", lambda: solid(chevron(1))),
    (">", "chevron right", lambda: solid(chevron(2))),
    ("v", "chevron down", lambda: solid(chevron(3))),
    ("o", "ring", lambda: ring()),
    ("0", "disc", lambda: disc()),
    ("-", "minus", lambda: solid(minus())),
    ("+", "plus", lambda: solid(plus())),
    ("a", "arrow", lambda: solid(arrow())),
    ("c", "check", lambda: solid(check())),
    ("p", "pencil", lambda: pencil()),
    ("x", "cross", lambda: solid(cross())),
    ("!", "warning", lambda: solid(bang())),
    ("s", "refresh", lambda: solid(cycle())),
    ("2", "duplicate", lambda: sheets()),
    ("g", "play", lambda: solid(play())),
    # Not "s", which refresh already has. The letter is in the word and nothing else wanted it.
    ("t", "stop", lambda: solid(stop())),
    ("w", "finish", lambda: solid(finish())),
    # "p" and "a", "u" and "s" are taken; "e" is the one letter of the word left.
    ("e", "pause", lambda: solid(pause())),
    ("b", "blueprint", lambda: blueprint()),
    # "s" is refresh's; "q" is the next letter of the word nothing else wanted.
    ("q", "sequence", lambda: solid(steps())),
    # "s", "g" and "a" are taken; "n" is the next letter of the word.
    ("n", "signal", lambda: solid(pulse())),
    # "a", "c", "t" and "o" are taken; "i" is left.
    ("i", "action", lambda: solid(bolt())),
    # "v", "a", "u" and "e" are taken; "l" is left.
    ("l", "value", lambda: solid(query())),
    # "c" and "o" are taken; "k" is left.
    ("k", "clock", lambda: clock()),
    # Every letter of the word is taken in lower case.
    ("B", "block", lambda: cube()),
    # Every letter of "logic" is taken; "y" is the first of "system" left.
    ("y", "logic system", lambda: solid(logic())),
    # Every letter of the word is taken in lower case.
    ("I", "installation", lambda: installed()),
    # Hidden: "h" with the eye open, showing what is hidden, and "H" with it shut.
    ("h", "hidden shown", lambda: toolbar_eye()),
    ("H", "hidden hidden", lambda: toolbar_eye_shut()),
]


# A glyph drawn on another's scale and frame rather than its own, by code, so the two replace each other in place.
#
# Each glyph is otherwise fitted and centred on its own ink, which is right for a set read side by side and wrong for a
# pair that stands in for each other on one switch: the shut eye has less ink than the open one, so on its own frame its
# toolbar would grow and jump up as the eye closed.
FRAMED = {"H": "h"}


# How a glyph's shapes behind are drawn, as a part of the color the glyph is drawn in.
#
# Darker and solid, never fainter: a part drawn faint lets whatever is behind the glyph through and reads as a shadow.
# The game multiplies a font page's own color by the color text is drawn in, so this darkens with any tint.
BEHIND = 0.78

# What every mark is drawn to fill, as a fraction of the cell.
#
# A set of symbols is read as a set, so they have to agree with each other at one text size: a
# mark a third bigger than the one beside it reads as a mistake even when both are well drawn.
# They did not agree - the largest ran to 46 units of the cell and the smallest to 28 - because
# each was written to its own coordinates and nothing ever compared them.
#
# The number is the middle of where most of them already sat, so the set moves as little as it can
# to agree.
EXTENT = 36.0 / 64.0

# What a mark fills of its cell once it is finished, so a symbol asked for at 32 is a 32 by 32 icon.
#
# Every glyph is drawn at EXTENT and the whole set enlarged by the one factor, which keeps the weight
# of every stroke in the same proportion to its mark. Two texels short of each edge, so the
# antialiased rim of a mark has room inside the cell rather than being cut off by it.
FILL = 60.0 / 64.0


def fitted(make):
    """
    Builds a glyph at whatever size makes it fill <see cref="EXTENT" />.

    <b>Built twice, at two sizes, rather than drawn once and resized.</b> A stroke is a polygon by
    the time a glyph is finished, so scaling the finished artwork scales the line it is drawn with
    - and a set whose marks agree on size but not on weight is no better off than before. Building
    it again under a scale puts the scale on the points and leaves the width alone.

    Twice because a stroke adds its own width to whatever the points span, and how much depends on
    the angle it ends at. The first build says what this glyph actually came out as; the second
    lands it on the target.
    """

    global _fit

    _fit = 1.0
    for _ in range(2):
        drawn = ink(*make())
        if drawn is None:
            _fit = 1.0
            return make()

        widest = max(drawn[2] - drawn[0], drawn[3] - drawn[1]) / float(CELL * OVER)
        if widest <= 0:
            break

        _fit *= EXTENT / widest

    made = make()
    _fit = 1.0
    return made


def ink(shapes, holes, over=(), behind=()):
    """Where a set of shapes actually puts ink, at the oversampled size, with any drawn back over the holes."""
    from PIL import Image, ImageDraw

    size = CELL * OVER
    page = Image.new("L", (size, size), 0)
    draw = ImageDraw.Draw(page)

    for shape in behind:
        draw.polygon([(x * size, y * size) for (x, y) in shape], fill=255)

    for shape in shapes:
        draw.polygon([(x * size, y * size) for (x, y) in shape], fill=255)

    for hole in holes:
        draw.polygon([(x * size, y * size) for (x, y) in hole], fill=0)

    for shape in over:
        draw.polygon([(x * size, y * size) for (x, y) in shape], fill=255)

    return page.getbbox()


def frame(make):
    """The scale a glyph is fitted at, and where its ink lies at that scale."""
    global _fit


    _fit = 1.0
    for _ in range(2):
        drawn = ink(*make())
        _fit *= EXTENT / (max(drawn[2] - drawn[0], drawn[3] - drawn[1]) / float(CELL * OVER))

    scale, box = _fit, ink(*make())
    _fit = 1.0
    return scale, box


def render(glyph, framed=None):
    """
    One glyph, drawn oversized, centred on its own ink, and averaged down: how much of each pixel it covers, and how
    bright that ink is, which is full for every shape but those drawn behind.

    <b>Centred on what it drew, not on where it was drawn.</b> Each symbol is a handful of
    polygons written by hand in a unit square, and no two of them fill it the same way - the up
    chevron sat five units high of the middle and the down chevron four low, which is a symbol
    set that cannot be lined up with anything, because there is no one offset that is right for
    all of them. Centring here is what makes "a mark in the middle of its line" true of every
    glyph rather than of the average of them.

    It is done at the oversampled size, so the correction lands to a fraction of a final pixel.
    """

    from PIL import Image, ImageDraw

    global _fit

    if framed is None:
        made = fitted(glyph)
        box = None
    else:
        scale, box = frame(framed)
        _fit = scale
        made = glyph()
        _fit = 1.0

    shapes, holes = made[0], made[1]
    over = made[2] if len(made) > 2 else ()
    behind = made[3] if len(made) > 3 else ()
    size = CELL * OVER

    # Coverage, and brightness already multiplied by it, drawn alike but for the shapes behind.
    cover = Image.new("L", (size, size), 0)
    bright = Image.new("L", (size, size), 0)
    for page, dim in ((cover, 255), (bright, int(round(255 * BEHIND)))):
        draw = ImageDraw.Draw(page)
        for shape in behind:
            draw.polygon([(x * size, y * size) for (x, y) in shape], fill=dim)
        for shape in shapes:
            draw.polygon([(x * size, y * size) for (x, y) in shape], fill=255)
        for hole in holes:
            draw.polygon([(x * size, y * size) for (x, y) in hole], fill=0)
        for shape in over:
            draw.polygon([(x * size, y * size) for (x, y) in shape], fill=255)

    # The middle of the page, sized so the mark drawn at EXTENT comes out at FILL: of its own ink, or its partner's.
    ink = box or cover.getbbox()
    side = size * EXTENT / FILL
    edge = (size - side) / 2.0
    done = []
    for page in (cover, bright):
        if ink is not None:
            left, top, right, bottom = ink
            middled = Image.new("L", (size, size), 0)
            middled.paste(page.crop(ink), ((size - (right - left)) // 2, (size - (bottom - top)) // 2))
            page = middled
        done.append(page.resize((CELL, CELL), Image.BOX, box=(edge, edge, edge + side, edge + side)))

    return done[0], done[1]


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

    makers = dict((ch, glyph) for (ch, _, glyph) in GLYPHS)
    for i, (ch, label, glyph) in enumerate(GLYPHS):
        x, y = (i % across) * CELL, (i // across) * CELL
        alpha, bright = render(glyph, makers.get(FRAMED.get(ch)))

        # The page is straight alpha, so the color is the brightness without the coverage folded into it: white for
        # every shape but those drawn behind.
        color = Image.eval(bright, lambda v: v)
        a, b = alpha.load(), color.load()
        for py in range(CELL):
            for px in range(CELL):
                b[px, py] = min(255, (b[px, py] * 255 + a[px, py] // 2) // a[px, py]) if a[px, py] else 255

        cell = Image.merge("RGBA", (color, color, color, alpha))
        page.paste(cell, (x, y))
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
