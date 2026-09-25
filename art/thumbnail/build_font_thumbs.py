"""
Draws the Workshop thumbnails for the two Maltek font mods out of their own font pages.

Every mark on either picture is a glyph cut from the page the game will load, placed by the advance
widths and bearings in the font's own table, so a thumbnail shows exactly the letters and symbols a
subscriber gets. Run it again after either font is rebuilt.

    python art/thumbnail/build_font_thumbs.py
"""

import os
import xml.etree.ElementTree as ElementTree

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
SYMBOLS = os.path.join(ROOT, "src", "Maltek.Symbols")
MONO = os.path.join(ROOT, "src", "Maltek.Mono")

# The Workshop's own thumbnail proportions, and Sprocket's thumbnail palette.
WIDTH, HEIGHT = 720, 450
GROUND = (0x16, 0x1B, 0x22)
LIGHT = (0xF6, 0xFA, 0xFD)
AMBER = (0xEA, 0xB2, 0x56)
DIM = (0x6B, 0x78, 0x88)

NS = "{http://xna.microsoft.com/bitmapfont}"

SAMPLE = [
    ("ABCDEFGHIJKLMNOPQRSTUVWXYZ", LIGHT),
    ("abcdefghijklmnopqrstuvwxyz", LIGHT),
    ("0123456789 +-*/=<>()[]{}", LIGHT),
    ("", LIGHT),
    ("Oxygen      87.5 %", LIGHT),
    ("Hydrogen    42.0 %", LIGHT),
    ("Power      118.3 MW", LIGHT),
]


class Face:
    """A font as the game reads it: a page, and where each character sits on it."""

    def __init__(self, folder, name):
        table = ElementTree.parse(os.path.join(folder, name + ".xml")).getroot()
        self.height = int(table.get("height"))
        self.page = Image.open(os.path.join(folder, name + "-0.png")).convert("RGBA")
        self.glyphs = {}
        for glyph in table.iter(NS + "glyph"):
            x, y = (int(v) for v in glyph.get("origin").split(","))
            w, h = (int(v) for v in glyph.get("size").split("x"))
            self.glyphs[chr(int(glyph.get("code"), 16))] = (
                (x, y, x + w, y + h), int(glyph.get("aw")), int(glyph.get("lsb")))

    def width(self, text, scale=1.0):
        return sum(self.glyphs[c][1] for c in text) * scale

    def draw(self, canvas, text, left, top, colour, scale=1.0):
        pen = left
        for c in text:
            box, advance, bearing = self.glyphs[c]
            self.stamp(canvas, box, pen + bearing * scale, top, colour, scale)
            pen += advance * scale

    def stamp(self, canvas, box, left, top, colour, scale):
        cell = self.page.crop(box)
        if scale != 1.0:
            cell = cell.resize((round(cell.width * scale), round(cell.height * scale)), Image.LANCZOS)
        ink = Image.new("RGBA", cell.size, colour + (255,))
        ink.putalpha(cell.getchannel("A"))
        canvas.alpha_composite(ink, (round(left), round(top)))


def symbols_thumb(mono, symbols):
    canvas = Image.new("RGBA", (WIDTH, HEIGHT), GROUND + (255,))

    title, scale = "Maltek Symbols", 1.6
    mono.draw(canvas, title, (WIDTH - mono.width(title, scale)) / 2, 34, AMBER, scale)

    marks = [c for c in symbols.glyphs if c.strip()]
    columns = 7
    rows = (len(marks) + columns - 1) // columns
    pitch = 88
    left = (WIDTH - columns * pitch) / 2 + (pitch - symbols.height) / 2
    top = 140 + (HEIGHT - 140 - 30 - rows * pitch) / 2
    for i, c in enumerate(marks):
        box, _, _ = symbols.glyphs[c]
        symbols.stamp(canvas, box, left + (i % columns) * pitch, top + (i // columns) * pitch, LIGHT, 1.0)

    return canvas


def mono_thumb(mono):
    canvas = Image.new("RGBA", (WIDTH, HEIGHT), GROUND + (255,))

    title, scale = "Maltek Mono", 2.0
    mono.draw(canvas, title, (WIDTH - mono.width(title, scale)) / 2, 22, AMBER, scale)

    line = mono.height * 0.78
    top = 150
    left = (WIDTH - max(mono.width(text) for text, _ in SAMPLE)) / 2 + 20
    for n, (text, colour) in enumerate(SAMPLE):
        number = str(n + 1).rjust(2)
        mono.draw(canvas, number, left - mono.width(number) - 22, top + n * line, DIM)
        mono.draw(canvas, text, left, top + n * line, colour)

    credit, scale = "Roboto Mono, SIL Open Font License 1.1", 0.6
    mono.draw(canvas, credit, (WIDTH - mono.width(credit, scale)) / 2, HEIGHT - 42, DIM, scale)
    return canvas


def main():
    mono = Face(os.path.join(MONO, "Content", "Fonts", "Maltek"), "MaltekMono")
    symbols = Face(os.path.join(SYMBOLS, "Content", "Fonts", "Maltek"), "MaltekSymbols")

    symbols_thumb(mono, symbols).convert("RGB").save(os.path.join(SYMBOLS, "thumb.png"))
    mono_thumb(mono).convert("RGB").save(os.path.join(MONO, "thumb.png"))
    print("wrote", os.path.join(SYMBOLS, "thumb.png"), "and", os.path.join(MONO, "thumb.png"))


if __name__ == "__main__":
    main()
