---
name: sprocket-standards
description: How the Sprocket repository is built, constrained and verified - one mod project and the harness that drives it, the Space Engineers compiler ceiling and whitelist, the simulator as the behavioural guard, and the values that are transcriptions rather than choices. Use alongside the general csharp-standards skill whenever writing, reviewing or cleaning up code in this repository.
---

The general rules live in the user-level **csharp-standards** skill. This one holds only what is true
of Sprocket and would be wrong anywhere else.

## How this repository is put together

Four projects of code, and the first one is the mod:

- **`src/Sprocket.Host.SpaceEngineers`** is everything that runs in the game, in one tree, one folder
  per capability and each holding the whole of it. **Anything that belongs to the user interface lives
  within `Features/`, and code outside it reaches in there.** A sequence's model sits in `Sequences/`, and
  the sentence as drawn in `Features/Sentences/`, which also decides how a sentence is composed - not the
  same as holding the English, see below. Files are globbed, so a new file is in the build by being in
  the folder.

  **The model and the runtime hold ids and never words.** Every word a player reads comes from `Texts`,
  put there by the page that shows it.
- **`src/Lumel`** is the UI framework, a shared project the mod imports, so its files compile into the
  mod's own assembly. It knows nothing of Sprocket, so any other mod can import it as it is. Its files
  are globbed in the same way. **A piece of UI with nothing of Sprocket in it - a control, a layout, a
  drawing primitive - goes in Lumel, with its colors and sizes as properties; `Features/Look` holds
  Sprocket's own palette, sizes and styles, which it hands to them.**
- **`src/Sprocket.Host.SpaceEngineers.Sim`** is the harness. It references the mod and drives it over
  blocks it makes up, answering the game's own interfaces.
- **`src/Sprocket.Localization.Generator`** writes the mod's `Texts` class from its `MyTexts.resx` at
  build time, and reports every text that cannot be said. A Roslyn source generator, so `netstandard2.0`.

The mod's three test projects reference the mod, or the harness where they need a screen, and drive the
editor the way a target would through the harness's `Driving/Target`. The generator has a test project
of its own, which runs it as the compiler does and needs nothing of the game.

**Two things only the game can do, and each is an interface the mod asks through.**
`Lumel/Drawing/IScreenSpace` for drawing and `World/ISlots` for toolbars: the harness answers
both, which is how a frame is recorded rather than drawn and how a toolbar slot is read without one.
Everything else - mod storage, the grid's events, the block interfaces - the harness uses for real,
through Castle proxies over the game's own types. See `Fakes/Pretend.cs`.

**Shadowing a game type does not work across an assembly, so nothing tries.** A declaration that
replaces one of Keen's only wins inside the assembly making it, and the harness is not that
assembly. Anything the mod needs the harness to answer differently is an interface or it is nothing.

**A `.debug.cs` file is compiled locally, left out of what is packed and ignored by the analysers.**
That is the only way the mod grants its internals to the tests: the whitelist bans
`InternalsVisibleTo` outright, so a mod that shipped it would refuse to load.

## The compiler ceiling

The mod's own project pins it to **C# 6**, no nullable reference types, no implicit usings, and
says why.

**MDK's own analysers are the authority on what the game will load.** They fail the build with
`MDK01`, naming the member, and they know the real mod whitelist. There was a hand-written list
beside them for a while, stricter and guessed at, enforced on core alone by a project that has since
been merged away - it is gone, and it was wrong: it rejected `GetType().Name`, which a mod may
use.

The harness and the tests are `net48` - the same framework MDK spells `netframework48` - and are not
held to the ceiling: they are never compiled by the game.

The practical consequence, for the mod's own tree: **a refactor that would be natural in modern C#
is often illegal here.** No expression-bodied members beyond what C# 6 allows, no pattern matching, no tuples,
no `record`, no string interpolation into `FormattableString`, no `nameof` beyond C# 6's, no LINQ
where it allocates in a per-frame path. When in doubt the build says so, which is why the build runs
after every file rather than at the end.

## Building and running

```
dotnet test                                     # every test project
dotnet build src/Sprocket.Host.SpaceEngineers    # builds AND DEPLOYS the mod
dotnet run --project src/Sprocket.Host.SpaceEngineers.Sim
```

