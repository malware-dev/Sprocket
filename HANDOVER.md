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

62 tests, all green - 55 in core, 7 driving the host.

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
still open, and tickets cannot be described until those are made. Design.md holds nine of
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

- **The root is the machines on this ship**, not a list of sequences. A system is what a
  machine *is*; the ones on the ship are what somebody *has*, named *Airlock 1* the way the
  game names a block *Interior Light 4*. The word "instance" never reaches a screen.
- **A machine's screen has the same shape as the root** - ways to add on top, a rule, then
  what is already there. The starter chooser that used to stand between making a machine and
  building its first sequence is gone; `+ When...` and `+ Every...` live on the machine.
- **`PathComponent` is `PickComponent`**, drawn by `ColumnPickRenderer`. Core states the
  stages and what each needs back; columns, the search box, the kind list and the double-tap
  are all the host's choices.
- **Naming is on the heading**, using the game's word `CustomName`, edited in place.
- **A machine cannot be rewired while it runs.** Editing waits on the switch, and the switch
  is a fact about the machine rather than a mode the editor is in - you cannot rewire a live
  circuit either, so nobody has to be taught it. `Sequence.CanRun` needs both the sequence's
  own switch and its machine's, and switching off abandons any run in progress. A new sequence
  starts parked.
- **A running sequence is drawn as a thing being watched**, not as an editor with its hands
  tied: the arrows, crosses and add buttons are absent rather than greyed, and the sentence
  reads as prose. It is a first slice of the diagnosis view R28 will finish.
- **Every change is a reversible operation** with an undo beside Pause and Delete that names
  what it would undo. Undo cannot touch the switch, so no press of it can arm a machine.

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

1. **The configurator.** `+ add` on a machine and `Load system` both open a stub that says
   so. Neither can exist until roles do, and roles are the centre of gravity above.
2. **Calling one sequence from another.** `CalledTrigger` exists and nothing consumes a
   name, so it is reachable only through *run now*. Wanted by E, and R5's stop is worth
   nothing without it.
3. **Authoring `only if` and `once`.** The model handles them and the editor renders them;
   nothing creates one.
4. **Anything about diagnosis.** Skipped steps are recorded structurally; nothing surfaces
   them.
5. **The Space Engineers host.** Not started. It implements `Rendering/` against the same
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
dotnet test                                       # 62 tests
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
- **Command ids are re-issued on every rebuild and must never be remembered.** Read one at
  the moment of use. Captured ids are the cause of every dead search box we have had.
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
