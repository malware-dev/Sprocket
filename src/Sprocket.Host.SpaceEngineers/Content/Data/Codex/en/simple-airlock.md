# Simple airlock

This builds an airlock that cycles out when its button is pressed: the inner door closes, the room empties, and the outer door opens. It needs an inner door, an outer door, an air vent in the room between them and a button panel, all on one [construct](construct).

1. On the [home screen](home), press `+ logic system` and name it *Airlock*.
2. Under `Sequences`, press `+ sequence` and name it *Cycle Out*.
3. Press `+ step`, then `task`, [select](selector) the inner door, then `close`. `close` is awaitable, because the door takes time to shut, so the next step waits for it.
4. Add `depressurize` for the vent and `open` for the outer door the same way.
5. Go back. Under `Triggers`, press `+ trigger`, then `when`, select the button, and have it run *Cycle Out*.
6. Leave the screen, and switch the logic system on when asked.

Press the button to try it. While it runs, the sequence's row lights green, and the step being run is lit on the sequence's screen.

Back to [examples](examples).
