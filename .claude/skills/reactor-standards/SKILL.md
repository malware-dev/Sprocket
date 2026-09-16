---
name: reactor-standards
description: How the Reactor repository is built, constrained and verified - shared mixin projects and their .projitems, the Space Engineers compiler ceiling and whitelist, the simulator as the behavioural guard, and the values that are transcriptions rather than choices. Use alongside the general csharp-standards skill whenever writing, reviewing or cleaning up code in this repository.
---

The general rules live in the user-level **csharp-standards** skill. This one holds only what is true
of Reactor and would be wrong anywhere else.

## How this repository is put together

Four **shared projects** hold the code that has to run in the game: `Phosphor.Mixin`,
`Reactor.Core.Mixin`, `Reactor.Screens.Mixin`, `Reactor.Machine.Mixin`. They are `.shproj`/`.projitems`
pairs, not assemblies, because Space Engineers does not load assemblies - it compiles a mod's loose
`.cs` files in-game with its own bundled Roslyn.

**Every file in a shared project is listed by hand in its `.projitems`.** Adding a file without adding
its `<Compile Include>` line leaves it out of the build; removing a line while leaving the file drops
working code silently. Any pass that creates, moves, renames or deletes a file in a mixin edits the
`.projitems` in the same breath, and the entry's path has to match the file's new folder.

## The compiler ceiling

`build/Reactor.SharedSources.props` pins mixin code to **C# 6**, no nullable reference types, no
implicit usings. `build/BannedSymbols.txt` bans what the game's whitelist rejects: reflection,
`System.Type`, `System.IO`, threading, `Environment`, `Activator`. Both files explain themselves and
are the authority; do not restate their contents elsewhere.

The practical consequence for a cleanup: **a refactor that would be natural in modern C# is often
illegal here.** No expression-bodied members beyond what C# 6 allows, no pattern matching, no tuples,
no `record`, no string interpolation into `FormattableString`, no `nameof` beyond C# 6's, no LINQ
where it allocates in a per-frame path. When in doubt the build says so, which is why the build runs
after every file rather than at the end.

`Reactor.Core` is an ordinary assembly consumed by the tests. It is not the same thing as
`Reactor.Core.Mixin` and the two are not interchangeable.

## Building and running

```
dotnet test                                     # all three test projects
dotnet build src/Reactor.Host.SpaceEngineers    # builds AND DEPLOYS the mod
dotnet run --project src/Reactor.Host.SpaceEngineers.Sim
```

**`dotnet test` does not deploy the mod.** Nothing changes in the game until
`dotnet build src/Reactor.Host.SpaceEngineers` has run. Never tell Mal something is ready to look at
without having run it.

Space Engineers saves, mods and logs are at `E:\Data\sesaves`, never under `AppData`.

## The simulator is the guard

`Reactor.Host.SpaceEngineers.Sim` draws the real screen code against a substitute of the game's
graphics, and it is the only way to prove a cleanup changed nothing visible.

```
--shot <file.png> [at <w> <h>] <script>   render a script and write a picture
--dump                                     the current frame as readable text
--content                                  what the game's own assets say
--profile                                  how long saving and loading a program takes, worst cases included
```

**`--profile` is the parser's guard, and it only means anything in Release** (`-c Release`). Loading a
program lands inside a world load, once per Reactor on every grid on a server, so the column that
matters is cost per statement across the headroom table: flat means linear. Re-run it whenever the
language grows - expressions and branching will multiply program sizes - and before believing any claim
that a change to storage made it faster.

A script is a list of things done to the screen in order: a bare `x y` pair is a press, and then
`move x y`, `drag x y x y`, `scroll x y n`, `tick n`, `type "words"`, `clear`, `enter`, `escape`.
Shots default to 1920x1080 because that is the size a letter actually lands at for a player.

**The simulator has been wrong before, and when it disagrees with the game the simulator is what is
broken.** It has confirmed three wrong answers by returning line height where the game returns cell
height, and by lacking `==` on its substitute `VRageMath` types. Before trusting it on a measurement,
check it against `--content` or the game itself.

**Reference suite.** The cleanup's behavioural proof is a checked-in list of named scripts, one per
screen and state worth holding still, shot before the pass starts and re-shot after each folder. A
pixel difference is a defect in the cleanup, not a thing to explain. These stay in the repository
afterwards as the UI regression tests.

## Values that are transcriptions, not choices

The palette in `Reactor.Screens.Mixin` and the font metrics in `Sim/Content/SeFontMetrics.cs` are
taken from the game's own textures and constants. **They are never adjusted by eye and never
"improved".** Each carries a citation saying where it came from; that citation is the reason it may
not be touched, and it is why the values need no further comment.

`SeFontMetrics.GlyphBaselineInCell` is a known open number, recorded in `docs/`. Leave it alone.

## Drawing runs sixty times a second

`Draw` is called every frame; a screen is used perhaps twice a minute. **Nothing measures text in the
draw path, and nothing lays out unless something changed.** Layout is invalidation-driven:
`Element.InvalidateMeasure` and `InvalidateArrange` propagate upward, and a property that changes a
size or a position must call one of them. Colour changes call neither, because the views set colours
on every element on every tick.

A cleanup that touches a property setter checks which of the three it is. Getting this wrong breaks
scrolling in a way every test still passes, which has happened.

## Removing the Avalonia host

`Reactor.Host.Avalonia` is the prototype the screens were designed against and is being deleted. Its
loss must not take the tests with it, so it needs a replacement paper target in the test project
first.

**This does not mean removing Avalonia from the simulator.** The simulator uses Avalonia to rasterise
and to measure text, and it keeps it. Only the prototype host goes.

## The language's spec is `docs/Language.md`

**It holds the current truth of the script language and nothing else.** What the language is right
now: its shape, its values, its constructs, with an example of each. No history, no rationale, no
alternatives that were considered, no roadmap, and nothing about what is undecided.

**Any change to a rule, keyword, construct or spelling updates it in the same change as the code.**
The spec is never left trailing, and the old wording is deleted rather than kept beside the new.
`docs/Script.md` is the history of how the language got there and is not maintained as truth.

Six rules, each of which the document has already been wrong about:

- **Only the language.** How Reactor or an editor happens to behave is not a rule of the language.
  "Reactor writes the id" and "a name can be changed, an id cannot" were both wrong for that reason:
  the second is true of a visual editor and says nothing about a script, where renaming is merely
  likelier than renumbering.
- **A limit of the parser is not a rule.** Where something is unsupported because nothing needs it
  yet - a list inside a list - it is not mentioned at all. Writing it down as a rule only obstructs
  the build that adds it.
- **Say what the language is, not what it is not.** A sentence stating an absence earns its place
  only where a reader would otherwise assume the opposite: somebody arriving from another language
  assumes statements continue across lines, so "no continuations" is worth saying, while "no comma
  between a statement's parts" is not, because none is ever shown.
- **A recommendation for whatever writes scripts belongs beside the thing it is about**, not in a
  section of its own. Where `last sequence` is defined is where somebody looks to find out when it is
  worth writing.
- **A recommendation is offered, not demanded.** It reads as what the choice buys, never as an
  obligation.
- **Every sentence carries something.** A lead-in explaining the difference between a person and a
  tool, or a heading restated as prose, is cut rather than reworded.

## Documentation

`docs/` holds only what is not code yet: open questions, decisions taken but not implemented, and
numbers pinned to external evidence. Rationale for things already built is not migrated here during a
cleanup, it is deleted. Anything renamed by a pass gets its mention in `docs/` and `README.md` fixed
in the same change.
