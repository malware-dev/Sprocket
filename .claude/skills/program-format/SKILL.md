---
name: program-format
description: The rules for Sprocket's stored program format - its version line, what counts as a change to it, the upgrade every change carries, what the editor owes every form the format allows, and the world's memory. Use before changing anything a stored program says or means (a keyword, an id, a member, what a slot holds), before publishing, and when a tester's program misbehaves after a change.
---

These are the project's rules. Where one calls for a decision, the agent asks its developer, the person it works for; the
developer decides, and gets the maintainer's go-ahead before the change is made. The human version is `docs/Upgrades.md`.

## The version

1. **The first line is the version:** `sprocket <format> [alpha <draft>]`. A reader checks it before reading anything
   else, and refuses whole, keeping the text as written, a program from a draft it does not know. So the draft goes up for
   any change an older build could not read safely: a change that makes an existing program read differently or not at
   all, and every addition too, a new keyword, new syntax or a new member, so that an older build refuses a newer program
   cleanly instead of misreading it. The format number goes up only when a change would stop existing programs working. A
   finished version is written without `alpha`. `ScriptFormat.Version` and `ScriptFormat.Draft` hold what this build
   writes.

## What a change is

2. **A change to what a program says comes with its upgrade, in the same commit.** One `FormatChange` in
   `Storage/Upgrades/`, saying the draft it arrived in, put last in its version's list in `FormatUpgrades`. Never removed:
   every program written before it reads through it.
3. **What it says includes what it means.** A renamed id, a member moved to another type, a slot that now holds something
   else: each is a change, though the text looks the same. The draft number staying the same proves nothing.
4. **Every draft reaches the current one.** Testers come and go and skip drafts, so a program runs every change that
   arrived after the draft it was written for, in order. A program older than `FormatUpgrades.Oldest` is refused and kept
   as it was. A finished version counts as after all of its drafts; at release nothing is rewritten, and its list of
   changes is the permanent upgrade to the next version.
5. **Before publishing**, diff what a stored program can carry (ids of values, actions, signals, keywords, what slots
   hold) between the last published build and this one, and check an upgrade covers each difference.

## Through the developer

6. **The spec first.** A language change lands in `docs/Language.md` before any reader or writer code for it, in wording
   the developer approved; the agent never edits the spec without the developer's yes to that exact text.
7. **Nothing stored changes quietly.** What Sprocket writes to disk or a save is in the program format, or its format is
   shown to the developer and agreed before it is built.

## What the editor owes

8. **The editor opens and edits every form the format allows.** A form it no longer writes is edited correctly, upgraded,
   or flagged by the check with a repair the player can carry out. It is never misread silently.
9. **What no upgrade can carry over safely becomes a repair.** Where the check can see it in any program, it is a warning
   on what needs repair, leading to the fix. Where no check can see it, an upgrade would have to mark what it cannot fix;
   that upgrade is not designed yet.

## Proving it

10. **Every upgrade has a test.** `UpgradeTests` reads the old text, checks what it became and that it is written in the
    current format.

## The world's memory

11. **A member's number is the memory's format.** A `ProtoMember` number, once used, is never used again or given
    another type; a dropped member leaves its number unused.

## This file

12. **`docs/Upgrades.md` says what this file says.** A change here changes that page in the same commit, in plain words
    for a person, as short as it can be.
