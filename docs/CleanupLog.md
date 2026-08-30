# Cleanup log

Written as the pass goes. The plan is in `Cleanup.md`; the rules are the **csharp-standards** and
**reactor-standards** skills.

## Decisions

Choices made during the run that are not already settled by the two skills. Anything here was decided
without asking, and can be overruled.

**Frame dumps carry colour.** `FrameDump` did not print `DrawCommand.Color`, so a hover, a focus ring
or an error tint could change with no difference in the dump. Added before the baseline was captured,
since a guard that cannot see colour is half a guard.

**Text dumps are the committed baseline; pictures are not.** `tests/Reference/frames/*.txt` is in
version control, so `git diff` is the comparison and a change reads as text. Shots stay a thing to
look at during a run rather than an artefact.

**The suite runs in one process.** `--suite <list> <folder>` rather than a shell loop calling
`--dump` sixteen times: one process start instead of sixteen, and it will be run after every folder.
Each entry builds a fresh device and screen, so order cannot affect any entry.

**`--dump` takes an optional path first, or `-` for standard output.** Bare `--dump` behaves as it
always did.

**Drag is not in the reference suite.** The default fixture has no sequence with steps in it, so
there is nothing to pick up. Phase 1 moves `DragTests` onto this harness and covers it there.

## Findings

Things noticed but not acted on, or acted on outside the phase that owns them.

**`PhosphorSurface.cs` contained two raw NUL characters** - `char previous = '<NUL>';` written with an
actual zero byte instead of the `\0` escape. It compiled, but every text tool treated the file as
binary, so it was invisible to grep and to any search over the project. Fixed in phase 0 because it
blocked the rest of the run. Behaviour identical.

**`ScreenSpaceDevice.Current` is a static mutable field**, set by `Install()`. This is the statics
rule, and it is what makes a frame dump and a drawn frame share state. Owned by phase 6.

**`Step.Kind` is an `int` with nine `const` values** rather than an enum. Left alone while moving the
type out of `Shot`; owned by phase 6.

## Phase 0 - the guard

Done.

- `Step` moved out of `Shot` into its own file; `Script` added, holding both the parsing of a command
  line and the playing of it. `--shot` and `--dump` now drive a screen through the same code, so they
  cannot answer the same script differently.
- `--dump` takes a script and an output path. `--suite` runs a named list of them.
- `tests/Reference/suite.txt` holds sixteen states: the root and its rename, hover, tick and switch;
  a system and its rename and back; a sequence; the block picker hovered, scrolled, searched and
  picked; and adding a step.
- Baseline captured to `tests/Reference/frames/`.

**Proved:** the suite reproduces itself byte for byte across runs, and a one-unit change to a single
spacing constant in `Widgets.cs` moved all sixteen frames. The guard catches what it exists to catch.

**State:** mod builds, 92 tests green.

## Phase 1 - the Avalonia host

Done. `src/Reactor.Host.Avalonia` (2,559 lines) and `tests/Reactor.Host.Tests` are gone, and
`tests/Reactor.Screens.Tests` drives the simulator in their place.

Its eighteen tests were dealt with one at a time rather than in a batch, in a separate project
alongside the old one, so the suite stayed green throughout and nothing had to be trusted:

- **Two conformance tests were dropped.** `Reactor.Host.SpaceEngineers.Tests` already asks the same
  two questions of the real target. They were the prototype answering for itself.
- **One screenshot test was dropped.** It asserted only that a captured frame was not null; the
  images it existed to produce are what `--shot` and the reference suite now do.
- **The other fifteen moved**, and are seventeen now: scrolling split into "the list is taller than
  its column" and "scrolling reaches the end", and renaming split into editing in place, the header
  holding its height, and the heading lining up with back.
- **`PaperTarget` was written**, which was the condition `docs/Design.md` set for removing the host.

**The paper target is the replacement for what the prototype provided**, and it is not the
simulator. The simulator shares `Screens.Mixin` with the game and is not an independent consumer of
core, so porting onto it alone would have quietly dropped the only evidence that core can be
rendered by something which is not this game. `PaperTarget` lives in `Reactor.Core.Tests`, has no
drawing, no layout and no game near it, and **refuses to render a component type nobody has told it
about** - so a new component stops the tests until somebody has said what a target should show.

## Findings from phase 1

**The reference suite was not hermetic.** `EditorScene` loaded the persisted store at
`%LOCALAPPDATA%\Reactor\prototype.reactor`, which the windowed simulator keeps between runs on
purpose. Every test and every captured frame therefore started from whatever was last built by hand -
and `Simulation.Tick` saves every sixty frames, so the tests wrote into it. No harm done: the
simulator is a development tool and its save is mine, not Mal's, who tests in the game. The problem
was the guard, not the file.

