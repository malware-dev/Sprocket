# Step

A step is one line of a ![](mark:sequence) [sequence](#sequence). `+ step`, at the end of a sequence and of each branch of an if, adds one of these kinds:

- `task` does something to blocks: [an action](#action) such as `open`, or setting one of their values.
- `wait` [waits](#wait) a set time.
- `run` and `stop` [run or stop another sequence](#run-and-stop).
- `await all pending tasks`, offered where each wait is a line of its own, waits for the steps above it that take time, as the sequence page explains.
- `if` [checks blocks](#if), and runs the steps under it where the check holds.

## Moving and copying steps

Drag a step to move it, into an if's branch or out of one as well.

The copy mark on a step's row puts a copy right after it. An if is copied with every step in it.

Every line is numbered in one count, an if's own rows included. A problem names a step by that number, for example *Stopped at step 4 of Cycle Out*.

## Changing a step

Pressing a part of a step, such as its blocks or its action, opens the [picker](#picking) that made it, on that part.

A step can be paused with the pause mark on its row: the sequence skips it until it is resumed.

Steps can only be changed while the ![](mark:logic-system) [logic system](#logic-system) is switched off.
