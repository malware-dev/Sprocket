# Action

An action is something a block does, one of its functions, such as opening a door or extending a piston. Each kind of block brings its own: a door can open and close, and a sound block can play and stop.

For a [step](#step) that does an action, select `task` under `+ step`.

## Finding one

[Pick](#picking) the blocks, and `What to do` lists what they can do, grouped under headings such as `Power`, `Movement` and `Appearance`. For the blocks the automation knows, the common actions have plain names, such as a piston's `Move to`.

`Terminal ›`, at the end of the heading, lists instead everything the blocks offer in the terminal, under the terminal's own names. Using those is your own call. The organized list holds the ones known to work with the automation. A member the automation can't set yet is listed only here, dimmed, and says why when the pointer rests on it. `‹ Organized` goes back.

## Which actions take time

For most actions, the ![](mark:sequence) [sequence](#sequence) tells the block and moves straight on to the next step, without waiting for the block to finish, such as playing a sound. A few take time to finish, such as a door closing, a vent filling or emptying a room, or a piston or rotor moving to where it was sent, and a new step that does one waits for it: the next step begins only once the block is done. The sequence page explains how a step goes straight on while its block is still going.

In the picker, each action carries a mark:

- ![](mark:action) **A bolt** marks an action the sequence starts without waiting for the block to complete it.
- ![](mark:awaited) **A clock** marks one that takes time to finish. The sequence can wait for it, or go straight on while it runs.

Some actions with a clock offer a choice in the column beside them: a battery's or jump drive's `Recharge` can `wait` until the block is charged or `move on` to the next step at once, and a gas tank's `Stockpile` can wait `until full` or go straight on `without waiting`. They wait unless you select something else.

When a step fails or holds, for example because its block was lost or is stuck, the sequence page says what happens and how to put it right.

## Several blocks at once

One step can act on several blocks, such as every light in the airlock: tick them all in the picker, and `What to do` lists what all of them can do. Where the action takes time, the step finishes when the last of them does.

## Actions that take options

Some actions need more to go on, such as where to go and how fast. They take options, asked for in a column of the picker headed by the action's name, each option by its own name:

- A piston's `Move to` takes a position and a speed.
- A rotor's `Rotate to` takes an angle, a speed, and which way to turn.

A number is typed into its box, and up and down move it a step.

An option that must be given says so when it is left empty. One that may be left out can stay empty, and the action then does what it does without it.

## Setting a value

A value is one of a block's properties, such as a light's color or radius, and setting one is a task of its own, beside the actions. Values are listed in `What to do` with the actions, each marked with ![](mark:value) **`=?`**, the mark a value carries wherever it is named. Select one, and give the new value in the column headed by its name. The sequence moves straight on, as it does after an action with a bolt.

To check a value, and do something only if its condition passes, use an [if](#if).

## Examples

### The airlock's doors and vent

In the airlock's *Cycle Out*, `Close` for the inner door, `Depressurize` for the vent and `Open` for the outer door each carry a clock, and the sequence waits at each: the room empties only once the inner door is shut.

### A piston arm

To extend a drill arm to 5 m at 1 m/s, add a task, pick the arm's piston, select `Move to` under `Movement`, and give 5 for the position and 1 for the speed. The step lasts until the piston is at 5 m. With every piston of the arm picked, it lasts until the last of them gets there.

### The airlock's light

To turn the airlock's light red as the cycle starts, add a task and drag it to the top of *Cycle Out*. Pick the light, select its color under `Appearance`, and select red in the column beside it. Setting a value is not waited for, so the inner door starts closing straight after.
