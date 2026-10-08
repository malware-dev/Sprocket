# Step

A step is one line of a ![](mark:sequence) [sequence](#sequence): a task, a wait, or an if. The buttons at the end of a sequence, `+ task`, `+ wait`, `+ run`, `+ stop` and `+ if`, add one.

## Task

A task is one operation:

- an action on blocks, such as `open` or `depressurize`,
- setting one of their values, such as a light's `Radius` or `Color`,
- running or stopping another sequence.

An action is something a block does, and a value is one of its properties.

## Wait

A wait spends a set time before the next step. An action that takes time, such as a door closing, is already waited for, so a wait is for time itself, for example to give the crew a moment to step through.

## If

An if checks blocks once, when the sequence reaches it, and runs the steps under it where the check holds. Its rows read `if`, `otherwise if` and `otherwise`:

- `otherwise if` checks something else, where nothing above it held,
- `otherwise` runs where nothing above it held.

The if ends with a wordless row of its own, where `+ otherwise if` and `+ otherwise` add a branch.

## Moving steps

Drag a step to move it. Dragging an if's first row moves the whole if, with everything in it. To move a step into a branch or out of one, drag the step itself.

Every line is numbered in one count, the if's own rows included. A problem names a step by that number, for example *Stopped at step 4 of Cycle Out*.

## Changing a step

Pressing a part of a step, such as its blocks or its action, opens the [picker](#picking) that made it, on that part.

A step can be paused with the pause mark on its row: the sequence skips it until it is resumed.

Steps can only be changed while the ![](mark:logic-system) [logic system](#logic-system) is switched off.
