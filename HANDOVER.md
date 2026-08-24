# Handover

Where Reactor stands and what to pick up. Read [README.md](README.md) for the language;
this is the state of play.

## In one paragraph

Reactor is automation for people who have never programmed, aimed at becoming a Space
Engineers mod. A working prototype exists - `Reactor.Core.Mixin` holds the model, the runtime
and the whole editor, compiles under C# 6 / netstandard2.0 behind a banned-API gate, and
an Avalonia host renders it and simulates a ship. **The requirements are in a design phase
rather than a building one.** The prototype proved the shape works; the documents being
written are the attempt to find out what it actually has to do before more of it gets built.

The exception is the editor's own interface, which has been built rather than described.
The Avalonia host is the **template** for the in-game screens, so its shape is a design
artifact rather than throwaway scaffolding, and the Space Engineers screens are a
duplicate of it rather than an interpretation. `docs/Editor.md` is where that lives.

90 tests, all green - 70 in core, 18 driving the Avalonia host, 2 answering conformance
for Space Engineers.

## The documents, and which is which

| | |
|---|---|
| `docs/UserStories.md` | **Public.** Twenty-three wants in players' own words. No mechanism, no status, no cost - it exists so somebody outside can say whether we understood them. |
| `docs/StoryNotes.md` | Internal. Our judgement on each story: where it stands, what it implies, what was rejected, what the set settled on its own. |
| `docs/Requirements.md` | Internal. What Reactor must do, derived one story at a time. |
| `docs/Design.md` | Internal. The model, what is deliberately excluded, and the open decisions. |
| `docs/Editor.md` | Internal. The interface: what each screen is, what core states about it, and what is left to whoever draws it. |
| `docs/ApiUnknowns.md` | Internal. Eleven questions about Space Engineers that block requirements. Two have already been struck out as things we knew after all. |

Nothing from the internal five belongs in the public one.

## How work is supposed to flow

**story → requirement → (decision) → ticket**

- A **story** is a want, in a player's words. An idea that cannot be written as one is not
  a feature. Being on the list is a request, not a promise.
- A **requirement** is a thing Reactor needs together with how it works - "we need a stop
  statement, and this is what stopping does". Something a person could be handed. It is
  *not* an opinion, *not* a permission, and *not* a story's done-when restated. All three
  have been mistaken for requirements and retired.
- **Decisions** are made in conversation and recorded in Design.md. A decision is not a
  ticket; it is what has to have happened before work can be described.
- **Tickets** come from requirements, one requirement usually giving several, labelled
  with the story that sourced it. **None have been filed yet.**

Requirements are numbered permanently from the early re-cut onward. R18 and R19 are
retired and not reused.

## Where the pass has got to

**The pass is complete.** Every story from A to W has been worked through and the
requirements derived from each.

48 live user requirements, three technical (T1-T3, all provisional predictions about a game
nobody has run this in yet), two retired.

**Next: the decisions.** Requirements are written; several wait on design questions that are
still open, and tickets cannot be described until those are made. Design.md holds ten of
them, ApiUnknowns holds eleven questions about the game - and several of those turned out on
inspection to be things Morten simply knows, so the register is worth reading down together
before treating anything in it as blocking.

The biggest things the pass has produced:

- **Roles are wanted by four unrelated stories** - E for reuse, K for sharing, P for
  repair, T for blocks that are not there yet - which makes the system-as-template work
  the centre of gravity of the whole project.
- **The diagnosis window is one feature, not four.** M, Q, R and V each want a piece of
  the same view of a running sequence.
- **M is large and essential.** It is the only story that helps somebody whose automation
  is already broken, and this audience has no fallback.
- **O is a project in itself**, deliberately under-specified, wanted as a stretch goal.

## The editor, as of today

The screens were reworked around systems, and the shape settled. `docs/Editor.md` is the
long form; in brief:

- **The root is the systems on this ship**, not a list of sequences. A system is what a
  system *is*; the ones on the ship are what somebody *has*, named *Airlock 1* the way the
  game names a block *Interior Light 4*. The word "instance" never reaches a screen.
- **A system's screen has the same shape as the root** - ways to add on top, a rule, then
  what is already there. The starter chooser that used to stand between making a system and
  building its first sequence is gone; `+ When...` and `+ Every...` live on the system.
- **`PathComponent` is `PickComponent`**, drawn by `ColumnPickRenderer`. Core states the
  stages and what each needs back; columns, the search box, the kind list and the double-tap
  are all the host's choices.
