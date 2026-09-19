# Sprocket

Starting fresh, or on a different machine? Read [`HANDOVER.md`](HANDOVER.md) as well - it holds where
things stand, what comes next, and the things a clean checkout gets wrong. This file holds what the
project is, which changes slowly; that one holds the situation, which does not.

Automation for people who have never programmed, as a Space Engineers mod. The editor
runs in the game, as a screen the game owns.

## What problem this is actually solving

Removing syntax is not what makes programming approachable - Scratch removed syntax
and it is still hard. What remains is **mental simulation**: to know what a program
does, you have to run it in your head. That is the wall.

So Sprocket bets on something else: a machine's behavior should be something you *read
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
world, so it stops a player operating the door by hand too. Sprocket is an automation
system, not a collection of finished automations: anything expressible as trigger plus
steps does not get to be a primitive.

**Events and states are kept apart by construction.** An event is a moment and only
ever starts a sequence; a state is a situation and only ever gates or guards. People
reliably conflate "when the door opens" with "while the door is open", and the
resulting bug is invisible because the sentence still reads correctly.

**Authoring starts with the verb.** The front door is what the machine does, not a list
of blocks. An empty Sprocket asks *"what should set something off?"* and offers `When
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

Sprocket never counts ticks for itself - it is told the frame number. Space Engineers
only wakes a mod every 1, 10 or 100 frames, and the slower two do not land on exact
multiples, so a self-counted clock would drift with server load.

## Layout

```
build/     Where the game's own assemblies are found
src/
  Sprocket.Host.SpaceEngineers        The mod. All of it, in one project.
    Sequences/                       What a sequence is, and the sentence somebody reads and presses
    Editing/                         Navigation, pages, decisions, undo, what may go in a slot
    Picking/                         Choosing blocks and what happens to them
    Lists/                           Rows of things to choose or watch
    Runtime/                         Execution, waiting for what was started
    Storage/                         The script: the language, read and written
    World/                           The game's blocks, what they can do, and their toolbars
    Words/                           How a sentence is composed: word order, capitals, joining words
    Screen/                          The editor's surface, and the screen that draws it
    Display/                         The status panel: the ship at a glance, nothing to press
    Layout/                          Elements, containers, text - laying out, knowing nothing of Sprocket
    Drawing/                         What the screens draw through, and the game's backend
    Look/                            Palette, type scale, spacing, sizes, icons
    Controls/                        The pieces a screen is built from
    Input/                           Pointers, held keys, copied text, and the game's keyboard
    Terminal/                        The control that opens the editor, and the screen it opens
    Content/                         Materials, fonts, and the mod's own metadata
  Sprocket.Host.SpaceEngineers.Sim    The harness: the mod, run without the game
    Machine/                         The rig it runs on, as blocks the game would recognise
    Fakes/                           What the game declares and the harness has to answer for
    Content/                         The game's own fonts and block art, read off disk
    Simulation/                       A frame with nobody watching, and the scripts that drive one
tests/
  Sprocket.Core.Tests                 The model, the runtime, and the editor
  Sprocket.Host.SpaceEngineers.Presentation.Tests   The screens, driven and read through the harness
  Sprocket.Host.SpaceEngineers.Tests  What the mod says it does about each conformance point
  Reference/                         The screens held still, one frame per state, as committed text
