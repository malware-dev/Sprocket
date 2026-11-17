# Saves and storage

Where Sprocket keeps everything, and what must never change, since a player's saved world outlives every build.

## Where everything is kept

| What | Where | Code |
|---|---|---|
| A construct's program | The mod storage of its programmable block, under `BlockStorage.ProgramKey` | `Framework/Storage/BlockStorage.cs`, `Framework/Game/Grids/SprocketUnit.cs` |
| What is switched off, and why | The same block, under `BlockStorage.StateKey` | `Framework/Storage/BlockSwitches.cs` |
| What the world remembers: the clock, runs, watches, states, owners, pauses, players' settings | A file named `state` among the world's files, kept on the carrier | `Framework/Storage/Memory/`, `Framework/Storage/Carrier.cs` |
| The library | The game's Storage folder on the player's computer: `sprocket-bp.index`, and a `sprocket-bp-*.cog` for each blueprint | `Framework/Storage/LibraryShelf.cs` |
| Backups | Beside the world's save: `backups.ini`, and a `backup-N.cog` each | `Framework/Storage/Backups.cs` |
| Settings | `sprocket.ini`, in the game's Storage folder and in the world's | `Framework/Game/Players/Settings.cs` |
| The clipboard | `sprocket-clipboard.txt` in the game's Storage folder | `Framework/Storage/ClipboardFile.cs` |

**The carrier** is an entity of Sprocket's own, saved with the world and tied to no block: no model, no physics, never
sent to players. The game's own backups of a world miss world storage, but the carrier is saved into every one. It holds
the world's files as one text, `files 1` and then each file after an `=` line (`Framework/Storage/CarrierFiles.cs`),
read whole or not at all.

## The program

A program is a script in Sprocket's [language](Language.md), written by `ProgramWriter.Save` and read by
`ProgramReader.Load` (`Framework/Language`). Its first line says which format wrote it, such as `sprocket 2 alpha 1`.
A text that won't read, or comes from a format this build neither writes nor upgrades, is kept exactly as it was and
never written over, until a person deals with it. Format changes are in [Upgrades](Upgrades.md).

## The switches

Beside the program, a block keeps only what a copy built from its blueprint needs: each logic system and installation
switched off, and why (`Framework/Storage/Memory/BlockState.cs`), and nothing where everything is on. Blocks from earlier
builds hold it as lines of words, read by `Framework/Storage/EarlierSwitches.cs` and written packed at their next change.

## The world's memory

Protobuf through the game's own serializer, as base 64 (`Framework/Storage/Memory/GameBinary.cs`), rooted in
`WorldMemory`. It carries no version: **a member's number is the format.** A `ProtoMember` number, once used, is never
used again or given another type. `SequenceProblem` and `Cause` are stored as the numbers set on their values. Older
saves' words for them are read through `Framework/Storage/Memory/EarlierSpellings.cs`. A memory that won't read is
logged and set aside, and the world starts fresh.

## In multiplayer

The server keeps everything that belongs to the world. A player's game holds a copy of each construct's program,
switches and memory while that player edits it, and runs nothing. The library, the clipboard and view preferences live
on each player's own computer.

## What must never change

Each is written into saves, and changing it reads an old world as empty or wrong, often silently:

- The storage keys `BlockStorage.ProgramKey`, `BlockStorage.StateKey` and `Carrier.Key`, each also claimed in
  `Content/Data/EntityComponents.sbc`, without which the game drops it from the save.
- The world variable `sprocket.carrier`.
- Ids in the game's own data: the toolbar action `Sprocket_Trigger`, the LCD app `Sprocket_Status`, and
  `Sprocket_AutoClose`, the Door Auto-Close mod's delay slider, which programs name.
- Every word of the program text (`Framework/Language/Reading/Keywords.cs`, `ext`, `toolbar`, `s` for seconds),
  changing only with an upgrade.
- The words of the earlier switches (`off`, `instance`, `slot empty`) and of `EarlierSpellings.cs`.
- The memory's member numbers, and the numbers on `SequenceProblem` and `Cause`: a value is only ever added last.
- The file names above, `state` and `files 1`.

## Tools

The simulator reads a saved world's memory with `--state <world>`, naming each member. `--profile` times saving and
loading programs of every size; it means something only in a Release build.

## Editing a saved world by hand

A save keeps its sector twice: `SANDBOX_0_0_0_.sbs` (XML) and `SANDBOX_0_0_0_.sbsB5` (binary), and the game reads the
binary when it's there. An edit to the XML takes only once the `.sbsB5` is deleted; the next save writes a new one. A
replacement of exactly the same length can be made in both. Unload the world and copy both files first.

`grep -a "sprocket " SANDBOX_0_0_0_.sbs` prints the first line of each stored program, whose version says which build
wrote it.
