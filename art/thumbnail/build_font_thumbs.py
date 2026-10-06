"""
Draws the Workshop thumbnails for the font mods, Maltek's and the Space Engineers Font Styles, out of their own font pages.

Every mark on a picture is a glyph cut from the page the game will load, placed by the advance
widths and bearings in the font's own table, so a thumbnail shows exactly the letters and symbols a
subscriber gets. All four share one layout: the name across the top, what the font holds in the
middle, and a footer saying how much it holds and where it comes from. Run it again after any of the
fonts is rebuilt.

The Font Styles picture sets its regular line in the game's own page, found in the Steam library
folder the STEAM_COMMON environment variable names, or in E:/Steam/steamapps/common where it is not set.

    python art/thumbnail/build_font_thumbs.py
"""

import os
import xml.etree.ElementTree as ElementTree

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
SYMBOLS = os.path.join(ROOT, "src", "Maltek.Symbols")
MONO = os.path.join(ROOT, "src", "Maltek.Mono")
SANS = os.path.join(ROOT, "src", "Maltek.Sans")
STYLES = os.path.join(ROOT, "src", "SpaceEngineers.FontStyles")
STEAM = os.environ.get("STEAM_COMMON", r"E:\Steam\steamapps\common")
GAME = os.path.join(STEAM, "SpaceEngineers", "Content", "Fonts", "white")

# The Workshop's own thumbnail proportions, and the palette the pictures share.
WIDTH, HEIGHT = 720, 450
GROUND = (0x16, 0x1B, 0x22)
LIGHT = (0xF6, 0xFA, 0xFD)
AMBER = (0xEA, 0xB2, 0x56)
DIM = (0x6B, 0x78, 0x88)

# Where the shared layout puts its three parts.
TITLE_TOP, TITLE_SCALE, TITLE_MARGIN = 22, 2.0, 40
BODY_TOP, BODY_FOOT = 140, 380
FOOTER_TOP, FOOTER_SCALE = HEIGHT - 42, 0.6
LINE = 0.9

NS = "{http://xna.microsoft.com/bitmapfont}"

# Each line is a run of (text, bold) pieces, so a strong word can stand among plain ones.
SANS_SAMPLE = [
    [("ABCDEFGHIJKLMNOPQRSTUVWXYZ", False)],
    [("abcdefghijklmnopqrstuvwxyz", False)],
    [("0123456789 ÆØÅ æøå é ß", False)],
    [],
    [("The hangar doors are ", False), ("open", True), (" and the", False)],
    [("oxygen tanks are filling.", False)],
]

# Each line in one style, named for the game's face: regular, then the three this mod adds.
STYLES_SAMPLE = [
    ("Regular: the hangar doors are open", "FontDataPA"),
    ("Bold: the hangar doors are open", "SpaceEngineersBold"),
    ("Italic: the hangar doors are open", "SpaceEngineersItalic"),
    ("Bold italic: the hangar doors are open", "SpaceEngineersBoldItalic"),
]

MONO_SAMPLE = [
    "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
    "abcdefghijklmnopqrstuvwxyz",
    "0123456789 +-*/=<>()[]{}",
    "",
    "Oxygen      87.5 %",
    "Hydrogen    42.0 %",
    "Power      118.3 MW",
]

SYMBOL_COLUMNS = 8
SYMBOL_PITCH = 72
SYMBOL_SIZE = 48


class Face:
    """A font as the game reads it: a page, and where each character sits on it."""

    def __init__(self, folder, name):
        table = ElementTree.parse(os.path.join(folder, name + ".xml")).getroot()
        self.height = int(table.get("height"))
        png = os.path.join(folder, name + "-0.png")
        self.page = Image.open(png if os.path.exists(png) else png[:-4] + ".dds").convert("RGBA")
        self.glyphs = {}
        for glyph in table.iter(NS + "glyph"):
            x, y = (int(v) for v in glyph.get("origin").split(","))
            w, h = (int(v) for v in glyph.get("size").split("x"))
            self.glyphs[chr(int(glyph.get("code"), 16))] = (
                (x, y, x + w, y + h), int(glyph.get("aw")), int(glyph.get("lsb")))

    def width(self, text, scale=1.0):
        return sum(self.glyphs[c][1] for c in text) * scale

    def draw(self, canvas, text, left, top, color, scale=1.0):
        pen = left
        for c in text:
            box, advance, bearing = self.glyphs[c]
            self.stamp(canvas, box, pen + bearing * scale, top, color, scale)
            pen += advance * scale
        return pen

    def stamp(self, canvas, box, left, top, color, scale):
        cell = self.page.crop(box)
        if scale != 1.0:
            cell = cell.resize((round(cell.width * scale), round(cell.height * scale)), Image.LANCZOS)
        ink = Image.new("RGBA", cell.size, color + (255,))
        ink.putalpha(cell.getchannel("A"))
        canvas.alpha_composite(ink, (round(left), round(top)))