**`dotnet test` does not deploy the mod.** Nothing changes in the game until
`dotnet build src/Sprocket.Host.SpaceEngineers` has run. Never tell Mal something is ready to look at
without having run it.

Space Engineers saves, mods and logs are at `E:\Data\sesaves`, never under `AppData`.

## The simulator is the guard

`Sprocket.Host.SpaceEngineers.Sim` draws the real screen code against a substitute of the game's
graphics, and it is the only way to prove a cleanup changed nothing visible.

```
--shot <file.png> [at <w> <h>] <script>   render a script and write a picture
--dump                                     the current frame as readable text
--content                                  what the game's own assets say
--profile                                  how long saving and loading a program takes, worst cases included
```

**`--profile` is the parser's guard, and it only means anything in Release** (`-c Release`). Loading a
program lands inside a world load, once per Sprocket on every grid on a server, so the column that
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

The palette in `Features/Look/` and the font metrics in `Sim/Content/SeFontMetrics.cs` are
taken from the game's own textures and constants. **They are never adjusted by eye and never
"improved".** Each carries a citation saying where it came from; that citation is the reason it may
not be touched, and it is why the values need no further comment.

## Drawing runs sixty times a second

`Draw` is called every frame; a screen is used perhaps twice a minute. **Nothing measures text in the
draw path, and nothing lays out unless something changed.** Layout is invalidation-driven:
`Element.InvalidateMeasure` and `InvalidateArrange` propagate upward, and a property that changes a
size or a position must call one of them. Colour changes call neither, because the views set colours
on every element on every tick.

A cleanup that touches a property setter checks which of the three it is. Getting this wrong breaks
scrolling in a way every test still passes, which has happened.

## The runtime runs on the world's rota

One `Rota` per world, shared by every Sprocket on it. Its periods are the three the game wakes a mod
at - `Each`, `Ten`, `Hundred` - and a shift joining it is handed a place in that period in turn, so
two machines built the same afternoon do not land on the same tick. `Rota.Run` walks every tick
number from the last one it saw to now, so nothing is skipped when the game wakes it late.

**Repeating work joins the rota. It does not keep its own clock.** A `Shift` is a place, a callback
and the list it sits in; `Every` puts it on a period, `At` gives it a deadline, `Leave` takes it off.
A wait is a deadline rather than a countdown, and how often it looks is `Hold`'s to decide.

**Per tick is a judgement, not a ban.** What it costs, how often the answer actually changes, and who
suffers if it arrives a tick later. A reading behind a short-circuit is cheap, and the comment says
so; a reading on its own is not.

**Nothing counts its own wake-ups.** Elapsed time is `now - then` against the frame the game reports,
because the slower two periods are not guaranteed to land on exact multiples - when they fire depends
on what else is in the game's component list. A shift counting how often it was called is measuring
server load and calling it time.

## What Sprocket tells a person

**Every word on screen is clear, concise and unconfusing.** The fewest words that still say what
happened, why, and what to do. A sentence that is accurate but wordy, or says little, is a defect.

**An error names exactly what went wrong.** The step, the block, and the block's own words where it
gave any: "Stopped at step 3: Airlock 1 Outer Door was gone." Never "a block" where the block is
known.

**Details are recorded when the fault is found, and kept.** That moment is when Sprocket knows them
exactly; later the cause may be mended or the block gone, and nothing can be worked out again. What
is recorded travels with whatever it explains, saved state included.

**A fallback stays, short and plain.** For a cause that really is not known: "Stopped for an unknown
reason." Removing a message is never the fix for a bad one.

## The language's spec is `docs/Language.md`

**It holds the current truth of the script language and nothing else.** What the language is right
now: its shape, its values, its constructs, with an example of each. No history, no rationale, no
alternatives that were considered, no roadmap, and nothing about what is undecided.

**Any change to a rule, keyword, construct or spelling updates it in the same change as the code.**
The spec is never left trailing, and the old wording is deleted rather than kept beside the new.

**The language's slot is the model's `Part`.** The spec, the stored text and the screens all say
slot; `Part`, `PartFor` and `FindPart` are the same thing in code.


Six rules, each of which the document has already been wrong about:

- **Only the language.** How Sprocket or an editor happens to behave is not a rule of the language.
  "Sprocket writes the id" and "a name can be changed, an id cannot" were both wrong for that reason:
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
