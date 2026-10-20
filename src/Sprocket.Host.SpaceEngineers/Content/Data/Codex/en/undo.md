# Undo

Each screen keeps its own ![](mark:undo)&nbsp;undo and ![](mark:redo)&nbsp;redo: each ![](mark:sequence)&nbsp;[sequence](sequence), each [slot's](slot) page, the [installation panel](installation-panel), and the ![](mark:logic-system)&nbsp;[logic system's](logic-system) own screen. Undo takes back the last change made on the screen you're on.

Some changes reach several screens. Selecting blocks for a step changes the sequence and makes or fills the slots for those blocks, and adding a [trigger](trigger) can make the sequence it runs. Such a change can be undone on any screen it reached.

Undo always works. Where a later change on another screen rests on what it takes back, undo takes that change back with it, in one step, and its tooltip names those screens. Redo brings them back together.

A slot's name belongs to the whole logic system, so renaming a slot is undone on the slot's page or on the logic system's screen. A step brought back by undo names the slot by its current name.

## Leaving a screen

Pressing `Back`, or Escape, on a screen where you changed something asks `Keep your changes?`. `Keep them` goes back with them, and `Discard them` takes them back first. `Done` keeps them without asking. The logic system's screen, each sequence's screen, a slot's page and an installation's panel all ask.

Each screen asks only about the changes made on it. What you changed on a screen opened from it, like a sequence opened from the logic system's screen, was kept or discarded as you left that one, and stays as it is.

The name a new logic system or sequence is given as it's made is part of making it, and stays whichever you choose as you leave.