- **Naming is on the heading**, using the game's word `CustomName`, edited in place.
- **A system cannot be rewired while it runs.** Editing waits on the switch, and the switch
  is a fact about the system rather than a mode the editor is in - you cannot rewire a live
  circuit either, so nobody has to be taught it. `Sequence.CanRun` needs both the sequence's
  own switch and its system's, and switching off abandons any run in progress. A new sequence
  starts parked.
- **A running sequence is drawn as a thing being watched**, not as an editor with its hands
  tied: the arrows, crosses and add buttons are absent rather than greyed, and the sentence
  reads as prose. It is a first slice of the diagnosis view R28 will finish.
- **Every change is a reversible operation**, with undo and redo beside Pause and Delete as
  fixed-width arrows that never come and go, the description in a tooltip. A new edit ends the redo branch. Neither can touch
  the switch, so no press of either can arm a system, and both wait for it like any other
  edit.

## Roles, and the ones of a system

- **A role is derived, never declared.** `AutomationSystem.Parts` walks the sequences and
  reports what they act on; the same door in a step and a gate is one part, listed once. R23's
  argument is that a hand-kept list goes stale the first time somebody adds a step.
- **A role is identified by what the author built with**, and carries authored wording -
  `SystemRole.Name`, `Instructions`, `Optional` - kept in a side list that outlives the
  derivation. Delete the last step mentioning a door and the note written for it survives.
- **One of a system is a substitution**: `SystemInstance.Fill` says "where the author used that
  door, I use this one". The first one fills in nothing, which is why everything built before
  roles keeps working untouched. A part may hold several things, which needs no new rule -
  several on a trigger already means *any*, several in a condition already means *all*.
- **Three switches, all of which must be on** for a sequence to run for one of a system: the
  sequence's, the system's, and that one's. Three different intentions - "I parked this rule",
  "I am rewiring the whole thing", "leave the port airlock alone while I weld" - and none may
  overwrite another. All three are reachable from the ship's list or one press in.
- **The runtime runs one runner per instance and sequence.** `SequenceRunner` resolves targets,
  trigger sources and gate conditions through its instance, using three reused buffers so a
  tick allocates nothing. Three because a gate is asked while a step is acting and the trigger
  is sampled on the same tick.

## The model, as of today

- **Core says `entity`, the host says what players say.** `IWorld.Noun` carries block/blocks/
  Blocks for Space Engineers; another game supplies its own. Core never writes the player's
  noun into a sentence itself.
- **Properties are the primitive.** A reading is an `EntityValue` - flag, number or text. An
  `EntityEvent` is an edge on a property, an `EntityState` is a `Relation` against one. There
  are no "signals" any more; a flag is just a property whose readings are true and false.
- **`EntityKind.Icon`** names a picture the host resolves, and `PickStage.HasIcons` says
  whether a stage's answers have them. Core never learns what a picture is.
- **The prototype persists.** `Reactor.Storage.ProgramText` writes a program as text and reads
  it back; core never touches a file, because the game does not whitelist IO.

## What is not built

Ordered by how much they hurt.

1. **The configurator screen.** The model and the runtime under it are built - see "roles and
   the ones of a system" below - but nothing fills a role in from the editor. `+ add` on a
   system and `Load system` both still open a stub, and `SystemInstance.Fill` is only reachable
   from code and from a saved file.

   One thing goes with that screen: **the editor does not know which one of a system it is
   looking at**, so `AutomationRunner.Get(sequence)` answers for the first - right while there
   is one, a half-answer as soon as there are two.
2. **The undo history does not survive a restart.** *Next up, and starting with a
   conversation rather than with code.* `ProgramText` saves the program; the history lives in
   `ReactorEditor._history` and `_redo`, keyed by sequence id, in memory only.

   The mechanics are small - the edits are already compact records, shaped to be sendable, so
   writing them out is the same job as putting them on a wire and both should be done together.
   **Where it goes is the open question**, and it is not obvious. History is a fact about
   somebody's editing session rather than about the ship, which argues against the world save;
   but a mod has nowhere else that survives, and in multiplayer "somebody's session" is a
   different person on each client. Worth deciding before writing any of it.
3. **Calling one sequence from another.** `CalledTrigger` exists and nothing consumes a name,
   so nothing can reach it at all - the run-by-hand button that used to is gone. Wanted by E,
   and R5's stop is worth nothing without it.
4. **Authoring `only if` and `once`.** The model handles them and the editor renders them;
   nothing creates one.
5. **Anything about diagnosis.** Skipped steps are recorded structurally; nothing surfaces
   them.
