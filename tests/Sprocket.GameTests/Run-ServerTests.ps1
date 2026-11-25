<#
    Runs Sprocket's game tests on a local dedicated server, against the mod as built from this repository.

    1. Refuses to run outside the test lease, and unless the dedicated server is on the Steam branch the game is tested on.
    2. Builds the test plugin and packs the mod.
    3. Copies the packed mod over the server's copy of Sprocket Automation (Bleeding Edge). The server takes its copy as
       up to date, its record of the item marked current by Mal.SeWorkbench, so the local build is what runs.
    4. Puts back the clean test world, writes the scenarios' blueprints, and starts the server with the plugin, which saves the
       world once its tests are done.
    5. Starts the server again on that save, with the plugin running the tests of a reloaded world.
    6. Prints both reports, and checks the server's copy is still the build. The exit code is 0 when every check in both passed.

    The server's instance (its config, clean world template and Workshop downloads) is in -Instance and is never the
    server set up for playing.

    The run is started through the machine's test lease, which refuses while another run holds it, and beside any game or
    server it did not start; every process the run started ends with it:

        Mal.SeWorkbench.exe lease --project Sprocket --mode server --session <session> [--wait <minutes>] -- powershell -NoProfile -File Run-ServerTests.ps1 <arguments>
#>
param(
    [string]$Instance = 'E:\Data\sprocket-ds-test',
    [string]$Server = 'E:\Steam\steamapps\common\SpaceEngineersDedicatedServer',
    [string]$Branch = 'steamctg',
    [string[]]$Scenarios = @('Sprocket Stock Watch', 'Sprocket Move Items', 'Sprocket Move Amounts', 'Sprocket Inventory Readings', 'Sprocket Refinery Moves', 'Sprocket Piston Status', 'Sprocket Script Limit', 'Sprocket Forced Door', 'Sprocket Old Airlock', 'Sprocket Dock Unlocks'),
    [int]$TimeoutSeconds = 600,
    # The machine's test lease and the tool that puts the build over the server's copy of the mod.
    [string]$Workbench = 'D:\Space Engineers\Tools\Mal.SeWorkbench\Binaries\Mal.SeWorkbench.exe'
)
$ErrorActionPreference = 'Stop'
$repo = Resolve-Path "$PSScriptRoot\..\.."
$work = Join-Path $Instance 'run'
New-Item -ItemType Directory -Force $work | Out-Null

# 1. Inside the lease, which refuses to start beside any game or server a run did not start.
if (-not $env:SE_TEST_LEASE) {
    throw "Run it through the lease: & '$Workbench' lease --project Sprocket --mode server --session NAME -- powershell -NoProfile -File $($MyInvocation.MyCommand.Path)"
}

# The branch, from Steam's own record of the install.
$manifest = Join-Path (Split-Path (Split-Path $Server)) 'appmanifest_298740.acf'
$installed = (Select-String $manifest -Pattern '"BetaKey"\s+"([^"]*)"' | Select-Object -First 1).Matches.Groups[1].Value
if ($installed -ne $Branch) { throw "The dedicated server is on branch '$installed', not '$Branch'." }

# 2. The plugin, and the mod packed as the game would load it.
dotnet build "$repo\tests\Sprocket.GameTests" -v q -p:MdkBuildConfiguration=none | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'The test plugin did not build.' }
$pack = Join-Path $work 'pack'
if (Test-Path $pack) { Remove-Item $pack -Recurse -Force }
$packer = "$env:USERPROFILE\.nuget\packages\mal.mdk2.modpackager\2.3.0\tools\win-x64\mdk.exe"
& $packer pack "$repo\src\Sprocket.Host.SpaceEngineers\Sprocket.Host.SpaceEngineers.csproj" -output $pack -configuration Debug -interactive DoNothing | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'The mod did not pack.' }

# 3. The local build over the server's Workshop copy, with the server's record of the item marked current, so it never
# downloads the published item over the build.
& $Workbench server-mod --instance $Instance --item 3807234771 --from "$pack\Sprocket Automation (Bleeding Edge)" | Out-Null
if ($LASTEXITCODE -eq 107) { throw "The server has no copy of Sprocket; start it once on a world naming it, to download it." }
if ($LASTEXITCODE -ne 0) { throw "Putting the build over the server's copy failed (server-mod $LASTEXITCODE)." }

# 4. A clean world, the scenario's blueprint, and the run.
$world = Join-Path $Instance 'Saves\Sprocket Test'
Get-ChildItem $world | Remove-Item -Recurse -Force
Copy-Item (Join-Path $Instance 'WorldTemplate\*') $world -Recurse

$blueprints = Join-Path $work 'blueprints'
foreach ($scenario in $Scenarios) {
    dotnet run --project "$repo\src\Sprocket.Host.SpaceEngineers.Sim" -- --blueprint $scenario $blueprints | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "The blueprint for '$scenario' was not written." }
}

$report = Join-Path $work 'report.txt'
Remove-Item $report -ErrorAction SilentlyContinue
$env:SPROCKET_GAMETEST_BLUEPRINTS = $blueprints
$env:SPROCKET_GAMETEST_REPORT = $report
$plugin = "$repo\tests\Sprocket.GameTests\bin\Debug\net48\Sprocket.GameTests.dll"
$exe = Join-Path $Server 'DedicatedServer64\SpaceEngineersDedicated.exe'
$began = Get-Date
$process = Start-Process $exe -ArgumentList '-console', '-path', "`"$Instance`"", '-plugin', "`"$plugin`"" -WorkingDirectory (Split-Path $exe) -WindowStyle Minimized -PassThru
if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
    Stop-Process -Id $process.Id -Force
    "The server did not finish within $TimeoutSeconds s."
}

# 5. The saved world loaded again.
$reloaded = Join-Path $work 'report-reload.txt'
Remove-Item $reloaded -ErrorAction SilentlyContinue
$env:SPROCKET_GAMETEST_REPORT = $reloaded
$env:SPROCKET_GAMETEST_PHASE = 'reload'
$process = Start-Process $exe -ArgumentList '-console', '-path', "`"$Instance`"", '-plugin', "`"$plugin`"" -WorkingDirectory (Split-Path $exe) -WindowStyle Minimized -PassThru
if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
    Stop-Process -Id $process.Id -Force
    "The reloaded server did not finish within $TimeoutSeconds s."
}
Remove-Item Env:SPROCKET_GAMETEST_PHASE

# 6. The reports, and whether they are of the build the run put over the server's copy.
& $Workbench server-mod --check --instance $Instance --item 3807234771 --from "$pack\Sprocket Automation (Bleeding Edge)" | Out-Null
$clean = $LASTEXITCODE -eq 0
if (-not $clean) { "The server's copy of Sprocket is not the build (server-mod --check $LASTEXITCODE), so it ran the wrong mod." }
'{0:n0} s, branch {1}' -f ((Get-Date) - $began).TotalSeconds, $installed
foreach ($one in $report, $reloaded) {
    if (Test-Path $one) { Get-Content $one } else { "No report was written to $one." }
    if (-not ((Test-Path $one) -and (Select-String $one -Pattern '^finished .*, 0 failed$' -Quiet))) { $clean = $false }
}
if ($clean) { exit 0 }
exit 1
