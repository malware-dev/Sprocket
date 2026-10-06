"""
Writes vanilla-characters.txt: every character the game's own Latin-script face can draw, which is what Maltek Mono,
Maltek Sans and the Space Engineers Font Styles cover.

Run it from anywhere, on a machine with Space Engineers:  python art/vanilla_characters.py

Space Engineers is found in the Steam library folder the STEAM_COMMON environment variable names, or in
E:/Steam/steamapps/common where it is not set.

The list is read from Content/Fonts/white/FontDataPA.xml, the face the game draws every language but Chinese and Japanese
in, and kept in the repository so the faces can be cut again without the game. Its private use area is left out: those
are the game's controller buttons and marks, pictures rather than letters.
"""

import os
import re

HERE = os.path.dirname(os.path.abspath(__file__))
STEAM = os.environ.get("STEAM_COMMON", r"E:\Steam\steamapps\common")
GAME = os.path.join(STEAM, "SpaceEngineers", "Content", "Fonts", "white", "FontDataPA.xml")
LIST = os.path.join(HERE, "vanilla-characters.txt")


def main():
    with open(GAME, encoding="utf-8-sig") as f:
        codes = sorted({int(c, 16) for c in re.findall(r'code="([0-9A-Fa-f]+)"', f.read())})

    letters = [c for c in codes if not 0xE000 <= c <= 0xF8FF]
    with open(LIST, "w", encoding="utf-8", newline="\n") as f:
        f.write("# Every character Space Engineers' own Latin-script face, FontDataPA, can draw, outside its private use\n")
        f.write("# area. Written by art/vanilla_characters.py; Maltek Mono, Maltek Sans and the Space Engineers Font Styles cover exactly these.\n")
        for code in letters:
            f.write("%04X %s\n" % (code, chr(code) if chr(code).isprintable() and code != 0x20 else ""))

    print("%d characters written to %s" % (len(letters), LIST))


def read():
    """The characters in the list, in order."""

    with open(LIST, encoding="utf-8") as f:
        return [int(line.split()[0], 16) for line in f if line.strip() and not line.startswith("#")]


if __name__ == "__main__":
    main()
