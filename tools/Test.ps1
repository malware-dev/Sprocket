<#
.SYNOPSIS
    Runs every test project in Sprocket.slnx, each split into shards that run side by side as separate processes.

.DESCRIPTION
    The tests of one process run one at a time: the game reaches drawing and the world through statics, so two tests in
    one process would share a screen and a world (see Serially.cs in the test projects). Separate processes each have
    their own, so a suite split by class into shards runs as fast as its largest shard.

    Builds first, with MdkBuildConfiguration=none so nothing is deployed, unless -NoBuild is given.

.PARAMETER TestsPerShard
    About how many tests one shard runs, where the project has not run here before. A project with fewer runs as one shard.

.PARAMETER SecondsPerShard
    About how long one shard runs, once the project has run here: each run records what every class took, in the
    project's obj folder, and the next run balances its shards by that.

.PARAMETER MaxShards
    The most shards one project is split into.

.PARAMETER NoBuild
    Runs what was built last.

.PARAMETER Project
    Runs only the test projects whose folder name contains this text.

.EXAMPLE
    ./tools/Test.ps1
    ./tools/Test.ps1 -NoBuild -Project Presentation
#>
param(
    [int]$TestsPerShard = 200,
    [int]$SecondsPerShard = 60,
    [int]$MaxShards = 16,
    [switch]$NoBuild,
    [string]$Project = ""
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$started = Get-Date

if (-not $NoBuild) {
    dotnet build (Join-Path $root "Sprocket.slnx") -p:MdkBuildConfiguration=none -v:q -nologo -clp:ErrorsOnly | Out-Host
    if ($LASTEXITCODE -ne 0) {
        Write-Host "The build failed." -ForegroundColor Red
        exit 1
    }
}

$work = Join-Path ([IO.Path]::GetTempPath()) ("sprocket-tests-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $work | Out-Null

# One runner per test project: the executable named for its folder.
$runs = @()
foreach ($folder in Get-ChildItem (Join-Path $root "tests") -Directory) {
    if ($Project -and $folder.Name -notlike "*$Project*") {
        continue
    }

    $exe = Join-Path $folder.FullName ("bin/Debug/net48/" + $folder.Name + ".exe")
    if (-not (Test-Path $exe)) {
        continue
    }

    # Each class weighed by the seconds it took last time on this machine, or by its test count where it has not run
    # here, then dealt largest first to the lightest shard.
    $timesFile = Join-Path $folder.FullName "obj/shard-times.json"
    $times = @{}
    if (Test-Path $timesFile) {
        (Get-Content $timesFile -Raw | ConvertFrom-Json).PSObject.Properties | ForEach-Object { $times[$_.Name] = [double]$_.Value }
    }

    $tests = & $exe -list tests | Where-Object { $_ -match "^\S+\.\S+$" }
    $classes = & $exe -list classes | Where-Object { $_ -match "^\S+\.\S+$" }
    $weight = @{}
    foreach ($class in $classes) {
        $weight[$class] = 0.0
    }

    foreach ($test in $tests) {
        $class = $test.Substring(0, $test.LastIndexOf("."))
        if ($weight.ContainsKey($class) -and -not $times.ContainsKey($class)) {
            $weight[$class] += 0.3
        }
    }

    foreach ($class in $classes) {
        if ($times.ContainsKey($class)) {
            $weight[$class] = $times[$class]
        }
    }

    $seconds = ($weight.Values | Measure-Object -Sum).Sum
    $count = if ($times.Count -gt 0) { [Math]::Ceiling($seconds / $SecondsPerShard) } else { [Math]::Ceiling($tests.Count / $TestsPerShard) }
    $count = [Math]::Min($MaxShards, [Math]::Max(1, $count))
    $shards = @()
    for ($i = 0; $i -lt $count; $i++) {
        $shards += , @{ Classes = New-Object System.Collections.Generic.List[string]; Weight = 0 }
    }

    foreach ($class in ($weight.Keys | Sort-Object { $weight[$_] } -Descending)) {
        $lightest = $shards | Sort-Object { $_.Weight } | Select-Object -First 1
        $lightest.Classes.Add($class)
        $lightest.Weight += [Math]::Max(0.1, $weight[$class])
    }

    $n = 0
    foreach ($shard in $shards) {
        if ($shard.Classes.Count -eq 0) {
            continue
        }

        $n++
        $arguments = @("-noLogo", "-noColor")
        foreach ($class in $shard.Classes) {
            $arguments += "-class"
            $arguments += "`"$class`""
        }

        $out = Join-Path $work ("{0}.{1}.txt" -f $folder.Name, $n)
        $arguments += "-xml"
        $arguments += "`"$out.xml`""
        $process = Start-Process -FilePath $exe -ArgumentList $arguments -WorkingDirectory (Split-Path $exe) `
            -RedirectStandardOutput $out -RedirectStandardError ($out + ".err") -NoNewWindow -PassThru
        $runs += @{ Name = $folder.Name; Process = $process; Output = $out; Times = $timesFile }
    }
}

Write-Host ("Running {0} shards..." -f $runs.Count)
foreach ($run in $runs) {
    $run.Process.WaitForExit()
}

# Each shard ends with a line "Total: n, Errors: n, Failed: n, Skipped: n, ..."; a failed test is marked [FAIL].
$totals = @{}
$failures = @()
$broken = @()
foreach ($run in $runs) {
    $text = Get-Content $run.Output -ErrorAction SilentlyContinue
    $summary = $text | Where-Object { $_ -match "Total: (\d+), Errors: (\d+), Failed: (\d+), Skipped: (\d+)" } | Select-Object -Last 1
    if (-not $summary) {
        $broken += $run.Name
        continue
    }

    $null = $summary -match "Total: (\d+), Errors: (\d+), Failed: (\d+), Skipped: (\d+)"
    if (-not $totals.ContainsKey($run.Name)) {
        $totals[$run.Name] = @{ Total = 0; Failed = 0; Errors = 0 }
    }

    $totals[$run.Name].Total += [int]$Matches[1]
    $totals[$run.Name].Errors += [int]$Matches[2]
    $totals[$run.Name].Failed += [int]$Matches[3]
    $failures += $text | Where-Object { $_ -match "\[FAIL\]" } | ForEach-Object { $_.Trim() }
}

# What each class took, kept beside the project's build for the next run to balance its shards by.
$measured = @{}
foreach ($run in $runs) {
    if (-not (Test-Path ($run.Output + ".xml"))) {
        continue
    }

    if (-not $measured.ContainsKey($run.Times)) {
        $measured[$run.Times] = @{}
    }

    $taken = $measured[$run.Times]
    foreach ($test in ([xml](Get-Content ($run.Output + ".xml") -Raw)).SelectNodes("//test")) {
        $class = $test.GetAttribute("type")
        $taken[$class] = $(if ($taken.ContainsKey($class)) { $taken[$class] } else { 0.0 }) + [double]$test.GetAttribute("time")
    }
}

foreach ($file in $measured.Keys) {
    $measured[$file] | ConvertTo-Json | Set-Content $file
}

foreach ($name in ($totals.Keys | Sort-Object)) {
    $t = $totals[$name]
    $color = if ($t.Failed + $t.Errors -gt 0) { "Red" } else { "Green" }
    Write-Host ("{0,-50} {1,5} run, {2} failed" -f $name, $t.Total, ($t.Failed + $t.Errors)) -ForegroundColor $color
}

foreach ($failure in $failures) {
    Write-Host ("  " + $failure) -ForegroundColor Red
}

foreach ($name in $broken) {
    Write-Host ("A shard of $name ended without a summary; its output is in $work") -ForegroundColor Red
}

Write-Host ("Done in {0:mm\:ss}." -f ((Get-Date) - $started))
if ($failures.Count -gt 0 -or $broken.Count -gt 0) {
    Write-Host "Shard output: $work"
    exit 1
}

Remove-Item -Recurse -Force $work
exit 0
