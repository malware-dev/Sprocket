# Run and stop

## Running a sequence

Choosing `run` under `+ step` adds a [step](#step) that starts another ![](mark:sequence) [sequence](#sequence) of the same logic system, chosen from a list of its sequences. The next step begins once that sequence has finished.

An ![](mark:installation) [installation](#installation) runs a sequence once at a time. Where the sequence is already running, the run step waits for that run to finish, and the sequence counts the start as skipped, as it does for a [trigger](#trigger): `skipped 1x while busy`.

A sequence that run steps start needs no trigger of its own.

A sequence that would start itself again, through the sequences it runs, is a problem: for example, *Disembark* runs *Cycle Out*, and *Cycle Out* runs *Disembark*. The run step shows `This runs a sequence that runs this one again. Remove one of the two.`, and the logic system runs only once one of the two run steps is removed.

## Stopping a sequence

Choosing `stop` under `+ step` adds a step that stops another sequence of the same logic system. It acts at once, and the next step begins straight away. The stopped sequence's screen names what stopped it, for example `stopped at step 2 by Lockdown`. A stop that finds the sequence idle leaves it as it is.

A stop leaves the blocks as a stop by hand does, which the sequence page explains under *Starting it*.

## Examples

### Lockdown

A *Lockdown* sequence stops the airlock wherever it is in its cycle, and shuts it:

1. Stop *Cycle In*.
2. Stop *Cycle Out*.
3. Close the inner door, with the clock.
4. Close the outer door, with the clock.

The two stops act at once, so the inner door starts closing straight away. Where *Cycle Out* was emptying the room, its screen reads `stopped at step 2 by Lockdown`.

### Disembark

A *Disembark* sequence runs *Cycle Out*, then extends the ramp. The ramp waits for the whole of *Cycle Out*: it starts extending once the outer door is open.
