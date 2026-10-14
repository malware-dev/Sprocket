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
| `installation-panel` | Installation panel | The panel fills one installation's slots; the description and slots are written on the logic system's screen and the slot's page |
| `slot` | Slot |  |
| `rich-text` | Rich text | Opened by every description box's guide button; shows each piece of markdown-lite beside what it gives |
| `sequence` | Sequence | Its player note shows both ways of showing a wait while one is chosen |
| `step` | Step |  |
| `action` | Action |  |
| `wait` | Wait |  |
| `if` | If |  |
| `run-and-stop` | Run and stop |  |
| `tick` | Tick |  |
| `selector` | Selector | The block, signal and value selectors, and the search they share |
| `trigger` | Trigger |  |
| `signal` | Signal |  |
| `library` | Library | Describes lost constructs as the issue *A lost construct's logic systems can be put on another construct* plans them, and is checked again when that and the library rework are done |
| `blueprint` | Blueprint | Its player note tells how to copy a blueprint's file to another computer |
| `construct` | Construct |  |
| `changing-hands` | Changing hands | Opened by the resume screen's guide button |
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

The project's GitHub wiki is written from the codex, in English, by `tools/Sprocket.Wiki`, into a clone of
the wiki's repository that is then pushed:

```
dotnet run --project tools/Sprocket.Wiki -- <wiki folder>
```

It shows only Sprocket's own pictures, and stops at a locator naming anything else.

Still open: whether a mark's one ink, GitHub's muted grey, reads well enough on both of GitHub's themes or each
theme gets a copy of its own, and whether the wiki is written on every push to `main` rather than by hand.
