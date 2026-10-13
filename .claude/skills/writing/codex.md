# Codex pages

The codex is the editor's own small wiki: a page per term, each with a one-sentence tooltip. Its reader is
a player in the target group, inexperienced, who may never have heard a word the editor uses. The
playtest showed "construct", "logic system" and "installation" losing a reader who can read code; a real
player struggles harder. Mal: "It's sad... but true. We just have to do this job."

## Where the pages are

`Content/Data/Codex/<language>/<id>.md` is a page and `<id>.tldr.md` its tooltip, one sentence. `index`
lists the terms in the order the contents show them. Every page and tooltip exists in `en/` and `no/`.

## How a page is written

- **The codex explains everything that is built, and grows with the editor.** Mal: "Explain what we have.
  We just need to make damn sure we update the codex as Sprocket grows." Something not built yet is left
  out until it is, and the code review's codex step catches the change that builds it.
- **The codex teaches the editor, never the script.** What is on the screens and what to press; the
  script's syntax and keywords do not appear in a page.
- **Link a term once per page**, the first time it matters: `[logic systems](#logic-system)`. Mal: the
  wiki rule.
- **A kind's mark sits beside a link or a bold term**, never beside plain running text, and never in a
  title: `![](mark:blueprint) [blueprints](#blueprint)`, with empty words. Mal: "[icon] signal slot"
  makes sense, "sends the [icon] signal" does not.
- **What is on screen is quoted as it is on screen**, in backticks: `Make blueprint`.
- **A detail belongs to the page about the thing it is a detail of.** What only one kind of block or
  one action does goes on that block's or action's page, not on a general page that happens to meet it.
  Mal, on a vent's "move on" written into the sequence page: "the place it would belong, would be a page
  that describes that action specifically."
- **No list that pretends to be complete.** Where the cases depend on the block or the action (the ways
  a run can fail, the reasons a step holds), state the rule and give an example, not a list. Mal: "there's
  any number of errors that might stop a sequence, that is per-action determined."
- **Say a thing once,** on the page that owns it, and link there from the others.
- **Explain what a player would get wrong, and trust them with the rest.** A page that spells out what a
  player sees for themselves on the screen talks down to them.
- **Table-talk goes in the `::: player` box.** A passage about the game itself rather than the world (a file
  on the player's computer, a limit the game puts on mods) is table-talk, as `screen.md` defines it, and on a
  page it sits in that box.
- **A page describes the product for its own purpose.** A guide once said text "draws as a sign" because
  signs were the test case; what the format was tested on is not what it is for.

## Facts the pages rest on

- A construct is a grid and everything pistons, rotors and hinges attach to it. Connectors and landing
  gear dock one construct to another, and each stays its own.
- A construct keeps its automation in its programmable blocks, and loses it only when the last one is
  destroyed or ground down. The logic systems the library lacks are then kept as backups.
- Most actions happen at once (the bolt). Only a few take time (the clock), such as a door closing, a vent
  filling or emptying a room, or a piston or rotor moving to where it was sent. A new step that takes time is
  waited for; the sequence can instead go straight on from it, and one wait covers every step since the last.
- A blueprint is a copy in the library; editing the construct's logic system leaves the blueprint as it
  was.

## Checks of its own

```
# The name, which a player never reads
grep -ni "sprocket" <pages>

# A term linked twice on one page
grep -o "](#[a-z-]*)" <page> | sort | uniq -d

# A mark in a title
grep -n "^#.*mark:" <page>

# Both languages: every page and tooltip has its partner
diff <(ls Content/Data/Codex/en) <(ls Content/Data/Codex/no)

# Habits that make a page read as machine-written, counted per page; --show <page> lists every hit
python .claude/skills/writing/tells.py Content/Data/Codex/en/*.md
```

The tester's first remark on the codex was that it reads as AI-written. `tells.py` counts the habits behind
that: a page opening on a definition, "such as" and lists of three, pointing elsewhere, padding, uncontracted
negatives, every sentence the same length, and sheer length. Its counts are signals, never verdicts.

## The reader test

A page, or a set of them, is read by a second, simpler reader before Mal sees it. It is the gate in
front of his reading, not in front of a commit: it takes out the plain faults so fewer drafts go back and
forth, and Mal still reads every page. Its value is in what it cannot do: it has only the pages, so
whatever a page leans on without saying, it cannot know.

1. **Write the questions first, with their answers kept back.** Tasks a player would come to the page
   for, answered from what was decided: "Using only these pages: your ship lost its last programmable
   block. How do you get its airlock back?" Three to six per page, at least one that needs a second page.
2. **Run the reader on the smallest model** (Haiku), as a subagent given the page texts and nothing
   else: the pages gathered into one scratch file, each with its tooltip, which it is told to read alone,
   and no repository, no other file and no conversation. Tell it to answer only from the pages, to mark
   every guess, and to list every term it had to guess and every step it assumed.
   The reader is told what a player of the game already knows, since a small model knows the game less
   well than a player with hundreds of hours does. Give it this, and tell it none of it counts as a gap:

   > You know Space Engineers well: grids, the terminal and its blocks, toolbars and their slots and
   > pages, programmable blocks, pistons, rotors and hinges, connectors, landing gear and magnetic plates,
   > button panels, sensors, timer blocks and event controllers, cockpits, grinding and welding, survival
   > and creative mode, and the game's own blueprints.

3. **Compare.** A wrong or partial answer is a fault in the page, and where the reader went wrong shows
   where the page misled. A guessed term or an assumed step is a fault only where the guess was wrong, or
   would leave a player stuck or acting wrongly; a guess it got right from context stays a guess, and a
   term the game teaches is noise. The reader leans towards asking for more, and a page that answers every
   question it could raise reads as though it thinks its reader slow. Mal: "We must be careful about
   *over-explaining* as well." The opposite gap, something the reader knows and a player does not, shows
   only in a wrong answer, which is why the questions carry answers.
4. **Fix what it found, and run it again.** A page reaches Mal once the reader's answers match. Where
   they still do not after a second run, the question is looked at too: a question the page was never
   meant to answer is dropped and said so, and a page that should answer it is reworked before it goes.

What it does not catch, and Mal still does: wrong facts (the reader trusts the page), tone, and confusion
that comes from the screens themselves rather than the words.

## Open

- Whether pages address the player as "you". The existing pages do ("your blueprints"); that was written
  that way and not ruled on.
