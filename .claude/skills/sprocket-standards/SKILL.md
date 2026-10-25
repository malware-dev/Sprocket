---
name: sprocket-standards
description: What is true of code in the Sprocket repository and would be wrong anywhere else - its projects and where code goes, the C# 6 ceiling, building and deploying, the simulator and reference suite as guards, transcribed values, where views and styles go, drawing invalidation, the rota, how block changes are watched, and how Sprocket reports to a person. Load alongside csharp-standards for any code in this repository.
---

The general rules are in **csharp-standards**. These hold only here.

## Projects

- `src/Sprocket.Host.SpaceEngineers` is the mod: everything that runs in the game. One folder per
  capability. Files are globbed: a file in the folder is in the build.
- UI code lives in `Features/`. A sequence's model is in `Sequences/`; how a sentence is composed and
  drawn is in `Features/Sentences/`.
- The model and the runtime hold ids, never words. Every word a player reads comes from `Texts`, put
  there by the page that shows it.
- `src/Lumel` is the UI framework, a shared project compiled into the mod's assembly. It names nothing
  of Sprocket. It is a submodule of the private `malware-dev/Lumel` repository, which other mods build in too: a change
  to it is committed and pushed in the submodule, and Sprocket then records the submodule's new commit. A control, layout or drawing primitive with nothing of Sprocket in it goes in Lumel, with
  its colors and sizes as properties. `Features/Look` holds Sprocket's palette, sizes and styles.
