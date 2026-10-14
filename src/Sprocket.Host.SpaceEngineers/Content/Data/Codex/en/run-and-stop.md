# Run and stop

A [step](#step) can start or stop another ![](mark:sequence)&nbsp;[sequence](#sequence) of the same logic system, with `run` or `stop`. Both act on the ![](mark:installation)&nbsp;[installation](#installation) the step runs on, so the port airlock's sequences never start or stop the starboard airlock's.

## Running a sequence

A run step lets one sequence build on another: *Disembark* can run *Cycle Out*, then extend the ramp. The sequence waits for the one it runs to finish, as it does for any awaitable step, so the ramp starts extending once the outer door is open.

A sequence that is already running isn't started a second time. The run step waits for the run already going instead.

## Stopping a sequence

A stop step ends the other sequence's run at the step it has reached, the same way stopping it by hand does, and the sequence with the stop step carries on straight away. A *Lockdown* sequence can stop *Cycle In* and *Cycle Out*, then close both doors, wherever the airlock was in its cycle.