Fixed at the cause: `EditorScene.Nowhere()` builds on `Simulation.Fresh()`, and everything drawn
without a person in front of it - the suite, `--dump`, `--shot` and every test - now uses it. The
windowed simulator still remembers, because that is what it is for. `Store` had a docblock warning
about exactly this failure; it was right.

**Two tests had been passing for the wrong reason.** Once the fixture was clean they failed, because
they were written against blocks that only existed in the polluted save. Nothing was weakened to
make them pass again - the scripts were rewritten against an empty construct.

**The reference suite is now a walk through the product from nothing**: an empty root, naming a
system, the block picker hovered, scrolled, searched and picked, the choice, and the sequence that
walk arrives at. Eighteen states. That is a better suite than the one phase 0 captured, and it only
exists because the fixture problem forced it.

**The picker's header is the odd one out, and the picker is what wants redesigning.** The prototype
held one header height across every screen; the Space Engineers screens do not, because a picker's
header stands 77 units against a sequence's 53.

Settled with Mal: the header is deliberately designed *without* a subtitle. A subtitle was rare
enough that reserving its line made the header look wrong on every screen that had none. So the
picker showing one is the picker disagreeing with the design, and the fix belongs in the picker, not
in the header. Recorded in `docs/Editor.md`; it is a behaviour change and therefore not this pass's
to make.

**`Reactor.Machine.Mixin` now has exactly one consumer**, the simulator. It cannot be a mixin
anyway - it uses `System.IO` and file-scoped namespaces - so it can become one of the simulator's
own folders. Owned by phase 5.

**State:** mod builds, 94 tests green, reference suite reproduces exactly.

## Phase 2 - Phosphor.Mixin

Done. 3,452 lines to 3,264, all nineteen files reviewed.

Structure needed nothing: one public type per file already, folders already sorted by what the code
does - `Graphics/`, `Input/`, `Ui/`. Statics are clean, every one of them a pure function bar
`Clipboard._value`, which is shared deliberately and now says so: copying on one screen and pasting
on another is what a clipboard is for, so it has to outlive any one of them.

The work was comments. Every `<summary>` carrying a `<para>` became a summary and remarks, and the
rule was applied to each block: an external fact or a constraint the code cannot show stays, history
and rationale go. What survived is the load-bearing half - `MySprite` having no source rectangle,
the game's glyph cell being 45 against a line of 37, `MyGuiManager` computing its HUD scale over
1080, a text panel having one clip state and no stack, a symbol measuring its whole square cell -
and what went was the story of how each was found.

**Two defects found while reading:**

- `Label.Text` called `InvalidateMeasure()` twice. Harmless, since the second is a no-op on an
  already-dirty element, but it was two identical lines.
- `TextBox` documented itself as driven by `TextEditor`, a type that does not exist anywhere in the
  repository. The comment pointed at nothing.

Four members were missing the explicit `private` the rest of the codebase spells out.

## Phase 3 - Reactor.Core.Mixin

**Structure done.** 49 files to 84, one public type per file everywhere. Thirteen files were holding
more than one: `SequenceEdit.cs` and `EditorComponent.cs` held nine each, `Step.cs` five,
`SequenceComponent.cs` and `Trigger.cs` four.

Every new file was added to `Reactor.Core.Mixin.projitems` in the same change, which is not optional -
a shared project lists its files by hand, and a missing entry drops working code out of the build in
silence.

Nothing moved between folders. `Editing/`, `Runtime/`, `Sequences/`, `Storage/`, `Ui/` and `World/`
were already sorted by what the code does rather than what it is.

Verified after every batch: mod builds, 94 tests green, reference suite reproduces byte for byte.

**Comments done.** Core was written to a much better standard than Phosphor: not one `<summary>`
carried a `<para>`, and the sweep found 31 narrative phrases, of which 14 were false positives -
"no longer on the construct" describes an entity that has been removed, which is present behaviour.
The other seventeen were history, and each had a rule underneath it worth keeping: an id is a
reference and never a position; a name replaces what a sequence does and never what starts it; the
picker hands its answer back once rather than writing into the sequence as it goes.

**Three defects found while reading:**

- **`ReactorEditor.OfferAnother` carried three `<summary>`/`<remarks>` pairs.** Only the first is
  read by anything, so two blocks were invisible - and one of them was `OfferLoading`'s, which sat
  undocumented directly below it. The third described a screen that no longer exists. Put back where
  they belong; the orphan deleted.