- `src/Sprocket.Gravity` (a gravity field's size) is a shared project too, built into Sprocket and into the small mod
  that needs it.
- The network channel and its codec are the `Mal.MdkModMixin.Network` package, a mixin from the malforge feed whose
  source is built into Sprocket and into the small mods that send messages. It is shared with mods outside this
  repository, so a change to it is made in the package.
- `src/Sprocket.DoorAutoClose`, `src/Sprocket.GravityFit` and `src/Sprocket.MultiRename` are small mods of their
  own, split out of Sprocket, whose names do not say Sprocket. Each has its own session, its own texts and storage
  keys where it has any, and calls the game directly where Sprocket has machinery of its own.
- Lumel controls (anything deriving from `Element`) are never `sealed`. A user control derives from the
  layout it needs; there is no `UserControl` base.
- `src/Sprocket.Host.SpaceEngineers.Sim` is the harness: it references the mod and drives it over
  made-up blocks.
- `src/Sprocket.Localization.Generator` writes `Texts` from `MyTexts.resx` at build time and reports
  every text that cannot be said. Roslyn source generator, `netstandard2.0`.
- Test projects reference the mod, or the harness where they need a screen, and drive the editor
  through `Driving/Target`. The generator has a test project of its own.
- The game is reached through two interfaces only: `Lumel/Drawing/IScreenSpace` for drawing and
  `World/ISlots` for toolbars. The harness answers both. Everything else (mod storage, grid events,
  block interfaces) the harness uses for real through Castle proxies (`Fakes/Pretend.cs`).
- A game type is never shadowed: a shadowing declaration only wins inside its own assembly. What the
  harness must answer differently is an interface.
- A `.debug.cs` file is compiled locally, left out of the pack and ignored by the analyzers. It holds
  what only the harness needs.
- Nothing is `internal`. A type or member is public or private; analyzer STD001 reports anything else.

## Compiler ceiling

- The mod is C# 6: no nullable reference types, no implicit usings.
- MDK's analyzers are the authority on what the game loads. `MDK01` fails the build and names the
  member.
- Not available in the mod: pattern matching, tuples, `record`, `FormattableString` interpolation,
  default interface members.
- C# 6 has `=>` members, `?.`, `nameof` and string interpolation; csharp-standards' idiom applies.
- No LINQ that allocates in a per-frame or per-tick path.
- The harness and tests are `net48` and not held to the ceiling.
- The build runs after every file, not at the end.

## Building

```
dotnet test                                     # every test project
dotnet build src/Sprocket.Host.SpaceEngineers    # builds AND deploys the mod
dotnet run --project src/Sprocket.Host.SpaceEngineers.Sim
```

- `dotnet test` does not deploy. Nothing is ready to look at in the game until the mod's build has run.
- Saves, mods and logs are at `E:\Data\sesaves`, never under `AppData`.

## Guards

- The simulator draws the real screen code against a substitute of the game's graphics.

```
--shot <file.png> [at <w> <h>] <script>   render a script and write a picture
--dump                                     the current frame as readable text
--content                                  what the game's own assets say
--profile                                  save and load cost per program size
--runtime                                  the runtime's cost per tick, per block change and per save
--faces <folder> [zoom]                    the automation panel in each scene on every reference screen shape, a picture per page
--colors <folder> [zoom]                   the automation panel in a set of app colors, troubled and calm
--blueprint <scenario> [folder]            a game test scenario as a blueprint (test-scenarios skill)
--checks <scenario> [first]                its game-only checks, as Testing rows
```

- A script is screen actions in order: `press <text>`, `move x y`, `drag x y x y`, `scroll x y n`,
  `tick n`, `type "words"`, `clear`, `enter`, `escape`. A kept script presses by name, never by
  position. Shots default to 1920x1080.
- `--profile` is the parser's guard and is run in Release only. The column that matters is cost per
  statement across the headroom table; flat means linear. It runs whenever the language grows and
  before any claim that storage got faster.
- Where the simulator and the game disagree, the simulator is wrong. A measurement is checked against
  `--content` or the game before it is trusted.
- Every change to a screen, and every block given members, comes with presentation tests that press
  and read by name. A test about particular blocks uses its own rig with `Screen.On(rig)`. The harness
  uses stand-in font widths, so a test about fit picks words that fail under any font.
- The reference suite (`tests/Reference/suite.txt`) is shot before a cleanup and after each folder.
  A difference is a defect in the cleanup.

## Transcribed values

- The palette in `Features/Look/` and the font metrics in `Sim/Content/SeFontMetrics.cs` are
  transcribed from the game. Each cites its source. They are never adjusted.

## Drawing

- `Draw` runs every frame. Nothing measures text in the draw path. Nothing lays out unless something
  changed.
- A property that changes a size or position calls `Element.InvalidateMeasure` or
  `InvalidateArrange`. A color change calls neither.
- A change to a property setter checks which of the three it is.

## Views

- Every view, screen, control and style is built by the **lumel** skill's rules; load it for any UI code.
- Sprocket's styles, palette, sizes and symbols are in `Features/Look`: `Styles`, `Palette`, `TextSizes`, `Symbols`.
- Which look a thing wears.
  - A thing that can only be renamed is a name plate (`NamePlate`): its words with a pencil, renamed in place.
  - Everything else a person presses is a button, a part of a sentence included.

## The rota

- One `Rota` per world, shared by every Sprocket. Periods `Each`, `Ten`, `Hundred`; each shift gets
  its own place in the period.
- Repeating work joins the rota. Nothing keeps its own clock. `Every` puts a shift on a period, `At`
  gives a deadline, `Leave` takes it off. A wait is a deadline; `Hold` decides how often it looks.
- Elapsed time is `now - then` against the game's frame. Nothing counts its own wake-ups.

## Writing the block

- A construct's program and switches are written into its programmable block as they change: `Automation.Changed`
  marks the unit, and the roster writes it once the change is done and tells the clients. The game reads a block's
  storage whenever it writes the block out, for a blueprint, a copy or a save, on whichever machine is writing.
- The world save writes only what is still waiting. What the runs are doing goes into the carrier at the save.

## Watching blocks

- A block's members are actions, values, functions and signals. A value is a state; a function is a
  value with parameters; a signal is a change.
- A Sprocket value is declared on its kind with `EntityKind.WithValue(id, named, OwnValue)`. A signal
  watched for is declared after it with `WithChange(id, text, value, becomes)` and carries its state
  words (`Reading(state, unstate)`). A signal only a toolbar sends is an answered `EntityEvent`.
- A value is watched one of three ways, never a fourth:
  - `Announced()`: the game raises an event; the capability listens through `Capability.Listen`.
    Looked at the tick after, with a backstop every hundred ticks.
  - `RuledOutBy(reading)`: the game announces a reading that rules the change out. Looked for on its
    cadence only while a block is outside that reading.
  - `Looked(reason)`: the cadence through the rota, where neither event exists.
- An event first. A look only where the game has no event, and only while the change is possible.
- A lasting action whose block says when it gets there is declared with `LastingAnnounced()`: doors and pistons. A step
  waiting only on such blocks listens to them, with a backstop every hundred ticks; any other lasting action is looked
  at on the hold's pace.
- The game's terminal actions are never wrapped or replaced. What Sprocket asks of a block prompts a
  look by itself (`GridEntity.Invoke`).

## Reporting to a person

- Details of a fault are recorded when it is found and travel with what they explain, saved state
  included.
- Each kind of message has one surface. A new message is a case of one of these:
  - Something wrong with a sequence: `SequenceFault` + `SequenceProblem`, naming the step.
    `IsWarning` makes it amber. Worded in `SequenceText`.
  - What a sequence is doing: `SequenceStatus`.
  - A message about the screen: the banner's `Notice`, `NoticeLevel.Info` or `Problem`.
  - Why a logic system was switched off: `Halt`, worded by `SequenceText.Stopped`.
  - Knowledge about a kind of block: `Capability`. What a block offers: `Offer`. What a value asks
    for: `Field`.
- The wording of every text is the `writing` skill's.

## Documents

- `docs/Language.md` is the language spec. A change to a rule, keyword, construct or spelling updates
  it in the same change as the code. How it is written is in the `writing` skill (`language.md`).
- The language's slot is the model's `Part` (`Part`, `PartFor`, `FindPart`).
- `docs/` describes the repository as the code is. No planning: that is issues and the work ledger.
- A change to something a `docs/` page or `README.md` describes fixes the page in the same change.
- Agent instructions state their own facts and never lean on `docs/`, except the spec.
