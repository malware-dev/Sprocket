"""The codex's own checks, run over its pages: python .claude/skills/writing/codex-check.py

    name      the product's name in running text; a ::: player box and `quoted` text are left out, since a
              file name or a path can carry it there
    links     a term linked more than once on one page
    title     a mark in a title
    pairs     a page or tooltip in one language with no partner in the other

Every hit is a fault; a check that has to be argued with is a check that needs mending.
"""

import os
import re
import sys

ROOT = os.path.join("src", "Sprocket.Host.SpaceEngineers", "Content", "Data", "Codex")
LANGUAGES = ("en", "no")
NAME = re.compile(r"sprocket", re.I)
LINK = re.compile(r"\]\(#([a-z-]+)\)")
QUOTED = re.compile(r"`[^`]*`")
# Pages the name may stand on: the home page names the product by design.
NAMED = {"home.md", "home.tldr.md"}


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


def check(language, name, text, say):
    if name not in NAMED:
        for number, line in running(text):
            if NAME.search(line):
                say("name", "%s/%s:%d: %s" % (language, name, number, line.strip()))

    seen = {}
    for target in LINK.findall(text):
        seen[target] = seen.get(target, 0) + 1
    for target, count in sorted(seen.items()):
        if count > 1:
            say("links", "%s/%s: #%s linked %d times" % (language, name, target, count))

    for number, line in enumerate(text.splitlines(), 1):
        if line.startswith("#") and "mark:" in line:
            say("title", "%s/%s:%d: %s" % (language, name, number, line.strip()))


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    hits = []

    def say(kind, what):
        hits.append("%-6s %s" % (kind, what))

    files = {}
    for language in LANGUAGES:
        folder = os.path.join(ROOT, language)
        files[language] = set(f for f in os.listdir(folder) if f.endswith(".md"))
        for name in sorted(files[language]):
            with open(os.path.join(folder, name), encoding="utf-8") as page:
                check(language, name, page.read(), say)

    for language in LANGUAGES:
        for other in LANGUAGES:
            for name in sorted(files[language] - files[other]):
                say("pairs", "%s/%s has no %s partner" % (language, name, other))

    for hit in hits:
        print(hit)
    print("%d hit%s" % (len(hits), "" if len(hits) == 1 else "s"))
    return 1 if hits else 0


if __name__ == "__main__":
    sys.exit(main())
