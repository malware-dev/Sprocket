# Undo

Each screen keeps its own ![](mark:undo)&nbsp;undo and ![](mark:redo)&nbsp;redo: each ![](mark:sequence)&nbsp;[sequence](sequence), each [slot's](slot) page, the [installation panel](installation-panel), and the ![](mark:logic-system)&nbsp;[logic system's](logic-system) own screen. Undo takes back the last change made on the screen you're on.

Some changes reach several screens. Selecting blocks for a step changes the sequence and makes or fills the slots for those blocks, and adding a [trigger](trigger) can make the sequence it runs. Such a change can be undone on any screen it reached.

Undo always works. Where a later change on another screen rests on what it takes back, undo takes that change back with it, in one step, and its tooltip names those screens. Redo brings them back together.

A slot's name belongs to the whole logic system, so renaming a slot is undone on the slot's page or on the logic system's screen. A step brought back by undo names the slot by its current name.