6. **`IWorld` over a real grid.** The Space Engineers screens are built - see *The four shared
   projects* below - and every component the Avalonia host draws has a counterpart that draws
   it on the game's screen. What is still not there is the other half of the mod: nothing
   supplies an `IWorld` over an actual construct, so the only ship Reactor has ever run against
   is the invented one in `Reactor.Machine.Mixin`.

   Read "the small window is an opportunity" in `docs/Editor.md` before going further: keeping
   the window small enough to sit beside a usable world, and letting somebody pick a block by
   pointing at it, are both things the abstraction already allows and both are far cheaper
   designed in than retrofitted.

## The four shared projects

The mod compiles loose `.cs` files and cannot load an assembly, so everything shared between
targets is shared as **source** - `.shproj` plus `.projitems`, imported by whoever needs it.
That is why these are projects rather than libraries.

| | |
|---|---|
| `Phosphor.Mixin` | The drawing seam and the layout. `IGraphics`, `ClipStack`, `ScreenSpaceGraphics`, and `Element`/`Stack`/`Bar`/`Dock`/`Scroll`/`Wrap`/`Label`/`TextBox`/`Thickness`. Knows nothing about Reactor. |
| `Reactor.Machine.Mixin` | The invented ship. Shared so both hosts drive one world rather than two hand-made approximations of one. |
| `Reactor.Screens.Mixin` | The Space Engineers presentation - the files the mod ships. One view per component, plus the screen chrome. `Palette`, `Type`, `Space` and `Sizes` in `Widgets.cs` are the whole look: colours sampled from the game's textures, text scales, spacings and the few fixed sizes. A pass over the appearance is those four classes and nothing else. |
| `Reactor.Core.Mixin` | The model, the runtime and the editor. Pinned to C# 6 and netstandard2.0. |

Consumed by three things: the mod (`Reactor.Host.SpaceEngineers`), the simulator
(`Reactor.Host.SpaceEngineers.Sim`) and the Avalonia host, which takes the ship only.

## Phosphor

Space Engineers gained a screen-space drawing API - `MyScreenSpaceText` and
`MyScreenSpaceSprite` - which is the first time a mod could draw a UI over the world instead of
onto a block. `Phosphor` is the UI framework built on it, under a neutral namespace so it can
be lifted out when a second consumer wants it.

**It was parked pending real mod-owned GUI screens and has since been un-parked**, because the
simulator made it possible to build against the API without launching the game. The two gaps
below are unchanged and still real; what changed is that neither now blocks progress on
everything else.

### What it is

- **`IGraphics`** is the whole drawing surface, held to one rule: *every member must be
  reproducible on an `IMyTextSurface`*. That is what keeps the panel target possible, and it is
  why there is no sprite-sheet source rectangle, no rotation pivot, no draw layer, no text width
  limit and no alignment argument - `MySprite` has none of them. A backend may use them
  internally; it may not expose them.
- **Coordinates are a fixed UI size**, roughly 1440x1080, scaled to the screen the way the game
  scales its own HUD: the safe GUI rectangle is 4:3 at the full screen height and the HUD scale
  is that height over 1080. Working in raw window pixels comes apart, because the game multiplies
  a font scale by that same factor - boxes would stay put while the text inside them grew. The
  reference height is a constructor argument rather than a rule, and any choice stays consistent
  because the font scale is corrected by however far it sits from the game's.
- **How big one of the game's screens is, measured rather than remembered.** `MyGuiManager`
  normalizes against `m_safeGuiRectangle` and nothing else, so 1.0 in the game's GUI
  coordinates *is* the safe area. `MyGuiScreenTerminal` is `new Vector2(1.0157f, 0.9172f)` -
  fractionally wider than the safe area and about nine tenths of its height. That is worth
  writing down because it is not what it looks like: a working screen is essentially full
  width, and it is the game's *dialogs* that are small (creating a faction is 0.66 by 0.64,
  loading a world 0.92 by 0.97). `EditorView.ScreenWidth` and `ScreenHeight` hold it.
- **`ClipStack`** does the intersecting push and pop that neither target has, so a backend only
  ever has to honour one rectangle.
- **`Element` and the containers** are measure, arrange, draw. `Element.Draw` is not virtual on
  purpose: it pairs the clip push with its pop, and a subclass overrides `DrawContent` so it
  cannot get between them. `Stack` is a line of things, `Bar` is left/fill/right, `Dock` is
  top/fill/bottom, `Wrap` flows a sentence onto the next line, `Scroll` moves a region.
