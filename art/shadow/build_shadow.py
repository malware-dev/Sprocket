"""
Draws the shadow a floating window casts, as the nine-slice texture the editor stretches round it.

The picture is 64 pixels square: a dark middle 16 across, falling off to nothing over the 24 pixels round it, so each
corner of the nine-slice holds a quarter of the fall-off and each edge a straight run of it. It is black, and fully dark
where it is darkest; how dark it lies is the tint the editor draws it with. FontTool, beside the symbol font, writes the
texture premultiplied, as every sprite the game draws is. Run it again after changing the fall-off.

    python art/shadow/build_shadow.py
"""

import math
import os
import subprocess

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
PICTURE = os.path.join(HERE, "shadow.png")
TOOL = os.path.join(ROOT, "art", "symbols", "FontTool")
TEXTURE = os.path.join(ROOT, "src", "Sprocket.Host.SpaceEngineers", "Content", "Textures", "Sprites", "Sprocket_Shadow.dds")

SIZE = 64
CORNER = 24


def fall(distance):
    """How dark the shadow is at a distance outside its middle, from 1 at the middle's edge to almost nothing at the picture's."""
    return math.exp(-3.0 * (distance / CORNER) ** 2)


def main():
    picture = Image.new("RGBA", (SIZE, SIZE))
    pixels = picture.load()
    for y in range(SIZE):
        for x in range(SIZE):
            # The distance outside the middle along each axis, measured from the pixel's center.
            dx = max(0.0, CORNER - (x + 0.5), (x + 0.5) - (SIZE - CORNER))
            dy = max(0.0, CORNER - (y + 0.5), (y + 0.5) - (SIZE - CORNER))
            pixels[x, y] = (0, 0, 0, round(255 * fall(dx) * fall(dy)))

    picture.save(PICTURE)
    subprocess.run(["dotnet", "run", "--project", TOOL, "--", PICTURE, TEXTURE], check=True)


main()
