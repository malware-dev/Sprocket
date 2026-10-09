"""
Draws the Workshop thumbnail for Rich Text Panels, at the Workshop's thumbnail proportions: a panel's screen with
markdown as somebody writes it on the left, and the rich text a panel draws from it on the right.

The simulator draws both halves through the app's own layout, on a panel's default script colors, so the right half is
exactly what the app makes of the left. Run it again after the layout changes.

    python art/thumbnail/build_richtext_thumb.py
"""

import os
import subprocess
import tempfile

from PIL import Image, ImageChops, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
APP = os.path.join(ROOT, "src", "RichTextPanels")
SIM = os.path.join(ROOT, "src", "Sprocket.Host.SpaceEngineers.Sim")

# The Workshop's own thumbnail proportions, as the other mods' thumbnails have them.
WIDTH, HEIGHT = 720, 450

# A panel's default script colors: light blue on the game's LCD blue. What is written is drawn a little dimmer than
# what it gives, and a line between them halfway in between.
INK, WRITTEN_INK, SCREEN = (0xB3, 0xED, 0xFF), (0x7F, 0xB4, 0xD0), (0x00, 0x58, 0x97)
LINE = (0x40, 0x8C, 0xC0)

MARGIN = 36
GAP = 48
HALF = (WIDTH - 2 * MARGIN - GAP) // 2

WRITTEN = "\n".join([
    "`# Rich Text Panels`",
    "`` **bold** *italic* `code` ``",
    "`- A list`",
    "`1. Numbered`",
    "`> A quote`",
    "`---`",
    "`| Tank | Fill |`",
    "`|:--*|--:|`",
    "`| Ice | 80% |`",
    "`::: center`",
    "`Centered`",
    "`:::`",
    "`::: right`",
    "`Right`",
    "`:::`",
    "`![](Arrow 90deg)`",
])

SHOWN = "\n".join([
    "# Rich Text Panels",
    "",
    "**bold** *italic* `code`",
    "",
    "- A list",
    "",
    "1. Numbered",
    "",
    "> A quote",
    "",
    "---",
    "",
    "| Tank | Fill |",
    "|:--*|--:|",
    "| Ice | 80% |",
    "",
    "::: center",
    "Centered",
    ":::",
    "",
    "::: right",
    "Right",
    ":::",
    "",
    "![arrow](Arrow 90deg)",
])


def hex_of(color):
    return "%02X%02X%02X" % color


def drawn(markdown, ink, work, code=None):
    """What the simulator draws of markdown in a half of the picture, fitted to it, in ink on the panel's blue, with its
    code in the game's monospace face as a panel draws it, or in the face code names."""
    source = os.path.join(work, "half.md")
    picture = os.path.join(work, "half.png")
    with open(source, "w", encoding="utf-8", newline="\n") as file:
        file.write(markdown)

    face = [] if code is None else ["code=" + code]
    subprocess.run(["dotnet", "run", "--project", SIM, "-c", "Release", "--no-build", "--", "--panel", source, source,
                    picture, str(HALF), str(HEIGHT - 2 * MARGIN), "ink=" + hex_of(ink), "screen=" + hex_of(SCREEN), "fit"]
                   + face, check=True, stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL)
    return Image.open(picture).convert("RGB")


def centered(canvas, half, left):
    """Pastes what is drawn on half, cut to what was drawn, centered down the picture with its left edge at left."""
    used = ImageChops.difference(half, Image.new("RGB", half.size, SCREEN)).getbbox()
    if used is None:
        return

    piece = half.crop((0, used[1], half.width, used[3]))
    canvas.paste(piece, (left, (HEIGHT - piece.height) // 2))


def main():
    canvas = Image.new("RGB", (WIDTH, HEIGHT), SCREEN)
    with tempfile.TemporaryDirectory() as work:
        centered(canvas, drawn(WRITTEN, WRITTEN_INK, work, "MaltekMono"), MARGIN)
        centered(canvas, drawn(SHOWN, INK, work), MARGIN + HALF + GAP)

    middle = WIDTH // 2
    ImageDraw.Draw(canvas).line([(middle, MARGIN), (middle, HEIGHT - MARGIN)], fill=LINE, width=2)

    path = os.path.join(APP, "thumb.png")
    canvas.save(path)
    print("wrote", path, canvas.size)


if __name__ == "__main__":
    main()
