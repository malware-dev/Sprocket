<#
    Runs Sprocket's game tests on a local dedicated server, against the mod as built from this repository.

    1. Refuses to run unless the dedicated server is on the Steam branch the game is tested on.
    2. Builds the test plugin and packs the mod.
    3. Copies the packed mod over the server's copy of Sprocket Automation (Bleeding Edge). The server takes its copy as
       up to date, so the local build is what runs.
    4. Puts back the clean test world, writes the scenarios' blueprints, and starts the server with the plugin.
    5. Prints the report. The exit code is 0 when every check passed.

    The server's instance (its config, clean world template and Workshop downloads) is in -Instance and is never the
    server set up for playing.
#>
param(
    [string]$Instance = 'E:\Data\sprocket-ds-test',
    [string]$Server = 'E:\Steam\steamapps\common\SpaceEngineersDedicatedServer',
    [string]$Branch = 'steamctg',
    [string[]]$Scenarios = @('Sprocket Stock Watch', 'Sprocket Move Items', 'Sprocket Inventory Readings', 'Sprocket Refinery Moves'),
    [int]$TimeoutSeconds = 600
)
$ErrorActionPreference = 'Stop'
$repo = Resolve-Path "$PSScriptRoot\..\.."
$work = Join-Path $Instance 'run'
New-Item -ItemType Directory -Force $work | Out-Null

# 1. The branch, from Steam's own record of the install.
$manifest = Join-Path (Split-Path (Split-Path $Server)) 'appmanifest_298740.acf'
$installed = (Select-String $manifest -Pattern '"BetaKey"\s+"([^"]*)"' | Select-Object -First 1).Matches.Groups[1].Value
if ($installed -ne $Branch) { throw "The dedicated server is on branch '$installed', not '$Branch'." }

# 2. The plugin, and the mod packed as the game would load it.
dotnet build "$repo\tests\Sprocket.GameTests" -v q | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'The test plugin did not build.' }
$pack = Join-Path $work 'pack'
if (Test-Path $pack) { Remove-Item $pack -Recurse -Force }
$packer = "$env:USERPROFILE\.nuget\packages\mal.mdk2.modpackager\2.3.0\tools\win-x64\mdk.exe"
& $packer pack "$repo\src\Sprocket.Host.SpaceEngineers\Sprocket.Host.SpaceEngineers.csproj" -output $pack -configuration Debug -interactive DoNothing | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'The mod did not pack.' }

# 3. The local build over the server's Workshop copy, which the server must have downloaded once already.
$copy = Join-Path $Instance 'content\244850\3807234771'
if (-not (Test-Path $copy)) { throw "The server has no copy of Sprocket at $copy; start it once to download it." }
robocopy "$pack\Sprocket Automation (Bleeding Edge)" $copy /MIR /XF metadata.mod modinfo.sbmi /NJH /NJS /NP /NFL /NDL | Out-Null
if ($LASTEXITCODE -ge 8) { throw "Copying the mod failed (robocopy $LASTEXITCODE)." }

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

# 5. The report.
'{0:n0} s, branch {1}' -f ((Get-Date) - $began).TotalSeconds, $installed
if (Test-Path $report) { Get-Content $report } else { 'No report was written.' }
if ((Test-Path $report) -and (Select-String $report -Pattern '^finished .*, 0 failed$' -Quiet)) { exit 0 }
exit 1
