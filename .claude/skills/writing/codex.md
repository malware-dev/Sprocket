# Codex tips

Every tip is a codex page: one entry in `Content/Data/Localization/Codex.resx`, with its Norwegian in `Codex.no.resx`,
written in rich text. A few short lines, no title. It never scrolls.

A page links a term by its key without the `Sprocket.` prefix: `[slot](Term.Slot class=term)`. A link to a page that
doesn't exist fails the build. A term page names its term in bold in its first sentence, beside its mark where it has
one. A term with a mark always shows it, linked or bold: `[![](mark:blueprint)&nbsp;blueprints](Term.Blueprint class=term)`, with the mark inside the link. A mark is joined to its word by `&nbsp;`. A word that's only an everyday word gets neither link nor mark.

A term is marked once per page and once per screen. A control's own tip doesn't count. Link a word only where it means the Thing: "running" isn't "run". A tip never links what it already explains, and a button on a row doesn't link the thing the row is ("Delete this step").

## Facts the pages rest on

- A construct is a grid and everything pistons, rotors and hinges attach to it. Connectors and landing gear dock one
  construct to another, and each stays its own.
- A construct keeps its automation in a programmable block one of its owners owns, and loses it only when the last
  programmable block is destroyed or removed. The logic systems the library lacks are then kept as backups.
- Most actions happen at once. A few take time, like a door closing, a vent filling a room, or a piston moving to where
  it was sent, and so does a step that runs another sequence. A sequence waits only for those.
- A blueprint is a copy in the library. Editing the construct's logic system leaves the blueprint as it was.

## Checking

Run `python .claude/skills/writing/codex-check.py` from the repository root. Every hit is a fault.

A new term page gets a reader test before Mal sees it: write three to six questions a reader would come to it for, with
the answers kept back, and give a small model (Haiku) only the pages. It's told it knows Space Engineers well, and
to answer only from the pages and mark every guess. A wrong answer is a fault in the page. Fix it and run it again.
Mal still reads every page.
