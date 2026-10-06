"""
Draws the logo as the image an LCD shows and the sprite the editor draws, out of sprocket-mark.svg.

Both are 512 by 512, BC7 sRGB with a full mip chain, as the game's own LCD images are. The LCD's copy keeps
straight alpha and the sprite is premultiplied, which is how the game stores each of its own pairs. Chrome
renders the picture, because nothing else here reads an SVG with its gradients and its shadow; FontTool,
beside the symbol font, writes the textures. Run it again after the SVG changes.

    python art/thumbnail/build_mark.py
"""

import os
import subprocess
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
SOURCE = os.path.join(HERE, "sprocket-mark.svg")
PICTURE = os.path.join(HERE, "sprocket-mark.png")
TOOL = os.path.join(ROOT, "art", "symbols", "FontTool")
TEXTURES = os.path.join(ROOT, "src", "Sprocket.Host.SpaceEngineers", "Content", "Textures")
CHROME = r"C:\Program Files\Google\Chrome\Application\chrome.exe"

# The size of every image the game offers an LCD.
SIZE = 512


def render():
    page = os.path.join(tempfile.mkdtemp(), "mark.html")
    with open(page, "w", encoding="utf-8") as out:
        out.write("<!doctype html><html><head><style>html,body{margin:0;padding:0;background:transparent;"
                  "overflow:hidden}img{display:block;width:%dpx;height:%dpx}</style></head><body>"
                  "<img src=\"file:///%s\"></body></html>" % (SIZE, SIZE, SOURCE.replace("\\", "/")))

    subprocess.run([CHROME, "--headless=new", "--disable-gpu", "--hide-scrollbars", "--allow-file-access-from-files",
                    "--default-background-color=00000000", "--window-size=%d,%d" % (SIZE, SIZE),
                    "--screenshot=" + PICTURE, "file:///" + page.replace("\\", "/")],
                   check=True, stderr=subprocess.DEVNULL)


def texture(arguments):
    subprocess.run(["dotnet", "run", "--project", TOOL, "--"] + arguments, check=True)


render()
texture(["--straight", PICTURE, os.path.join(TEXTURES, "Models", "Sprocket_Mark.dds")])
texture([PICTURE, os.path.join(TEXTURES, "Sprites", "Sprocket_Mark.dds")])
