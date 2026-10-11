# Sequence

A sequence is what a ![](mark:logic-system) [logic system](#logic-system) does: a list of [steps](#step), run from top to bottom. Each ![](mark:installation) [installation](#installation) runs it on its own blocks, so one sequence cycles every airlock on the ship.

Under `Sequences` on the logic system's screen, `+ sequence` adds one, and pressing a sequence's row opens it.

## Starting it

A [trigger](#trigger) starts it, for example when the airlock's button is pressed. The play mark on its row runs it by hand, even while the logic system is switched off, on the installation chosen under `Showing the slots filled by installation`. That is how to try a sequence while building it.

An installation runs a sequence once at a time. A trigger that fires while it is still running is skipped, and the sequence counts how often that happens.

While it runs, the play mark turns into a stop mark, which stops it. A stopped run starts nothing more. A door already moving finishes, and a vent goes on filling or emptying the room, while a piston or a rotor stops where it is.

## Steps that are waited for

For most actions, the sequence tells the block and moves straight on to the next step, without waiting for the block to finish, such as switching a light on or playing a sound. A few are waited for, such as a door closing or a vent emptying a room: the next step begins only once the block is done. So the vent starts once the inner door is shut, and the outer door opens once the room is empty.

In the [picker](#picking), each action carries a mark:

- ![](mark:action) **A bolt** marks an action the sequence starts without waiting for the block to complete it.
- ![](mark:awaited) **A clock** marks one the sequence waits for the block to complete.

## When a run fails

A run fails when one of its steps can't be done at all, for example because the block it acts on is gone. Its installation is switched off, the other installations keep running, and the installation's row on the [home screen](#home) names the step and what went wrong:

`Stopped at step 1 of Cycle Out: Airlock Inner Door was gone.`

Put right what it names, and switch the installation on again. A block that is gone, or a [slot](#slot) that was never filled, is filled on the installation's [installation panel](#installation-panel).

## When a step holds

A step holds when its block is stuck, such as a rotor that is held or a vent whose air has nowhere to go. The step waits and says why, for example `cannot empty - nowhere to put the air`.

Clear the way, and it carries on by itself: free the rotor, or give the air somewhere to go, such as an oxygen tank with room in it. To give up on the run, stop it by hand.

## Example: the airlock's Cycle Out

*Cycle Out* has three steps, each an action with a clock:

1. `close Airlock Inner Door` lasts until the inner door is shut.
2. `depressurize Airlock Vent` begins once the door is shut, and lasts until the room is empty.
3. `open Airlock Outer Door` begins once the room is empty, and the run ends when the door is open.

Press the airlock's button, and while it runs:

- On the home screen, the airlock's installation row lights green.
- On the logic system's screen, the row of *Cycle Out* lights green. While a door moves or the vent empties the room, it reads `running - waiting for this step`.
- On the screen of *Cycle Out* itself, the step being run is lit.

When the outer door is open, the run is over, and the row of *Cycle Out* reads `ready`.

Press the button again during the run, and the press is skipped. The screen of *Cycle Out* counts it, for example `skipped 1x while busy`. Stop the run by hand during step 2, and its screen reads `stopped at step 2 by hand`. Both doors stay shut.
