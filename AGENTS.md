# Sprocket

Sprocket Automation is a Space Engineers mod: an automation editor inside the game, the script language its programs
are stored in, and a simulator that runs the real mod outside the game for tests. The game compiles the mod from its
source when a world loads, as C# 6 under a whitelist. Everything else here is ordinary .NET.

## Setup

- Windows, Space Engineers and the .NET SDK. Lumel, the UI framework, and the network mixin are source packages from
  the public malforge feed. A restore that fails on a `Mal.*` package means the machine lacks the feed; CONTRIBUTING.md
  has the one-time setup.
- The simulator draws with the font mods Sprocket requires, read from Steam's Workshop folder. Without them it stops
  and says so: load a world with Sprocket in the game once, which downloads them.
- An `mdk.local.ini` beside a project (gitignored) names the game's `binarypath=` and, for the mod, where it deploys
  (`output=`). Without one, the Steam install and the game's own Mods folder are used. A new worktree has none: copy
  the main checkout's, with `output=` pointing at a scratch folder unless that worktree is meant to deploy.

## Commands

Run from the repository root.

```
dotnet build src/Sprocket.Host.SpaceEngineers -p:MdkBuildConfiguration=none   # compile the mod
dotnet build src/Sprocket.Host.SpaceEngineers                                 # compile it and deploy it into the game
dotnet test -p:MdkBuildConfiguration=none                                     # every test project in Sprocket.slnx
./tools/Test.ps1                                                              # the same, split into shards run side by side
dotnet test tests/Sprocket.Core.Tests -p:MdkBuildConfiguration=none --filter "FullyQualifiedName~AbortTests"
```

One test: `--filter "FullyQualifiedName~AbortTests.ABareAbortEndsTheRunSilently"`.

The simulator is built once, then run without building:

```
dotnet build src/Sprocket.Host.SpaceEngineers.Sim -p:MdkBuildConfiguration=none
dotnet run --no-build --project src/Sprocket.Host.SpaceEngineers.Sim -- --content
dotnet run --no-build --project src/Sprocket.Host.SpaceEngineers.Sim -- --suite tests/Reference/suite.txt tests/Reference/frames
```

`--content` says what this machine has of the game's content; without it the simulator draws with stand-in fonts, so
it is checked before a picture is judged. `--suite` rewrites `tests/Reference/frames`, and an empty `git diff` there
means no screen moved.

Inspection and cleanup, with `jb` from `dotnet tool install -g JetBrains.ReSharper.GlobalTools`:

```
jb inspectcode Sprocket.slnx --swea --format=Sarif --output=<file> --severity=WARNING --properties:MdkBuildConfiguration=none
jb cleanupcode Sprocket.slnx --profile=Narrow
```

Sarif, because the Xml report drops some line numbers. Cleanup runs with the `Narrow` profile only: the default
profile restyles every file.

## Deploys and the game

- Every build of the mod packs it into the game's Mods folder, including the builds that `dotnet test`, the simulator
  and `jb inspectcode` start. Pass `MdkBuildConfiguration=none` unless deploying is the point. On `update` and
  `bleeding-edge` the mod deploys as "Sprocket Automation (Bleeding Edge)", and on `main` as the stable mod; `mdk.ini`
  maps branches to folders.
- `main` holds what is published on the stable Workshop item, and `bleeding-edge` what is published on Bleeding Edge,
  nothing more. New code is made on `update`, and a published branch takes it only as it is published.
- No work is done on `update` itself. Each piece of work has its own branch from `update`, and is merged into it only
  when it is done and reviewed, so `update` holds only finished work.
- Nothing is released by accident. Before any upload, the maintainer is shown everything it carries that the item
  does not have yet: every commit (`git log --oneline origin/<published branch>..<source>`) and the format version it
  writes beside the one published. He approves that content, not only the act of uploading. An upload that would carry
  more than he asked for is not made: it waits, or is built from the published branch with only what was asked for.
- Only `main` builds into the stable folder; every other local build that deploys goes into the Bleeding Edge folder,
  which is experimental by nature. So a local folder is never uploaded as it stands: an upload is built from its
  published branch at the approved commit, and the packed folder is checked against that commit before it goes out.
- A pack clears the deployed folder before it writes, so a pack that fails on a file the running game holds leaves the
  mod half deployed. Check the folder after a failed pack.
- Never close, kill or restart a game the developer is running. After a deploy, say what takes it in: code and the
  mod's own texts (`Content/Data/Localization/*.resx`) need a quickload; anything else under `Content` (textures,
  `.sbc` definitions, fonts) is read once at start and needs the game restarted.
- Before changing a world save or anything in the game's user data folder, ask the developer, who may want to save
  first. Unload the world and copy the files before touching them.
- A save keeps its sector twice, `SANDBOX_0_0_0_.sbs` (XML) and `SANDBOX_0_0_0_.sbsB5` (binary), and the binary wins:
  an edit to the XML takes only once the `.sbsB5` is deleted, and the next save writes a new one. A blueprint's
  `bp.sbcB5` wins over its `bp.sbc` the same way. A replacement of exactly the same length can be made in both files.