- **Padding, clipping, the edge hairline and `Collapsed` all live on `Element`**, for one reason
  repeated four times: each is a property of a rectangle rather than of any particular widget.
  `Collapsed` is out of the layout entirely rather than merely invisible, and containers skip a
  collapsed child completely, spacing included - an empty label still holds its line, which is
  right for anything that must not move and wrong for a warning that is usually absent.
- **A horizontal `Stack` centres its children across the line**, and `Wrap` centres within each
  line. Not a preference: a row is a line of words and controls of different sizes, and hanging
  the short ones from the top makes them read as superscript rather than as part of the line.
- **Input suspension is server business, and that is not a preference.**
  `SetPlayerInputBlacklistState` raises a multiplayer event, so a client cannot suspend its own
  input. The client resolves which controls to suspend, because only it has the bindings, and
  ships the list. A heartbeat exists because a client that crashes mid-edit would otherwise leave
  a player unable to move for the life of that world. Single player dispatches through the same
  handler as a client rather than short-cutting, so the one configuration that can be tested is
  the one that will run.

### The two things that made it a workaround

- **No clip rect in screen space.** The renderer has a scissor and the panel API reaches it
  through `MySprite.CreateClipRect`, but the screen-space queue never enqueues one and
  `MyRenderProxy.SpriteScissorPush` is not whitelisted. Filip has said he will add it.

  **`ScreenSpaceGraphics.SetClip` and `ClearClip` call it anyway, so the mod project does not
  compile.** Two errors, both on those lines, and that is deliberate: the mod is unusable until
  the API lands, and writing a fallback would mean building a rule about scrolling by whole rows
  into the framework to serve a gap that is not meant to be there. Everything above those two
  lines assumes it works. The simulator implements the same calls and honours them, so the
  behaviour is already built and tested against.
- **Mouse look cannot be suppressed** *(no longer applies - kept so nobody re-derives it)*.
  `MyInputExtensions.GetRotation` seeds itself from `GetMouseXForGamePlayF`/`GetMouseYForGamePlayF`
  unconditionally and only *then* adds the `ROTATION_*` controls, which carry the analog
  contribution alone. So the input blacklist can stop a gamepad turning the view and can never
  stop a mouse. `LOOKAROUND` is the hold-to-look-around modifier, not mouse-look, so it was never
  the lever it looks like.

  This mattered only while the plan was to draw over live gameplay, and that is not the plan.

**We are working from the assumption that Keen gives us a real screen.** That changes what the
rest of this section is about. A screen owns its input the way the terminal does, so nothing has
to fight the camera, nothing has to blacklist controls, and a pointer is simply a pointer -
which means hover, drag and everything else a pointer implies are ordinary rather than blocked.
The input-suspension machinery below is written down because it was worked out and it was right
for the world it was written for; it is not the world we are building for.

What survives the assumption is the clip rect, because that is about drawing rather than input.

## The simulator

`Reactor.Host.SpaceEngineers.Sim` runs the mod's own screens without the game. It compiles the
same `Phosphor.Mixin` and `Reactor.Screens.Mixin` sources the mod does - not a copy and not a
port - over substitute `VRageMath` and screen-space types that carry only the members Phosphor
actually calls. Substitutes rather than the real assemblies because `MyScreenSpace*` are static
classes and static methods cannot be intercepted; there was never a version of this that
referenced the game.

It exists for two reasons. Development stops needing a game launch and a world reload per
change, and the whole thing keeps working on a machine that cannot have Space Engineers
installed at all.

**What is true and what is a stand-in.** The letterforms, the advances, the block artwork and
the palette are all the game's own. The palette values were sampled out of the game's textures
and each one names the file it came from - the eye remembers Space Engineers as dark navy and it
is not, its panels are a light grey-green steel and its highlight is a pale ice blue. What is
invented is the backdrop behind the screen, which is a hull and a horizon so that the veil the
overlay is drawn behind has something to veil.

**Extraction runs where the game is.** `--extract` decodes the font page, the cursors and the
block icons out of DDS and writes PNG into `%LOCALAPPDATA%\Reactor\SimContent`. The DDS is never
cached, only what comes out of it, so the machine without the game needs no decoder - **but that
cache folder has to be copied there by hand.** Nothing does it automatically and nothing can:
that machine has no game to read. `--content` prints which machine this is, whether the game was
found and what is in the cache.

