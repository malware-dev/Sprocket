# Action

An action is something a block can do, like a door opening or a piston extending. Each kind of block brings its own, and a [step](step) does one when you add it with `task`.

In the [block selector](selector), an action marked ![](mark:awaited)&nbsp;is [awaitable](sequence): it runs over more than one [tick](tick), and the sequence can wait for it to finish. With one marked ![](mark:action), the sequence starts the action and moves on, regardless of how long the block takes to finish it.

The block selector lists the actions known to work with the automation, under plain names. `Terminal ›` lists everything the blocks offer in the terminal instead, under the terminal's own names. These are offered without any promise: they haven't been checked with the automation, and may not work as you expect.

## Several blocks at once

One step can act on several blocks, like every light in the airlock, and the block selector then offers only what all of them can do. An awaitable step finishes when the last of its blocks does.

## Setting a value

A value is one of a block's properties, like a light's color. Setting one is a task too, listed with the actions and marked ![](mark:value). It takes effect straight away, so the sequence never waits for it. A value that is a state the block is in, like a tank stockpiling or an antenna broadcasting, reads as enabling or disabling it: the step says `enable stockpiling on Tank` when it sets the state on and `disable stockpiling on Tank` when it sets it off, and pressing the first word switches between them. To check a value instead, use an [if](if); the same state reads `if Tank is stockpiling`.

The [drill arm](drill-arm) example puts actions with options to use.
