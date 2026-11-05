---
name: writing
description: How text in this repository is written - who reads it, how it sounds, how terms work, and what stays out of the world - with a file for codex tips, one for texts on screen, and ones for script errors and the spec. Use whenever writing or reviewing any text a person will read, and before showing a draft to Mal.
---

Who reads this: an engineer, who knows ships and blocks but isn't a programmer. Write what you'd say to one at the
keyboard, in plain English or plain Norwegian. Read it aloud. If it sounds odd, it's wrong, whatever else it passes.

Short and clear. Say what the reader needs and stop.

Sounding like an AI gives the text away, so never:

- use em dashes or en dashes
- refer to a conversation, a change or a history the reader wasn't part of
- pad with signposts or filler
- use the stock habits: "not X but Y", neat lists of three, closing summaries

## Terms

Sprocket's own words have one name each, the one on screen, and a page of their own. A tip that uses one links it the
first time and doesn't explain it again. Some of them are also everyday words. Where a word is only the everyday word, it's just a word: a state's value isn't a block value.

- **Action** (handling): something a block does, such as open or extend.
- **Await** (avvent): the step that waits for the steps above it that take time.
- **Block value** (blokkverdi): a block's property, such as a light's color.
- **Blueprint** (plantegning): a copy of a logic system, kept in the library.
- **Condition** (betingelse): what an if checks of its blocks.
- **Construct** (konstruksjon): a set of grids joined by pistons, rotors and hinges.
- **Installation** (installasjon): a set of blocks and signals a logic system works on, such as one airlock among several.
- **Logic system** (logikksystem): the automation for one machine, like an airlock.
- **Option** (valg): a setting an action takes, such as a piston's speed.
- **Selector** (velger): the screen something is selected on. The verb is select.
- **Sequence** (sekvens): a list of steps a logic system runs from top to bottom.
- **Signal** (signal): what a block sends to start a trigger.
- **Slot** (plass): a named place in a logic system, such as the inner door, that each installation fills.
- **State** (tilstand): something a logic system remembers.
- **State type** (tilstandstype): a named list of choices a state can hold. The script's word is `type`.
- **Task** (oppgave): one operation: run, stop, an action, or setting a value.
- **Tick** (tikk): the smallest step of time, sixty to a second.
- **Trigger** (utløser): a line that runs or stops a sequence. A trigger **fires** (utløses).

## Staying in the world

Table talk about the game or the player goes in a player box, and only if there's no way to avoid it. In-game text
never names Sprocket.

## Also

Screen words are quoted as they appear. Icons are shown, never described. Both languages always match. No script
syntax outside the script's own texts. US English; game data stays as the game spells it.

Quote a text to Mal in its template form, `Switch off **{name}**?`, and label a filled-in example as one.

## Other files

- `codex.md`: codex pages and tips.
- `screen.md`: labels, buttons, questions and tooltips on screen.
- `errors.md`: errors from reading a script, whose reader is a developer.
- `language.md`: `docs/Language.md`, which changes only with Mal's yes to the exact text.
- A ticket's form is in the `issues` skill. A commit message or code comment is in the `code-review` skill.
