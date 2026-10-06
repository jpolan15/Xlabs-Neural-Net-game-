<#
.SYNOPSIS
    Chamber 01 objective checks for the work packages that have been added.

.DESCRIPTION
    Prints one line per check and writes the same lines to
    Documentation/Design/captures/verify_{wp}.log.
    Exits non-zero when any check FAILs.
    WARN is an uncalibrated threshold miss. SKIP is a missing operator step.
    This work package implements V-01, V-02, V-03, V-05, V-06, V-07, V-08, and V-10.

.PARAMETER Wp
    Capture set token. Default B0.

.PARAMETER WhatIf
    Print the checks that would run. Does not launch Unity and does not write the log.

.PARAMETER DryRun
    Same as -WhatIf.
#>
[CmdletBinding()]
param(
    [string]$Wp = "B0",
    [switch]$WhatIf,
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
$ProjectRoot = Split-Path -Parent $scriptDir
Set-Location -LiteralPath $ProjectRoot

$dry = $WhatIf.IsPresent -or $DryRun.IsPresent
$logPath = Join-Path $ProjectRoot ("Documentation/Design/captures/verify_{0}.log" -f $Wp)
$thresholdsPath = Join-Path $ProjectRoot "Tools/chamber01-thresholds.json"
$numbersPath = Join-Path $ProjectRoot "Documentation/Design/captures/editor_numbers.json"
$metricsPath = Join-Path $ProjectRoot "Documentation/Design/captures/metrics.json"
$unityExe = "C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe"
$forbiddenPrefixes = @(
    "Assets/ThirdParty/",
    "Assets/Samples/",
    "Assets/Scripts/Core/"
)

$script:Lines = New-Object System.Collections.Generic.List[string]
$script:FailCount = 0
$script:WarnCount = 0
$script:SkipCount = 0
$script:V01Passed = $false

function Add-CheckLine([string]$Line) {
    $script:Lines.Add($Line)
    Write-Host $Line
}

function Add-Pass([string]$Detail) { Add-CheckLine ("[PASS] " + $Detail) }
function Add-Fail([string]$Detail) { $script:FailCount++; Add-CheckLine ("[FAIL] " + $Detail) }
function Add-Warn([string]$Detail) { $script:WarnCount++; Add-CheckLine ("[WARN] " + $Detail) }
function Add-Skip([string]$Detail) { $script:SkipCount++; Add-CheckLine ("[SKIP] " + $Detail) }

function Get-NormalizedPath([string]$Path) {
    return [System.IO.Path]::GetFullPath($Path).TrimEnd('\', '/')
}

function Test-UnityProjectLock {
    $lockFile = Join-Path $ProjectRoot "Temp/UnityLockfile"
    if (Test-Path -LiteralPath $lockFile) {
        return $true
    }

    $root = Get-NormalizedPath $ProjectRoot
    $processes = @(Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" -ErrorAction SilentlyContinue)
    foreach ($process in $processes) {
        $command = [string]$process.CommandLine
        if ([string]::IsNullOrWhiteSpace($command)) { continue }
        $normalized = $command.Replace('/', '\')
        if ($normalized.IndexOf($root, [StringComparison]::OrdinalIgnoreCase) -ge 0) {
            return $true
        }
    }

    return $false
}

function Invoke-UnityMethod([string]$Method, [string]$LogName) {
    if (-not (Test-Path -LiteralPath $unityExe)) {
        throw "Unity executable is missing: $unityExe"
    }

    $logPathForUnity = Join-Path $ProjectRoot ("Temp/" + $LogName)
    $tempDir = Split-Path -Parent $logPathForUnity
    if (-not (Test-Path -LiteralPath $tempDir)) {
        New-Item -ItemType Directory -Path $tempDir | Out-Null
    }

    $unityArgs = "-batchmode -nographics -projectPath `"$ProjectRoot`" -executeMethod $Method -quit -logFile `"$logPathForUnity`""
    $process = Start-Process -FilePath $unityExe -ArgumentList $unityArgs -Wait -PassThru
    return @{
        ExitCode = $process.ExitCode
        LogPath = $logPathForUnity
    }
}

function Test-LogContains([string]$Path, [string]$Needle) {
    if (-not (Test-Path -LiteralPath $Path)) { return $false }
    $match = Select-String -LiteralPath $Path -Pattern $Needle -SimpleMatch -Quiet
    return [bool]$match
}

function Get-ValidatorHost {
    $found = Get-Command pwsh -ErrorAction SilentlyContinue
    if ($found -and $found.Source) { return $found.Source }
    return (Get-Process -Id $PID).Path
}

function Invoke-LoggedPwsh([string]$File, [string]$LogStem) {
    $tempDir = Join-Path $ProjectRoot "Temp"
    if (-not (Test-Path -LiteralPath $tempDir)) {
        New-Item -ItemType Directory -Path $tempDir | Out-Null
    }

    $stdout = Join-Path $tempDir ($LogStem + ".out")
    $stderr = Join-Path $tempDir ($LogStem + ".err")
    $hostExe = Get-ValidatorHost
    $process = Start-Process -FilePath $hostExe -ArgumentList @("-NoProfile", "-File", $File) -Wait -PassThru -WindowStyle Hidden -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    return $process.ExitCode
}

function Get-RepoNames {
    $names = New-Object System.Collections.Generic.List[string]
    $commands = @(
        , @("diff", "--name-only")
        , @("diff", "--name-only", "--cached")
        , @("ls-files", "--others", "--exclude-standard")
    )
    $previous = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        foreach ($command in $commands) {
            $output = & git @command 2>&1
            if ($LASTEXITCODE -ne 0) {
                throw ("git " + ($command -join " ") + " failed with exit " + $LASTEXITCODE)
            }
            foreach ($line in @($output)) {
                if ($line -isnot [string]) { continue }
                if ([string]::IsNullOrWhiteSpace($line)) { continue }
                $names.Add(($line -replace '\\', '/'))
            }
        }
    }
    finally {
        $ErrorActionPreference = $previous
    }
    return $names
}

function Invoke-V01 {
    if ($dry) {
        Add-CheckLine "[DRY] V-01 would build Level01 twice with the Unity invocation from Tools/Build-Level01Scene.ps1, dump the hierarchy each time, and compare the dumps. This dry run does not launch Unity."
        return
    }

    if (Test-UnityProjectLock) {
        Add-Skip "V-01 builder idempotent: Unity Editor holds the project lock"
        return
    }

    $dumpPath = Join-Path $ProjectRoot "Temp/chamber01_hierarchy.txt"
    $firstDump = Join-Path $ProjectRoot "Temp/chamber01_hierarchy_a.txt"
    $secondDump = Join-Path $ProjectRoot "Temp/chamber01_hierarchy_b.txt"

    try {
        $firstBuild = Invoke-UnityMethod "Convergence.EditorTools.Level01SceneBuilder.BuildLevel01" "chamber01_build_a.log"
        $firstOk = ($firstBuild.ExitCode -eq 0) -and (Test-LogContains $firstBuild.LogPath "[Level01SceneBuilder] Level 1 generated and saved successfully")
        if (-not $firstOk) {
            Add-Fail ("V-01 first build failed (exit " + $firstBuild.ExitCode + ", log " + $firstBuild.LogPath + ")")
            return
        }

        $firstHierarchy = Invoke-UnityMethod "Convergence.EditorTools.CaptureChamberViews.DumpHierarchy" "chamber01_dump_a.log"
        $firstDumpOk = ($firstHierarchy.ExitCode -eq 0) -and (Test-Path -LiteralPath $dumpPath)
        if (-not $firstDumpOk) {
            Add-Fail ("V-01 first hierarchy dump failed (exit " + $firstHierarchy.ExitCode + ")")
            return
        }
        Copy-Item -LiteralPath $dumpPath -Destination $firstDump -Force

        $secondBuild = Invoke-UnityMethod "Convergence.EditorTools.Level01SceneBuilder.BuildLevel01" "chamber01_build_b.log"
        $secondOk = ($secondBuild.ExitCode -eq 0) -and (Test-LogContains $secondBuild.LogPath "[Level01SceneBuilder] Level 1 generated and saved successfully")
        if (-not $secondOk) {
            Add-Fail ("V-01 second build failed (exit " + $secondBuild.ExitCode + ", log " + $secondBuild.LogPath + ")")
            return
        }

        $secondHierarchy = Invoke-UnityMethod "Convergence.EditorTools.CaptureChamberViews.DumpHierarchy" "chamber01_dump_b.log"
        $secondDumpOk = ($secondHierarchy.ExitCode -eq 0) -and (Test-Path -LiteralPath $dumpPath)
        if (-not $secondDumpOk) {
            Add-Fail ("V-01 second hierarchy dump failed (exit " + $secondHierarchy.ExitCode + ")")
            return
        }
        Copy-Item -LiteralPath $dumpPath -Destination $secondDump -Force

        $left = @(Get-Content -LiteralPath $firstDump)
        $right = @(Get-Content -LiteralPath $secondDump)
        $diffAt = -1
        $count = [Math]::Max($left.Count, $right.Count)
        for ($i = 0; $i -lt $count; $i++) {
            $a = if ($i -lt $left.Count) { $left[$i] } else { "<missing>" }
            $b = if ($i -lt $right.Count) { $right[$i] } else { "<missing>" }
            if ($a -ne $b) {
                $diffAt = $i + 1
                break
            }
        }

        if ($diffAt -ge 0) {
            Add-Fail ("V-01 builder not idempotent (hierarchy dumps differ at line " + $diffAt + ")")
        }
        else {
            $script:V01Passed = $true
            Add-Pass ("V-01 builder idempotent (2 builds, 0 diffs, " + $left.Count + " hierarchy lines)")
        }
    }
    catch {
        Add-Fail ("V-01 builder idempotent: " + $_.Exception.Message)
    }
}

function Invoke-V02([string[]]$Names, [bool]$NamesKnown) {
    if ($dry) {
        Add-CheckLine "[DRY] V-02 would run Tools/Validation/Validate-RepositoryLayout.ps1. Validate-CoreBoundaries.ps1 runs only when Assets/Scripts/Core or Assets/Scripts/Presentation changed."
        return
    }

    $layout = Join-Path $ProjectRoot "Tools/Validation/Validate-RepositoryLayout.ps1"
    $boundaries = Join-Path $ProjectRoot "Tools/Validation/Validate-CoreBoundaries.ps1"
    $hostNote = ""
    if ((Split-Path -Leaf (Get-ValidatorHost)) -ne "pwsh.exe") {
        $hostNote = " Validator host was " + (Split-Path -Leaf (Get-ValidatorHost)) + " because pwsh was not on PATH."
    }
    $layoutExit = Invoke-LoggedPwsh $layout "v02-layout"
    if ($layoutExit -ne 0) {
        Add-Fail ("V-02 repository layout failed (exit " + $layoutExit + ")." + $hostNote)
        return
    }

    if (-not $NamesKnown) {
        Add-Fail ("V-02 repository layout passed; core-boundary was not run because git could not list changed paths. " + $script:GitError + $hostNote)
        return
    }

    $coreChanged = @($Names | Where-Object {
            $_.StartsWith("Assets/Scripts/Core/") -or $_.StartsWith("Assets/Scripts/Presentation/")
        })
    if ($coreChanged.Count -eq 0) {
        Add-Pass ("V-02 repository layout valid; core-boundary not run (Core and Presentation unchanged)." + $hostNote)
        return
    }

    $boundaryExit = Invoke-LoggedPwsh $boundaries "v02-boundaries"
    if ($boundaryExit -ne 0) {
        Add-Fail ("V-02 core-boundary failed (exit " + $boundaryExit + ")." + $hostNote)
    }
    else {
        Add-Pass ("V-02 repository layout valid; core-boundary passed." + $hostNote)
    }
}

function Invoke-V03 {
    if ($dry) {
        Add-CheckLine "[DRY] V-03 would read Documentation/Design/captures/text_audit.json written by BuildLevel01. It checks angular size, facing, cone, word count, and one prompt panel."
        return
    }

    if (-not $script:V01Passed) {
        Add-Skip "V-03 text audit: scene was not rebuilt in this run"
        return
    }

    $auditPath = Join-Path $ProjectRoot "Documentation/Design/captures/text_audit.json"
    if (-not (Test-Path -LiteralPath $auditPath)) {
        Add-Fail "V-03 text audit file missing after the build"
        return
    }

    $audit = Get-Content -LiteralPath $auditPath -Raw | ConvertFrom-Json
    $selfTest = $false
    if ($null -ne $audit.PSObject.Properties["selfTestMirroredFlagged"]) {
        $selfTest = [bool]$audit.selfTestMirroredFlagged
    }
    if (-not $selfTest) {
        Add-Fail "V-03 facing self-test did not flag a mirrored forward"
        return
    }

    $violations = @()
    if ($null -ne $audit.PSObject.Properties["violations"] -and $null -ne $audit.violations) {
        $violations = @($audit.violations)
    }

    $checked = 0
    if ($null -ne $audit.PSObject.Properties["checked"]) { $checked = [int]$audit.checked }
    if ($violations.Count -eq 0) {
        Add-Pass ("V-03 text audit: " + $checked + " texts, 0 violations")
        return
    }

    $first = [string]$violations[0]
    Add-Fail ("V-03 text audit: " + $violations.Count + " violations; first: " + $first)
}

function Invoke-V07 {
    if ($dry) {
        Add-CheckLine "[DRY] V-07 would read Documentation/Design/captures/state_test.json written by BuildLevel01. Passed must converge to cyan, back to amber, then cyan, with flash at or below 3 Hz."
        return
    }

    if (-not $script:V01Passed) {
        Add-Skip "V-07 state test: scene was not rebuilt in this run"
        return
    }

    $statePath = Join-Path $ProjectRoot "Documentation/Design/captures/state_test.json"
    if (-not (Test-Path -LiteralPath $statePath)) {
        Add-Fail "V-07 state test file missing after the build"
        return
    }

    $state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
    $ok = $false
    if ($null -ne $state.PSObject.Properties["passed"]) { $ok = [bool]$state.passed }
    $flash = 0
    if ($null -ne $state.PSObject.Properties["flashHz"]) { $flash = [double]$state.flashHz }
    $damage = 0
    if ($null -ne $state.PSObject.Properties["damageElements"]) { $damage = [int]$state.damageElements }
    if ($ok -and $flash -le 3) {
        Add-Pass ("V-07 state test passed (flashHz " + $flash + ", damage elements " + $damage + ")")
        return
    }

    Add-Fail ("V-07 state test failed (passed " + $ok + ", flashHz " + $flash + ", damage elements " + $damage + ")")
}

function Invoke-V08 {
    if ($dry) {
        Add-CheckLine "[DRY] V-08 would read Documentation/Design/captures/puzzle_audit.json. Reference solutions must pass and XOR must stay unsolvable by one neuron."
        return
    }

    if (-not $script:V01Passed) {
        Add-Skip "V-08 puzzle data: scene was not rebuilt in this run"
        return
    }

    $auditPath = Join-Path $ProjectRoot "Documentation/Design/captures/puzzle_audit.json"
    if (-not (Test-Path -LiteralPath $auditPath)) {
        Add-Fail "V-08 puzzle audit file missing after the build"
        return
    }

    $audit = Get-Content -LiteralPath $auditPath -Raw | ConvertFrom-Json
    $ok = $false
    if ($null -ne $audit.PSObject.Properties["passed"]) { $ok = [bool]$audit.passed }
    $error = ""
    if ($null -ne $audit.PSObject.Properties["error"] -and $null -ne $audit.error) { $error = [string]$audit.error }
    if ($ok) {
        Add-Pass "V-08 puzzle references passed and XOR is unsolvable by one neuron"
        return
    }

    Add-Fail ("V-08 puzzle data failed: " + $error)
}

function Invoke-V05($Thresholds) {
    if ($dry) {
        Add-CheckLine "[DRY] V-05 would compare Documentation/Design/captures/editor_numbers.json with Tools/chamber01-thresholds.json. trianglesMax is a fail. drawCallsMax is a warning while calibrated is false."
        return
    }

    if (-not (Test-Path -LiteralPath $numbersPath)) {
        Add-Skip "V-05 editor numbers: file missing (operator step)"
        return
    }

    $numbers = Get-Content -LiteralPath $numbersPath -Raw | ConvertFrom-Json
    if ($null -eq $numbers.PSObject.Properties["triangles"] -or $null -eq $numbers.PSObject.Properties["drawCalls"]) {
        Add-Fail "V-05 editor numbers file is missing triangles or drawCalls"
        return
    }

    $triangles = [double]$numbers.triangles
    $drawCalls = [double]$numbers.drawCalls
    $triangleFail = $triangles -gt [double]$Thresholds.trianglesMax
    $drawFail = $drawCalls -gt [double]$Thresholds.drawCallsMax
    $detail = "triangles " + $triangles + " / " + $Thresholds.trianglesMax + ", drawCalls " + $drawCalls + " / " + $Thresholds.drawCallsMax

    if ($triangleFail) {
        Add-Fail ("V-05 editor numbers over the triangle budget: " + $detail)
        return
    }

    if ($drawFail) {
        if ($Thresholds.calibrated) {
            Add-Fail ("V-05 editor numbers over the draw-call budget: " + $detail)
        }
        else {
            Add-Warn ("V-05 drawCalls over 150 (uncalibrated, advisory); " + $detail)
        }
        return
    }

    Add-Pass ("V-05 editor numbers within thresholds (" + $detail + ")")
}

function Get-MetricMisses($Record, $Thresholds) {
    $misses = New-Object System.Collections.Generic.List[string]
    $label = [string]$Record.name

    foreach ($pair in @(
            @{ Field = "clippedConsolePct"; Max = "clippedConsolePctMax" },
            @{ Field = "clippedCenterExclWindowPct"; Max = "clippedCenterExclWindowPctMax" }
        )) {
        $field = $pair.Field
        if ($null -eq $Record.PSObject.Properties[$field]) {
            $misses.Add($label + " missing " + $field)
            continue
        }
        $value = $Record.$field
        if ($null -eq $value) {
            $misses.Add($label + " " + $field + " is null")
            continue
        }
        if ([double]$value -gt [double]$Thresholds.($pair.Max)) {
            $misses.Add($label + " " + $field + "=" + $value)
        }
    }

    if ($null -ne $Record.PSObject.Properties["consolePixelCount"] -and [int]$Record.consolePixelCount -eq 0) {
        $misses.Add($label + " console rect has no on-screen pixels")
    }
    if ($null -ne $Record.PSObject.Properties["windowPixelCount"] -and [int]$Record.windowPixelCount -eq 0) {
        $misses.Add($label + " window rect has no on-screen pixels")
    }

    if ($Thresholds.windowPeakBelowConsolePeak) {
        $windowPeak = $Record.windowPeakLum
        $consolePeak = $Record.consolePeakLum
        if ($null -eq $windowPeak -or $null -eq $consolePeak) {
            $misses.Add($label + " window or console peak is missing")
        }
        elseif ([double]$windowPeak -ge [double]$consolePeak) {
            $misses.Add($label + " windowPeakLum " + $windowPeak + " is not below consolePeakLum " + $consolePeak)
        }
    }

    return $misses
}

function Invoke-V06($Thresholds) {
    if ($dry) {
        Add-CheckLine ("[DRY] V-06 would compare Documentation/Design/captures/metrics.json with Tools/chamber01-thresholds.json for wp " + $Wp + ". Misses are warnings while calibrated is false.")
        return
    }

    if (-not (Test-Path -LiteralPath $metricsPath)) {
        Add-Skip "V-06 capture metrics: file missing (operator capture)"
        return
    }

    $metrics = Get-Content -LiteralPath $metricsPath -Raw | ConvertFrom-Json
    if ($null -eq $metrics.PSObject.Properties["records"] -or $null -eq $metrics.records) {
        Add-Skip "V-06 capture metrics: no records (operator capture)"
        return
    }

    $matched = New-Object System.Collections.Generic.List[object]
    foreach ($record in @($metrics.records)) {
        $token = ""
        if ($null -ne $record.PSObject.Properties["wp"]) { $token = [string]$record.wp }
        $name = ""
        if ($null -ne $record.PSObject.Properties["name"]) { $name = [string]$record.name }
        if ($token -eq $Wp -or $name.EndsWith("_" + $Wp)) {
            $matched.Add($record)
        }
    }

    if ($matched.Count -eq 0) {
        Add-Skip ("V-06 capture metrics: no records for wp " + $Wp + " (operator capture)")
        return
    }

    $misses = New-Object System.Collections.Generic.List[string]
    foreach ($record in $matched) {
        foreach ($miss in @(Get-MetricMisses $record $Thresholds)) {
            $misses.Add($miss)
        }
    }

    if ($misses.Count -eq 0) {
        Add-Pass ("V-06 " + $matched.Count + " " + $Wp + " captures within thresholds")
        return
    }

    $sample = $misses[0]
    $summary = $misses.Count.ToString() + " metric miss(es); first: " + $sample
    if ($Thresholds.calibrated) {
        Add-Fail ("V-06 " + $summary)
    }
    else {
        Add-Warn ("V-06 thresholds uncalibrated: " + $summary)
    }
}

function Invoke-V04 {
    if ($dry) {
        Add-CheckLine "[DRY] V-04 would read Temp/chamber01_scale_guard.txt written by the builder. Kenney roots must be scale (1,1,1) and Mat_Kenney_SpaceStation must use variation-a."
        return
    }

    if (-not $script:V01Passed) {
        Add-Skip "V-04 scale guard: scene was not rebuilt in this run"
        return
    }

    $report = Join-Path $ProjectRoot "Temp/chamber01_scale_guard.txt"
    if (-not (Test-Path -LiteralPath $report)) {
        Add-Fail "V-04 scale guard report missing after the build"
        return
    }

    $text = Get-Content -LiteralPath $report -Raw
    if ($text.StartsWith("PASS")) {
        Add-Pass "V-04 scale guard and variation-a atlas"
        return
    }

    Add-Fail ("V-04 scale guard failed: " + (($text -split "`n")[0]))
}

function Invoke-V09 {
    if ($dry) {
        Add-CheckLine "[DRY] V-09 would read Documentation/Design/captures/device_numbers.json. A missing file is an operator skip."
        return
    }

    $devicePath = Join-Path $ProjectRoot "Documentation/Design/captures/device_numbers.json"
    if (-not (Test-Path -LiteralPath $devicePath)) {
        Add-Skip "V-09 on-device numbers: file missing (operator step)"
        return
    }

    $device = Get-Content -LiteralPath $devicePath -Raw | ConvertFrom-Json
    if ($null -eq $thresholds) {
        Add-Fail "V-09 on-device numbers: thresholds file missing"
        return
    }

    $triangles = 0
    if ($null -ne $device.PSObject.Properties["triangles"]) { $triangles = [double]$device.triangles }
    if ($triangles -gt [double]$thresholds.trianglesMax) {
        Add-Fail ("V-09 on-device triangles " + $triangles + " over " + $thresholds.trianglesMax)
        return
    }

    Add-Pass ("V-09 on-device numbers within budget (triangles " + $triangles + ")")
}

function Invoke-V10([string[]]$Names, [bool]$NamesKnown) {
    if ($dry) {
        Add-CheckLine "[DRY] V-10 would run git diff --name-only, plus staged and untracked names, against Assets/ThirdParty/, Assets/Samples/, and Assets/Scripts/Core/."
        return
    }

    if (-not $NamesKnown) {
        Add-Fail ("V-10 forbidden-path diff: git could not list names. " + $script:GitError)
        return
    }

    $hits = New-Object System.Collections.Generic.List[string]
    foreach ($path in @($Names)) {
        if ([string]::IsNullOrWhiteSpace($path)) { continue }
        foreach ($prefix in $forbiddenPrefixes) {
            if ($path.StartsWith($prefix)) {
                if (-not $hits.Contains($path)) { $hits.Add($path) }
                break
            }
        }
    }

    if ($hits.Count -eq 0) {
        Add-Pass "V-10 forbidden-path diff: no forbidden prefixes"
        return
    }

    $shown = ($hits | Select-Object -First 8) -join ", "
    Add-Fail ("V-10 forbidden-path diff: " + $hits.Count + " path(s): " + $shown)
}

if ($dry) {
    Add-CheckLine ("[DRY] Verify-Chamber01 wp " + $Wp + ". Unity will not be launched.")
}

$names = @()
$namesKnown = $false
$thresholds = $null
if (-not $dry) {
    try {
        $names = @(Get-RepoNames)
        $namesKnown = $true
    }
    catch {
        $namesKnown = $false
        $script:GitError = $_.Exception.Message
    }

    if (Test-Path -LiteralPath $thresholdsPath) {
        $thresholds = Get-Content -LiteralPath $thresholdsPath -Raw | ConvertFrom-Json
    }
}

Invoke-V01
Invoke-V02 -Names $names -NamesKnown:$namesKnown
Invoke-V03
Invoke-V07
Invoke-V08
Invoke-V04
if ($dry) {
    Invoke-V05 -Thresholds $null
    Invoke-V06 -Thresholds $null
}
elseif ($null -eq $thresholds) {
    Add-Fail "V-05 editor numbers: thresholds file missing"
    Add-Fail "V-06 capture metrics: thresholds file missing"
}
else {
    Invoke-V05 -Thresholds $thresholds
    Invoke-V06 -Thresholds $thresholds
}
Invoke-V10 -Names $names -NamesKnown:$namesKnown
Invoke-V09

if ($dry) {
    Add-CheckLine "RESULT: DRY-RUN (Unity was not launched)"
}
else {
    $result = if ($script:FailCount -gt 0) { "FAIL" } else { "PASS" }
    Add-CheckLine ("RESULT: " + $result + " (" + $script:FailCount + " fail, " + $script:WarnCount + " warn, " + $script:SkipCount + " skip)")
}

if (-not $dry) {
    $logDir = Split-Path -Parent $logPath
    if (-not (Test-Path -LiteralPath $logDir)) {
        New-Item -ItemType Directory -Path $logDir | Out-Null
    }
    $script:Lines | Set-Content -LiteralPath $logPath -Encoding utf8
}

if ($script:FailCount -gt 0) { exit 1 }
exit 0
