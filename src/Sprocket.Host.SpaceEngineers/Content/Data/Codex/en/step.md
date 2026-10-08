# Step

A step is one line of a ![](mark:sequence) [sequence](#sequence). The buttons at the end of a sequence add one:

- `+ task` does something to blocks: [an action](#action) such as `open`, or setting one of their values.
- `+ run` and `+ stop` [run or stop another sequence](#run-and-stop).
- `+ wait` [waits](#wait) a set time.
- `+ if` [checks blocks](#if), and runs the steps under it where the check holds.

## Moving steps

Drag a step to move it, into an if's branch or out of one as well.

Every line is numbered in one count, an if's own rows included. A problem names a step by that number, for example *Stopped at step 4 of Cycle Out*.

## Changing a step

Pressing a part of a step, such as its blocks or its action, opens the [picker](#picking) that made it, on that part.

A step can be paused with the pause mark on its row: the sequence skips it until it is resumed.

Steps can only be changed while the ![](mark:logic-system) [logic system](#logic-system) is switched off.
