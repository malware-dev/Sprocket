# Saves and storage

Where Sprocket keeps everything, what format each is in, and what must never change, since a player's saved world
outlives every build.

## Where everything is kept

| What | Where | Written | By |
|---|---|---|---|
| A construct's program | The mod storage of its programmable block, the unit's hero, under `BlockStorage.ProgramKey` | When the program may have changed, and as the world saves | `World/SprocketUnit.cs`, `Storage/Scribe.cs`, `Storage/Reader.cs` |
| What is switched off, and why | The same block, under `BlockStorage.StateKey` | With the program | `Storage/StateText.cs` |
| What the world remembers while it runs: Sprocket's clock, every run and every watch, owners, pauses, players' settings, doors' close times | A file named `state` among Sprocket's files for the world, kept on the carrier | As the world saves, on the server, where anything changed | `Memory/`, `World/Carrier.cs` |
| A door's auto-close delay | The door's mod storage, under `CloseDelay.Key` | When it is set | `Features/Doors/CloseDelay.cs` |
| The library | The game's Storage folder on the player's own computer: `sprocket-bp.index`, and a `sprocket-bp-*.cog` for each blueprint | When a blueprint is kept | `Storage/Shelf.cs` |
| Backups | Beside the world's save: `backups.ini`, and a `backup-N.cog` for each | When a construct's automation is lost to damage | `Storage/Backups.cs` |
| The world's settings | `sprocket.ini`, in the game's Storage folder and in the world's | When an admin changes them | `World/Settings.cs` |
| A player's view preferences | `sprocket-view.ini` in the game's Storage folder | When one changes | `Storage/Preferences.cs` |
| The clipboard | `sprocket-clipboard.txt` in the game's Storage folder | On every copy | `Storage/ClipboardFile.cs` |

**The carrier** (`World/Carrier.cs`) is an entity of Sprocket's own, saved with the world and tied to no block: no
model, no physics, never sent to a player's game. World storage misses the game's own backups of a world, and the
carrier is saved into every one. It holds Sprocket's files for the world as one block of text, `files 1` and then each
file after an `=` line (`Storage/CarrierFiles.cs`), read whole or not at all.

## The program, as text

A program is stored as a script in Sprocket's language ([Language](Language.md)), written by `ProgramWriter.Save` and
read by `ProgramReader.Load` through the `Lexer` and `Parser` in `Storage/`. Its first line says which format wrote it:

```
sprocket 1 alpha 16
```

`ScriptFormat.Version` and `ScriptFormat.Draft` hold the numbers this build writes.

A text that fails to read is read as nothing. The program is marked not intact and kept as the text it was, and
nothing writes over it: the block keeps the text until a person deals with it. A text from a format this build
neither writes nor upgrades is refused the same way, and marked incompatible.

## Changing the format

**What the text says changes only together with an upgrade for it.** `Storage/Upgrades.cs` names the formats it
carries forward, and turns what the parser read in an older one into the current model: the upgrade runs on the
model, never on the text, so a name that happens to hold a keyword is left as it is. The program is written in the
current format at its next save.

Every format so far is an alpha draft. A draft is upgraded only where an upgrade is chosen for it, named in
`FormatUpgrades.From`; any other draft is refused rather than misread. An upgrade between released formats is kept for good, since every program behind
it reads through it.

Changing what the text says:

1. Raise `ScriptFormat.Draft`.
2. Where programs in the old draft are to carry forward, add it to `FormatUpgrades.From` and its changes to `FormatUpgrades.Apply`.
3. Update `docs/Language.md` in the same change: it is the language's spec.
4. Test it: `UpgradeTests` reads an old text and checks what it became and that it is written in the current format;
   `ScriptTests` and `ProgramTextTests` read and write programs whole.

## What the world remembers

`Memory/` is the world's memory: protobuf, through the game's own serializer, as base 64 (`Memory/GameBinary.cs`).
`WorldMemory` is the root, holding the clock, the constructs, the players and the doors.

**A member's number is the format.** The memory carries no version: a `ProtoMember` number, once used, is never used
again and never given another type, and a member dropped leaves its number unused. Reasons and causes are stored as the
fixed spellings in `Memory/Spellings.cs`, never as the names of the types behind them.

A memory that fails to read is said in the log and set aside: the world starts fresh, and its next save writes over
it.

## In multiplayer

The server keeps everything above that belongs to the world: the blocks' storage, the carrier and its memory, the
backups and the world's settings. A player's game keeps a copy of each construct's program and switches, filled by the
server as it lends the construct to that player's editor, and never its runs. The library, the view preferences and the
clipboard belong to each player's own computer.

## What must never change

Each of these is written into saves, and a change reads an old world as empty or wrong, often without a word:

- **The storage keys**: `BlockStorage.ProgramKey` and `StateKey`, `Carrier.Key` and `CloseDelay.Key`. Each is also
  claimed in `Content/Data/EntityComponents.sbc`, without which the game drops it from the save.
- **The world variable** naming the carrier, `sprocket.carrier`.
- **Ids written into the game's own data**: the toolbar action `Sprocket_Trigger`, the LCD app `Sprocket_Status`, and
  the door control `Sprocket_AutoClose`.
- **Every word of the program text**: the keywords (`Storage/Keywords.cs`), `ext`, `toolbar`, and `s` for seconds,
  each changing only with an upgrade.
- **The words of `StateText`**, such as `by hand`, `owned` and `paused`.
- **The memory's member numbers and spellings**, as above.
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