```

One folder per capability, each holding the whole of it. A sequence's model, the slots a screen is
asked to show for it and the views that draw them all sit in `Sequences/`, so a change to how a
sentence works is a change in one place. `Editing/` decides where pressing something leads, `Words/`
decides what any of it says, and the folders that know nothing of Sprocket - `Layout/`, `Drawing/`,
`Look/` - sit beside the features that use them rather than under a layer of their own.

The one thing the folders no longer keep apart is words: what a screen is asked to show carries
structure and never English, which is stated on `Screen/EditorComponent` and proved by the paper
target in the tests.

## The Space Engineers constraint

SE does not load assemblies - it compiles a mod's `.cs` files in-game with its own Roslyn, under a
whitelist. So the mod project's sources *are* the mod, and the packer lays them out as loose files
for the game to compile. There is no copy step that can quietly fall behind, and nothing that can be
in the build without being in the folder.

The harness references that project and drives the mod over blocks it makes up, so what is seen
there is what ships rather than a preview of it - down to the sprites, because it runs the mod's own
screen code against its own screen space rather than drawing its own way. Drawing and toolbars are
the two things the game alone can do, so each is an interface the mod asks through and the harness
answers: `IScreenSpace` and `ISlots`.

The mod also ships **content**: a symbol font for the things a screen needs and Space
Engineers has no artwork for - undo, redo, delete. It is drawn rather than borrowed, and it
is built by `art/symbols` rather than committed by hand. It is meant to become a mod of its
own that any display mod can depend on, so nothing about it is named for Sprocket.

That constraint fails silently, so it is enforced at build time instead:

- `LangVersion 6` pins the language to SE's assumed ceiling. It is a one-line change in the
  mod's own project once the real ceiling has been verified in-game.
- MDK's analysers fail the build on anything the game's whitelist rejects, naming the member.

**The harness and the tests are exempt** - they are never compiled by the game, and they use
whatever C# they like.

## Running it

```
dotnet test                                          # 236 tests
dotnet build src/Sprocket.Host.SpaceEngineers         # builds AND deploys the mod
dotnet run --project src/Sprocket.Host.SpaceEngineers.Sim   # the screens, without the game
```

In the game, a programmable block's terminal grows a **Sprocket** button, which opens the
editor as a screen the game owns.

The simulator opens on an empty Sprocket asking what should set something off. The
simulated ship has a hangar door, an airlock, lights, a sensor, a button and a
hydrogen tank; the strip along the bottom pokes the world so you can watch sequences
react. Doors travel and vents cycle, because instant entities hide every problem worth
finding.

### The simulator

`Sprocket.Host.SpaceEngineers.Sim` runs the mod without launching the game: the same sources, the
same screens, over blocks it makes up. What is seen there is what ships. Coordinates are UI units
rather than window pixels; `--dump` prints both, so a point can be converted.

```
Sim.exe                    # the window
Sim.exe --content          # which machine, is the game here, what is cached
Sim.exe --extract          # decode font, cursors and block art into the cache
Sim.exe --dump             # the frame as a draw list
Sim.exe --shot out.png 657 311 tick 40 drag 100 200 100 400
Sim.exe --shot out.png at 2560 1440 657 311     # at a resolution somebody actually has
```

`--shot` takes a script: a bare pair of numbers is a press, `tick n` advances the ship's
clock, `drag x y x y` picks something up and puts it down, `scroll x y n` turns the wheel
over a point. Order matters, and it is drawn between every step, because nothing knows
where it is until it has been arranged.

The symbol font is content rather than code, and is built rather than committed by hand.
`build_font.py` holds the shapes as coordinates, so the whole set re-cuts at a different
weight by changing one number.

```
python art/symbols/build_font.py                          # glyphs, page and metrics
art/symbols/FontTool/... FontTool.exe page.png page.dds   # what the game will load
```

`FontTool` exists because the game will not load anything else: BC7_UNORM_SRGB, a full mip
chain, premultiplied alpha. Both halves are invisible until they are wrong - straight alpha
looks right in every viewer and only goes dirty once the game blends it, and a page without
mips draws a ring as a dotted circle.

### Things that will bite

- **Every build of the mod deploys it**, Debug included. MDK2 packs it to wherever the local
  MDK settings point rather than to the default mods folder, and the build output says where it
  went. `mdk.local.ini` is per-machine and gitignored.
- **The running simulator locks the mod's assembly.** Close it before rebuilding.
- **`%LOCALAPPDATA%\Sprocket\SimContent` is not in the repository**, because it is Keen's
  content and not ours to redistribute. Run `--extract` once. Without it the simulator still
  runs and fails loudly: block artwork becomes magenta crossed boxes and text falls back to a
  desktop face whose widths run 9 to 17 percent narrow, so any layout judgement made in that
  state is made against the wrong numbers. Check `--content` before trusting a screenshot.
- **Tick once before believing a screen.** Core re-reads a screen's title, subtitle and name
  on the tick and nowhere else, so a headless script that presses Enter and looks straight
  away sees the old name and reads exactly like a silent failure.
- **Git identity is repo-local.** `gh` defaults to a different account; use
  `GH_TOKEN=$(gh auth token -u malware-dev)` for API calls rather than switching accounts.
- **No em dashes anywhere.** Ordinary hyphens.

## Testing it on people

Three tasks, chosen because they are things SE players actually want:

1. Make the hangar door open when a ship approaches.
2. Build a working airlock.
3. Flash the lights when hydrogen runs low.

Watch for where they stall. The open question is whether people can build something
that works and tell what it is doing - and the thing most likely to be missing is an
answer to *"why didn't it happen?"*, which is deliberately not built yet.

## The documents

| | |
|---|---|
| `docs/UserStories.md` | **Public.** Twenty-three wants in players' own words. |
| `docs/StoryNotes.md` | Our judgement on each story. |
| `docs/Requirements.md` | What Sprocket must do, derived one story at a time. |
| `docs/Design.md` | The model, what is excluded, and the open decisions. |
| `docs/Editor.md` | The interface: what each screen is and what core states about it. |
| `docs/Language.md` | The script language as it is right now, with an example of each construct. |
| `docs/Wording.md` | Words on screen that read wrong, gathered for one pass over them. |
| `docs/ApiUnknowns.md` | What we still do not know about the game. |
| `docs/Catalogue.md` | What the game offers a mod, and what Sprocket needs to use it. |

Nothing from the internal ones belongs in the public one.
