# Overview

Sprocket Automation is a Space Engineers mod: a player automates a ship from any programmable block, building logic
systems of sequences and triggers on screens in the game. This guide is for people working on Sprocket itself.

The other pages: [the runtime](Runtime.md), [saves and storage](Saves.md), [upgrades](Upgrades.md), [the
language](Language.md) and [the codex](Codex.md). How to set up, build and test is in
[CONTRIBUTING.md](../CONTRIBUTING.md).

## The mod

`src/Sprocket.Host.SpaceEngineers` is everything that runs in the game. It has two folders:

- `Framework`, what every feature stands on, which names no feature: `Model` (logic systems, sequences, steps,
  triggers), `Language` (the program's text, read and written), `Running` (sequences running), `Editing` (changes and
  undo), `Game` (blocks, constructs, players, toolbars), `Storage`, `Network`, `Text` and `Ui`.
- `Features`, one folder for each thing a player can name: `LogicSystems`, `Sequences`, `Triggers`, `Slots`,
  `Library`, `StatusPanel` and so on, each holding its screens and its own edits.

`Content` is what the game reads as data: definitions, textures, and the texts and codex in `Data/Localization`.

The model and the runtime hold ids, never words. Every word a player reads comes from the texts, looked up by the
screen that shows it.

## Beside it

- `src/Lumel`: the user interface framework, a shared project that knows nothing of Sprocket. A submodule of
  `malware-dev/Lumel`.
- `src/Sprocket.Host.SpaceEngineers.Sim`: the simulator. It runs the real mod outside the game over made-up blocks and
  draws its screens to pictures. The tests go through it.
- `src/Sprocket.Localization.Generator`: writes `Texts` and `Codex` from the text files at build time.
- `src/Sprocket.Standards`: an analyzer for the one rule the compiler can check: nothing is `internal` (STD001).
- `art`: the sources of the logo and other pictures, each with the script that draws it.
- `Workshop`: the Workshop page texts, one for the stable release and one for Bleeding Edge.

The simulator draws with the font mods Sprocket requires, read from Steam's Workshop folder once the game has loaded a
world with Sprocket. Where one is missing, it stops and says so.

## The tests

- `tests/Sprocket.Core.Tests`: the model, the runtime, storage and the blocks.
- `tests/Sprocket.Host.SpaceEngineers.Presentation.Tests`: the screens, pressed and read by name, as a player would.
- `tests/Sprocket.Host.SpaceEngineers.Tests`: the mod on its own, outside the simulator.
- `tests/Sprocket.Localization.Generator.Tests` and `tests/Sprocket.Standards.Tests`: the generator and the analyzer,
  run as the compiler runs them.
- `tests/Sprocket.Benchmarks`: how long laying out a screen and reading a program take.
- `tests/Sprocket.GameTests`: checks that only the game can do, run in the game and on a dedicated server.

## What shapes the code

**The game compiles the mod**, from source as a world loads, as C# 6 and only what its whitelist allows. MDK's
analyzers fail the build on anything else. The simulator, tests and tools are ordinary modern .NET.

**One assembly**, so `internal` hides nothing: a type or member is public or private.

**Two things only the game can do**: drawing on screen (`Lumel/Drawing/IScreenSpace`) and reading toolbars
(`Framework/Game/Wiring/ISlots`). The mod asks through interfaces, and the simulator answers both.

**Drawing runs every frame**, while a screen changes rarely, so nothing measures text while drawing and nothing lays
out unless something changed.

**Repeating work joins the rota.** One rota a world runs every Sprocket at three periods (every tick, every ten and
every hundred), and spreads alike machines across the ticks.

**Saves outlive builds.** A program is stored as text, and older formats load through the upgrades in
`Framework/Language/Format/Upgrades`. See [Upgrades](Upgrades.md).
