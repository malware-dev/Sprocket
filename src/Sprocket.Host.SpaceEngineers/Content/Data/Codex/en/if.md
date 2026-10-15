# If

Sometimes you need a ![](mark:sequence)&nbsp;[sequence](sequence) to do one thing or another, depending on the state of its blocks when it runs. An airlock with a single button is the usual case: when the room has air, the button should cycle you out, and when it's empty, cycle you in.

An if, also known as a "conditional", is the [step](step) for that. It checks blocks, and runs the steps under it only when its condition passes. Add its `otherwise`, and the steps there run when the condition doesn't pass, so one sequence covers both ways through the airlock.

The states a block can be in come first in the list, like a door being open or working, and read as the block itself: *if Airlock Door is open*. Any other value is named before its blocks, *if power on Airlock Door is on*. How it compares, here `is`, is a part of its own: press it to choose another, like `is not`, or `is below` for a number.

## More than two ways

An airlock's room isn't always full or empty. Caught halfway, it's neither, and then the safest thing is to close both doors. For a case like that, add an `otherwise if` with a condition of its own:

- `if` the room has air, cycle out.
- `otherwise if` the room is empty, cycle in.
- `otherwise`, close both doors.

The if tries its branches from the top, and runs the first one whose condition passes. Only one branch ever runs: once one has, the rest are skipped, and the sequence carries on after the whole if. The `otherwise` runs only when none of the conditions passed.

## Several blocks

When you need to know about more than one block, like every door on a hangar, select them all. A word in front of the value then says how they are read. Press it, or use the drop-down in the blocks column of the [value selector](selector), to choose:

- `any`: one of them is enough for the condition to pass.
- `all`: it takes every one of them.
- `the sum of`: the value added up over the blocks, *if the sum of uranium on Reactor 1 and Reactor 2 is below 10*.
- `the average of`: that sum divided by how many blocks were read.

The sum and the average work on numbers, so choosing either leaves only numbers in the list of what to check. The word appears only with more than one block, where the four differ.

## Checking once, not waiting

An if looks at its blocks once, at the moment the sequence reaches it. If the condition doesn't pass then, the if doesn't wait for it to. When you need something to happen as soon as a block reaches a state, whenever that is, use a [trigger](trigger) instead.

Dragging an if's first row moves the whole if, with everything in it.
