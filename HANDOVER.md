# Handover

Where Reactor stands and what to pick up. Read [README.md](README.md) for the language;
this is the state of play.

## In one paragraph

Reactor is automation for people who have never programmed, aimed at becoming a Space
Engineers mod. A working prototype exists - `Reactor.Core` holds the model, the runtime
and the whole editor, compiles under C# 6 / netstandard2.0 behind a banned-API gate, and
an Avalonia host renders it and simulates a ship. **The project is currently in a design
phase rather than a building one.** The prototype proved the shape works; the documents
now being written are the attempt to find out what it actually has to do before more of
it gets built.

The one exception is the editor's own interface, which has just had a working-over: the
prototype is a template for the in-game screens, so its shape is a design artifact rather
than throwaway scaffolding. `docs/Editor.md` is where that lives.

88 tests, all green - 70 in core, 18 driving the host.

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
6. **The Space Engineers host.** Not started. It implements `Rendering/` against the same
   `Reactor.Ui` components and supplies an `IWorld` over the real grid. Read "the small window
   is an opportunity" in `docs/Editor.md` before sketching it: keeping the window small enough
   to sit beside a usable world, and letting somebody pick a block by pointing at it, are both
   things the abstraction already allows and both are far cheaper designed in than retrofitted.

## Known problems

- **A jammed entity stalls a sequence silently.** Deliberate - proceeding is how a stuck
  door vents a ship - but nothing shows it. Now specified as R30.
- **Momentary signals can be missed.** A one-frame press between updates on `Update10` or
  `Update100` is never sampled. See ApiUnknowns 10; it may decide the whole runtime shape.
- **Locking is disabling**, so a sequence stopped mid-way can leave somebody sealed in an
  airlock behind a disabled door.

## Working notes

```
dotnet test                                       # 88 tests
dotnet run --project src/Reactor.Host.Avalonia    # the prototype
dotnet test --filter Screenshot                   # PNGs to %TEMP%\reactor-*.png
```

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
