# Drill arm

This builds a sequence that pushes a drill arm out slowly with its drills running, and brings it back once it's done. It needs pistons in the arm and drills at the end of it, all on one [construct](construct).

1. On the [home screen](home), press `+ logic system` and name it *Drill arm*.
2. Under `Sequences`, press `+ sequence` and name it *Dig*.
3. Press `+ step`, then `task`, [select](selector) the drills, then `power`, and set it to on.
4. Add a task for the arm's pistons with `move to`, and give 5 for the position and 0.5 for the speed. Each piston moves out to 5 m, and the step lasts until the last of them is there.
5. Add another `move to` for the pistons, with 0 for the position and 2 for the speed, to bring the arm back in.
6. Add a task that sets the drills' `power` to off.
7. Leave the screen, and switch the logic system on when asked.

Run *Dig* by hand with ![](mark:play) to try it, or give it a [trigger](trigger), like a button on the arm's control panel.

Back to [examples](examples).
