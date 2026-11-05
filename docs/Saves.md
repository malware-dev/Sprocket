# Saves and storage

Where Sprocket keeps everything, what format each is in, and what must never change, since a player's saved world
outlives every build.

## Where everything is kept

| What | Where | Written | By |
|---|---|---|---|
| A construct's program | The mod storage of its programmable block, the unit's hero, under `BlockStorage.ProgramKey` | When the program may have changed, and as the world saves | `World/SprocketUnit.cs`, `Storage/Scribe.cs`, `Storage/Reader.cs` |
| What is switched off, and why | The same block, under `BlockStorage.StateKey`, as a `BlockState` | With the program | `Storage/BlockSwitches.cs` |
| What the world remembers while it runs: Sprocket's clock, every run and every watch, what each installation holds in its states, owners, pauses, each player's settings (lengths in ticks or seconds, the pickers' eye, how colors are given and the colors last chosen) | A file named `state` among Sprocket's files for the world, kept on the carrier | As the world saves, on the server, where anything changed | `Memory/`, `World/Carrier.cs` |
| The library | The game's Storage folder on the player's own computer: `sprocket-bp.index`, and a `sprocket-bp-*.cog` for each blueprint | When a blueprint is kept | `Storage/Shelf.cs` |
| Backups | Beside the world's save: `backups.ini`, and a `backup-N.cog` for each | When a construct's automation is lost to damage | `Storage/Backups.cs` |
| The world's settings | `sprocket.ini`, in the game's Storage folder and in the world's | When an admin changes them | `World/Settings.cs` |
| The clipboard | `sprocket-clipboard.txt` in the game's Storage folder | On every copy | `Storage/ClipboardFile.cs` |

**The carrier** (`World/Carrier.cs`) is an entity of Sprocket's own, saved with the world and tied to no block: no
model, no physics, never sent to a player's game. World storage misses the game's own backups of a world, and the
carrier is saved into every one. It holds Sprocket's files for the world as one block of text, `files 1` and then each
file after an `=` line (`Storage/CarrierFiles.cs`), read whole or not at all.

## The program, as text

A program is stored as a script in Sprocket's language ([Language](Language.md)), written by `ProgramWriter.Save` and
read by `ProgramReader.Load` through the `Lexer` and `Parser` in `Storage/`. Its first line says which format wrote it:

```
sprocket 1 alpha 21
```

`ScriptFormat.Version` and `ScriptFormat.Draft` hold the numbers this build writes.

A text that fails to read is read as nothing. The program is marked not intact and kept as the text it was, and
nothing writes over it: the block keeps the text until a person deals with it. A text from a format this build
neither writes nor upgrades is refused the same way, and marked incompatible.

## The switches

Beside the program, the block keeps only what a copy built from a blueprint of it needs: each logic system and
installation switched off, and why, where Sprocket switched it off. It is a `BlockState` (`Memory/BlockState.cs`),
packed as the world's memory is, and nothing at all where everything is on. Owners, pauses and runs are the world's,
never the block's.

A block written by an earlier build holds its switches as lines of words, one `off` line for each. Such a block is read
by `Storage/EarlierSwitches.cs` and written packed at its next change.

## Changing the format

A program's text changes only together with an upgrade for it, and the rules and steps for a format change are in
[Upgrades](Upgrades.md). Each change is a `FormatChange` in `Storage/Upgrades/`, and `FormatUpgrades` keeps them in the
order they arrived. A program is written in the current format at its next save.

## What the world remembers

`Memory/` is the world's memory: protobuf, through the game's own serializer, as base 64 (`Memory/GameBinary.cs`).
`WorldMemory` is the root, holding the clock, the constructs, the players, the blueprints shared in the world and the
players barred from sharing. Its member 4 is retired.

**A member's number is the format.** The memory carries no version: a `ProtoMember` number, once used, is never used
again and never given another type, and a member dropped leaves its number unused. Reasons and causes are stored as the
numbers set on each value of `SequenceProblem` and `Cause`, held as whole numbers so that a build meeting a newer one reads
it as none. Memories and blocks from earlier builds hold words for them instead, read through `Memory/EarlierSpellings.cs`.

A memory that fails to read is said in the log and set aside: the world starts fresh, and its next save writes over
it.

## In multiplayer

The server keeps everything above that belongs to the world: the blocks' storage, the carrier and its memory, the
backups and the world's settings. A player's game keeps a copy of each construct's program and switches, filled by the
server as it lends the construct to that player's editor, together with the construct's memory, so the editor shows
its runs. The copy runs nothing. The library, the view preferences and the
clipboard belong to each player's own computer.

## What must never change

Each of these is written into saves, and a change reads an old world as empty or wrong, often without a word:

- **The storage keys**: `BlockStorage.ProgramKey` and `StateKey`, and `Carrier.Key`. Each is also claimed in
  `Content/Data/EntityComponents.sbc`, without which the game drops it from the save.
- **The world variable** naming the carrier, `sprocket.carrier`.
- **Ids written into the game's own data**: the toolbar action `Sprocket_Trigger` and the LCD app `Sprocket_Status`.
  A program also names `Sprocket_AutoClose`, the delay slider Door Auto-Close adds to every door.
- **Every word of the program text**: the keywords (`Storage/Keywords.cs`), `ext`, `toolbar`, and `s` for seconds,
  each changing only with an upgrade.
- **The words of the earlier switches**, such as `off`, `instance` and `slot empty`, which blocks of earlier builds
  still hold.
- **The memory's member numbers**, as above, and **the numbers set on `SequenceProblem` and `Cause`**: a value is
  only ever added after the last.
- **The words of `Memory/EarlierSpellings.cs`**, which saves from earlier builds hold.
- **The file names**: `state`, `files 1`, and every file in the table at the top.

## Tools

The simulator (`src/Sprocket.Host.SpaceEngineers.Sim`) reads a saved world's memory, `--state <world>`, writing each
member by name. `--profile` times saving and loading programs of every size, and is the guard on the parser's speed;
it means something only in a Release build.

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
