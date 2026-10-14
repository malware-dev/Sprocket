# The runtime

How what a player built runs: one update a frame, the rota that decides what is due, sequences advancing step by
step, triggers firing, and what reaches the screens. The parts being run are set out in [The model](Model.md). Most
of this is in `src/Sprocket.Host.SpaceEngineers/Runtime`.

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

- `Every(shift, period)` makes it due once each period: every tick, every ten, or every hundred, the same rates the
  game offers a mod.
- `At(shift, tick)` makes it due once, on that tick. A wait is a deadline, never a countdown.
- `Leave(shift)` takes it off.

A tick visits only what is due on it, so a piece of work with nothing going on costs nothing until its turn. Each
shift that joins is given the next of a hundred places, and is due on the ticks matching its place, so work built
alike is spread across the ticks rather than landing on one.

The rota walks every tick number from the last it ran up to the current one, so a frame counter that jumps leaves no
deadline behind. A gap of more than a hundred ticks is walked as a hundred, since that visits every list once.

## Running a sequence

The `AutomationRunner` of each construct (`Runtime/AutomationRunner.cs`) keeps a `SequenceRunner` for every sequence
on every installation, so each installation's runs are its own. A run's state, which step it is on, what it waits
for and since when, is held in a `RunMemory` and saved with the world.

A run advances step by step in the same tick until something holds it:

| Step | At run time |
|---|---|
| `Do`, `Set` | Acts on the blocks. A block that is missing, adrift or switched off fails the step. An action that takes time is noted as outstanding, and the run moves on |
| `Wait` | Pins a deadline on Sprocket's clock, and the run is due again then |
| `Await` | Holds until every outstanding action has settled and every sequence started above it has finished |
| `Run`, `Stop` | Starts or stops another sequence on the same installation |
| `If` and its branches | Reads the blocks once, as the run reaches the test, and takes the first branch whose test holds |

A run that holds is looked at again at a pace `Runtime/Hold.cs` picks from how long the wait is expected to take:
the slowest period that still fits twice into the time left.

## Triggers

A **signal from a toolbar** is Sprocket's own terminal action, `Sprocket_Trigger` (`Features/Terminal/SignalAction.cs`).
A player puts it on a button or a toolbar slot like any other action, and pressing it runs or stops the sequence its
trigger names.

**Everything else a trigger watches**, a change a block announces, blocks reaching a state, or a set time passing, is
watched by a `Poller` (`Runtime/Poller.cs`) on the rota. How often it looks depends on how the block tells of the
change, declared with each value and signal of a kind of block (`World/EntityKind.cs`):

- **Announced:** the game raises an event when the value may have changed, so the poller looks on the tick after, with
  a look every hundred ticks as a backstop.
- **Ruled out:** the game announces nothing for the change itself, but does announce a reading that rules it out, so
  the poller looks only while that reading allows the change. A door can only start to close between starting to open
  and having closed.
- **Looked for:** the game announces nothing, so the poller looks on a cadence: every ten ticks, unless the trigger
  sets a rate of its own.

A trigger that fires while its sequence is already running on that installation is skipped, and the skip counted. A
call refused because the logic system is off or unpowered is counted too. Both counts show beside the sequence.

## When something goes wrong

A run that cannot go on **fails**: a block it needs is lost, or one of its steps cannot be done. A failure switches off
what the run belongs to, the whole logic system or only the installation it ran on, and records why as a `Halt`
(`Sequences/Halt.cs`). So the port airlock missing a door leaves the starboard one working. Nothing switches it back on
by itself: starting a machine again is a person's decision.

A run whose block is stuck, or switched off while waited on, **holds** and says why, and carries on once the way is
clear. A run **ends** where something else takes over a block it waits on, such as a person opening the inner door from the
terminal while the run waits for it to close. The run ends with a warning, and the next run starts afresh.

What is wrong with a sequence is a `SequenceFault` with a `SequenceProblem` naming the step (`Runtime/SequenceFault.cs`).
A fault is red and counted as broken; one marked as a warning is amber and counted as a remark. What a sequence is doing
when nothing is wrong is its `SequenceStatus`: off, never run, waiting, stopped, running, or running and waiting.

## What reaches the screens

Each construct's runner keeps a change number, raised whenever something a screen shows changes: a fault found or
cleared, a run starting, holding or ending. An open screen compares that number once each refresh and recounts only
when it moved, so an idle screen costs nothing, and a fault shows on the tick it is found.

The home screen, a logic system's screen and its sequences' screens show the faults, statuses and halts. The banner
across the editor is for the editor itself: a program that will not read, another player editing the construct, or a
construct that changed hands. An LCD can run Sprocket's status app (`LcdApps/StatusPanel`), which shows every logic
system's state and works its facts out every hundred ticks.

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

The cost of watching is counted in calls into the game, and tests hold it there (`tests/Sprocket.Core.Tests`):

- `BudgetTests`: nothing to watch asks the game nothing; a door that announces its changes costs one call a hundred
  ticks; a vent, which announces nothing, four calls every ten ticks; a `when` on one of Sprocket's own values, none.
- `RotaTests`: an idle world costs only its share of the turn.
- `FaultCostTests`: an idle program, and one just edited, is checked once, and an idle editor looks at nothing.

`tests/Sprocket.Host.SpaceEngineers.Presentation.Tests/LiveScreenTests.cs` holds the screens to the same: a fault
shows on the tick it is found, an idle screen recounts nothing, and an open screen leaves the timing of every run as it is.