Decoding uses `DirectXTexNet`, the same package Malforge's `DocGen` and `MyTsDesigner` already
use for the same job. It is native and win-x64/x86 only, which makes the simulator formally
Windows-only - it already was in practice.

## Pick up here

### Working without the game installed

**Possible, and proven rather than assumed - but treat it as the exception.** The realistic
expectation is that this project lives on the machine with the game. What follows is what
actually happens on one without it, so that the state is recognisable rather than mysterious.

With the game unreachable and only the cache present, the simulator renders identically - the game's own letterforms, the game's own
block artwork, the same layout. Everything the code reaches for has been extracted already: the
font page, both cursors, and the six block icons.

**One thing has to travel: `%LOCALAPPDATA%\Reactor\SimContent`.** Thirteen files, 468 KB. It is
outside the repository on purpose - it is Keen's content and not ours to redistribute - so
nothing copies it for you and nothing can, because a machine without the game has nothing to
copy it from.

Forget it and the simulator still runs and is still usable. It fails *loudly*, which was the
point: block artwork becomes magenta crossed boxes, and text falls back to a desktop face whose
widths run 9 to 17 percent narrow. Layout work done in that state would be layout work done
against the wrong numbers, so check `--content` before trusting a screenshot.

What you cannot do without the game is **add** anything - a new symbol, another block icon, a
colour to sample. That needs `--extract` on a machine that has it. So the polish pass wants one
sitting of extraction here, taking everything the symbol work is going to need, before moving.

### What the last sitting added

The polish pass started, and turned out to be as much about the harness as about the screens.
See **The polish pass** below for what is done and what is left.

- The game's on/off switch, and `MaltekSymbols` - a symbol font this project draws and ships,
  which replaced the sprite symbols entirely rather than sitting beside them.
- Hover everywhere, on a rule that also fixed the inert heading: an element lights up only if it
  was given a highlight, so declaring one *is* declaring that it can be pressed.
- Tooltips, on the rule that a control with a word on it does not get one.
- `ToPixels` rounds position and size apart, which is a change to what the **mod** draws and not
  only to what the harness shows.
- The harness draws at a real resolution and fits the picture, rather than laying out against the
  window. It also remembers where its window was.

Five things were diagnosed wrongly on the way, and each wasted real time. Worth reading once,
because none of them will look different the second time:

1. **A broken capital N was the window being 690 pixels tall**, not the font, not the sampling.
   Check what resolution a screenshot was taken at before believing anything about legibility.
2. **A switch a pixel shorter than its twin was `ToPixels`**, and the numbers were sitting in
   `--dump` the whole time. `--dump` answers "what is actually being drawn" and it is quicker
   than looking.
3. **A press that appeared not to register was a row that has no command.** A system's row on the
   ship's list is a heading; only its switch answers. Nothing was broken.
4. **Symbols drawn twelve pixels low were one hardcoded constant.** `GlyphBaselineInCell` was
   measured on the game's 45 cell and applied to every font. It is now taken in proportion to the
   cell actually being read.
5. **A ring drawn low and to the right was a glyph arranged without being measured.** A glyph
   centres its own line using the size it recorded at measure time; `Mark` never called it, so it
   centred against zero. Arrange without measure is silent and looks like bad artwork.

### What to check next time

The layout and behaviour are where they should be **on the path I walked**, which was home to a
system to a sequence to a picker. Both bugs found by using it - the options jumping, the wheel
doing nothing - were on paths I had not. These are the ones still unwalked:

1. **Drag-to-reorder.** Verified before the spacing pass, and `RowGap` moved from 3 to 4.8 in it.
   The drag arithmetic has to agree with the layout or the gap opens somewhere the row does not
   land. `--shot out.png <nav> drag x1 y1 x2 y2`.
2. **A pick with three or more stages.** The only way the horizontal strip scrolling is
   reachable, and the fallback in it - a column with nowhere to go carries the whole strip
   sideways - is a judgement nobody has looked at yet.
3. **A sequence with a wait step, a problem banner, a skipped badge.** All three are drawn from
   state that has never been produced, so they have never been seen at all.

### A gotcha that cost an hour

**Tick once before believing a screen.** `ReactorEditor.Tick` re-reads `Title`, `Subtitle` and
`CustomName` every tick, and nothing else does. A rename therefore lands in the model
immediately and appears in the heading on the next tick - so a headless script that presses
Enter and looks straight away sees the old name and reads exactly like a silent failure. It is
not one. `SetText` also ignores an id it does not know without a word, which makes the wrong
diagnosis very easy to reach for.

## The polish pass

### Done

