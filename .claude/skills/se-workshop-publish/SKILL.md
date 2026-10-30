---
name: se-workshop-publish
description: >
  Publish or update a Space Engineers mod on the Steam Workshop headlessly via
  SEWorkshopTool (no game launch). Use ONLY when the user explicitly asks to
  push/upload/publish with the tool ("do the workshop upload", "push it via
  SEWorkshopTool", "re-upload the alpha"). Do NOT auto-invoke for routine
  builds, packaging, dry-runs the user didn't ask for, or when they've said
  they'll publish from the game.
---

# SE Workshop Publish (headless via SEWorkshopTool)

## Default — do NOT publish unprompted

The user publishes from the in-game UI by default. Only run SEWorkshopTool when they explicitly ask **this turn** ("do the reupload with the tool", "push it"). An ambiguous "go" or "proceed" is **not** authorization to upload — if unsure, ASK. A previous turn's authorization does not carry over.

## Fixed paths (this user)

- SE install / Bin64: `E:\Steam\steamapps\common\SpaceEngineers\Bin64`
- SE app-data (mods packed here by MDK2 `output=auto`): `E:\Data\sesaves`  → mods at `E:\Data\sesaves\Mods\<ModFolder>`
- SEWorkshopTool source: `D:\Repos\SE\SEWorkshopTool`
- SEWorkshopTool build output: `D:\Repos\SE\SEWorkshopTool\SEWorkshopTool\bin\Release\net48\SEWorkshopTool.exe`
- Deployed tool runs from: SE `Bin64` (after the four copy steps in setup)

## Standard publish workflow (existing Workshop item)

PowerShell, from SE `Bin64`. Steam must be running and logged in.

```powershell
cd "E:\Steam\steamapps\common\SpaceEngineers\Bin64"

# 1. Dry-run FIRST — validates Steam init, folder presence, pre-upload compile.
.\SEWorkshopTool.exe push --appdata "E:\Data\sesaves" --mods "<ModFolder>" --update-only --dry-run

# 2. Actual push (only after dry-run succeeds).
.\SEWorkshopTool.exe push --appdata "E:\Data\sesaves" --mods "<ModFolder>" --update-only --message "<changelog>"
```

`<ModFolder>` is the deployable folder name under `E:\Data\sesaves\Mods\` (e.g. `Mal.DockingAid.Alpha`, `Mal.Logistics.Alpha`). Space-separate multiple mods.

## The changelog

Every upload carries a changelog, and it is kept as the work goes, not written at upload time.

- Each time a change a player can see lands on the branch a mod is published from, one plain line for players is added to that mod's running list of changes since its last upload.
- At the next upload, that list is the `--message`. Show it to the user before the push, then start a new, empty list.
- For Sprocket Automation (Bleeding Edge), the list is the `changelog-next` memory in the Reactor project's memory folder.

## Why each flag matters

- `--appdata "E:\Data\sesaves"` — **REQUIRED**. This user's MDK2 `output=auto` packs to the custom data path, not the tool's default `%AppData%\SpaceEngineers\Mods`. Without it the tool finds nothing.
- `--update-only` (`-u`) — safety net: tool refuses to create a new Workshop item; only updates an existing one. The Workshop id lives in the deployable's `modinfo.sbmi` (`<WorkshopIds><WorkshopId><Id>…`), written by the first publish and preserved across rebuilds.
- `--dry-run` (`-d`) — always run first. Validates Steam init + folder + compile, prints target item id + visibility, skips upload.
- `--message "<changelog>"` — Workshop changelog entry for this update.
- **Omit `--visibility`** — preserves the item's current state (Unlisted / Public / Private). Only set `--visibility Unlisted` for a brand-new item, and only if the user has approved creating one.
- **Don't pass `-c`/`-C` (--compile/--no-compile)** — their CommandLineParser comparer collides at runtime ("Sequence contains more than one matching element"). Compile defaults ON, which is what we want (runs the in-game whitelist compiler pre-upload as a real validation signal). Use the long form `--no-compile` only if explicitly asked to skip it.

## First-time setup (only if tool isn't built/deployed)

Run only if `E:\Steam\steamapps\common\SpaceEngineers\Bin64\SEWorkshopTool.exe` is missing or out of date relative to a game patch. Check with:

```powershell
Test-Path "E:\Steam\steamapps\common\SpaceEngineers\Bin64\SEWorkshopTool.exe"
```

If missing:

```powershell
# 1. Junction the project-expected Bin64 → the live install. Idempotent check first.
$junction = "D:\Repos\SE\SEWorkshopTool\SEWorkshopTool\Bin64"
if (-not (Test-Path $junction)) {
    New-Item -ItemType Junction -Path $junction -Target "E:\Steam\steamapps\common\SpaceEngineers\Bin64"
}

# 2. Build FROM POWERSHELL (bash mangles the backslashes). SolutionDir is
# required — the PreBuild Tools\LinkBinaries.ps1 uses it; without the
# junction OR SolutionDir, the prebuild tries to DepotDownload the
# dedicated-server binaries.
dotnet build "D:\Repos\SE\SEWorkshopTool\SEWorkshopTool\SEWorkshopTool.csproj" -c Release -p:SolutionDir="D:\Repos\SE\SEWorkshopTool"

# 3. Copy the four files into SE Bin64 (Bin64 already has steam_appid.txt
# + native steam_api64.dll, but the tool ships its own steam_appid.txt
# at 244850 as a fallback).
$src = "D:\Repos\SE\SEWorkshopTool\SEWorkshopTool\bin\Release\net48"
$dst = "E:\Steam\steamapps\common\SpaceEngineers\Bin64"
Copy-Item "$src\SEWorkshopTool.exe","$src\SEWorkshopTool.exe.config","$src\CommandLine.dll","$src\steam_appid.txt" $dst -Force
```

**Don't use the GitHub prebuilt binary** — it's pinned to whatever SE DLL versions existed at its release tag and breaks on game updates. Building from source against the current install is the only durable approach. The csproj refs use `SpecificVersion=False` + `HintPath>Bin64\…`, so they re-bind to current DLLs without code changes.

## Verifying after publish

Confirm the Workshop item was updated:

```powershell
# Extract the WorkshopId stamped into the deployable.
Select-String -Path "E:\Data\sesaves\Mods\<ModFolder>\modinfo.sbmi" -Pattern '<Id>(\d+)</Id>' | ForEach-Object { $_.Matches.Groups[1].Value }
```

Open `https://steamcommunity.com/sheet/filedetails/?id=<that-id>` in a browser to confirm the changelog landed.

## Failure modes seen in the wild

- **"Sequence contains more than one matching element"** — you passed `-c` or `-C`. Use `--no-compile` (or just omit; compile defaults on).
- **"Nothing to publish" / mod folder not found** — `--appdata` missing or wrong. For this user it must be `E:\Data\sesaves`.
- **Steam init failure** — Steam not running, or not logged in, or `steam_appid.txt` missing from `Bin64`.
- **Build fails on `LinkBinaries.ps1`** — `-p:SolutionDir=…` not set, or the `Bin64` junction is missing. Set both.

## Reference

SE's own headless recipe (for context, not to invoke): `Sandbox.Engine.Networking.MyWorkshop.PublishItemBlocking` → `MyGameService.CreateWorkshopPublisher("Steam")`, set Title/Description/Visibility/Tags/Id/Folder/Metadata(metadata.mod)/Thumbnail, tag `"mod"` (+`TAG_NO_SCRIPTS` if the folder has no script files), `Publish()`.
