"""
Draws the textures the color picker shows every color with.

Sprocket_Hues is every hue at full saturation and value, red at its top round to red at its bottom: the strip a hue is
chosen on. Sprocket_Pale is white, solid at its left edge and gone at its right: over a fill of one hue it gives
saturation across. Sprocket_Dark is black, gone at its top edge and solid at its bottom: over both it gives value up. Each
fade is a straight run of alpha, so the square they make is exact rather than a likeness. FontTool, beside the symbol
font, writes the textures premultiplied, as every sprite the game draws is. Run it again after changing one.

    python art/color/build_picker.py
"""

import colorsys
import os
import subprocess

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
TOOL = os.path.join(ROOT, "art", "symbols", "FontTool")
SPRITES = os.path.join(ROOT, "src", "Sprocket.Host.SpaceEngineers", "Content", "Textures", "Sprites")

LONG = 256
SHORT = 4


def texture(name, size, pixel):
    """Writes a texture <size> across, each pixel given by <pixel> from how far along the long side it is, 0 to 1."""
    picture = Image.new("RGBA", size)
    pixels = picture.load()
    across = size[0] >= size[1]
    for y in range(size[1]):
        for x in range(size[0]):
            pixels[x, y] = pixel((x if across else y) / (LONG - 1))

    png = os.path.join(HERE, name + ".png")
    picture.save(png)
    subprocess.run(["dotnet", "run", "--project", TOOL, "--", png, os.path.join(SPRITES, name + ".dds")], check=True)


def hue(along):
    """The hue <along> the way round the wheel, at full saturation and value, solid."""
    return tuple(round(255 * part) for part in colorsys.hsv_to_rgb(along % 1.0, 1.0, 1.0)) + (255,)


def main():
    texture("Sprocket_Hues", (SHORT, LONG), hue)
    texture("Sprocket_Pale", (LONG, SHORT), lambda along: (255, 255, 255, round(255 * (1.0 - along))))
    texture("Sprocket_Dark", (SHORT, LONG), lambda along: (0, 0, 0, round(255 * along)))


main()
