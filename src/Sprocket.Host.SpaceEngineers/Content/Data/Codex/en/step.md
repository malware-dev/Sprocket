# Step

A step is one line of a ![](mark:sequence)&nbsp;[sequence](sequence), and the sequence runs its steps in order from the top. `+ step` adds one of these:

- `task` does something to blocks: an [action](action) like `open`, or setting one of their values.
- `delay` [delays the next step](delay) by a set time.
- `run` and `stop` [start or stop another sequence](run-and-stop).
- `set state` gives one of the logic system's [states](logic-system-states) a new value.
- `if` [checks blocks](if), or one of the logic system's states, and runs the steps under it only when its condition passes.
- `wait for pending tasks` waits for the awaitable steps before it.

Drag a step to move it, into an if or out of one. To try a sequence without some of its steps, pause them with ![](mark:pause)&nbsp;on their rows, and the sequence skips them until they're resumed.
