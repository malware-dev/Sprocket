Sprocket has three settings.

| Setting | Values | Default |
|---|---|---|
| `fastestUpdate` | 1, 10 or 100 | 1 |
| `developerMode` | true or false | false |
| `debugLog` | true or false | false |

**fastestUpdate** is how often, in ticks, timers and block checks can repeat. Button presses, other game events and timed waits aren't affected. Any other number gets rounded to the nearest of 1, 10 or 100, with a warning in the game's log.

**developerMode** shows each automation's script as text you can edit and adds extra tools to the screens. It's for testing and making content, not normal play.

**debugLog** writes everything every automation does to the Space Engineers log. That can slow the game down, so only switch it on while you're tracking down a problem, then send the log along with your report. You get what started each run, each step as written in the script, what each block was told to do, which way each if went and what the blocks read, waits, faults with the step and block, skipped starts and why, and how each run ended or what stopped it. Every line starts with `[Sprocket]` and gives the tick, construct, logic system and installation, and sequence:

```
[Sprocket] tick 51234, "Mining Barge", "Airlock" on installation 1, sequence 1 "Cycle": step 3 failed: Unable, "Port Outer Door"
```

Every ten seconds it also writes how long Sprocket's own work took per tick, on average and at worst:

```
[Sprocket] last 600 ticks: 3.2 µs a tick on average, worst 41.0 µs, 12 constructs with automation
```

In multiplayer the automation runs on the server, so these lines are in the server's log, not the players'. When it's off it costs nothing you could measure.

## Changing them in game

If you're an admin with creative tools on, the home screen of the automation editor (the one you open from the programmable block) has an **Admin** button. It opens Admin settings, with two tabs: **This world** and **Every world**. Changes apply right away for everyone on the world and are saved.

## sprocket.ini

You can also set them in a file called `sprocket.ini`, under `[Sprocket]`:

```ini
[Sprocket]
fastestUpdate=10
debugLog=true
```

There are two of these, both on the machine that runs the world (the server, or your own PC in single player):

- **Every world:** `sprocket.ini` directly in the `Storage` folder of the Space Engineers data folder.
- **This world:** `sprocket.ini` in the mod's folder under `Storage` in the world's save folder.

The world's file wins. Anything it leaves out comes from the every-world file, and anything that one leaves out uses the default.

In multiplayer the server decides. Players' own `sprocket.ini` files aren't read, so everyone runs on the server's settings.
