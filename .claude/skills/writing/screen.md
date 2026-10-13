# Texts on screen

Labels, buttons, questions, tooltips, statuses and the errors a player sees. Their reader is a player in
the middle of play.

**Clear, short, and what happened.** Mal rejected "Stopped because it could not work" (it says nothing),
then "Stopped before the world was last saved, for a reason that was not kept." ("a very wordy way to say
very little"), then "a block it acts on" ("What block?"). He accepted "Stopped at step 3: Airlock 1 Outer
Door was gone. Switching the system on releases it." and, where the cause is truly unknown, "Stopped for an
unknown reason." Cut to the fewest words that say what happened, why, and what to do; carry the step and
the block through wherever they are known; keep a short fallback rather than removing a message.

**Never "Sprocket" on screen, and nothing outside the world.** No server, admin, mod or configuration. A
limit is the device's capacity ("This block holds 20 sequences in a system"), and the feature is
"automation", the terminal button's word. Mal: "we will avoid using the name in-game"; the reason is to
keep a rename possible. Exceptions are his to grant (the logo is one), and table-talk is the one kind he set.

**Table-talk** (Mal's word) is a text about the game itself rather than the world: a limit the game puts on mods, a
file on the player's computer, a member the automation cannot set yet. It may say "the game" and "mods"; it never says
"Sprocket". It is written in italics. A tooltip that is all table-talk is italic throughout; a tooltip that mixes puts
the in-world words first, then a rule (`---`), then the table-talk. In the codex it sits in the `::: player` box. Mal,
2026-10-13: "A tooltip where everything is table talk probably don't need a 'player box'. Most likely, in a tooltip,
even when we mix, we can leave the table-talk to the end, separated by a HR. In the codex we probably need the player
box."

A screen wholly about the game, such as the admin dialog, is table-talk throughout. It may name the game, the
server and mods, and its texts are not italic: italics mark table-talk among in-world words (Mal, 2026-10-13).

**Tooltips are liberal.** Anything whose meaning or effect is not obvious from the screen gets one. Mal:
"We should also be quite liberal with tooltips." Their wording is his to judge, so show it to him.

**A sentence leaves out what an action does anyway.** A step that holds when stuck reads "depressurize
Airlock Vent"; only "or move on" is worth the words.

**A full sentence ends with a period; a fragment does not.** A label, a button's word, a heading, a list word or a
short command such as "Close the editor" has none; "Switch this logic system on, locking it against changes." has
one, and a text of several sentences ends every one with a period. A two-part tooltip keeps its bold first line bare
and its sentences punctuated. Mal adopted it on 2026-10-13 from the common practice of the major style guides
(PatternFly, SAP, GitLab, Apple, Microsoft's InfoTips). The build checks the several-sentence case; whether one line
is a sentence or a fragment is read.

**Every text's comment in `MyTexts.resx` stands alone.** It names the screen, the element and when the text
shows, and says what each placeholder is filled with, without leaning on the texts beside it: no "the button
above", "the same, naming the block". Mal: "Each individual info description must be readable and
understandable alone", and of the comments, "We can't make assumptions on how people will make their
translations, they may use tools". A reviewer and a translation tool see one text at a time.

**The wording as a whole is reworked on the screens.** Mal: "It must be done looking at the screens in
question and figuring out the best way to display it." A sentence that reads wrong goes into
`docs/Wording.md` - what is on screen, where it shows, what is wrong with it - with no replacement
proposed, and is not debated string by string.

## How a tooltip reads

Taken from Mal's rewrites on the text review of 2026-10-13. Each rule quotes the text as it was and as he
wrote it.

**The verb comes first, and it is the word software uses for it.** Duplicate, Select, Update, Repeat,
Perform. "Copy this step. The copy goes right after it." became "Duplicate this step."; "Pick which
installation..." became "Select which installation..."; "Put this logic system in place of its blueprint in
the library." became "Update the blueprint from the copy on your current construct."

**One sentence on what the control does, then stop.** What follows from it goes. Mal cut "It goes wherever
you go." from making a blueprint, "and choose which blocks it uses" from adding an installation, and "Every
installation of it changes with it." from opening a logic system, which became "Opens the **{name}** logic
system editor."

**A menu choice's tooltip names what the player gets.** "At a set interval, over and over." became "Repeat
a task at a set interval."; "When a block sends a signal, or reaches a state." became "Perform a task when a
block sends a signal or reaches a requested state."

**A term is explained by the sentence it sits in, never by a colon.** "Add a trigger: something that starts
a sequence, or does one task." became "Add a trigger that determines what starts a sequence."; "The
**library**: the logic systems on this construct, ..." became "The **library** contains ...".

**Plain words over coined ones.** "A logic system keeps at least one installation." became "needs"; "with
nothing to start it" became "without triggers".

**The text speaks to the player and points at the game's own screens.** "Update your construct's version.";
of the grid's name, "You can rename the grid here, same as in the control panel's Info tab."

**A refusal says what has to happen and why.** "{name} is on. Turn it off to edit." became "{name} must be
turned off in order to be edited safely."; "Turn the system off to edit." became "The system must be turned
off in order to make changes safely." Texts that refuse for the same reason are worded alike.

## Checks of its own

```
# The name, which a player never reads (values only)
grep -n "<value>" Content/Data/Localization/MyTexts.resx | grep -i "sprocket"
```

The build reports a text missing from either language as LOC005 or LOC006, and a change goes in with
neither.
