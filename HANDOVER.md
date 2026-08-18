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
- **Naming and running by hand.** A sequence can be named - optional, and it looks
  optional - and the name then stands in for the sentence wherever it is listed.
  `Run now` starts one as if its trigger had fired, so an airlock can be tested
  without walking to the button.
- **Headless screenshots.** `ScreenshotTests` writes PNGs to `%TEMP%\reactor-*.png`
  without opening a window. Useful for reviewing the design without launching.

21 tests, all green.

## What is not built

Ordered roughly by how much they hurt.

1. **Calling one sequence from another.** Naming exists, but nothing consumes a name
   yet - there is no step that runs a named sequence, so `CalledTrigger` is reachable
   only through `Run now`. This is what makes a shared "Let me out" worth having.
2. **Authoring `only if` and `once`.** The model handles them and the editor renders
   them if present, but nothing creates one.
3. **"Why didn't it happen?"** The runtime records skipped steps structurally
   (`SequenceRunner.LastSkippedStep`); nothing surfaces it yet.
4. **The Space Engineers host.** Not started. It implements `Rendering/` against the
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

Either calling a named sequence from a step - which is what naming was for, and is
the last part of the language that is modelled but unreachable - or start the Space
Engineers host. The sooner a second target exists, the sooner the component contract
gets tested by something that is not Avalonia.

## Working notes

```
dotnet test Reactor.slnx                          # 21 tests
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