- `grep -a "sprocket " SANDBOX_0_0_0_.sbs` prints the first line of each stored program. Its version says which format
  draft wrote it.

## Every change

- Sprocket never lets a player do or see more than the game itself allows them.
- Sprocket runs inside the game's update loop, on servers with many players and constructs. Nothing runs per tick or
  per frame without a reason written beside it, repeating work joins the world's `Rota`, and a claim about cost is
  proved by a test that counts.
- Before adding a way to report, store, show or choose something, find the one the repository already has and add a
  case to it. A new thing looks and behaves like its nearest sibling: its control, mark, wording and what a press
  opens. A behavior added to one screen goes to the screens of the same kind in the same change.
- Anything a program names that does not exist is an error the player sees where it can be mended, never ignored.
- Change only the behavior that was asked for. Where a change shows two behaviors that disagree, keep both and raise it.
- A failing test is a finding: fix its cause. A test is never weakened, shortened or deleted to pass, and a gap in the
  tests is closed with a test, simulator support included.
- A finding from a review or an inspection is fixed, or raised as a question with a recommendation. Who wrote the code
  is never a reason to leave it.

## Stored programs and the language

- A program a player saved keeps loading in every later build. A change to what a stored program says or means, a
  renamed id included, comes with its upgrade in the same commit.
- Anything else Sprocket writes to disk or a save, and anything added to what a construct's block stores, is agreed
  before it is built, with the maintainer's go-ahead.
- The stable release writes a finished format version, without `alpha`, and Bleeding Edge writes the same until the
  next change to the language. That change opens the next version's first draft: after a stable `sprocket 2`, the
  first language change makes Bleeding Edge write `sprocket 3 alpha 1`.
- `docs/Language.md` is the language's spec. A language change lands there first, its exact wording approved, before
  any reader or writer code for it. Where code and spec disagree, the code is suspect: raise it, and never edit the
  spec to match.

## Lumel

- Lumel, the UI framework, and `Mal.MdkModMixin.Network` are packages. Their code is not in this repository and is not
  changed here.
- A control, layout or drawing primitive with nothing of Sprocket in it belongs in Lumel, with its colors and sizes as
  properties. A common control Lumel lacks is requested in an issue on Lumel's repository, never built here.

## Text

- Everything in the repository is US English (color, behavior), identifiers included. Words from the game's own data
  keep the game's spelling. No em or en dashes anywhere: a hyphen, or a different sentence.
- Comments, commit messages, docs and issues are read by somebody who was not in the conversation. They say what is
  true now, in the present tense, with nothing of how it got there.
- English, in `MyTexts.resx` and `MyTextsCodex.resx`, is the original. Each language is a pair of files beside it
  named for its culture, such as `MyTexts.no.resx`; adding a language is adding those files.
- A change to an English text changes every translation in the same change: rewritten by whoever can write that
  language, or otherwise removed from it. A removed text shows in English until it is translated, and the build lists
  it (`LOC006`). A translation is never left saying what the English no longer says.
- A change a player can see keeps the codex true, in every language it has.
- `docs/Language.md` is the one document in the repository beside `README.md` and `CONTRIBUTING.md`. A change to
  something one of them describes fixes it in the same change. Planning goes in issues. Agent rules live in this file.
- Only rules the maintainer decided go into this file. A decision no rule covers is told to
  the developer as a judgement call, never written in here as a rule.

## Keen's game

- No game files are copied into the repository. A value transcribed from the game's data, such as a color or a font
  metric, cites where it was read and is never adjusted.
- No DLC content (textures, models, skins) is used, ever. Keen prohibits it in mods.

## Names that mislead

- The player's slot is the model's `Part` (`Part`, `PartFor`, `FindPart`). `SentenceSlot` is a pressable part of a
  sentence on screen.
- A script given to the simulator (`--shot`, `--dump`, `suite.txt`) is screen actions such as `press` and `type`, not
  the script language.

## Tests

- A test in the simulator comes first: it runs the real mod, and fastest. What only the game can show becomes a game
  check. The simulator models no power and no conveyors, and gas only as far as a vent's flow.
- A screen change comes with presentation tests that press and read by name. Tests cover behavior and what a control
  promises; a test that restates one line of a screen is not added.
- `tests/Sprocket.GameTests` runs inside the game and on a dedicated server through its `Run-*.ps1` scripts, whose
  headers say what each needs. `dotnet test` does not run it.

## Commits

- Before every commit: `dotnet test` passes, and the inspection above runs over the files the change touches, with
  every finding fixed or decided by the developer.
- A sweep (a rename, a move, a reformat) is a commit of its own, with other work committed before it.
- A commit message's first line is one sentence saying what is now true of the product, in the present tense, with no
  prefix and no closing period. The body says what a player or a developer now meets.

