# Overview

Sprocket Automation is a Space Engineers mod that lets a player automate a ship from any programmable block: logic
systems of sequences and triggers, built on screens in the game rather than written as code. This guide is for
people working on Sprocket itself. It explains how the repository is put together and how the parts work, as they
are in the code today.

The guide's other pages:

- [The runtime](Runtime.md): how what a player built runs, from the game's update to triggers firing.
- [Saves and storage](Saves.md): where everything is kept, its formats, and what must never change.
- [Language](Language.md): the script language Sprocket stores its programs in.
- [Codex](Codex.md): the codex every tip shows.

## The mod

`src/Sprocket.Host.SpaceEngineers` is everything that runs in the game, compiled by the game itself when a world
loads. Its folders each hold one capability whole:

| Folder | What it holds |
|---|---|
| `World` | The game as Sprocket sees it: blocks and the values, signals and actions they offer, constructs, and the catalogue of kinds |
| `Framework/Game/Blocks` | What Sprocket knows about each kind of block, one file a kind: a door's states, a piston's position, a vent's pressure |
| `Sequences` | The model a player builds: logic systems, installations, slots, sequences, steps and triggers, and every edit made to them |
| `Framework/Running` | Running sequences: the world's rota, waits, faults and statuses |
| `Storage` | The program as text, its reader and writer, the upgrades that load older formats, backups and the files kept on the player's computer |
| `Framework/Storage/Memory` | What a world remembers while it runs, saved with it: Sprocket's own clock, each player's settings, each construct's state |
| `Network` | Multiplayer: the server lends each Sprocket to one editor at a time, and the editor's changes travel back |
| `Framework/Text` | Where words come from: the mod's texts in each language, and the codex's pages |
| `Features/StatusPanel` | The status panel, an app an LCD can run to show every logic system's state |
| `Features` | Every screen of the editor, one folder a screen or a piece shared between screens |
| `Content` | What the game reads as data: definitions, textures, and the texts and the codex in `Data/Localization` |

The model and the runtime hold ids and never words. Every word a player reads is looked up in the texts by the screen
that shows it.

## The projects beside it

| Project | What it is |
|---|---|
| `src/Lumel` | The user interface framework: elements, layout, controls, text, drawing. A shared project, compiled into each mod that uses it, and it knows nothing of Sprocket, so any mod can use it as it is. A submodule of the `malware-dev/Lumel` repository. Its `Sim` folder is a shared project of its own, for a harness and never a mod: it draws a Lumel screen on a desktop from the game's content and a mod's, in the game's own fonts, and writes it as a picture |
| `src/Sprocket.Host.SpaceEngineers.Sim` | The simulator: runs the real mod outside the game, over blocks it makes up, and draws its screens to pictures. The tests and every check of what a screen looks like go through it |
| `src/Sprocket.Localization.Generator` | A source generator that writes a class from each of the mod's text files at build time, `Texts` from `MyTexts.resx` and `Codex` from `MyTextsCodex.resx`, and reports every text that cannot be said |
| `src/Sprocket.Standards` | An analyzer holding the mod to a rule the compiler can check: nothing is `internal`, since the game compiles a mod into one assembly where that hides nothing (STD001) |

`art` holds the sources the logo, the color picker, the shadow and the stripes are drawn from, each with the script that
draws it.
`build` holds what the projects running the mod outside the game share: the reference to the game's own assemblies.

## What it reads from outside

The simulator draws with the font mods Sprocket requires, read from Steam's Workshop folder, where the game puts them
once it has loaded a world with Sprocket: Maltek Symbols, Maltek Mono, and Space Engineers Font Styles, which has bold,
italic and bold italic cuts of the game's own face. Where one is missing, the simulator stops and says so.

## The tests

| Project | What it proves |
|---|---|
| `tests/Sprocket.Core.Tests` | The model, the runtime, storage and the blocks, run through the simulator, below the screens |
| `tests/Sprocket.Host.SpaceEngineers.Tests` | The mod tested on its own, outside the simulator: a player's settings |
| `tests/Sprocket.Host.SpaceEngineers.Presentation.Tests` | The screens: each test presses and reads by name, as a player would |
| `tests/Sprocket.Localization.Generator.Tests` | The generator, run as the compiler runs it |
| `tests/Sprocket.Standards.Tests` | The analyzer, run as the compiler runs it, over code written in the tests |
| `tests/Sprocket.Benchmarks` | How long laying out a screen and reading a program take |

## Building and testing

How a machine is set up, built and tested is in [CONTRIBUTING.md](../CONTRIBUTING.md).

## What shapes the code

**The game compiles the mod.** Space Engineers compiles a mod from its source when a world loads, with C# 6 and only
the parts of .NET its whitelist allows. A refactor that is natural in modern C# is often illegal here. The MDK
analyzers know the real whitelist and fail the build on anything outside it. The simulator, the tests and the tools
are never compiled by the game and use modern C#.

**One assembly.** A mod is one assembly, so `internal` hides nothing: a type or member is public or private.

**Two things only the game can do.** Drawing on screen (`Lumel/Drawing/IScreenSpace`) and reading toolbars
(`Framework/Game/Wiring/ISlots`) are interfaces the mod asks through. The simulator answers both, which is how it records a frame
rather than drawing it. Everything else the simulator gives the mod for real, through proxies over the game's own
types.

**Drawing runs sixty times a second.** A screen is drawn every frame and changes perhaps twice a minute, so nothing
measures text while drawing and nothing lays out unless something changed. A property that changes a size or a
position invalidates the layout; a color changes nothing else.

**Repeating work joins the rota.** One rota a world runs every Sprocket on it at three periods, each tick, every ten
and every hundred, and spreads machines built alike across the ticks. Nothing keeps a clock of its own.

**Saves outlive builds.** A program is stored as text in the world, and a format it was written in earlier loads
through the changes in `Framework/Language/Format/Upgrades/` that arrived after it, rather than being misread. What the text says
changes only together with an upgrade for it. [Saves and storage](Saves.md) has the rest.
