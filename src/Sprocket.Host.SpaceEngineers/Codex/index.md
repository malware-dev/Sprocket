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
| `configuration-panel` | Configuration panel | Not written |
| `library` | Library | Not written |
| `picking` | Picking blocks | Not written |
| `trigger` | Trigger | Not written; not in the game yet |
| `step` | Step | Not written; not in the game yet |
| `slot` | Slot | Written; not in the game yet. Uses a `[!WARNING]` aside, which needs the tagged quote issue |
| `signal` | Signal | Written; not in the game yet |
| `blueprint` | Blueprint | Not written; not in the game yet |
| `construct` | Construct | Not written; not in the game yet |

## Owed to pages not yet written

- **Sequence:** only tasks marked with the clock wait for their block; a run step waits for the
  sequence it runs; a signal that comes while the sequence is running is skipped and counted; a run
  saved halfway carries on from the same step after a reload.
- **Trigger:** a trigger can start on a signal, when a block changes (for example a door finishing
  closing), or at a set time; only the first needs a toolbar slot.
