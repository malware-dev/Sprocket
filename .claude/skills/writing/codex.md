# Codex tips

The codex is the mod's tips. Every tip is a codex page: a short page that opens beside the element it belongs to,
whose links open further pages inside it, with back and forward. Its reader is a player in the target group,
inexperienced, who may never have heard a word the editor uses. Mal: "It's sad... but true. We just have to do this
job." Each tip is "short, to the point and clear as day", and a term is explained on a page of its own instead of on
every tip that uses it.

## Where the pages are

Every tip is a codex page: one entry in `Content/Data/Localization/Codex.resx`, with its Norwegian in
`Codex.no.resx`, written in rich text. A page links another by its key without the `Sprocket.` prefix:
`[slot](Term.Slot)`. A link to a page that does not exist fails the build. Text shown anywhere in the mod outside a
tip lives in `MyTexts.resx`, never in the codex; a text that only ever shows inside a tip stays in the codex.

A page is a few short lines of rich text with no title; a term page names its term in bold, with its mark, in its first sentence. It never scrolls.

## Which element gets a tip

- **Something self-explanatory gets no tip.**
- **A tip that only repeats its element's label or words is removed.** A tip on an element that shows a value (a
  name, a number, a state) and says what that value is does not repeat it; it says what the field is for.
- **An icon that is part of a text needs no tip of its own. An icon that alone stands for an action keeps its tip,**
  so people understand without a doubt what they are doing.
- **A part of a sentence that shows something live keeps its tip,** such as which blocks fill a slot. A part that
  only restates itself has none.
- **An element that is itself a term shows that term's page as its tip,** the way the home headings show the logic
  system and installation pages. It gets no tip of its own that links the same word.
- **A term on a screen is explained once:** where a heading with a term is repeated on a screen, only its first
  occurrence has the term look and the tip.
- **Undo and redo say exactly what happens,** never "undo the last action": the change, by the item's name and
  where it is, and what undoing makes of it.

## How a page is written

- **Short, to the point and clear as day.** A page that does not fit is too long.
- **It says only what the person needs.** Mal, on a tip saying a rename applies "everywhere it is used": "Why does
  the user need to know this?"
- **It never explains the obvious.**
- **It says what the thing does, never what it doesn't.**
- **Terms get their own pages, linked.** A tip does not explain a term in place.
- **A link explains the word it is on. Never a "see the..." link.**
- **Link a term once per page**, the first time it matters. Mal: the wiki rule.
- **The codex teaches the editor, never the script.** What is on the screens and what to press; the script's syntax
  and keywords do not appear in a page.
- **A kind's mark sits beside a link or a bold term**, never beside plain running text:
  `![](mark:blueprint)&nbsp;[blueprints](Term.Blueprint)`. Mal: "[icon] signal slot" makes sense, "sends the [icon]
  signal" does not.
- **Every mark is joined to the word after it by `&nbsp;`**, so a line never ends between them (Mal, 2026-10-14).
- **An icon is shown, never described.** `![](mark:<name>)` draws the real symbol, and the words say what it means.
  Mal: "it's a very bad idea to describe the icons by words. They can change at any time."
- **What is on screen is quoted as it is on screen**, in backticks: `Make blueprint`.
- **A detail belongs to the page about the thing it is a detail of.** Mal: "the place it would belong, would be a
  page that describes that action specifically."
- **No list that pretends to be complete.** Where the cases depend on the block or the action, state the rule and
  give an example. Mal: "there's any number of errors that might stop a sequence, that is per-action determined."
- **Say a thing once,** on the page that owns it, and link there from the others.
- **Out-of-world facts go in the `::: player` box.** The people in the world are engineers, never players.
- **The codex describes the finished design,** and nothing a ticket leaves open.

## Facts the pages rest on

- A construct is a grid and everything pistons, rotors and hinges attach to it. Connectors and landing
  gear dock one construct to another, and each stays its own.
- A construct keeps its automation in a programmable block one of its owners owns, and loses it only when
  the last programmable block is destroyed or removed (Mal, 2026-10-21). The logic systems the library lacks
  are then kept as backups.
- Most actions happen at once. A few are awaitable, like a door closing, a vent filling or emptying a room,
  or a piston or rotor moving to where it was sent, and so is a step that runs another sequence. A sequence
  waits only for those; an action the block keeps on with after it starts, like a sound, is not waited for.
- A blueprint is a copy in the library; editing the construct's logic system leaves the blueprint as it
  was.

## Checks

Both scripts read `Codex.resx` and `Codex.no.resx`, where every entry is a page. Run them from the repository root.

```
python .claude/skills/writing/codex-check.py
python .claude/skills/writing/tells.py src/Sprocket.Host.SpaceEngineers/Content/Data/Localization/Codex.resx
python .claude/skills/writing/tells.py --show src/Sprocket.Host.SpaceEngineers/Content/Data/Localization/Codex.resx <key>
```

`codex-check.py` reports the product's name in running text outside `::: player` boxes and quotes, a page linked
twice from one page, a mark followed by a plain space, and a page with no partner in the other language. Every hit is a
fault.

`tells.py` gives one line of counts per page, worst first: openers that define the subject, "such as", lists of three,
signposts, filler, uncontracted negatives, how much sentence length varies, and length. `--show` lists every hit on one
page with its sentence. A count is a signal, not a verdict.

## The reader test

A tip is a page here: the reader is given the tip pages and the term pages they link.

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

- Whether pages address the player as "you". Mal's home page and every page he approved on 2026-10-14 do, and he has
  not ruled on it as a rule.
