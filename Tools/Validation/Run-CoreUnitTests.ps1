<#
.SYNOPSIS
    Compiles and executes pure C# EditMode unit tests for Core assemblies.
.DESCRIPTION
    Builds Convergence.Core.Math, Neural, Training, Puzzles, and tests using Mono Roslyn,
    then executes all NUnit [Test] methods.
#>

$ErrorActionPreference = "Stop"

$workspace = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$tempDir = Join-Path $workspace "Temp"
if (-not (Test-Path $tempDir)) {
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
}

$monoExe = "C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Data\MonoBleedingEdge\bin\mono.exe"
$cscExe = "C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Data\MonoBleedingEdge\lib\mono\msbuild\Current\bin\Roslyn\csc.exe"
$nunitDll = "C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.ext.nunit\net472\unity-custom\nunit.framework.dll"

Write-Host "Compiling Core assemblies..." -ForegroundColor Cyan

# 1. Core.Math
$mathOut = "/out:" + (Join-Path $tempDir "Convergence.Core.Math.dll")
$mathSrc = (Get-ChildItem (Join-Path $workspace "Assets\Scripts\Core\Math\*.cs")).FullName
& $monoExe $cscExe "/target:library" $mathOut $mathSrc
if ($LASTEXITCODE -ne 0) { throw "Compilation failed for Core.Math" }

# 2. Core.Neural
$neuralOut = "/out:" + (Join-Path $tempDir "Convergence.Core.Neural.dll")
$mathRef = "/reference:" + (Join-Path $tempDir "Convergence.Core.Math.dll")
$neuralSrc = (Get-ChildItem (Join-Path $workspace "Assets\Scripts\Core\Neural\*.cs")).FullName
& $monoExe $cscExe "/target:library" $mathRef $neuralOut $neuralSrc
if ($LASTEXITCODE -ne 0) { throw "Compilation failed for Core.Neural" }

# 3. Core.Training
$trainingOut = "/out:" + (Join-Path $tempDir "Convergence.Core.Training.dll")
$trainingSrc = (Get-ChildItem (Join-Path $workspace "Assets\Scripts\Core\Training\*.cs")).FullName
& $monoExe $cscExe "/target:library" $trainingOut $trainingSrc
if ($LASTEXITCODE -ne 0) { throw "Compilation failed for Core.Training" }

# 4. Core.Puzzles
$puzzlesOut = "/out:" + (Join-Path $tempDir "Convergence.Core.Puzzles.dll")
$neuralRef = "/reference:" + (Join-Path $tempDir "Convergence.Core.Neural.dll")
$puzzlesSrc = (Get-ChildItem (Join-Path $workspace "Assets\Scripts\Core\Puzzles\*.cs")).FullName
& $monoExe $cscExe "/target:library" $mathRef $neuralRef $puzzlesOut $puzzlesSrc
if ($LASTEXITCODE -ne 0) { throw "Compilation failed for Core.Puzzles" }

# 5. Tests
$testsOut = "/out:" + (Join-Path $tempDir "Convergence.Tests.EditMode.dll")
$puzzlesRef = "/reference:" + (Join-Path $tempDir "Convergence.Core.Puzzles.dll")
$nunitRef = "/reference:$nunitDll"
$testsSrc = (Get-ChildItem (Join-Path $workspace "Tests\EditMode\Core\*.cs")).FullName
& $monoExe $cscExe "/target:library" $mathRef $neuralRef $puzzlesRef $nunitRef $testsOut $testsSrc
if ($LASTEXITCODE -ne 0) { throw "Compilation failed for EditMode tests" }

Write-Host "Running EditMode Unit Tests..." -ForegroundColor Cyan

[Reflection.Assembly]::LoadFrom((Resolve-Path (Join-Path $tempDir "Convergence.Core.Math.dll"))) | Out-Null
[Reflection.Assembly]::LoadFrom((Resolve-Path (Join-Path $tempDir "Convergence.Core.Neural.dll"))) | Out-Null
[Reflection.Assembly]::LoadFrom((Resolve-Path (Join-Path $tempDir "Convergence.Core.Puzzles.dll"))) | Out-Null
[Reflection.Assembly]::LoadFrom($nunitDll) | Out-Null
$testAsm = [Reflection.Assembly]::LoadFrom((Resolve-Path (Join-Path $tempDir "Convergence.Tests.EditMode.dll")))

$passed = 0
$failed = 0

foreach ($type in $testAsm.GetTypes()) {
    $fixture = $type.GetCustomAttributes($true) | Where-Object { $_.GetType().Name -eq 'TestFixtureAttribute' }
    if ($fixture) {
        $instance = [Activator]::CreateInstance($type)
        foreach ($method in $type.GetMethods()) {
            $testAttr = $method.GetCustomAttributes($true) | Where-Object { $_.GetType().Name -eq 'TestAttribute' }
            if ($testAttr) {
                try {
                    $method.Invoke($instance, $null)
                    Write-Host "  PASS: $($type.Name).$($method.Name)" -ForegroundColor Green
                    $passed++
                } catch {
                    Write-Host "  FAIL: $($type.Name).$($method.Name) -> $($_.Exception.InnerException.Message)" -ForegroundColor Red
                    $failed++
                }
            }
        }
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Test Results: Total $($passed + $failed) | Passed: $passed | Failed: $failed" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Red" })
Write-Host "========================================" -ForegroundColor Cyan

if ($failed -gt 0) {
    exit 1
}