def frame(title_face, title, footer_face, footer):
    """A picture in the shared layout with its name and footer drawn, and the middle left for the font."""
    canvas = Image.new("RGBA", (WIDTH, HEIGHT), GROUND + (255,))
    scale = min(TITLE_SCALE, (WIDTH - 2 * TITLE_MARGIN) / title_face.width(title))
    top = TITLE_TOP + title_face.height * (TITLE_SCALE - scale) / 2
    title_face.draw(canvas, title, (WIDTH - title_face.width(title, scale)) / 2, top, AMBER, scale)
    footer_face.draw(canvas, footer, (WIDTH - footer_face.width(footer, FOOTER_SCALE)) / 2, FOOTER_TOP, DIM, FOOTER_SCALE)
    return canvas


def characters(face):
    return sum(1 for c in face.glyphs if c != " ")


def lines(canvas, sample, face_of, height):
    """Draws a sample of runs as one block, centered across the picture and in the room between name and footer."""
    widths = [sum(face_of(strong).width(text) for text, strong in pieces) for pieces in sample]
    left = (WIDTH - max(widths)) / 2
    step = height * LINE
    top = BODY_TOP + (BODY_FOOT - BODY_TOP - len(sample) * step) / 2
    for n, pieces in enumerate(sample):
        pen = left
        for text, strong in pieces:
            pen = face_of(strong).draw(canvas, text, pen, top + n * step, LIGHT)


def sans_thumb(sans, bold):
    canvas = frame(bold, "Maltek Sans", sans, "%d characters · Inter, SIL Open Font License 1.1" % characters(sans))
    lines(canvas, SANS_SAMPLE, lambda strong: bold if strong else sans, sans.height)
    return canvas


def mono_thumb(mono):
    canvas = frame(mono, "Maltek Mono", mono, "%d characters · Roboto Mono, SIL Open Font License 1.1" % characters(mono))
    lines(canvas, [[(text, False)] if text else [] for text in MONO_SAMPLE], lambda strong: mono, mono.height)
    return canvas


def styles_thumb(faces):
    bold = faces["SpaceEngineersBold"]
    canvas = frame(bold, "Space Engineers Font Styles", faces["FontDataPA"],
                   "%d characters \u00b7 cut from the game's own face" % characters(bold))
    lines(canvas, [[(text, name)] for text, name in STYLES_SAMPLE], lambda name: faces[name], bold.height)
    return canvas


def symbols_thumb(symbols, title_face):
    marks = [c for c in symbols.glyphs if c.strip()]
    canvas = frame(title_face, "Maltek Symbols", title_face, "%d symbols" % len(marks))

    scale = SYMBOL_SIZE / symbols.height
    columns = min(SYMBOL_COLUMNS, len(marks))
    rows = (len(marks) + columns - 1) // columns
    left = (WIDTH - columns * SYMBOL_PITCH) / 2 + (SYMBOL_PITCH - SYMBOL_SIZE) / 2
    top = BODY_TOP + (BODY_FOOT - BODY_TOP - rows * SYMBOL_PITCH) / 2 + (SYMBOL_PITCH - SYMBOL_SIZE) / 2
    for i, c in enumerate(marks):
        box, _, _ = symbols.glyphs[c]
        symbols.stamp(canvas, box, left + (i % columns) * SYMBOL_PITCH, top + (i // columns) * SYMBOL_PITCH, LIGHT, scale)

    return canvas


def main():
    fonts = lambda folder: os.path.join(folder, "Content", "Fonts", "Maltek")
    mono = Face(fonts(MONO), "MaltekMono")
    sans = Face(fonts(SANS), "MaltekSans")
    bold = Face(fonts(SANS), "MaltekSansBold")
    symbols = Face(fonts(SYMBOLS), "MaltekSymbols")
    styles = {name: Face(os.path.join(STYLES, "Content", "Fonts", "SpaceEngineers"), name)
              for _, name in STYLES_SAMPLE if name != "FontDataPA"}
    styles["FontDataPA"] = Face(GAME, "FontDataPA")

    written = []
    for picture, folder in ((symbols_thumb(symbols, bold), SYMBOLS), (mono_thumb(mono), MONO), (sans_thumb(sans, bold), SANS),
                            (styles_thumb(styles), STYLES)):
        path = os.path.join(folder, "thumb.png")
        picture.convert("RGB").save(path)
        written.append(path)
    print("wrote", ", ".join(written))


if __name__ == "__main__":
    main()
