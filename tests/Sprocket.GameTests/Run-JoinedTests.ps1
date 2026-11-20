<#
    Runs Sprocket's tests of a game client joined to a local dedicated server, against the mod as built from this repository. The
    server's plugin stands in for the other players; the client's plugin drives the screens and takes the pictures.

    1. Refuses to run while Space Engineers or its dedicated server is running: neither is ever closed by this script.
    2. Puts back the Workshop copy of Sprocket an earlier run left swapped, where one did.
    3. Builds the test plugin and packs the mod.
    4. Puts the local build over the server's copy, as Run-ServerTests.ps1 does, and over the client's Workshop copy, which a
       joining client loads from (the game refuses unpublished mods to a client joining a server). The client's copy is backed up
       first and put back at the end, however the run ends.
    5. Starts the server on a clean world with the plugin in its joined phase, waits for it to be ready, and starts the client
       through SevrLoader, which joins it. A game or server not finished by -TimeoutSeconds is stopped: only the ones this
       script started.
    6. Prints both reports. The exit code is 0 when every check in both passed.

    Runs only while Mal is away from the PC: the client takes the screen.
#>
param(
    [string]$AppData = 'E:\Data\sprocket-client-test',
    [string]$Instance = 'E:\Data\sprocket-ds-test',
    [string]$Game = 'E:\Steam\steamapps\common\SpaceEngineers',
    [string]$Server = 'E:\Steam\steamapps\common\SpaceEngineersDedicatedServer',
    [string]$Loader = 'D:\Repos\Malforge\SevrLoader\dist\SevrLoader.exe',
    [string]$Workshop = 'E:\Steam\steamapps\workshop\content\244850\3807234771',
    [string]$Address = '127.0.0.1:27021',
    [int]$TimeoutSeconds = 1200
)
$ErrorActionPreference = 'Stop'
$repo = Resolve-Path "$PSScriptRoot\..\.."
$work = Join-Path $AppData 'joined'
New-Item -ItemType Directory -Force $work | Out-Null
$backup = Join-Path $AppData 'workshop-backup'
$mod = 'Sprocket Automation (Bleeding Edge)'

# 1. Nothing of the game running.
if (Get-Process -Name 'SpaceEngineers', 'SevrLoader', 'SpaceEngineersDedicated' -ErrorAction SilentlyContinue) {
    throw 'Space Engineers or its dedicated server is running; neither is ever closed for a test.'
}

# 2. The player's own copy back, where a run that ended badly left it swapped.
function Restore-Workshop {
    if (-not (Test-Path $backup)) { return }
    robocopy $backup $Workshop /MIR /NJH /NJS /NP /NFL /NDL | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "Putting the Workshop copy back failed (robocopy $LASTEXITCODE); it is kept in $backup." }
    Remove-Item $backup -Recurse -Force
}
Restore-Workshop

# 3. The plugin, and the mod packed as the game would load it.
dotnet build "$repo\tests\Sprocket.GameTests" -v q -p:MdkBuildConfiguration=none | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'The test plugin did not build.' }
$pack = Join-Path $work 'pack'
if (Test-Path $pack) { Remove-Item $pack -Recurse -Force }
$packer = "$env:USERPROFILE\.nuget\packages\mal.mdk2.modpackager\2.3.0\tools\win-x64\mdk.exe"
& $packer pack "$repo\src\Sprocket.Host.SpaceEngineers\Sprocket.Host.SpaceEngineers.csproj" -output $pack -configuration Debug -interactive DoNothing | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'The mod did not pack.' }
# The pack's folder is named by the branch (mdk.ini), so the one folder it wrote is the build.
$packed = Get-ChildItem $pack -Directory | Select-Object -First 1
if ($null -eq $packed) { throw 'The pack wrote no mod folder.' }

# 4. The local build over the server's copy and, once backed up, over the client's.
$serverCopy = Join-Path $Instance 'content\244850\3807234771'
if (-not (Test-Path $serverCopy)) { throw "The server has no copy of Sprocket at $serverCopy; start it once to download it." }
if (-not (Test-Path $Workshop)) { throw "The client has no Workshop copy of Sprocket at $Workshop; subscribe to it once." }
robocopy $packed.FullName $serverCopy /MIR /XF metadata.mod modinfo.sbmi /NJH /NJS /NP /NFL /NDL | Out-Null
if ($LASTEXITCODE -ge 8) { throw "Copying the mod to the server failed (robocopy $LASTEXITCODE)." }

robocopy $Workshop $backup /MIR /NJH /NJS /NP /NFL /NDL | Out-Null
if ($LASTEXITCODE -ge 8) { throw "Backing up the Workshop copy failed (robocopy $LASTEXITCODE)." }

