# The runtime

How what a player built runs. Most of it is in `src/Sprocket.Host.SpaceEngineers/Framework/Running`.

## One update a frame

The game calls `SprocketSession` once a frame, after its simulation. It brings every construct's Sprocket up to date
with its blocks and toolbars, runs the rota, and lets any open editor show what changed. A world with nothing to run
returns at once. Where the world also loads Sprocket Automation (Bleeding Edge), the main copy does nothing at all.

**Sprocket keeps its own clock** (`Framework/Storage/Memory/Memories.cs`): the game's frame carried on from where the
world was saved, since the game's own counter restarts every session. A wait half done at a save finishes when it
should.

## The rota

One `Rota` serves the world (`Framework/Running/Rota.cs`). Anything that repeats joins it as a `Shift`:
`Every(shift, period)` for every tick, ten or hundred, `At(shift, tick)` for once (a wait is a deadline, never a
countdown), and `Leave(shift)`. A tick visits only what is due on it. Each shift takes the next of a hundred places, so
work built alike is spread across the ticks. The rota walks every tick from its last run to now, so a jump leaves no
deadline behind.

## Running a sequence

Each construct's `AutomationRunner` keeps a `SequenceRunner` for every sequence on every installation. A run's place,
what it waits for and since when, is a `RunMemory`, saved with the world. A run goes step by step in the same tick
until something holds it: a wait, an action not yet settled, or a sequence it awaits. A held run is looked at again at
a pace `Framework/Running/Hold.cs` picks from how long the wait should take.

**Mutual exclusion.** Sequences naming the same `exclusive` id run one at a time per installation: a start while
another of them runs is missed, like a start while the sequence itself runs, and remembers which one held it.

**Catch and finally.** A run that fails, through a fault or an abort as a warning or problem, goes on in its `catch`,
then its `finally`. Any other ending (done, stopped, switched off) goes to its `finally`. While a run is in either,
switching off doesn't stop it, and a stop ends it at once.

## Triggers

A **signal from a toolbar** is Sprocket's own terminal action (`Framework/Game/Wiring/SignalAction.cs`), written into
the slot of each button a program listens to. **Everything else** is watched by a `Poller` on the rota, as often as the
block's kind says the change can be noticed (`Framework/Game/Blocks/EntityKind.cs`): announced by the game, ruled out
by something the game announces, or looked at on a cadence.

## When something goes wrong

A fault records a `SequenceFault` naming the step and the problem. A problem switches off what it belongs to, the logic
system or the installation, recorded as a `Halt` (`Framework/Model/Halt.cs`). Nothing switches back on by itself:
starting a machine again is a person's call.

Each construct's runner keeps a change number, raised when something a screen shows changes. An open screen checks it
once each refresh, so an idle screen costs nothing.

## Saving and resuming

Every run's and every poller's place is written into the world's memory as it saves, and each takes up from there on
load. A run that was waiting rebuilds what it was waiting for from the steps since its last await, so a reload goes
unnoticed.

## In multiplayer

The server runs everything. A player's game edits a copy of the construct's program, on a rota that runs nothing, and
the server lends each construct to one editor at a time (`Framework/Network/Handover.cs`). A run or stop by hand
reaches the server as an order, and changes travel back to it.

## What the tests hold it to

The cost of watching is counted in calls into the game: `BudgetTests`, `RotaTests` and `FaultCostTests` in
`tests/Sprocket.Core.Tests`, and `LiveScreenTests` for the screens.
