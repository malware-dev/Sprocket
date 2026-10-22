---
name: writing
description: How text in this repository is written - the rules every text follows, the vocabulary, and the mechanical check every draft passes - with a file of its own for each audience - codex pages, texts on screen, script errors. Use whenever writing or reviewing any text a person will read, and before showing a draft to Mal.
---

Every rule here is one Mal decided, with his words where they exist. A gap these files do not reach is
said out loud as a choice when it is made, and is never written in here as though it were settled. A rule
written into a skill comes back later as authority, and a guess written down is indistinguishable from a
decision the next time it is read.

When Mal corrects a draft, the correction goes in here, in the file for that audience, so it reaches every
writer and every later draft.

## Who reads it, and which file to read next

Decide this first; it changes the words. Read this file, then only the one for the text in hand.

- **A codex page or its tooltip:** a player learning the editor. Read `codex.md`.
- **A text on a screen** - a label, a button, a question, a tooltip, a status, an error a player sees:
  read `screen.md`.
- **An error from reading a script:** somebody editing the script by hand. Read `errors.md`.
- **A ticket:** the `issues` skill holds its form. **A commit message or a code comment:** the
  `code-review` skill holds what they may say. The rules below apply to both as well.
- **`docs/Language.md`:** nothing in it changes without Mal's yes to that exact text, and every addition
  to the language goes in there first. Read `language.md`.

## What every text does

**State what it does.** Mal: "We don't state what it doesn't do. We state what it does." A limit becomes
a claim about the shape that was chosen, or it goes. Before a sentence with no, not, never, without or
nothing in it, try the one that says what is there.

**It stands alone.** It is true in the present tense and says nothing about how it came to be written: no
history ("now", "used to", "was going to be"), no marks of the conversation ("as discussed"), no
hedging ("for now"). What is being tested, worked on or still open stays in chat.

**Fewest words that carry it.** One sentence per fact, no second example where one carries it, no reason
attached to what is obvious without it.

**Specific.** Where the block, the step or the cause is known, it is named.

**Spelling and punctuation.** US English in everything in the repository (color, behavior); game data
stays as the game spells it. No em dashes and no en dashes anywhere; a hyphen, or a different sentence.

**Both languages, always matched.** Every English text added or changed in `MyTexts.resx` has its
Norwegian written or rewritten in `MyTexts.no.resx` in the same change, and every codex page in `en/` has
its partner in `no/`. A change to one of a matched pair (a button pair, sibling texts, both languages)
is made to its partner too, without asking.

## The words

The vocabulary is settled; use it exactly, on screen, in the codex, in tickets and in chat.

- **Action** is a block's function (open, extend). **Value** is a block's property. **Task** is one
  operation: run, stop, an action, or setting a value. A value carries its own mark, `=?`, wherever it is
  named, set as well as checked; setting one is not an action. A wait is not a task.
- **Option** is a setting an action takes, such as a piston's position and speed. The picker asks for them in a
  column headed by the action's name, each by its own name, and a problem names the one at fault ("This step is
  missing its speed"). An option is never called a value, and a value never an option.
- **The installation panel** is the screen that fills an installation's slots. It is never "the
  blueprint editor"; a blueprint is edited on its logic system's screen.
- **An installation** is "a set of blocks and signals a logic system works on", never a copy of one and never
  itself called a logic system. **A blueprint** is a copy, kept in the library.
- **Signal slot** and **toolbar slot** are the Space Engineers host's words for where a signal comes from.
- A word for one thing is the same word everywhere.
- **Select** is the verb for taking something from a list, on screen and in the codex (Mal, 2026-10-13). The
  screen it is done on is a **selector**: block selector, value selector, signal selector (Mal, 2026-10-14), and the state selector, for the logic system's states (Mal, 2026-10-21; Norwegian tilstandsvelger). "Picker"
  is the code's name, never the player's.

Each term in one line, with its Norwegian word.

- **Action** (handling): a block's function, such as open or extend.
- **Await** (avvent): the step `await all pending tasks`, which waits for the steps above it that take more
  than one tick to complete.
- **Blueprint** (plantegning): a copy of a logic system, kept in the library.
- **Condition** (betingelse): what an if checks of its blocks; it passes, or the branch below is skipped.
- **Construct** (konstruksjon): a set of grids joined by mechanical blocks such as pistons, rotors and hinges.
- **Installation** (installasjon): a set of blocks and signals a logic system works on, such as one airlock
  among several.
- **Logic system** (logikksystem): the automation for one machine, like an airlock, with all the jobs that go
  with running it (Mal, 2026-10-14). A job is one task of it, like cycling out.
- **Option** (valg): a setting an action takes, such as a piston's position and speed.
- **Sequence** (sekvens): a list of steps a logic system runs from top to bottom.
- **Signal** (signal): what a block sends to start a trigger.
- **Slot** (plass): a named place in a logic system, such as the inner door, that each installation fills with
  its own blocks or signal.
- **Task** (oppgave): one operation: run, stop, an action, or setting a value.
- **Tick** (tikk): the smallest step of time, sixty to a second.
- **Trigger** (utløser): a line that runs or stops a sequence when a signal arrives, blocks
  reach a state, or a set time passes. A trigger **fires** (utløses) (Mal, 2026-10-13; "goes off" read
  as switching off).
- **Value** (verdi): a block's property, such as a light's color.

**Script, presentation and prose are three layers.** An id in the script is not a word on screen, and a
rename on screen changes no id.

## When quoting product text to Mal

Quote a text in its template form, `Switch off **{name}**?`, and label a filled-in example as one ("for a
logic system called Airlock: ..."). Mal sees only the words in the reply; an example value reads as a
literal.

## The mechanical check

Run on every draft before anyone reads it, and fix every hit or justify it out loud. On a diff, `git diff
-U0 | grep "^+"` is the input; on a page, the file. The audience files add checks of their own.

```
# Dashes: none at all
grep -nP "\x{2014}|\x{2013}" <files>

# Negations: each one read, and rewritten as what is there wherever it can be
grep -niwE "not|never|no|nothing|without|doesn't|don't|isn't|can't|won't|cannot" <files>

# History and the conversation
grep -niwE "now|used to|previously|no longer|instead of|originally|for now|as discussed" <files>
```

A hit that quotes words on a screen, or a negation that states a real limit a reader would otherwise get
wrong, stays, said out loud.