- **The switches are the game's own on/off control.** Copied from `MyGuiControlOnOffSwitch`
  rather than invented: two halves, both words always readable, the half in force filled light
  against a near-black other half. It needs no artwork - every part of it is a flat fill, a
  one-pixel border or a word. It says **what state the thing is in**, where everything else on
  these screens says what pressing it does; that is the game's convention and it was taken
  deliberately, because an audience who have never programmed should not have to work out
  whether a lone word is a label or a button. One size everywhere.
- **Symbols are a font we ship**, `MaltekSymbols` - undo, redo, a bin, four chevrons, a ring, a
  disc, a minus and a plus. A font rather than sprites because both targets already draw text and
  both resolve a font by subtype out of one registry, where a sprite wants a material declared
  once per target per icon. It also measures, so a symbol can sit inline in a sentence.
  **It has to move into a mod of its own before this one is published** - it is meant to be
  depended on rather than copied, and `Fonts.sbc` says so in capitals.
- **The font is drawn, not scavenged.** The game has no undo, no redo and no delete glyph; its
  only cross is `button_close_symbol`, which means dismiss. Borrowing it for destroy was the
  mistake this replaced. `art/symbols/build_font.py` draws the shapes and `art/symbols/FontTool`
  encodes the page - both live together because both move out together.
- **What rectangles can make is drawn, not fetched.** The drag grip is six squares, the checkbox
  is a rectangle inside another. A rectangle is exact at any size and a texture squeezed to
  eleven pixels is six grey smudges.
- **The picker marks say how many may be chosen** - square where several may, round where only
  one may. The one thing on screen borrowed from desktops rather than from the game, kept
  because the shape carries an answer the words do not.
- **Hover, and the rule under it.** An element lights up only if it has been given a `Highlight`,
  so **setting one is how a thing declares it can be pressed**. That single mechanism also fixed
  the inert system heading: it carries no command, so it gets no highlight and no panel, and now
  reads as a heading over its sequences rather than as a row that ignores you.
- **The game keeps two families of the same four states**, both starting from `(41,54,62)`, and
  they go opposite ways: a panel lightens under the pointer, a control darkens. Sampled, not
  guessed - `ControlHot` and `ControlFocus` were the two the palette was missing. Nearly every
  desktop lightens everything, and that pair is much of why the game's chrome reads as pressed
  metal rather than as a web page.
- **Tooltips**, on the rule that **a tooltip is for a control that cannot say what it is**.
  Anything with a word on it gets none. The wording is the host's, except that undo and redo
  borrow core's `UndoText` and `RedoText`, which exist for exactly this. A switch's note says
  what it governs, from core's `SwitchGovernsAll` rather than from the indent, and only the off
  direction carries the consequence - switching a system on starts nothing by itself.

### Left

- **Scroll indication.** A region scrolls and nothing says so. Cheap to draw - it is a
  rectangle - and it needs deciding whether it shows always or only while moving.
- **The U+2192 in `SequenceSummary.cs`.** Core's own wording, and the one character core emits
  that the game's font cannot draw. Every string literal in `Reactor.Core.Mixin` was checked
  against the font's 496 glyphs and this is the only one. Either core says it in ASCII or the
  screens translate it; the simulator already draws the replacement box so it cannot be missed.
  There is now a third option that did not exist before - put an arrow in `MaltekSymbols` and
  have the screens draw it - but that one waits on **Design decision 11**, because it is the
  same question in miniature: who owns the words.
- **Who owns the words.** Design decision 11, raised by tooltips and parked deliberately. Core
  hands over a lot of finished English; whether it should hand over facts instead is the sort of
  thing that gets decided by accident if new core strings keep being added. **So do not add
  any** until it is settled.

## Known problems

- **A jammed entity stalls a sequence silently.** Deliberate - proceeding is how a stuck
  door vents a ship - but nothing shows it. Now specified as R30.
- **Momentary signals can be missed.** A one-frame press between updates on `Update10` or
  `Update100` is never sampled. See ApiUnknowns 10; it may decide the whole runtime shape.
- **Locking is disabling**, so a sequence stopped mid-way can leave somebody sealed in an
  airlock behind a disabled door.

## Working notes

```
dotnet test                                       # 90 tests
dotnet run --project src/Reactor.Host.Avalonia    # the prototype
dotnet test --filter Screenshot                   # PNGs to %TEMP%\reactor-*.png

dotnet build -c Release src/Reactor.Host.SpaceEngineers   # builds AND deploys the mod
```