- **`EditorPage.Heading` and `Relation` each carried two `<remarks>` blocks**, where the second is
  silently dropped. Merged.
- A sweep for the same fault across `src` and `tests` found no others.

## Phase 4 - Reactor.Screens.Mixin

### The folder scheme, decided before anything moved

Seven files hold 32 types. One type per file makes 32 files, and 32 files in one folder has no shape
at all, so the scheme is settled here rather than improvised per file.

Sorted by what the code does, mirroring the words core already uses so that a reader looking for how
sequences are drawn goes to the folder called `Sequences`:

| Folder | What happens in it | Types |
|---|---|---|
| `Look/` | What Reactor looks like on this target | `Palette`, `Type`, `Space`, `Sizes`, `Icons` |
| `Controls/` | The pieces a screen is built from | `Rule`, `Dot`, `Grip`, `Mark`, `Signpost`, `Slot`, `Switch`, `Sized`, `IconBox`, `Widgets` |
| `Host/` | The screen itself and its furniture | `IScreenHost`, `EditorView`, `Hit`, `Pin`, `HeadingRow`, `ComponentView`, `HeadingView`, `NoticeView`, `UnsupportedView` |
| `Lists/` | Rows of things to choose or watch | `StartView`, `ChoiceListView`, `StatusListView`, `StatusRow` |
| `Picking/` | Choosing blocks and what happens to them | `PickView`, `StageView` |
| `Sequences/` | Drawing a sentence | `SequenceView`, `SentenceView` |

### Namespaces

The standard says namespaces follow folders. Counting what the repository actually does:
`Reactor.Core.Mixin` follows it, and so does the simulator - `Reactor.Sim.Simulation`,
`Reactor.Sim.Content`, `Reactor.Sim.Shell`. `Phosphor.Mixin` is the only project that does not,
keeping one flat namespace across `Graphics/`, `Input/` and `Ui/`.

I first wrote that Phosphor's flat namespace was deliberate. It is not: nothing in the repository
records any decision about namespaces at all, and that code was written without one being made. One
convention with one exception is not a choice between two conventions, so the agreed rule applies and
Phosphor is what moves.

`Screen/` was renamed to `Host/` in the same change. `Reactor.Screens.Screen` reads badly, which is
the folder name being weak rather than the rule being wrong - and `IScreenHost` was already calling
it that.

### What was done

7 files to 33. Every type now has its own file, in the folders above, and every one is registered in
`Reactor.Screens.Mixin.projitems`. Mod builds, 94 tests green, reference suite byte for byte
identical after each batch.

`Overlay` was lifted out of `EditorView`: three fields and four methods that were only ever about
themselves - where the thing sits, whether it is up, and whether a press landed on it. 1,446 lines
to 1,195.

### EditorView, and what building it taught

`Overlay` and `Ask` are now types of their own, each owning its own state, its own placement and its
own hit test. `EditorView` went 1,446 lines to 1,141.

They were extracted independently, and they arrived at nearly the same members - `IsUp`, `Covers`,
`Draw`, and a pair to open and close. That was the test for whether an `ILayer` was real.

**It is not, and doing the work is what showed it.** The plan was a loop over layers topmost-first,
each owning its own press. Two of the three cases fit: a question and a refused rename both own the
screen outright, and a press that misses them is used up dismissing them. The overlay does not. Its
own comment says why:

> An overlay owns the screen the same way a question does, **except that what is inside it is
> ordinary and can be pressed through the usual list.**

A press inside an overlay is not handled by the overlay. It goes to the same hit list as everything
else, filtered to the hits that fall within it - which is exactly what
`_overlay.Covers(Middle(hit.Bounds))` is doing inside that loop. So the overlay is not a layer that
takes presses. It is a **region restriction on the body**, and an interface that made it look like
the other two would have been wrong in a way that only showed up once thirty files had been made to
agree with it.

What is actually there is three different things:

- **Modal** - a question, a refused rename. Owns the press; a miss dismisses it.
- **A restriction** - the overlay. Narrows what the body will answer.
- **The body** - the hit list.

An interface over the two modal cases would be an abstraction over about fourteen lines, which is
not worth having. So: no `ILayer`. The two types were worth extracting on their own merits, `Press`
now reads as three named things rather than three hand-written variations of one, and the shape that
would have been imposed on everything after it has been ruled out on evidence rather than taste.

**Comments done** across Screens, the mod host, the machine and the simulator: 18 summaries carrying
a `<para>` were split into a summary and remarks, and 21 narrative comments were rewritten as the
rule underneath them.

