# The runtime

How what a player built runs: one update a frame, the rota that decides what is due, sequences advancing step by
step, and triggers firing. Most of this is in `src/Sprocket.Host.SpaceEngineers/Runtime`.

## One update a frame

The game calls `SprocketSession` once a frame, after its simulation (`SprocketSession.cs`). Each frame it brings
every construct's Sprocket up to date with its blocks and toolbars, runs the rota, and then lets any open editor show
what changed. A world with nothing to run returns at once.

**Sprocket keeps a clock of its own** (`Memory/Memories.cs`). The game's frame counter starts from nought with every
session, so a tick remembered across a save would mean a different moment after loading. Sprocket's clock is the
game's frame carried on from where the world was saved, so a wait half done when the world was saved finishes when it
should.

## The rota

One `Rota` serves the whole world (`Runtime/Rota.cs`), shared by every Sprocket in it. Anything that repeats joins it
as a `Shift`, rather than keeping a clock of its own:

- `Every(shift, period)` makes it due once each period: every tick, every ten, or every hundred.
- `At(shift, tick)` makes it due once, on that tick. A wait is a deadline, never a countdown.
- `Leave(shift)` takes it off.

A tick visits only what is due on it, so a piece of work with nothing going on costs nothing until its turn. Each
shift that joins is given the next of a hundred places, and is due on the ticks matching its place, so work built
alike is spread across the ticks rather than landing on one.

The rota walks every tick number from the last it ran up to the current one, so a frame counter that jumps leaves no
deadline behind.

## Running a sequence

The `AutomationRunner` of each construct (`Runtime/AutomationRunner.cs`) keeps a `SequenceRunner` for every sequence
on every installation, so each installation's runs are its own. A run's state, which step it is on, what it waits
for and since when, is held in a `RunMemory` and saved with the world.

A run advances step by step in the same tick until something holds it: a wait, an action that has yet to settle, or
another sequence it awaits. A run that holds is looked at again at a pace `Runtime/Hold.cs` picks from how long the
wait is expected to take.

## Triggers

A **signal from a toolbar** is Sprocket's own terminal action (`Features/Terminal/SignalAction.cs`), which Sprocket
writes into the slot of each button its program listens to.

**Everything else a trigger watches** is watched by a `Poller` (`Runtime/Poller.cs`) on the rota. How often it looks
depends on how the block tells of the change, declared with each value and signal of a kind of block
(`World/EntityKind.cs`): announced by the game, ruled out by a reading the game announces, or looked for on a cadence.

## When something goes wrong

A run that cannot go on fails, and switches off what it belongs to, the whole logic system or only the installation it
ran on, recording why as a `Halt` (`Sequences/Halt.cs`). Nothing switches it back on by itself: starting a machine
again is a person's decision.

What is wrong with a sequence is a `SequenceFault` with a `SequenceProblem` naming the step (`Runtime/SequenceFault.cs`).
What a sequence is doing when nothing is wrong is its `SequenceStatus`.

Each construct's runner keeps a change number, raised whenever something a screen shows changes. An open screen
compares that number once each refresh and recounts only when it moved, so an idle screen costs nothing.

## Saving and resuming

What a running construct has reached, every run's place and every poller's, is written into the world's own file as
it saves (`Memory/`), and read back as it loads. Each runner and poller takes its state up from there and rejoins the
rota where it was. A run that was waiting rebuilds what it was waiting for from the steps since its last await, so a
reload passes unnoticed.

## In multiplayer

The server runs everything. A player's game edits a copy of the construct's program, on a rota of its own that runs
nothing, and the server lends each construct to one editor at a time (`Network/Handover.cs`). A sequence a player runs
or stops by hand reaches the server as an order, and the program the editor changes travels back to it.

## What the tests hold it to

The cost of watching is counted in calls into the game, and `BudgetTests`, `RotaTests` and `FaultCostTests` in
`tests/Sprocket.Core.Tests` hold it there. `LiveScreenTests` holds the screens to the same.
