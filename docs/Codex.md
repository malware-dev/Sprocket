# Codex

The codex is every tip Sprocket shows. A tip is a codex tip (`Lumel/CodexTip.cs`): it opens beside the element the
pointer rests on, and the pointer can move onto it. Resting on a link in it opens the link's page in a tip of its own
beside it, up to three side by side; moving back onto an earlier tip closes those opened after it, and leaving them all
closes all.

An admin trial, `tipKeeping`, sets whether a tip always lets the pointer onto it (`stay`) or only while Alt is held
(`alt`).

## Pages

The pages are the texts of `src/Sprocket.Host.SpaceEngineers/Content/Data/Localization/Codex.resx`, one entry a page,
translated in `Codex.no.resx` as `MyTexts.resx` is in `MyTexts.no.resx`. The game loads both pairs into its table of
texts together, and the text generator writes the class `Codex` from the file, one property a page, reached as
`texts.Codex`, so a page the code names and the file lacks fails the build.

A page is markdown-lite, and the tip shows the whole of it, with no title. A link names the page it leads to by its
key without `Sprocket.`, as in `[slot](Term.Slot)`, and `CodexPages` finds the page for it. Every link in every page,
in every language, has to lead to a page: the build fails on a link to a key no file of texts has (LOC021), and counts
a page a link leads to as read. The presentation tests hold that every mark and box a page asks for is one the editor
has.

The term pages are keyed `Sprocket.Term.<Name>`; the rest are the tips of the screens' elements, keyed by screen and
element as the texts in `MyTexts.resx` are. A text shown anywhere outside a tip, a label or the words a tip and a screen
share, is in `MyTexts.resx`, and a tip that needs it reads it there. The guide to rich text, shown in a window of its own
by the Markup button beside every description box, is `Sprocket.Guide.RichText` in `MyTexts.resx`.

A section's heading that is a term, such as the home screen's `LOGIC SYSTEMS`, is written as a link to its page:
it shows in the link color, takes no press, and has the page as its tip. The heading is as wide as its words, so the
tip stands beside them.

The words a step opens with on a sequence's screen are written the same way where they are a term: `if` leads to the
page on ifs, `otherwise if` and `otherwise` to the page on branches, `delay` to the page on delays, and `run` and
`stop` to the page on running and stopping. Each page is linked only from the first row on the screen that leads to
it, so a `stop` after a `run` is plain; on a paused row the link is as quiet as the rest of the sentence.

## Marks

Each mark has one job:

- `code` (Maltek Mono, on a faint box) for anything on screen that is pressed or read, such as `+ installation`.
- `**strong**` (bold) for the lead of a list item or a key distinction, used sparingly.
- `*emphasis*` (italic) for names typed by a player or shown by the game, such as *Cycle Out*.

A link shows in the link color, the same everywhere, with no line under it.

A term with a kind icon carries it where the page sets the term apart, such as `![](mark:signal) **signal slot**`.
The icon is an image whose locator is `mark:` and the kind, which the editor draws from Maltek Symbols at three
quarters of the line: `sequence`, `signal`, `block`, `screen`, `blueprint`, `logic-system`, `installation`, `action`,
`awaited`, `value` and `trigger`. A button's mark names the button by what it shows, as in `![](mark:play)`: `play`, `stop`,
`pause`, `delete`, `duplicate`, `rename`, `undo` and `redo`.

A box, `::: names` up to a line of only `:::`, sets blocks apart. `left`, `center` and `right` are markdown-lite's own;
the codex gives `warning` a look for what can go wrong, and `player` one for a note about the player's own computer.
