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
keep a rename possible. Exceptions are his to grant (the logo is one).

**Tooltips are liberal.** Anything whose meaning or effect is not obvious from the screen gets one. Mal:
"We should also be quite liberal with tooltips." Their wording is his to judge, so show it to him.

**A sentence leaves out what an action does anyway.** A step that holds when stuck reads "depressurize
Airlock Vent"; only "or move on" is worth the words.

**The wording as a whole is reworked on the screens.** Mal: "It must be done looking at the screens in
question and figuring out the best way to display it." A sentence that reads wrong goes into
`docs/Wording.md` - what is on screen, where it shows, what is wrong with it - with no replacement
proposed, and is not debated string by string.

## Checks of its own

```
# The name, which a player never reads (values only)
grep -n "<value>" Content/Data/Localization/MyTexts.resx | grep -i "sprocket"
```

The build reports a text missing from either language as LOC005 or LOC006, and a change goes in with
neither.
