# Sequence

A sequence is what a ![](mark:logic-system) [logic system](#logic-system) does: a list of [steps](#step), run from top to bottom. Each ![](mark:installation) [installation](#installation) runs it on its own blocks, so one sequence cycles every airlock on the ship.

Under `Sequences` on the logic system's screen, `+ sequence` adds one, and pressing a sequence's row opens it. The bin on its row deletes it at once, and undo brings it back.

## Starting it

A [trigger](#trigger) starts it, for example when the airlock's button is pressed. The play mark on its row runs it by hand, even while the logic system is switched off, on the installation selected under `Installation`. That is how to try a sequence while building it.

An installation runs a sequence once at a time. A trigger that fires while it is still running is skipped, and the sequence counts how often that happens.

While it runs, the play mark turns into a stop mark, which stops it. A stopped run starts nothing more. A door already moving finishes, and a vent goes on filling or emptying the room, while a piston or a rotor stops where it is.

Switching the logic system or the installation off stops its runs at the step they reached, and once it is switched on again the sequence reads `stopped at step 2 when switched off`. A run started by hand ignores the switch and carries on.

## Steps that take time

For most actions, the sequence tells the block and moves straight on to the next step, without waiting for the block to finish, such as switching a light on or playing a sound. A few take time to finish, such as a door closing or a vent emptying a room, and a new step that does one waits for it: the next step begins only once the block is done. So the vent starts once the inner door is shut, and the outer door opens once the room is empty.

In the [picker](#picking), each action carries a mark:

- ![](mark:action) **A bolt** marks an action the sequence starts without waiting for the block to complete it.
- ![](mark:awaited) **A clock** marks one that takes time to finish. The sequence can wait for it, or go straight on while it runs.

## Going straight on

A step that takes time can let the sequence go straight on, so the steps after it run while it is still going, such as closing both airlock doors together. One wait covers every step that takes time since the wait before it, so several steps can run together and one wait catches them all. Where the sequence goes straight on from its last step that takes time, the run is over at its last step, and what it started finishes on its own.

A world shows its waits in one of two ways:

- **`Switch on each step`**: under each step that takes time hangs a line, joined to it as an if's steps are. `Wait for completion` waits there for this step and every step above it since the last wait. Pressing it switches it to `Do not wait`, and the sequence goes straight on; pressing again waits.
- **`Wait as its own step`**: each wait is a line of its own, reading `await all pending tasks`, after the steps it waits for. It is moved, copied, paused and deleted like a step, and `+ step` offers `await all pending tasks` to add one. A new step that takes time comes with one after it.

Either way, resting the pointer on a wait lights its clock and those of the steps it waits for, and resting it on a step lights the step's clock and the wait that covers it. A pulsing line beside the line numbers joins the lit lines and runs on to lines scrolled out of view; a world can switch it off. A wait with no step above it that takes time is marked with a warning, since it has nothing to wait for.

## When a run fails

A run fails when one of its steps can't be done at all, for example because the block it acts on was lost. Its installation is switched off, the other installations keep running, and the installation's row on the [home screen](#home) names the step and what went wrong:

`Stopped at step 1 of Cycle Out: Airlock Inner Door was lost.`

Put right what it names, and switch the installation on again. A block that was lost, or a [slot](#slot) that was never filled, is filled on the installation's [installation panel](#installation-panel).

## When a step holds

A step holds when its block is stuck, such as a rotor that is held or a vent whose air has nowhere to go. The step waits and says why, for example `Airlock Vent cannot empty because there is nowhere to put the air.`

Clear the way, and it carries on by itself: free the rotor, or give the air somewhere to go, such as an oxygen tank with room in it. To give up on the run, stop it by hand.

## When something else takes over

A run ends at its step when something else takes a block over while the step waits on it, such as somebody opening the inner door from the terminal while *Cycle Out* waits for it to close. The installation stays switched on. The step's line shows an amber warning naming the block and what happened, until the sequence next runs:

`Airlock Inner Door stopped closing because something else opened it.`

Run the sequence again, and it starts afresh.

## Example: the airlock's Cycle Out

*Cycle Out* has three steps, each an action with a clock that the sequence waits for:

1. `close Airlock Inner Door` lasts until the inner door is shut.
2. `depressurize Airlock Vent` begins once the door is shut, and lasts until the room is empty.
3. `open Airlock Outer Door` begins once the room is empty, and the run ends when the door is open.

Press the airlock's button, and while it runs:

- On the home screen, the airlock's installation row lights green.
- On the logic system's screen, the row of *Cycle Out* lights green. While a door moves or the vent empties the room, it reads `running - waiting for this step`.
- On the screen of *Cycle Out* itself, the step being run is lit.

When the outer door is open, the run is over, and the row of *Cycle Out* reads `ready`.

Press the button again during the run, and the press is skipped. The screen of *Cycle Out* counts it, for example `skipped 1x while busy`. Stop the run by hand during step 2, and its screen reads `stopped at step 2 by hand`. Both doors stay shut.
