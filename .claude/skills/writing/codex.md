# Codex pages

The codex is the editor's own small wiki: a page per term, each with a one-sentence tooltip. Its reader is
a player in the target group, inexperienced, who may never have heard a word the editor uses. The
playtest showed "construct", "logic system" and "installation" losing a reader who can read code; a real
player struggles harder. Mal: "It's sad... but true. We just have to do this job."

## Where the pages are

`Content/Data/Codex/<language>/<id>.md` is a page and `<id>.tldr.md` its tooltip, one sentence. `index`
lists the terms in the order the contents show them. Every page and tooltip exists in `en/` and `no/`.

## How a page is written

- **Link a term once per page**, the first time it matters: `[logic systems](#logic-system)`. Mal: the
  wiki rule.
- **A kind's mark sits beside a link or a bold term**, never beside plain running text, and never in a
  title: `![](mark:blueprint) [blueprints](#blueprint)`, with empty words. Mal: "[icon] signal slot"
  makes sense, "sends the [icon] signal" does not.
- **What is on screen is quoted as it is on screen**, in backticks: `Make blueprint`.
- **Explain what a player would get wrong, and trust them with the rest.** A page that spells out what a
  player sees for themselves on the screen talks down to them.
- **A page describes the product for its own purpose.** A guide once said text "draws as a sign" because
  signs were the test case; what the format was tested on is not what it is for.

## Facts the pages rest on

- A construct is a grid and everything pistons, rotors and hinges attach to it. Connectors and landing
  gear dock one construct to another, and each stays its own.
- A construct keeps its automation in its programmable blocks, and loses it only when the last one is
  destroyed or ground down. The logic systems the library lacks are then kept as backups.
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
```

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
