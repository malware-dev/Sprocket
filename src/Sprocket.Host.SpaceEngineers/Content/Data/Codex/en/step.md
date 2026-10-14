# Step

A step is one line of a ![](mark:sequence)&nbsp;[sequence](sequence), and the sequence runs its steps in order from the top. `+ step` adds one of these:

- `task` does something to blocks: an [action](action) like `open`, or setting one of their values.
- `wait` [waits](wait) a set time.
- `run` and `stop` [start or stop another sequence](run-and-stop).
- `if` [checks blocks](if), and runs the steps under it only when its condition passes.
- `await all pending tasks` waits for the awaitable steps before it.

Drag a step to move it, into an if or out of one. To try a sequence without some of its steps, pause them with ![](mark:pause)&nbsp;on their rows, and the sequence skips them until they're resumed.