The simulator, which is where the in-game screens are actually worked on. Coordinates are UI
units, not window pixels; `--dump` prints both so a point can be converted.

```
Sim.exe                              # the window
Sim.exe --quiet                      # the window, behind everything, without taking focus
Sim.exe --raise                      # bring it forward   /  --dismiss  send it back
Sim.exe --content                    # which machine, is the game here, what is cached
Sim.exe --extract                    # decode font, cursors and block art into the cache

Sim.exe --dump                       # the frame as a draw list
Sim.exe --shot out.png 657 311 tick 40 drag 100 200 100 400
Sim.exe --shot out.png at 2560 1440 657 311      # at a resolution somebody actually has
```

`--shot` takes a script: a bare pair of numbers is a press, `tick n` advances the ship's clock,
`drag x y x y` picks something up and puts it down, `scroll x y n` turns the wheel over a point.
Order matters and it is drawn between every step, because nothing knows where it is until it has
been arranged - and because a light that is lit for one tick is invisible to a script that
advances forty and then looks once.

**The screen is always drawn at a real resolution and the picture of it is what gets fitted.**
Both the window and `--shot` build the frame at 1920x1080 by default and letterbox that into
whatever they have; `at W H` changes it. This is a harness concern and lives in
`PhosphorSurface.Target` - Phosphor knows nothing about it, and the mod needs nothing like it,
because in the game the screen *is* the monitor. It exists because laying out against the window
meant that dragging a corner asked the layout to fit 690 units of height, which is not a height
anybody plays at, so every judgement made while looking at it was a judgement about a screen that
will never exist. **The old default was 1280x690 and it lied**: at that size a hairline diagonal
in the game's font falls between the samples and a capital N comes out broken, which cost an
evening being diagnosed as a font problem.

The symbol font, which is content rather than code and is built rather than committed by hand.

```
python art/symbols/build_font.py                        # draws the glyphs, writes page + xml
art/symbols/FontTool/... FontTool.exe page.png page.dds # encodes what the game will load
art/symbols/FontTool/... FontTool.exe --check page.dds back.png   # and reads it back
```

`build_font.py` holds the shapes as coordinates rather than as image files, so the whole set
re-cuts at a different weight by changing one number. `FontTool` exists because the game will not
load anything else: **BC7_UNORM_SRGB, a full mip chain, premultiplied alpha**, checked against
`FontDataPA-0.dds` rather than assumed. Two things it is easy to get wrong and hard to see -
straight alpha looks right in every viewer and only goes dirty once the game blends it, and a page
without mips draws a ring as a dotted circle.

**The metrics are the game's own proportions.** A 45 cell with height 37, base 30 and lsb -7,
scaled to whatever cell this font uses. Nothing here can see what the renderer then does with those
numbers - that drawing lives in the render assembly, not the ModAPI - so rather than guess the rule,
the font is built to the same shape. Whatever the rule is, it lands the same way on both. The first
cut declared a baseline of 56 in a 64 cell and every symbol drew twelve pixels below the words
beside it.

**The window remembers where it was**, in `%LOCALAPPDATA%\Reactor\SimWindow.txt` - position,
size and whether it was maximised. Restored only if a decent corner of it would land on a screen
that still exists, so unplugging a monitor cannot hide it. It fails silently: a tool that will
not open because a preferences file is malformed is worse than one that forgets.

A note on rounding, because it is subtle and it bit hard. `ScreenSpaceGraphics.ToPixels` rounds
**where a rectangle is** and **how big it is** separately, so that the same size is always the
same number of pixels wherever it sits. It used to round outward - floor the near edge, ceil the
far one - so that two things meant to touch could never leave a seam, and the price of that was
never counted: the same switch came out 30 pixels tall in one row and 29 in another, and a
one-unit border came out two pixels on the sides whose span crossed a boundary. The rule and its
two companions are written on `IGraphics`, the short version being that **rounding is the
backend's business and must never migrate into `Element.Arrange`** - the screen has to round
because `DrawPixels` takes a `Rectangle`, and a text panel must not because `MySprite` takes
floats and a UI unit is a quarter of a texel there.

A note on scrolling, because it was wrong for a while and the fault is easy to reintroduce:
**the wheel is routed by where the pointer is**, not sent to the screen's own scroller. A
component that scrolls itself is not inside that scroller and on a picker is not even in the
layout, so a screen that sent every turn to its own would leave a picker unable to scroll at
all. Views get first refusal through `ComponentView.ScrollBy`; a picker moves the column under
the pointer, and carries the whole strip sideways when that column has nowhere left to go.