**Nine comments still described the deleted prototype** - "the other host draws a circle", "the
roles are the Avalonia host's", "it was a step down for a while". Those are the ones the standard
exists to catch: prose that was true about a decision at the moment it was made, left behind to be
read as though it were true about the code.

`Under` and `Press` were walking the hit list with the same three filter conditions written out
twice, which is the pairing that has to agree exactly: a thing somebody can see light up and then
cannot press is worse than one that never lit. Now one `Answers` method, used by both.

**Still to do:** `Body` - the hit list and its walk as a type of its own. Worth doing and no longer
urgent: the duplicated filter was the part that was actively rotting.

## Phases 5 and 6 - the mod host, the machine, and the simulator

**One type per file everywhere.** Seven more files held more than one: `GameApi.cs` held ten,
`VRageMath.cs` five, `BlockExtensions.cs` six, `SimEntity.cs` seven.

The fakes needed a second attempt and are worth recording. The first splitter assumed one namespace
per file and gave all ten of `GameApi.cs`'s types the first namespace it found, which put
`MyAPIGateway` in `VRage.Utils`. It did not compile, so nothing was at risk - but the guard that
caught it was the build rather than the tests, and a file that mirrors several of somebody else's
namespaces is exactly where that assumption breaks. Restored from the phase snapshot and split again
tracking the namespace each type was written in.

**`Reactor.Machine.Mixin` is gone.** It could never have been a mixin - it uses `System.IO` and
file-scoped namespaces, neither of which survives the game's compiler - and the simulator was its
only consumer. It is now `Reactor.Host.SpaceEngineers.Sim/Machine/`, namespace `Reactor.Sim.Machine`,
and the shared project and its solution entry are deleted. Six projects where there were eight.

### Namespaces done

`Phosphor.Mixin` now has `Phosphor.Graphics`, `Phosphor.Input` and `Phosphor.Ui`, matching its
folders and the rest of the repository. Every consumer was given the usings it needs and stripped of
the ones it does not - the first sweep added 354 using lines and 277 of them were unnecessary, which
is the sort of diff that buries the real change. `MainWindow` in the simulator keeps none of them: it
uses Avalonia's `Dock` and `Thickness`, so the Phosphor types it wants are written out in full.

`Reactor.Core.Tests` had Phosphor usings added to it by that same sweep and cannot see Phosphor at
all - it references core and nothing else. Removed.

**Still to do in this phase:** the comment pass over all 33 files.

## The namespace rule, finished properly

Mal spotted that the rule was still broken in several places after I had claimed to apply it. It
was: I fixed Phosphor, said in this log that Screens was left flat "matching Phosphor", and then
never went back to Screens once Phosphor moved. The mod host was untouched as well.

Now audited rather than asserted - every folder in every project, checked against the namespace its
files declare:

- `Reactor.Screens.Mixin` - all six folders now name themselves: `Reactor.Screens.Look`,
  `.Controls`, `.Host`, `.Lists`, `.Picking`, `.Sequences`.
- `Reactor.Host.SpaceEngineers` - `Ui/`, `World/` and `Input/` all now say so.

  `Input/` took two goes. It held `namespace Phosphor`, and I first "fixed" it to `Phosphor.Input`
  on the reasoning that these are Phosphor's input implemented against the game's API. Mal caught
  it: the folder is inside the mod host, so the namespace is the mod host's, and whose types they
  are conceptually is not what the rule is about. Deciding an exception on a judgement like that is
  how a rule stops being predictable. They are `Reactor.Host.SpaceEngineers.Input`.

**One exception, and it is a real one:** the simulator's `Fakes/` declares `Sandbox.ModAPI`,
`VRage.Game`, `VRage.Utils` and `VRageMath`. Those are substitutes standing in for the game's own
types and have to be in the game's own namespaces, or nothing they exist for would compile against
them. A folder that mirrors somebody else's namespaces is the one case the rule cannot reach.

### `Type` was a name that only worked by accident

Splitting the namespaces broke the build in one place worth recording: the type scale was called
`Type`, and once it stopped sharing a namespace with its callers it became ambiguous with
`System.Type` everywhere. Renaming it to `Text` broke it again, differently - `Slot : Label`, and
`Label.Text` shadows a type of that name in every subclass.

It is `Lettering` now: `Lettering.Title`, `Lettering.Body`. A name that only resolves because
nothing has yet been imported alongside it is a name waiting to fail, and the flat namespace had
been hiding two separate collisions.

**State:** solution builds, mod packs and deploys, 94 tests green, reference suite byte for byte
identical to the capture taken before the cleanup began.
