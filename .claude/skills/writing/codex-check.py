"""The codex's own checks, run over its pages: python .claude/skills/writing/codex-check.py

Every entry of MyTextsCodex.resx and MyTextsCodex.no.resx is a page.

    name      the product's name in running text; a ::: player box and `quoted` text are left out, since a
              file name or a path can carry it there
    links     a page linked more than once from one page
    marks     a mark followed by a plain space, where &nbsp; keeps it on one line with the word after it
    pairs     a page in one language with no partner in the other

Every hit is a fault; a check that has to be argued with is a check that needs mending.
"""

import os
import re
import sys
import xml.etree.ElementTree as ET

FOLDER = os.path.join("src", "Sprocket.Host.SpaceEngineers", "Content", "Data", "Localization")
FILES = {"en": "MyTextsCodex.resx", "no": "MyTextsCodex.no.resx"}
NAME = re.compile(r"sprocket", re.I)
LINK = re.compile(r"(?<!!)\[(?:!\[[^\]]*\]\([^)]*\)|[^\]])*\]\(([A-Za-z][A-Za-z0-9]*(?:\.[A-Za-z0-9]+)*)(?: class=[A-Za-z0-9_-]+)?\)")
QUOTED = re.compile(r"`[^`]*`")
SPACED = re.compile(r"\]\(mark:[a-z-]+\)[ \t]+\S")
# Pages the name may stand on: the home page names the product by design.
NAMED = {"Sprocket.Term.Home"}


def pages(language):
    """Every page of one language, by key."""
    root = ET.parse(os.path.join(FOLDER, FILES[language])).getroot()
    return dict((data.get("name"), data.findtext("value") or "") for data in root.findall("data"))


def running(text):
    """The lines of a page outside ::: player boxes, with quoted text taken out."""
    lines = []
    depth = 0
    in_player = []
    for number, line in enumerate(text.splitlines(), 1):
        stripped = line.strip()
        if stripped.startswith(":::"):
            words = stripped[3:].split()
            if words:
                in_player.append("player" in words)
                depth += 1
            elif depth:
                in_player.pop()
                depth -= 1
            continue
        if any(in_player):
            continue
        lines.append((number, QUOTED.sub("", line)))
    return lines


def check(language, key, text, say):
    if key not in NAMED:
        for number, line in running(text):
            if NAME.search(line):
                say("name", "%s %s:%d: %s" % (language, key, number, line.strip()))

    seen = {}
    for target in LINK.findall(QUOTED.sub("", text)):
        seen[target] = seen.get(target, 0) + 1
    for target, count in sorted(seen.items()):
        if count > 1:
            say("links", "%s %s: %s linked %d times" % (language, key, target, count))

    for number, line in enumerate(text.splitlines(), 1):
        if SPACED.search(QUOTED.sub("", line)):
            say("marks", "%s %s:%d: %s" % (language, key, number, line.strip()))


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    hits = []

    def say(kind, what):
        hits.append("%-6s %s" % (kind, what))

    read = {}
    for language in FILES:
        read[language] = pages(language)
        for key in sorted(read[language]):
            check(language, key, read[language][key], say)

    for language in FILES:
        for other in FILES:
            for key in sorted(set(read[language]) - set(read[other])):
                say("pairs", "%s %s has no %s partner" % (language, key, other))

    for hit in hits:
        print(hit)
    print("%d hit%s" % (len(hits), "" if len(hits) == 1 else "s"))
    return 1 if hits else 0


if __name__ == "__main__":
    sys.exit(main())
