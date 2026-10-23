"""Counts the habits that make a page read as machine-written, so a draft can be cut and rewritten where they gather.

Usage:
    python tells.py <file.resx> [<file.resx> ...]   one line of counts per page, worst first; every entry is a page
    python tells.py --show <file.resx> <key>        every hit on one page, with its sentence

Every count is a signal, never a verdict: a page with no hits can still read flat, and a hit can be the right word.
What it looks for:
    opener    the page starts by defining its subject ("A blueprint is ...")
    such-as   "such as", the reflex example
    triads    lists of exactly three ("a door, a vent or a piston")
    signpost  pointing elsewhere instead of saying it ("the sequence page explains", "see")
    filler    words that pad or hedge (simply, just, usually, note that, at once, whether ...)
    formal    "does not", "cannot", "is not" where a person would contract
    rhythm    how much sentence length varies; low means every sentence is the same length
    words     length of the page; most pages say what they need in far fewer
"""

import re
import statistics
import sys
import xml.etree.ElementTree as ET

OPENER = re.compile(r"^(?:An?|The)\s+(?:\S+\s+){0,4}?(?:is|are)\s", re.I)
SUCH_AS = re.compile(r"\bsuch as\b", re.I)
# Three short items, each a word or two, the last ending the clause: "a door, a vent or a piston".
ITEM = r"(?:an? |the |its |their )?`?[\w'-]+(?: [\w'-]+)?`?"
TRIAD = re.compile(r"(?:^|[\s(:])(" + ITEM + r"), (" + ITEM + r"),? (?:and|or) (" + ITEM + r")(?=[.,;:)]|$)", re.I)
SIGNPOST = re.compile(r"\b(?:explains?|see \[|this page|the [a-z-]+ page|as described|described below|below|above)\b", re.I)
FILLER = re.compile(r"\b(?:simply|just|usually|typically|generally|note that|keep in mind|in other words|that is,|whether|at once|any number of|in turn|as well)\b", re.I)
FORMAL = re.compile(r"\b(?:does not|do not|cannot|is not|are not|was not|will not|has not|have not)\b", re.I)


def prose(text):
    """The page's running text: headings, list marks, links, marks and code taken out."""
    lines = []
    for line in text.splitlines():
        if line.startswith("#") or line.strip().startswith(":::") or not line.strip():
            lines.append("")
            continue
        line = re.sub(r"^\s*[-*]\s+|^\s*\d+\.\s+", "", line)
        line = re.sub(r"!\[[^\]]*\]\([^)]*\)", "", line)
        line = re.sub(r"\[([^\]]*)\]\([^)]*\)", r"\1", line)
        line = line.replace("**", "").replace("*", "")
        lines.append(line)
    return "\n".join(lines)


def sentences(text):
    flat = re.sub(r"\s+", " ", text)
    return [s.strip() for s in re.split(r"(?<=[.!?])\s+(?=[A-Z`])", flat) if len(s.split()) > 2]


def pages(path):
    """Every entry of a .resx file, by key: each one a page."""
    root = ET.parse(path).getroot()
    return [(data.get("name"), data.findtext("value") or "") for data in root.findall("data")]


def measure(page):
    text = prose(page)
    said = sentences(text)
    lengths = [len(s.split()) for s in said]
    words = sum(lengths)
    spread = statistics.pstdev(lengths) / statistics.mean(lengths) if len(lengths) > 2 else 1.0
    first = said[0] if said else ""
    hits = {
        "opener": [first] if OPENER.match(first) else [],
        "such-as": [s for s in said if SUCH_AS.search(s)],
        "triads": [s for s in said if any(m.group(2).lower() != m.group(3).lower() for m in TRIAD.finditer(s))],
        "signpost": [s for s in said if SIGNPOST.search(s)],
        "filler": [s for s in said if FILLER.search(s)],
        "formal": [s for s in said if FORMAL.search(s)],
    }
    return words, spread, hits


def main(args):
    sys.stdout.reconfigure(encoding="utf-8")
    if args and args[0] == "--show":
        for key, page in pages(args[1]):
            if key != args[2]:
                continue
            words, spread, hits = measure(page)
            print("%s: %d words, rhythm %.2f" % (key, words, spread))
            for name, found in hits.items():
                for s in found:
                    print("  %-8s %s" % (name, s))
        return

    rows = []
    for path in args:
        for key, page in pages(path):
            words, spread, hits = measure(page)
            per = max(words, 1) / 100.0
            score = sum(len(v) for v in hits.values()) / per + (0.5 - spread) * 10
            rows.append((score, key, words, spread, hits))
    rows.sort(reverse=True)
    print("%-40s %6s %6s %6s %6s %6s %6s %6s %6s" % ("page", "words", "rhythm", "opener", "suchas", "triads", "signp", "filler", "formal"))
    for score, key, words, spread, hits in rows:
        name = key
        print("%-40s %6d %6.2f %6d %6d %6d %6d %6d %6d" % (
            name, words, spread, len(hits["opener"]), len(hits["such-as"]), len(hits["triads"]),
            len(hits["signpost"]), len(hits["filler"]), len(hits["formal"])))


if __name__ == "__main__":
    main(sys.argv[1:])