$serverReport = Join-Path $work 'report-server.txt'
$clientReport = Join-Path $work 'report-client.txt'
$serverProcess = $null
$clientProcess = $null
$began = Get-Date
try {
    robocopy $packed.FullName $Workshop /MIR /XF metadata.mod modinfo.sbmi /NJH /NJS /NP /NFL /NDL | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "Copying the mod to the client failed (robocopy $LASTEXITCODE)." }

    # 5. A clean world, Kept Count's and Status Pages' blueprints, the server, and the client once the server is ready.
    $world = Join-Path $Instance 'Saves\Sprocket Test'
    Get-ChildItem $world | Remove-Item -Recurse -Force
    Copy-Item (Join-Path $Instance 'WorldTemplate\*') $world -Recurse
    $blueprints = Join-Path $work 'blueprints'
    dotnet run --project "$repo\src\Sprocket.Host.SpaceEngineers.Sim" -- --blueprint 'Sprocket Kept Count' $blueprints | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "The blueprint for 'Sprocket Kept Count' was not written." }
    dotnet run --project "$repo\src\Sprocket.Host.SpaceEngineers.Sim" -- --blueprint 'Sprocket Status Pages' $blueprints | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "The blueprint for 'Sprocket Status Pages' was not written." }
    dotnet run --project "$repo\src\Sprocket.Host.SpaceEngineers.Sim" -- --blueprint 'Sprocket Forced Door' $blueprints | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "The blueprint for 'Sprocket Forced Door' was not written." }

    # A folder both machines write to, for the client to ask the server to switch a logic system, and the server to answer.
    $signals = Join-Path $work 'signals'
    if (Test-Path $signals) { Remove-Item $signals -Recurse -Force }
    New-Item -ItemType Directory -Force $signals | Out-Null
    $env:SPROCKET_GAMETEST_SIGNALS = $signals

    Remove-Item $serverReport, $clientReport -ErrorAction SilentlyContinue
    $plugin = "$repo\tests\Sprocket.GameTests\bin\Debug\net48\Sprocket.GameTests.dll"
    $env:SPROCKET_GAMETEST_BLUEPRINTS = $blueprints
    $env:SPROCKET_GAMETEST_REPORT = $serverReport
    $env:SPROCKET_GAMETEST_PHASE = 'joined'
    $exe = Join-Path $Server 'DedicatedServer64\SpaceEngineersDedicated.exe'
    $serverProcess = Start-Process $exe -ArgumentList '-console', '-path', "`"$Instance`"", '-plugin', "`"$plugin`"" -WorkingDirectory (Split-Path $exe) -WindowStyle Minimized -PassThru
    Remove-Item Env:SPROCKET_GAMETEST_PHASE, Env:SPROCKET_GAMETEST_REPORT

    # The server logs that it is ready once it takes players; three minutes is many times what it takes.
    $ready = $false
    for ($waited = 0; $waited -lt 180 -and -not $ready -and -not $serverProcess.HasExited; $waited += 2) {
        Start-Sleep -Seconds 2
        $log = Get-ChildItem $Instance -Filter 'SpaceEngineersDedicated_*.log' | Where-Object { $_.LastWriteTime -ge $began } | Sort-Object LastWriteTime | Select-Object -Last 1
        $ready = $log -and (Select-String $log.FullName -Pattern 'Game ready' -Quiet)
    }
    if (-not $ready) { throw 'The server did not get ready within three minutes.' }

    $env:SPROCKET_CLIENTTEST_REPORT = $clientReport
    $env:SPROCKET_CLIENTTEST_JOIN = $Address
    $env:SPROCKET_CLIENTTEST_SHOTS = Join-Path $work 'shots'
    Remove-Item Env:SPROCKET_CLIENTTEST_WORLD -ErrorAction SilentlyContinue
    $clientProcess = Start-Process $Loader -ArgumentList '--game', "`"$Game\Bin64`"", '--plugin', "`"$plugin`"", '-appdata', "`"$AppData`"", '-nosplash', '-skipintro' -WorkingDirectory (Split-Path $Loader) -PassThru

    if (-not $clientProcess.WaitForExit($TimeoutSeconds * 1000)) {
        Stop-Process -Id $clientProcess.Id -Force
        "The client did not finish within $TimeoutSeconds s, and was stopped."
    }
    # The server ends once the client has left it.
    if (-not $serverProcess.WaitForExit(120 * 1000)) {
        Stop-Process -Id $serverProcess.Id -Force
        'The server did not end after the client left, and was stopped.'
    }
}
finally {
    # Only the game and server this script started, and only where they are still running.
    if ($clientProcess -and -not $clientProcess.HasExited) { Stop-Process -Id $clientProcess.Id -Force }
    if ($serverProcess -and -not $serverProcess.HasExited) { Stop-Process -Id $serverProcess.Id -Force }
    Remove-Item Env:SPROCKET_CLIENTTEST_JOIN -ErrorAction SilentlyContinue
    Restore-Workshop
}

# 6. The reports.
'{0:n0} s' -f ((Get-Date) - $began).TotalSeconds
$clean = $true
foreach ($one in $serverReport, $clientReport) {
    if (Test-Path $one) { Get-Content $one } else { "No report was written to $one." }
    if (-not ((Test-Path $one) -and (Select-String $one -Pattern '^finished .*, 0 failed$' -Quiet))) { $clean = $false }
}
$shots = Join-Path $work 'shots'
if (Test-Path $shots) { Get-ChildItem $shots | ForEach-Object { "picture: $($_.FullName)" } }
if ($clean) { exit 0 }
exit 1
