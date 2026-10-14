# Sequence

A sequence is a list of [steps](step) that a ![](mark:logic-system)&nbsp;[logic system](logic-system) runs from top to bottom, like an airlock's *Cycle Out*: close the inner door, empty the room, open the outer door. Each ![](mark:installation)&nbsp;[installation](installation) runs it on its own blocks and signals.

## Starting and stopping

While the logic system is switched on, a [trigger](trigger) starts a sequence, for example when the airlock's button is pressed.

To try a sequence while you build it, run it by hand with ![](mark:play)&nbsp;on its row. A run by hand works even while the logic system is switched off, and uses the installation selected under `Installation`.

An installation runs a sequence only once at a time, so a trigger that fires while it's still running is skipped.

Stopping a run ends it at the step it has reached, but a block that is already moving may still finish: a closing door still closes, and a vent goes on emptying the room, while a piston or a rotor stops where it is. Switching off the logic system or the installation stops its runs the same way, apart from those started by hand.

## Steps that run over more than one [tick](tick)

Most actions are done within a tick, like switching a light on. Others run longer, like a door closing or a vent emptying a room. Those marked ![](mark:awaited)&nbsp;are **awaitable**: the sequence waits for the block to finish before the next step, so the vent starts once the inner door is shut. A step that [runs another sequence](run-and-stop) is awaitable too, and waits until that sequence has finished.

A sequence can wait only for awaitable actions, which are the ones whose end the block can be watched for. An action marked ![](mark:action)&nbsp;is started, and the sequence moves on even where the block keeps going: a sound plays to its end, and a timer block counts down, while the next steps run. To give a block like that time, add a [wait](wait) of a set time after it, or have a trigger start the next sequence when the block reaches the state you need.

## Running steps together

An awaitable step can let the sequence go straight on, so the steps after it run while it's still going, like closing both airlock doors together. A wait further down then catches every awaitable step since the wait before it. If the sequence goes straight on from its last awaitable step, the run ends there, and the blocks finish on their own.

::: player
Two ways of showing a wait are being tried, and your world uses one of them. Both are kept here until one is chosen.
:::

### A wait under each step

Under each awaitable step hangs a `Wait for completion` line, which waits for that step and every awaitable step before it since the last wait. Switch it to `Do not wait`, and the sequence goes straight on.

### A wait as a step of its own

Adding an awaitable step also adds an `await all pending tasks` step after it. Move the await further down, and the steps in between run while the block is still going; delete it, and the sequence goes straight on.

Rest the pointer on a wait to see which steps it waits for, or on an awaitable step to see which wait catches it.

## When a run goes wrong

A run **fails** when a step can't be done at all, for example because its block was lost. That installation switches off, the others keep running, and the [home screen](home) says which step failed and why. Put right what it names, and switch the installation on again. A lost block or an empty [slot](slot) is filled on the [installation panel](installation-panel).

A step **holds** when its block is stuck, like a rotor that is held or a vent with nowhere to put the air. It waits and says why, and carries on by itself once the way is clear. To give up instead, stop the run.

A run **ends** when something else takes over a block a step is waiting on, like somebody opening the inner door from the terminal while *Cycle Out* waits for it to close. The step shows a warning saying what happened, the installation stays on, and the next run starts afresh.

The [simple airlock](simple-airlock) builds a sequence from the start.
