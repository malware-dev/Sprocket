# Codex

Every term the in-game codex explains, in the order its margin lists them. A term's id is what a
link names it by, as in `[installations](#installation)`. Its page is `<id>.md` and its tooltip is
`<id>.tldr.md`.

The game still reads the codex from `MyTexts.resx`, which these files have not reached yet. The
issue *Each codex entry is a file of its own in a Codex folder* connects the two.

| Id | Name | Page |
|---|---|---|
| `home` | The home screen | Not written |
| `logic-system` | Logic system | Written |
| `installation` | Installation | Written |
| `sequence` | Sequence | Not written |
| `configuration-panel` | Configuration panel | Written; says the logic system must be off to set it up, until the issue *The configuration panel can be set up while its logic system runs* is done |
| `library` | Library | Written; describes lost constructs as the issue *A lost construct's logic systems can be put on another construct* plans them, and is checked again when that and the library rework are done |
| `picking` | Picking blocks | Not written |
| `trigger` | Trigger | Not written; not in the game yet |
| `step` | Step | Not written; not in the game yet |
| `slot` | Slot | Written; not in the game yet. Uses a `[!WARNING]` aside, which needs the tagged quote issue |
| `signal` | Signal | Written; not in the game yet |
| `blueprint` | Blueprint | Written; not in the game yet. Its player note needs the tagged quote issue, and its taking-off section waits on the issue *Taking a logic system off a construct asks before it changes the blueprint* |
| `construct` | Construct | Written; not in the game yet. Describes the automation surviving the loss of every programmable block, which waits on the issue *A construct's automation survives losing every programmable block* |

## Marks

The game's fonts have no bold or italic face, so each mark has one job:

- `code` (a monospace face) for anything on screen that is pressed or read, such as `+ installation`.
- `**strong**` (orange) for the lead of a list item or a key distinction, used sparingly.
- `*emphasis*` (brighter) for names typed by a player or shown by the game, such as *Cycle Out*.

## Owed to pages not yet written

- **Sequence:** only tasks marked with the clock wait for their block; a run step waits for the
  sequence it runs; a signal that comes while the sequence is running is skipped and counted; a run
  saved halfway carries on from the same step after a reload.
- **Trigger:** a trigger can start on a signal, when a block changes (for example a door finishing
  closing), or at a set time; only the first needs a toolbar slot.
- **Blueprint:** sharing a blueprint with another player from inside a world is planned; when it
  exists, the blueprint page's player note gains it beside copying the files by hand.
