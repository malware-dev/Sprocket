# Codex

Every term the in-game codex explains, in the order its margin lists them. A term's id is what a
link names it by, as in `[installations](#installation)`. Its page is `<id>.md`, named by its first
`#` heading, which is taken off the page into the screen's title where the page opens with it, and
stays on the page where something comes first. Its tooltip is `<id>.tldr.md`, a sentence or two
that names the term in `**strong**`. Both are in `src/Sprocket.Host.SpaceEngineers/Content/Data/Codex/<culture>/`: `en` is
the codex as written, and a translation is a folder named for its culture, such as `no`. A file a
translation lacks is read in English.

A term is in the game once `Features/Codex/Terms.cs` lists it. The build reports a page without its
heading (LOC007), either file of a term without the other (LOC008), a link to a term with no page
(LOC009), a box never closed (LOC010), a link in a tooltip, where nothing can be pressed (LOC011), and a
translation missing a file or holding a stray one (LOC006, LOC005).

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

A link is underlined, in the link color, the same everywhere.

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
