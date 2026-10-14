# If

Sometimes you need a ![](mark:sequence)&nbsp;[sequence](sequence) to do one thing or another, depending on the state of its blocks when it runs. An airlock with a single button is the usual case: when the room has air, the button should cycle you out, and when it's empty, cycle you in.

An if, also known as a "conditional", is the [step](step) for that. It checks blocks, and runs the steps under it only when its condition passes. Add its `otherwise`, and the steps there run when the condition doesn't pass, so one sequence covers both ways through the airlock.

## More than two ways

An airlock's room isn't always full or empty. Caught halfway, it's neither, and then the safest thing is to close both doors. For a case like that, add an `otherwise if` with a condition of its own:

- `if` the room has air, cycle out.
- `otherwise if` the room is empty, cycle in.
- `otherwise`, close both doors.

The if tries its branches from the top, and runs the first one whose condition passes. Only one branch ever runs: once one has, the rest are skipped, and the sequence carries on after the whole if. The `otherwise` runs only when none of the conditions passed.

## Several blocks

When you need to know about more than one block, like every door on a hangar, select them all. Then choose: `any one` of them is enough for the condition to pass, or it takes `all of them`.

## Checking once, not waiting

An if looks at its blocks once, at the moment the sequence reaches it. If the condition doesn't pass then, the if doesn't wait for it to. When you need something to happen as soon as a block reaches a state, whenever that is, use a [trigger](trigger) instead.

Dragging an if's first row moves the whole if, with everything in it.
