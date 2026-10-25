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
| `Blocks` | What Sprocket knows about each kind of block, one file a kind: a door's states, a piston's position, a vent's pressure |
| `Sequences` | The model a player builds: logic systems, installations, slots, sequences, steps and triggers, and every edit made to them |
| `Runtime` | Running sequences: the world's rota, waits, faults and statuses |
| `Storage` | The program as text, its reader and writer, the upgrades that load older formats, backups and the files kept on the player's computer |
| `Memory` | What a world remembers while it runs, saved with it: Sprocket's own clock, each player's settings, each construct's state |
| `Network` | Multiplayer: the server lends each Sprocket to one editor at a time, and the editor's changes travel back |
| `Localization` | Where words come from: the mod's texts in each language, and the codex's pages |
| `LcdApps` | The status panel, an app an LCD can run to show every logic system's state |
| `Features` | Every screen of the editor, one folder a screen or a piece shared between screens |
| `Content` | What the game reads as data: definitions, textures, and the texts and the codex in `Data/Localization` |

The model and the runtime hold ids and never words. Every word a player reads is looked up in the texts by the screen
that shows it.

## The projects beside it

| Project | What it is |
|---|---|
| `src/Lumel` | The user interface framework: elements, layout, controls, text, drawing. A shared project, compiled into each mod that uses it, and it knows nothing of Sprocket, so any mod can use it as it is |
| `src/Sprocket.Gravity` | A shared project: how big a gravity generator's field must be to cover part of its grid. Sprocket and Gravity Field Fit build it in |
| `src/Sprocket.DoorAutoClose` | A mod of its own, Door Auto-Close: the slider on every door that closes it a set time after it finishes opening |
| `src/Sprocket.MultiRename` | A mod of its own, Multi-Rename: the game's own Name box renames several selected blocks at once |
| `src/Sprocket.GravityFit` | A mod of its own, Gravity Field Fit: the buttons on a box gravity generator that size its field to its grid or to its pressurized rooms |
| `src/Sprocket.Host.SpaceEngineers.Sim` | The simulator: runs the real mod outside the game, over blocks it makes up, and draws its screens to pictures. The tests and every check of what a screen looks like go through it |
| `src/Sprocket.Localization.Generator` | A source generator that writes a class from each of the mod's text files at build time, `Texts` from `MyTexts.resx` and `Codex` from `Codex.resx`, and reports every text that cannot be said |
| `src/Sprocket.Standards` | Analyzers holding the mod to two of its rules: nothing is `internal` (STD001), and nothing goes unused (STD002) |
| `src/Maltek.Sans`, `src/Maltek.Mono`, `src/Maltek.Symbols` | Font mods: the editor's lettering, its code face, and the symbols its buttons and marks are drawn in. Each is a mod of its own that Sprocket requires |
| `src/SpaceEngineers.FontStyles` | A font mod with bold, italic and bold italic cuts of the game's own face, for any display mod to require |
| `src/RichTextPanels` | A mod of its own: a Rich Text app for LCD panels, showing a panel's text as markdown-lite, built on Lumel |
| `tools/rename-world.py` | Points a saved world at the mod under a new name |

`art` holds the sources the fonts, the symbols and the logo are drawn from, each with the script that draws it.
`build` holds what the projects running the mod outside the game share: the reference to the game's own assemblies.

## The tests

| Project | What it proves |
|---|---|
| `tests/Sprocket.Core.Tests` | The model, the runtime, storage and the blocks, run through the simulator, below the screens |
| `tests/Sprocket.Host.SpaceEngineers.Tests` | The mod tested on its own, outside the simulator: a player's settings |
| `tests/Sprocket.Host.SpaceEngineers.Presentation.Tests` | The screens: each test presses and reads by name, as a player would |
| `tests/Sprocket.DoorAutoClose.Tests` | Door Auto-Close, over the simulator's doors |
| `tests/Sprocket.Localization.Generator.Tests` | The generator, run as the compiler runs it |
| `tests/Sprocket.Standards.Tests` | The analyzers |
| `tests/Sprocket.Benchmarks` | How long laying out a screen and reading a program take |

## Building and testing

A checkout needs Windows with Space Engineers installed, and the .NET SDK. [MDK2](https://github.com/malforge/mdk2)
builds and packs the mods; the build restores it. Each mod project reads an `mdk.local.ini` beside it, kept out of git
because it belongs to the machine:

```ini
[mdk]
; Where the packed mod is written; auto is the game's own Mods folder.
output=auto
; The game's Bin64, which the mod is compiled against; auto finds a Steam install.
binarypath=auto
```

```
dotnet build src/Sprocket.Host.SpaceEngineers                                # builds and packs into the Mods folder
dotnet build src/Sprocket.Host.SpaceEngineers -p:MdkBuildConfiguration=none  # builds without packing
dotnet test -p:MdkBuildConfiguration=none                                    # every test project
```

Pack only while no world with the mod is loaded: packing replaces the mod's folder.

## What shapes the code

**The game compiles the mod.** Space Engineers compiles a mod from its source when a world loads, with C# 6 and only
the parts of .NET its whitelist allows. A refactor that is natural in modern C# is often illegal here. The MDK
analyzers know the real whitelist and fail the build on anything outside it. The simulator, the tests and the tools
are never compiled by the game and use modern C#.

**One assembly.** A mod is one assembly, so `internal` hides nothing: a type or member is public or private.

**Two things only the game can do.** Drawing on screen (`Lumel/Drawing/IScreenSpace`) and reading toolbars
(`World/ISlots`) are interfaces the mod asks through. The simulator answers both, which is how it records a frame
rather than drawing it. Everything else the simulator gives the mod for real, through proxies over the game's own
types.

**Drawing runs sixty times a second.** A screen is drawn every frame and changes perhaps twice a minute, so nothing
measures text while drawing and nothing lays out unless something changed. A property that changes a size or a
position invalidates the layout; a color changes nothing else.

**Repeating work joins the rota.** One rota a world runs every Sprocket on it at three periods, each tick, every ten
and every hundred, and spreads machines built alike across the ticks. Nothing keeps a clock of its own.

**Saves outlive builds.** A program is stored as text in the world, and a format it was written in earlier loads
through the changes in `Storage/Upgrades/` that arrived after it, rather than being misread. What the text says
changes only together with an upgrade for it. [Saves and storage](Saves.md) has the rest.
