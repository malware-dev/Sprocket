# Reactor

Automation for people who have never programmed. The end goal is a Space Engineers
mod; the Avalonia app in this repo is the prototype used to find out whether the idea
works before any of it goes near the game.

## What problem this is actually solving

Removing syntax is not what makes programming approachable - Scratch removed syntax
and it is still hard. What remains is **mental simulation**: to know what a program
does, you have to run it in your head. That is the wall.

So Reactor bets on something else: a machine's behavior should be something you *read
like a dashboard*, not something you *execute in your head*.

## The language

The whole of it:

- **Triggers** - `When something happens`, `Every so often`, or called by name.
- **Steps** - do this to these blocks, in order.
- **Optional, and rare** - `only if …` to guard a step, `once …` to wait on something
  the sequence did not cause, and a flag to carry on without waiting.
- **Names** - optional. A name makes a list legible and lets more than one trigger
  start the same sequence. It is never required to get anything done.

An airlock, in full:

```
When the Airlock Button is pressed
  1. close the inner door
  2. lock the inner door
  3. depressurize
  4. unlock the outer door
  5. open the outer door
```

That is a transcript of what a player says out loud, and it is the entire program.

### Why there is nothing else in it

**Steps wait for their own action to finish.** "Close the inner door" is done when the
door is *shut*. A numbered list already means "and then" to anyone who has followed a
recipe, so saying `once the inner door is shut` on every line writes down what the
reader already assumed - and pushes people towards guessing a duration instead, which
is how an airlock vents a ship the day a door sticks.

**That also removes conditionals.** A step that is already true finishes instantly, so
"make sure the outer door is shut" and "close the outer door" are the same sentence.
Nobody writes `if it is open, close it`.

**There is no interlock feature.** "Both doors must never be open" is two ordinary
sequences - lock the other door when one opens, unlock when it shuts - and that is
*stronger* than a built-in rule would be, because locking is a real action on the real
world, so it stops a player operating the door by hand too. Reactor is an automation
system, not a collection of finished automations: anything expressible as trigger plus
steps does not get to be a primitive.

**Events and states are kept apart by construction.** An event is a moment and only
ever starts a sequence; a state is a situation and only ever gates or guards. People
reliably conflate "when the door opens" with "while the door is open", and the
resulting bug is invisible because the sentence still reads correctly.

**Authoring starts with the verb.** The front door is what the machine does, not a list
of blocks. An empty Reactor asks *"what should set something off?"* and offers `When
something happens` / `Every so often`; answering leads straight into the next question
rather than parking you on a half-empty form. The ship browser is still there, as a
way to look things up rather than as the only way in.

**Going back always means going back.** A run of questions has two directions and they
are kept separate: `Back` retreats one question, all the way out to where you started,
and a distinct `Next` / `Done` moves you on. Overloading back with "finished" breaks
the one navigation idea everybody already has. A new sequence is not created until the
questions are answered, so backing out leaves nothing behind.

### Ticks

Time is measured in **ticks** - one step of the game's simulation. It is the only
honest unit: simulation speed sags under load, so anything promising seconds would
drift exactly when the user's machine matters most.

The word is never explained in the UI. A repeating sequence shows an indicator
pulsing at its true rate, so you learn what a tick is by watching one.

Reactor never counts ticks for itself - it is told the frame number. Space Engineers
only wakes a mod every 1, 10 or 100 frames, and the slower two do not land on exact
multiples, so a self-counted clock would drift with server load.

## Layout

```
build/     Shared build settings and the Space Engineers whitelist gate
src/
  Reactor.Core.Mixin      Everything that matters. No pixels, no platform.
    Sequences/            The model: sequences, steps, gates, triggers
    Runtime/              Execution, completion waiting, guards
    World/                What a host must supply: entities, kinds, signals
    Editing/              The editor itself - navigation, pages, wording
    Ui/                   The contract a target renders. Start here; it is small.
  Reactor.Core            The same sources as an assembly, for the hosts that load one
  Reactor.Machine.Mixin   The fake ship (Space Engineers supplies the real one)
  Phosphor.Mixin          Drawing and layout for the game's screen. Knows nothing of Reactor.
  Reactor.Screens.Mixin   One view per component, for Space Engineers - what the mod ships
  Reactor.Host.Avalonia   The prototype, and the template the screens above duplicate
    Rendering/            One renderer per component - what a target reimplements
  Reactor.Host.SpaceEngineers      The mod: the same sources, compiled in-game
  Reactor.Host.SpaceEngineers.Sim  Those screens, run without the game
tests/
  Reactor.Core.Tests      The model, the runtime, and the editor
  Reactor.Host.Tests      That every component has a renderer and lays out
  Reactor.Host.SpaceEngineers.Tests   The same, for the in-game screens
```

The split is the point: Core holds the model, the runtime **and the whole editor**. A
target implements a renderer per component and nothing else - it decides nothing about
what a sequence says, what can go in a slot, or where pressing something leads.

## The Space Engineers constraint

SE does not load assemblies - it compiles a mod's `.cs` files in-game with its own
Roslyn, under a whitelist. The core is therefore consumed **two ways**: as a normal
assembly reference here, and as loose source files there.

That is why the sources live in `Reactor.Core.Mixin`, an MDK2 mixin - a shared project
rather than a library. `Reactor.Core` imports it to produce the assembly the Avalonia
host and the tests reference; `Reactor.Host.SpaceEngineers` imports the same sources so
the packer lays them out as loose files for SE to compile. One set of files, and no
copy step that can quietly fall behind.

The same reasoning made three more: `Phosphor.Mixin` for the drawing and layout,
`Reactor.Screens.Mixin` for the views the mod ships, and `Reactor.Machine.Mixin` for the
invented ship both hosts drive. The simulator imports the first two, so what is seen
there is what ships rather than a preview of it.

The mod also ships **content**: a symbol font for the things a screen needs and Space
Engineers has no artwork for - undo, redo, delete. It is drawn rather than borrowed, and it
is built by `art/symbols` rather than committed by hand. It is meant to become a mod of its
own that any display mod can depend on, so nothing about it is named for Reactor.

That constraint fails silently, so it is enforced by the compiler instead:

- `netstandard2.0` shrinks the API surface to roughly .NET Framework shape.
- `LangVersion 6` pins the language to SE's assumed ceiling.
- `build/BannedSymbols.txt` blocks what survives both: reflection, IO, threading,
  LINQ, wall-clock time. A banned call is a build error, not a warning.

`LangVersion` is a one-line change in `build/Reactor.SharedSources.props` once SE's
real ceiling has been verified in-game. **Host code is exempt** - the Avalonia project
uses whatever C# it likes.

## Running it

```
dotnet test Reactor.slnx
dotnet run --project src/Reactor.Host.Avalonia
```

The prototype opens on an empty Reactor asking what should set something off. The
simulated ship has a hangar door, an airlock, lights, a sensor, a button and a
hydrogen tank; the strip along the bottom pokes the world so you can watch sequences
react. Doors travel and vents cycle, because instant entities hide every problem worth
finding.

## Testing it on people

Three tasks, chosen because they are things SE players actually want:

1. Make the hangar door open when a ship approaches.
2. Build a working airlock.
3. Flash the lights when hydrogen runs low.

Watch for where they stall. The open question is whether people can build something
that works and tell what it is doing - and the thing most likely to be missing is an
answer to *"why didn't it happen?"*, which is deliberately not built yet.

## Where to pick it up

See [HANDOVER.md](HANDOVER.md) for what is built, what is not, and the open questions.
