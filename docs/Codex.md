# Codex

Every term the in-game codex explains, in the order its margin lists them. A term's id is what a
link names it by, as in `[installations](installation)`. Its page is `<id>.md`, named by its first
`#` heading, which is taken off the page into the screen's title where the page opens with it, and
stays on the page where something comes first. Its tooltip is `<id>.tldr.md`, a sentence or two
that names the term in `**strong**`. Both are in `src/Sprocket.Host.SpaceEngineers/Content/Data/Codex/<culture>/`: `en` is
the codex as written, and a translation is a folder named for its culture, such as `no`. A file a
translation lacks is read in English.

A term is in the game once the codex's `index` lists it. The index is a plain list beside the language
folders, one id a line in the order the margin lists them, where a line starting with `#` is a comment and
a line starting with `>` begins a section, headed by that term, with the terms after it listed under it.
Every language reads the same one. An id is its page's English title in lowercase, with a hyphen for each
space, apart from `home`. The build reports a page without its heading (LOC007), either file
of a term without the other (LOC008), a link to a term with no page (LOC009), a box never closed (LOC010),
a link in a tooltip, where nothing can be pressed (LOC011), a translation missing a file or holding a stray
one (LOC006, LOC005), a codex with no index (LOC012), an id the index lists with no page (LOC013), a page
the index leaves out (LOC014), and an id listed twice (LOC015). A term a screen links to by name, and the
index leaves out, is said in the game's log as the codex is read.

| Id | Name | Notes |
|---|---|---|
| `home` | Welcome to Sprocket Automation | An introduction to the automation, named by the heading under the logo; the one page that names Sprocket |
| `logic-system` | Logic system |  |
| `installation` | Installation |  |
| `sequence` | Sequence | Its player note shows both ways of showing a wait while one is chosen |
| `installation-panel` | Installation panel | The panel fills one installation's slots; the description and slots are written on the logic system's screen and the slot's page |
| `library` | Library | Describes lost constructs as the issue *A lost construct's logic systems can be put on another construct* plans them, and is checked again when that and the library rework are done |
| `selector` | Selector | The block, signal and value selectors, and the search they share |
| `trigger` | Trigger |  |
| `step` | Step |  |
| `action` | Action |  |
| `run-and-stop` | Run and stop |  |
| `wait` | Wait |  |
| `tick` | Tick |  |
| `if` | If |  |
| `slot` | Slot |  |
| `signal` | Signal |  |
| `blueprint` | Blueprint | Its player note tells how to copy a blueprint's file to another computer |
| `construct` | Construct |  |
| `changing-hands` | Changing hands | Opened by the resume screen's guide button |
| `rich-text` | Rich text | Opened by every description box's guide button; shows each piece of markdown-lite beside what it gives |
| `examples` | Examples | Heads the section of worked examples |
| `simple-airlock` | Simple airlock |  |
| `drill-arm` | Drill arm |  |
| `hangar-door` | Hangar door |  |

Every page and tooltip is translated into Norwegian, in `no`, which is also the example of how a translation is laid out.

## Marks

Each mark has one job:

- `code` (Maltek Mono, on a faint box) for anything on screen that is pressed or read, such as `+ installation`.
- `**strong**` (bold) for the lead of a list item or a key distinction, used sparingly.
- `*emphasis*` (italic) for names typed by a player or shown by the game, such as *Cycle Out*.

A link is underlined, in the link color, the same everywhere. A term is linked once on a page, at its first
mention.

A term with a kind icon carries it where the page sets the term apart: beside its link, and where it is
named in bold, such as `![](mark:signal) **signal slot**`. Running text stays plain. The icon is an image
whose locator is `mark:` and the kind, which the editor draws from Maltek Symbols at three quarters of the
line: `sequence`, `signal`, `block`, `blueprint`, `logic-system`, `installation`, `action`, `awaited` and
`value`, `trigger`. A logic system's icon is a chip, its blueprint's is that chip cut out of a square, the mould it is made
from, and an installation's is the chip in front of a darker one, one of however many. Its words are left empty, so a host
that knows no such locator draws nothing in its place. A page's title carries none, since the title is the
term's name. A button's mark names the button by what it shows, as in `![](mark:play)`: `play`,
`stop`, `pause`, `delete`, `duplicate`, `rename`, `undo` and `redo`.

A box, `::: names` up to a line of only `:::`, sets blocks apart. `left`, `center` and `right` are
markdown-lite's own; the codex gives `warning` a look for what can go wrong, and `player` one for a
note about the player's own computer.

## Owed to the pages

- **Sequence:** a signal that comes while the sequence is running is skipped and counted; a run saved
  halfway carries on from the same step after a reload.

## The wiki

The codex is to be published as the project's GitHub wiki, in English, by a generator.
What is decided:

- Each page is a file named for its English title, with a hyphen for each space, so `logic-system` is
  `Logic-system.md` and the wiki shows it as *Logic system*. `home` is the wiki's `Home.md`. The page's own
  `#` heading is left out, since the wiki shows the title above it.
- A link is written as it is in the codex. The wiki matches `[installations](installation)` to
  `Installation.md` whatever the case, so every link carries over unchanged.
- A picture is rewritten from a table of Sprocket's own pictures, each kept in the wiki as a PNG: every
  `mark:` drawn from Maltek Symbols, and `Sprocket_Mark`. A locator outside the table stops the generator,
  so only Sprocket's own pictures are published. A mark is shown, never described in words.
- `=WxH` becomes an `<img>` with its width and height, the only sizing the wiki has.

Still open: what a `::: player` and a `::: center` box become (the wiki has `> [!NOTE]` alerts), whether a
link carries its term's tooltip as a title (the wiki shows `[words](id "tooltip")` on hover), and where the
generator lives and when it runs.
