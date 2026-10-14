# Wait

A wait is a [step](step) that holds its ![](mark:sequence)&nbsp;[sequence](sequence) for a set time, in seconds or [ticks](tick).

Use one to give a block time the sequence can't watch for, like a sound playing to its end, or to leave a pause, like keeping the airlock's light red for a few seconds after the outer door opens.

A wait counts only time, and blocks still moving from earlier steps carry on through it. Where an action is awaitable, the sequence can wait for the block itself, and needs no wait to give it time.
