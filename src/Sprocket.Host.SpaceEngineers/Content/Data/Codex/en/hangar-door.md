# Hangar door

This builds a hangar whose lights come on while its doors open or close, with one button to open it and another to close it. It needs hangar doors, lights, and a button panel with two buttons, all on one [construct](construct).

1. On the [home screen](home), press `+ logic system` and name it *Hangar*.
2. Under `Sequences`, press `+ sequence` and name it *Open*.
3. Press `+ step`, then `task`, [select](selector) the lights, then `power`, and set it to on.
4. Add a task for the hangar doors with `open`. It's awaitable, so the next step waits until the last door is open.
5. Add a task that sets the lights' `power` to off again.
6. Go back and make a second sequence, *Close*, the same way, with `close` for the doors.
7. Under `Triggers`, press `+ trigger`, then `when`, select the button panel, then `button is pressed`. Button 1 is chosen for you; have it run *Open*. Add a second trigger the same way. Button 1 is taken, so button 2 is chosen; have it run *Close*.
8. Leave the screen, and switch the logic system on when asked.

Press the first button to try it.

Back to [examples](examples).
