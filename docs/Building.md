# Building and testing

How to build the mod, run its tests and its simulator, and see a change in the game. The projects themselves are set
out in the [Overview](Overview.md).

## What a checkout needs

- Windows, with Space Engineers installed.
- The .NET SDK: the mod, the simulator and most tests target .NET Framework 4.8, and the wiki tool and its tests .NET 10.
- [MDK2](https://github.com/malforge/mdk2), whose packages build and pack the mods. The build restores them itself.

Each mod project (the mod, the font mods and Rich Text Panels) reads an `mdk.local.ini` beside it, which is kept out of
git because it belongs to the machine:

```ini
[mdk]
; Where the packed mod is written; auto is the game's own Mods folder.
output=auto
; The game's Bin64, which the mod is compiled against; auto finds a Steam install.
binarypath=auto
```

The simulator and the tests run the mod outside the game, so they need the game's own assemblies too. They find the game
the same way and copy its assemblies into `bin`. Those are Keen's, and stay out of the repository.

## Building

```
dotnet build src/Sprocket.Host.SpaceEngineers
```

This builds the mod and packs it into the Mods folder: a build is a deploy. The folder is named for the branch where
`mdk.ini` has a section for it, so `main` packs as *Sprocket Automation (Bleeding Edge)*; a branch without one packs
under the project's name.

To build without packing, which suits every build meant for anything but the game:

```
dotnet build src/Sprocket.Host.SpaceEngineers -p:MdkBuildConfiguration=none
```

Pack only while the game is closed or no world with the mod is loaded: packing replaces the mod's folder, and a world
running from it can find the folder empty. A second checkout on the same machine gets an `output=` of its own in each
`mdk.local.ini`, so its builds never land on the first one's mod.

## Testing

```
dotnet test -p:MdkBuildConfiguration=none
```

runs every test project. What each one proves is in the [Overview](Overview.md#the-tests). A test about a screen presses
and reads by name, the way a player does, so a test still passes when only the layout moves.

**The reference suite** holds a handful of screens still. Each is walked to from an empty construct and dumped whole,
and the dumps are committed, so `git diff` is the comparison:

```
dotnet run --project src/Sprocket.Host.SpaceEngineers.Sim -- --suite tests/Reference/suite.txt tests/Reference/frames
```

An empty diff means every one of those screens is as it was.

**The texts and the codex** have checks of their own, run from the repository's root:

```
python .claude/skills/writing/codex-check.py
python .claude/skills/writing/tells.py src/Sprocket.Host.SpaceEngineers/Content/Data/Codex/en/*.md
```

The build itself reports a text that cannot be said, a translation out of step with English, and a codex page without
its partner or with a link to no page.

## The simulator

Run with no arguments, the simulator opens a window to drive the editor in, as in the game. From the command line it
draws, dumps and measures:

| Command | What it does |
|---|---|
| `--shot <file.png> [at <w> <h>] <script>` | Plays a script and draws the screen it ends on, at 1920 by 1080 unless told otherwise |
| `--dump <script>` | Plays a script and writes the screen it ends on as text |
| `--suite <suite.txt> <folder>` | Dumps every screen a suite names into the folder |
| `--rich <file.md> <file.png> <width> [tip\|description\|codex]` | Draws a page of markdown-lite as one of the editor's text styles draws it |
| `--panel <full.md> <brief.md> <file.png> <width> <height>` | Draws a Rich Text panel's text as the panel shows it |
| `--profile` | Times saving and loading programs, worst cases included; meaningful only in Release (`-c Release`) |
| `--content` | Says whether the game is on this machine and what the simulator can read of it |
| `--extract [path]` | Copies what the simulator draws from the game, its letterforms and block artwork, into `%LocalAppData%\Sprocket\SimContent`, once |
| `--state <world>` | What a saved world remembers of Sprocket's runtime, read out of its binary |

A script is what is done to the screen, in order: `press <text>` presses whatever says that text, a symbol included,
and `type "words"`, `enter`, `escape`, `clear`, `tick <n>`, `move <x> <y>`, `drag <x> <y> <x> <y>` and `scroll <x> <y> <n>`
do what they say. A script that is kept presses by name, never by position, so it still lands when a layout moves.

The simulator draws the game's font with stand-in widths, so where a measurement matters, check it against `--content`
or the game itself.

## In the game

After a deploy, what the game sees of a change depends on what changed:

| Changed | Seen after |
|---|---|
| The mod's code | Reloading the world: the game compiles the mod's code as a world loads |
| The texts, `Data/Localization/*.resx` | Reloading the world: Sprocket reads them itself as a world loads |
| A codex page | Opening the page again: the codex reads its pages from the mod's folder when it shows them |
| The codex's `index` | Reloading the world: the codex reads its index once a world |
| Anything else under `Content`: fonts, textures, `.sbc` definitions | Restarting the game: it reads them once, as it starts |

## Editing a saved world by hand

A save keeps its sector twice. `SANDBOX_0_0_0_.sbs` is XML and readable; `SANDBOX_0_0_0_.sbsB5` is a binary copy
beside it, and the game reads the binary whenever it is there. So an edit to the XML takes effect only once the
`.sbsB5` is deleted: the game writes a fresh one from the XML at its next save.

The binary holds its strings as they are, so a replacement of exactly the same length can be made in both files, and
each stays valid. Anything that changes a length goes through the XML, with the binary deleted.

Unload the world first, and copy both files somewhere before touching either.

A stored program can be read straight out of the XML, and the version on its first line says which build wrote it:

```
grep -a "sprocket " SANDBOX_0_0_0_.sbs
```

`tools/rename-world.py` points a saved world at the mod under a new name, changing every place the save names it.
