<#
    Runs Sprocket's client tests in the game itself, against the mod as built from this repository.

    1. Refuses to run while Space Engineers is running: the game is never closed by this script.
    2. Builds the test plugin and packs the mod into the test's own user data folder, -AppData, never the one played in.
    3. Puts back a clean test world there, its Sprocket mod pointed at that local copy, and writes the scenarios' blueprints.
    4. Starts the game through SevrLoader with the plugin, which loads the world, runs the tests, takes its pictures and closes
       the game. A game that has not finished by -TimeoutSeconds is stopped.
    5. Prints the report and where the pictures are. The exit code is 0 when every check passed.
#>
param(
    [string]$AppData = 'E:\Data\sprocket-client-test',
    [string]$Game = 'E:\Steam\steamapps\common\SpaceEngineers',
    [string]$Loader = 'D:\Repos\Malforge\SevrLoader\dist\SevrLoader.exe',
    [string]$Template = 'E:\Data\sprocket-ds-test\WorldTemplate',
    [string[]]$Scenarios = @('Sprocket Piston Status', 'Sprocket Kept Count', 'Sprocket Move Items', 'Sprocket Status Pages'),
    [int]$TimeoutSeconds = 900,
    # The parts to play, of scene, kept, pages, editor, menus, move, remote and banner; every part where none is named.
    [string[]]$Only = @()
)
$ErrorActionPreference = 'Stop'
$repo = Resolve-Path "$PSScriptRoot\..\.."
$work = Join-Path $AppData 'run'
New-Item -ItemType Directory -Force $work | Out-Null

# 1. Nothing of the game running.
if (Get-Process -Name 'SpaceEngineers', 'SevrLoader' -ErrorAction SilentlyContinue) { throw 'Space Engineers is running; it is never closed for a test.' }

# 2. The plugin, and the mod packed as the game would load it, as a local mod of the test's own user data.
dotnet build "$repo\tests\Sprocket.GameTests" -v q | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'The test plugin did not build.' }
$pack = Join-Path $work 'pack'
if (Test-Path $pack) { Remove-Item $pack -Recurse -Force }
$packer = "$env:USERPROFILE\.nuget\packages\mal.mdk2.modpackager\2.3.0\tools\win-x64\mdk.exe"
& $packer pack "$repo\src\Sprocket.Host.SpaceEngineers\Sprocket.Host.SpaceEngineers.csproj" -output $pack -configuration Debug -interactive DoNothing | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'The mod did not pack.' }
$mod = 'Sprocket Automation (Bleeding Edge)'
# The pack's folder is named by the branch (mdk.ini), so the one folder it wrote is copied under the name the world loads.
$packed = Get-ChildItem $pack -Directory | Select-Object -First 1
if ($null -eq $packed) { throw 'The pack wrote no mod folder.' }
robocopy $packed.FullName (Join-Path $AppData "Mods\$mod") /MIR /NJH /NJS /NP /NFL /NDL | Out-Null
if ($LASTEXITCODE -ge 8) { throw "Copying the mod failed (robocopy $LASTEXITCODE)." }

# 3. A clean world whose Sprocket is the local copy: a mod with no Workshop id is read from the Mods folder by its name.
$world = Join-Path $AppData 'Saves\Sprocket Client Test'
if (Test-Path $world) { Remove-Item $world -Recurse -Force }
New-Item -ItemType Directory -Force $world | Out-Null
Copy-Item (Join-Path $Template '*') $world -Recurse
# The checkpoint and its world configuration both list the mods and the settings, and the configuration wins where they
# differ, so both are changed: Sprocket pointed at the local copy, offline, since the game loads a local mod only in an
# offline world, and spectator on for the pictures taken from it.
foreach ($file in (Join-Path $world 'Sandbox.sbc'), (Join-Path $world 'Sandbox_config.sbc')) {
    $xml = [IO.File]::ReadAllText($file)
    $xml = $xml -replace '<Name>3807234771\.sbm</Name>(\s*)<PublishedFileId>3807234771</PublishedFileId>', "<Name>$mod</Name>`$1<PublishedFileId>0</PublishedFileId>"
    if ($xml -notmatch "<Name>$([regex]::Escape($mod))</Name>") { throw "$file names no Sprocket mod to point at the local copy." }
    $xml = $xml -replace '<EnableSpectator>false</EnableSpectator>', '<EnableSpectator>true</EnableSpectator>'
    $xml = $xml -replace '<OnlineMode>[A-Z]+</OnlineMode>', '<OnlineMode>OFFLINE</OnlineMode>'
    [IO.File]::WriteAllText($file, $xml)
}

# An empty library, as a player new to Sprocket has; it is kept in the user data, beside the worlds.
Get-ChildItem (Join-Path $AppData 'Storage') -Filter 'sprocket-*' -ErrorAction SilentlyContinue | Remove-Item -Force

# The world's own mod storage, where Sprocket keeps its backups: for a world outside a player's folder the game keeps it in
# Saves\<player id>\<session name>\Storage, which the fresh world above does not replace (seen 2026-11-12).
$session = [regex]::Match([IO.File]::ReadAllText((Join-Path $world 'Sandbox.sbc')), '<SessionName>([^<]*)</SessionName>').Groups[1].Value
if ($session) {
    Get-ChildItem (Join-Path $AppData 'Saves') -Directory | ForEach-Object { Join-Path $_.FullName $session } |
        Where-Object { Test-Path $_ } | Remove-Item -Recurse -Force
}

$blueprints = Join-Path $work 'blueprints'
foreach ($scenario in $Scenarios) {
    dotnet run --project "$repo\src\Sprocket.Host.SpaceEngineers.Sim" -- --blueprint $scenario $blueprints | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "The blueprint for '$scenario' was not written." }
}

# 4. The run.
$report = Join-Path $work 'report.txt'
$shots = Join-Path $work 'shots'
Remove-Item $report -ErrorAction SilentlyContinue
$env:SPROCKET_CLIENTTEST_REPORT = $report
$env:SPROCKET_CLIENTTEST_WORLD = $world
$env:SPROCKET_CLIENTTEST_SHOTS = $shots
$env:SPROCKET_CLIENTTEST_ONLY = $Only -join ','
$env:SPROCKET_GAMETEST_BLUEPRINTS = $blueprints
$plugin = "$repo\tests\Sprocket.GameTests\bin\Debug\net48\Sprocket.GameTests.dll"
$began = Get-Date
$process = Start-Process $Loader -ArgumentList '--game', "`"$Game\Bin64`"", '--plugin', "`"$plugin`"", '-appdata', "`"$AppData`"", '-nosplash', '-skipintro' -WorkingDirectory (Split-Path $Loader) -PassThru
if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
    # The game this script started, and only that one: the first step refuses to run beside any other.
    Stop-Process -Id $process.Id -Force
    "The game did not finish within $TimeoutSeconds s, and was stopped."
}

# 5. The report.
'{0:n0} s' -f ((Get-Date) - $began).TotalSeconds
if (Test-Path $report) { Get-Content $report } else { 'No report was written.' }
if (Test-Path $shots) { Get-ChildItem $shots | ForEach-Object { "picture: $($_.FullName)" } }
if ((Test-Path $report) -and (Select-String $report -Pattern '^finished .*, 0 failed$' -Quiet)) { exit 0 }
exit 1
