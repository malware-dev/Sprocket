# Codex

Every term the in-game codex explains, in the order its margin lists them. A term's id is what a
link names it by, as in `[installations](#installation)`. Its page is `<id>.md`, named by its first
`#` heading, which is taken off the page into the screen's title where the page opens with it, and
stays on the page where something comes first. Its tooltip is `<id>.tldr.md`, a sentence or two
that names the term in `**strong**`. Both are in `src/Sprocket.Host.SpaceEngineers/Content/Data/Codex/<culture>/`: `en` is
the codex as written, and a translation is a folder named for its culture, such as `no`. A file a
translation lacks is read in English.

A term is in the game once the codex's `index` lists it. The index is a plain list beside the language
folders, one id a line in the order the margin lists them, where a line starting with `#` is a comment,
and every language reads the same one. The build reports a page without its heading (LOC007), either file
of a term without the other (LOC008), a link to a term with no page (LOC009), a box never closed (LOC010),
a link in a tooltip, where nothing can be pressed (LOC011), a translation missing a file or holding a stray
one (LOC006, LOC005), a codex with no index (LOC012), an id the index lists with no page (LOC013), a page
the index leaves out (LOC014), and an id listed twice (LOC015). A term a screen links to by name, and the
index leaves out, is said in the game's log as the codex is read.

| Id | Name | Page |
|---|---|---|
| `home` | Welcome to Sprocket Automation | Written; an introduction to the automation, named by the heading under the logo |
| `logic-system` | Logic system | Written |
| `installation` | Installation | Written |
| `sequence` | Sequence | Not written; the page says so |
| `configuration-panel` | Configuration panel | Written; says the logic system must be off to set it up, until the issue *The configuration panel can be set up while its logic system runs* is done |
| `library` | Library | Written; describes lost constructs as the issue *A lost construct's logic systems can be put on another construct* plans them, and is checked again when that and the library rework are done |
| `picking` | Picking blocks | Not written; the page says so. A first draft exists, held back to be written with sequence, trigger and step |
| `trigger` | Trigger | Not written; the page says so |
| `step` | Step | Not written; the page says so |
| `slot` | Slot | Written. Uses a `::: warning` box |
| `signal` | Signal | Written |
| `blueprint` | Blueprint | Written. Its player note is a `::: player` box |
| `construct` | Construct | Written |

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
`value`. A logic system's icon is a chip, its blueprint's is that chip cut out of a square, the mould it is made
from, and an installation's is the chip in front of a darker one, one of however many. Its words are left empty, so a host
that knows no such locator draws nothing in its place. A page's title carries none, since the title is the
term's name.

A box, `::: names` up to a line of only `:::`, sets blocks apart. `left`, `center` and `right` are
markdown-lite's own; the codex gives `warning` a look for what can go wrong, and `player` one for a
note about the player's own computer.

## Owed to pages not yet written

- **Sequence:** only tasks marked with the clock wait for their block; a run step waits for the
  sequence it runs; a signal that comes while the sequence is running is skipped and counted; a run
  saved halfway carries on from the same step after a reload.
- **Trigger:** a trigger can start on a signal, when a block changes (for example a door finishing
  closing), or at a set time; only the first needs a toolbar slot.
- **Picking:** covers more than blocks, for example what is done to them, signals, values and
  sequences; its name in the game may become *Picking*. A search stays for every list opened from
  the same screen until that screen is left, which waits on the issue *A picker's search is kept for
  the screen it was opened from, until that screen is left*.
- **Blueprint:** sharing a blueprint with another player from inside a world is planned; when it
  exists, the blueprint page's player note gains it beside copying the files by hand.
