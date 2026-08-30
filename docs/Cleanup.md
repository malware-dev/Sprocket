# The cleanup

A single pass over the whole project, run unattended, against rules agreed in advance. The rules are
not in this document: they are the **csharp-standards** skill (general, user level) and the
**reactor-standards** skill (`.claude/skills/`, this repository). This is only the order of work and
the safety around it.

## Ground rules

- **Behaviour does not change.** Not what is drawn, not what is stored, not what the mod does in the
  game. Structure is free.
- **Every phase is proved, not believed.** Tests green and the reference suite identical, or the run
  stops.
- **Work happens on a branch.**
- **No commit without being asked for it in that message.** Commits are proposed at four points
  below; between them the tree stays dirty.

## Checkpointing without writing history

Git is the obvious safety net and it is the one thing this run may not reach for on its own. Instead,
at every phase boundary the working tree is copied to the session scratchpad, excluding `.git`, `bin`
and `obj`. The whole of `src` and `tests` is under 30,000 lines, so a snapshot is instant and a
rollback is a copy back. Nothing touches the index and nothing touches history.

The four points where a commit is genuinely worth proposing, because each is a clean boundary with
everything green:

1. After **phase 0** - the harness and the captured reference suite are an asset in their own right.
2. After **phase 1** - a large deletion, best not left loose.
3. After **phase 4** - the three mixin projects done, which is the bulk of the work.
4. After **phase 7** - the end.

If a phase turns out larger than expected, one more will be proposed rather than quietly skipped.

## Phase 0 - build the guard

Nothing else can start until the project can prove it has not changed.

- Teach `--dump` to take a script, reusing the existing parser and step runner. `--shot` already
  does; a text frame that can be diffed says *what* moved, where two PNGs only say *that* something
  did.
- Define a **reference suite**: named scripts, one per screen and state worth holding still. Root,
  systems, sequence, picker, picker searched, picker scrolled, rename in progress, rename rejected,
  switch on and off, row hover, drag mid-carry, overlay open, a running system after ticks.
- A runner that produces a shot and a dump per entry into a folder, and a comparer that reports
  differences.
- Capture the baseline from the current tree.

**Exit:** the suite reproduces itself exactly twice in a row, and a deliberately introduced one-unit
change is detected. A guard that has not been shown to catch anything is not a guard.

## Phase 1 - remove the Avalonia host

`src/Reactor.Host.Avalonia` is 2,559 lines across 10 files and is on its way out. It goes early:
cleaning code that is about to be deleted is waste, and everything after this phase is smaller.

The obstacle is that `tests/Reactor.Host.Tests` references it directly, and its 14
`[AvaloniaFact]`/`[AvaloniaTheory]` tests - conformance, drag, rename, repeated ticks, screenshots,
scroll, watching, window smoke - drive it headlessly. They move onto the phase 0 harness. The
simulator is the better target anyway: it runs the real Space Engineers screen code rather than a
prototype of it.

**This removes the Avalonia host, not Avalonia.** The simulator uses Avalonia to rasterise and to
measure text and keeps it.

`README.md` and `docs/` lose their references to the prototype in the same change.

**Exit:** 90 tests still 90 and green. Reference suite identical.

## Phases 2 to 6 - the code, in dependency order

| Phase | Project | Lines | Files | The work |
|---|---|---|---|---|
| 2 | `Phosphor.Mixin` | 3,452 | 19 | Smallest and most self-contained. The rules get their first real exercise where a mistake is cheapest. |
| 3 | `Reactor.Core.Mixin` | 8,057 | 49 | The domain. Folders are already sorted by capability, so mostly comments, statics and type splits. |
| 4 | `Reactor.Screens.Mixin` | 4,776 | 7 | The worst of it. `EditorView.cs` at 1,446 lines doing six jobs, `Widgets.cs` at 1,042 holding twelve types, and no folders at all. |
| 5 | `Reactor.Host.SpaceEngineers` + `Reactor.Machine.Mixin` | 2,294 | 12 | The mod entry point and the machine model. |
| 6 | `Reactor.Host.SpaceEngineers.Sim` | 4,336 | 24 | Test scaffolding, held to the same structural rules. |

The loop inside every phase is the same:

1. Read every file in the phase before changing any of it.
2. Apply the rules. Every file created, moved, renamed or deleted in a mixin updates its
   `.projitems` in the same change.
3. `dotnet build src/Reactor.Host.SpaceEngineers` and `dotnet test` after each file.
4. Re-run the reference suite after each folder.
5. **Any difference stops the run** until the cause is understood. Never build on top of one.
6. Findings and decisions written as they happen, not reconstructed at the end.

## Phase 7 - documents and report

`docs/` reduces to what is not code yet: open questions, decisions taken but not implemented, and
numbers pinned to external evidence. Rationale for what is now built is deleted rather than
migrated. Anything a phase renamed has its mention here and in `README.md` fixed.

The run ends with a findings file and a dirty tree.

## What is most likely to go wrong

**Context.** A pass this size will be summarised several times, and the real risk is that late-phase
work forgets an early-phase decision. Everything chosen that is not already in the two skills - the
folder scheme for `Reactor.Screens.Mixin`, in particular - is written to a decisions log in the
repository, not left in the conversation.

**Phase 4 is where over-reach lives.** Splitting `EditorView` into six types is a design act. The
scheme gets decided once, at the start of the phase, written down, and then followed rather than
improvised per file.

**Unguarded changes.** Some things neither the tests nor the suite can see. Those still get made,
and they go in the findings naming what changed and what to look at.
