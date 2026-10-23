"""
Draws the hazard stripes a warning or an error lies on, as the square texture the editor lays edge to edge across a row.

The picture is 64 pixels square: white stripes 12 pixels across, every 32 pixels along the row, at forty-five degrees,
running from bottom left to top right, transparent between. A period that divides the picture's side makes each tile
meet the next with no seam. It is white so the editor tints it the color it wants; how strong the stripes stand is that
tint. Edges are smoothed by sampling each pixel four by four. FontTool, beside the symbol font, writes the texture
premultiplied, as every sprite the game draws is. Run it again after changing the stripes.

    python art/stripes/build_stripes.py
"""

import os
import subprocess

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
PICTURE = os.path.join(HERE, "stripes.png")
TOOL = os.path.join(ROOT, "art", "symbols", "FontTool")
TEXTURE = os.path.join(ROOT, "src", "Sprocket.Host.SpaceEngineers", "Content", "Textures", "Sprites", "Sprocket_Stripes.dds")

SIZE = 64
PERIOD = 32
WIDE = 12
SAMPLES = 4


def covered(x, y):
    """Whether the point at x, y lies on a stripe: across the diagonal, where (x + y) falls in the first part of each period."""
    return (x + y) % PERIOD < WIDE


def main():
    picture = Image.new("RGBA", (SIZE, SIZE))
    pixels = picture.load()
    for y in range(SIZE):
        for x in range(SIZE):
            hits = 0
            for sy in range(SAMPLES):
                for sx in range(SAMPLES):
                    if covered(x + (sx + 0.5) / SAMPLES, y + (sy + 0.5) / SAMPLES):
                        hits += 1
            pixels[x, y] = (255, 255, 255, round(255 * hits / (SAMPLES * SAMPLES)))

    picture.save(PICTURE)
    subprocess.run(["dotnet", "run", "--project", TOOL, "--", PICTURE, TEXTURE], check=True)


main()
