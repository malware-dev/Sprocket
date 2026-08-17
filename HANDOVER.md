# Handover

Where Reactor stands, what to do next, and what is still undecided. Read
[README.md](README.md) first for the language and the reasoning behind it; this
document is the state of play.

## In one paragraph

Reactor is an automation system for people who have never programmed, aimed at
becoming a Space Engineers mod. The language is settled and proven by tests: sequences
of steps that each wait for their own action to finish, started by `When` or `Every`.
`Reactor.Core` holds the model, the runtime **and the whole editor**, and compiles
under C# 6 / netstandard2.0 behind a banned-API gate, so it is already legal for SE to
compile in-game. The Avalonia app is a prototype host that renders the editor and
simulates a ship; it is throwaway in principle, and the SE target reimplements only
its `Rendering/` folder.

## What works

- **The model and runtime.** Sequences, ordered steps, blocking on completion,
  `only if` guards, `once` / `after` gates, named sequences, live status.
- **The airlock, both ways.** `tests/Reactor.Core.Tests/AirlockTests.cs` builds it as
  a cycle sequence and as a mutual exclusion, and asserts on every beat of an 80-beat
  run that both doors are never open at once — with no safety feature in the language.
- **The editor**, entirely in Core: navigation stack, ship browser with search and
  kind filters, block pickers that only offer combinations that could run, choice
  pickers, the sentence view with live state.
- **The prototype host.** Simulated ship where doors travel and vents cycle, a poke
  strip standing in for the world, and one renderer per component.
- **Headless screenshots.** `ScreenshotTests` writes PNGs to `%TEMP%\reactor-*.png`
  without opening a window. Useful for reviewing the design without launching.

14 tests, all green.

## What is not built

Ordered roughly by how much they hurt.

1. **Naming a sequence.** The model supports it and the title falls back to the whole
   sentence, so the airlock reads as "When Airlock Button is pressed → close Airlock
   Inner Door (+4 more)" instead of "Let me out". Most visible gap.
2. **Run this now.** A sequence with a `CalledTrigger` has nothing to start it, and
   testing an airlock means walking to the button.
3. **Authoring `only if` and `once`.** The model handles them and the editor renders
   them if present, but nothing creates one.
4. **"Why didn't it happen?"** The runtime records skipped steps structurally
   (`SequenceRunner.LastSkippedStep`); nothing surfaces it yet.
5. **The Space Engineers host.** Not started. It implements `Rendering/` against the
   same `Reactor.Ui` components and supplies an `IWorld` over the real grid.

## Known problems

- **Picking a block can empty the list.** After choosing a Button, the picker narrows
  to blocks sharing an event with it — which is nothing — so everything vanishes and
  it reads like a bug. Correct behavior, wrong presentation: incompatible blocks
  probably want to be shown greyed with a reason rather than disappearing.
- **A jammed device stalls a sequence silently.** Deliberate — proceeding is how a
  stuck door vents a ship — but there is no visible "stuck" state yet, and no way to
  react to one. Design sketched, not agreed: stuck becomes a state after a while, and
  a trigger so recovery is ordinary automation.
- **Momentary signals can be missed.** If the mod runs on SE's `Update10` or
  `Update100`, a one-frame button press between updates never gets sampled. Level-held
  states survive being sampled late; momentary ones do not. Either the host latches
  them, or anything listening for one forces `Update1`. Undecided.
- **Locking is disabling**, so a sequence that stops mid-way can leave a player sealed
  in an airlock behind a disabled door. No answer yet, and it may simply be survivable
  in a game where you can grind a door.

## Open questions

- Should the block picker chain onward automatically after `Next`, or stop and let
  the user look at the sentence? Currently the new-sequence wizard chains
  (blocks → event → sequence); editing an existing slot does not.
- How long is "too long" before a stuck sequence says so — one forgiving global
  default, or per-step? Strong lean toward global; per-step is a number to guess.
- Does SE expose enough "I can't do this" state on vents and doors to detect stuck
  precisely, or does a timer carry all the weight?

## The next thing I would do

Naming a sequence, then run-this-now. Both are small, both are needed before anyone
can test an airlock without a tour of the ship, and together they make the sequence
list readable. After that, the SE host — the sooner a second target exists, the sooner
the component contract gets tested for real.

## Working notes

```
dotnet test Reactor.slnx                          # 14 tests
dotnet run --project src/Reactor.Host.Avalonia    # the prototype
```

- **Core is pinned to C# 6 and netstandard2.0**, with `build/BannedSymbols.txt`
  failing the build on reflection, LINQ, IO, threading and wall-clock time. That is
  not incidental — it is what keeps Core compilable by SE in-game. Host code is
  exempt and uses modern C# freely.
- **The running prototype locks `Reactor.Core.dll`**, so close it before rebuilding.
- **Screenshots** come from `dotnet test --filter Screenshot`, writing to
  `%TEMP%\reactor-*.png`. Cheaper than launching, and it does not steal focus.
- **Git identity is repo-local** (`Malware` / the `malware-dev` noreply address), so
  commits are correctly attributed regardless of the global config. Pushing uses Git
  Credential Manager and does not care which `gh` account is active; only `gh` API
  commands do.

## Things not to quietly undo

These were argued out rather than guessed, and the reasoning is in the README:

- Steps block by default. Duration waits are a substitute for a completion signal.
- No interlock, no "airlock mode", no primitive for anything expressible as a trigger
  plus steps. Reactor is an automation system, not a set of finished automations.
- Events and states stay structurally separate.
- Beats come from the game's frame counter and are never counted locally.
- Back means back. Moving on is its own action.