- **The mod deploys on a Release build and only then.** MDK2 packs it to wherever the local MDK
  settings point, which is not necessarily the AppData mods folder; the build output says where it
  went. `mdk.local.ini` is per-machine and gitignored.
- **`Reactor.Core.Mixin` is a shared project**, listed in the solution but marked not to build: the
  dotnet CLI cannot build one, so leaving it as a build target fails `dotnet build` on the solution
  while every real project succeeds. Rider builds it either way, which is why the arrangement
  differs from the other repos.

- **The prototype remembers its program** between runs, in
  `%LOCALAPPDATA%\Reactor\prototype.reactor`. Delete that file to start from an empty ship.
  The format is `Reactor.Storage.ProgramText` in core - core formats the text and never
  touches a file, because Space Engineers does not whitelist IO. Tests build their window
  with `Simulation.Fresh()` so they never read it.
- **Core is pinned to C# 6 and netstandard2.0**, with `build/BannedSymbols.txt` failing
  the build on reflection, LINQ, IO, threading and wall-clock time. That gate was silently
  inert for a while and is now verified to fire. Host code is exempt.
- **The running prototype locks `Reactor.Core.dll`**, so close it before rebuilding.
- **Git identity is repo-local** (`Malware` / the `malware-dev` noreply address). Pushing
  uses Git Credential Manager. `gh` defaults to a different account - use
  `GH_TOKEN=$(gh auth token -u malware-dev)` for API calls rather than switching the
  active account.
- **No em dashes anywhere.** Ordinary hyphens.

## Things not to quietly undo

Argued out rather than guessed. The reasoning is in Design.md.

- Steps block by default, and a step whose action changes nothing completes at once.
- Nothing a sequence did is undone by time, by the sequence ending, or by a stop.
- No interlock, no "airlock mode", no primitive for anything expressible as trigger plus
  steps.
- Events and states stay structurally separate.
- Ticks come from the game's frame counter and are never counted locally.
- A word programmers already know is not given a different meaning, and the game's own
  words are used for the game's own things. A merge block is not a docking block.
- Creating a system never stands between somebody and their first sequence.
- **Narrowing runs left to right and never back.** Picking blocks decides what can be said
  about them; it never decides which blocks exist. An impossible pair is allowed to be made
  and answered by an empty next stage, because a player watching their own blocks disappear
  is worse than a sentence saying why.
- **Rebuild only when necessary, update otherwise, and say nothing when nothing differs.**
  Core compares before it writes and bumps a revision only on a real change; a target
  compares that one integer and stops. Hundreds of sequences will run on a server.
- **A target must answer every point in `Reactor.Ui.Conformance`**, and each host has a test
  that fails until it does. Core cannot enforce that an affordance exists; this enforces that
  nobody forgot it existed. Nothing reads the list at runtime.
- **A command id says what it is** - a `Cmd` purpose plus up to two numbers, packed - so the
  same control keeps the same id across rebuilds and a target may hold one. Registering an
  identity twice throws. This replaced a counter reset per rebuild, which made an id a
  position and killed anything holding one, silently.
- **Nothing on screen appears or disappears with how much data there is.** A search box that
  arrives at eight rows, a count that comes and goes: the screen has no shape to learn.

  **And holding the room is not the way to obey this.** The any/all choice on a pick used to
  arrive on the second block, which put it under the pointer at the moment somebody was reaching
  for a third. Reserving space for it fixed the jump and bought a hole that sat open the rest of
  the time, which is barely an improvement. The answer was to stop hiding it: it is two short
  phrases, it goes on one line across, and it is simply always there on a stage that can ask -
  which also means it can be set before it matters instead of only after. Reach for that shape
  first; a gap held open is the same mistake wearing a different face.
- **Every screen holding a list has the same top** - the ways to add something, a rule, then
  what is already there - and everything that adds something carries a plus.
- **Naming happens on the heading**, and an empty name is an answer rather than a missing
  one. It hands the heading back to the sentence.
- **An operation is what the user would say they did**, anything the runtime can see holds
  only states somebody chose, and a run of changes folds into one exactly when the states
  between were not chosen. Design.md, "What counts as one operation" - the rule that was
  missing when the picker was written.
- **Every change to a sequence is a reversible operation**, applied and recorded in one call
  so they cannot come apart, with a round-trip test per edit. Snapshotting and log-replay were
  both considered and lost to the same fact: Reactor is live, so rebuilding a sequence to undo
  would orphan every reference the runner and the open pages hold.
