# Codex

The codex is every tip Sprocket shows. A tip (`Lumel/CodexTip.cs`) opens beside the element the pointer rests on, and
the pointer can move onto it. Resting on a link in it opens that page in a tip of its own beside it, up to three side
by side. Holding Alt keeps a tip open and lights its links, and the line "Hold Alt to explore" at a tip's foot says so.

The question mark in the heading bar turns tips off and on. That's the player's own choice, kept in the `[Player]`
section of their machine's global `sprocket.ini`, never sent to a server. An admin trial, `tipKeeping`, sets whether a
tip always lets the pointer onto it (`stay`) or only while Alt is held (`alt`).

## Pages

The pages are `Content/Data/Localization/MyTextsCodex.resx`, one entry a page, translated in `MyTextsCodex.no.resx`.
The text generator writes the class `Codex` from it (`texts.Codex`), so a page the code names and the file lacks fails
the build.

A page is markdown-lite, shown whole with no title. A link names its page by key without `Sprocket.`, as in
`[slot](Term.Slot class=term)`. Every link in every language must lead to a page: the build fails otherwise (LOC021).
Term pages are `Sprocket.Term.<Name>`; the rest are the tips of screen elements, keyed by screen and element. A text
shown outside a tip lives in `MyTexts.resx`, and a tip that needs it reads it there.

A heading that is a term, such as `LOGIC SYSTEMS`, is written as a link to its page: link-colored, not pressable, with
the page as its tip. The words a step opens with link the same way where they are a term (`if`, `otherwise`, `delay`,
`run`, `stop`, `abort`, `if it fails`, `finally`), only on the first row on the screen that leads to that page.

## Marks

- `code` (Maltek Mono, on a faint box) for anything on screen that is pressed or read, such as `+ installation`.
- `**strong**` for the lead of a list item or a key distinction, sparingly.
- `*emphasis*` for names typed by a player or shown by the game, such as *Cycle Out*.
- `[words]{.hint}` for a hint, drawn in the color terms light up in. The parse must be told to read classes
  (`Markdown.Parse(text, true)`).

A link shows in the link color everywhere, with no line under it.

A term with a kind icon carries it where the page sets the term apart, as in `![](mark:signal) **signal slot**`, drawn
from Maltek Symbols: `sequence`, `signal`, `block`, `screen`, `blueprint`, `logic-system`, `installation`, `action`,
`awaited`, `value`, `trigger`, `state`. A button's mark names what it shows: `play`, `stop`, `pause`, `delete`,
`duplicate`, `rename`, `undo`, `redo`.

A box, `::: names` up to a line of only `:::`, sets blocks apart: `left`, `center` and `right`, `warning` for what can
go wrong, and `player` for a note about the player's own computer.
