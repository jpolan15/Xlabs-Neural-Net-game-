<#
.SYNOPSIS
    Runs PlayMode integration tests headlessly in Unity batchmode.
#>

$unityExe = "C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe"
$projectPath = (Get-Item .).FullName
$resultsPath = Join-Path $projectPath "Tests\Results\playmode_results.xml"
$logPath = Join-Path $projectPath "Temp\playmode_test.log"

if (-not (Test-Path (Split-Path $resultsPath))) {
    New-Item -ItemType Directory -Path (Split-Path $resultsPath) -Force | Out-Null
}

$unityArgs = "-batchmode -nographics -projectPath `"$projectPath`" -runTests -testPlatform PlayMode -testFilter `"Convergence.Tests.PlayMode`" -testResults `"$resultsPath`" -logFile `"$logPath`""

Write-Host "Running Unity PlayMode Integration Tests..." -ForegroundColor Cyan
$process = Start-Process -FilePath $unityExe -ArgumentList $unityArgs -Wait -PassThru

Write-Host "Unity exited with code: $($process.ExitCode)"
if (Test-Path $resultsPath) {
    [xml]$xml = Get-Content $resultsPath
    $testSuite = $xml.SelectSingleNode("//test-suite[@type='TestFixture']")
    $total = $testSuite.GetAttribute("total")
    $passed = $testSuite.GetAttribute("passed")
    $failed = $testSuite.GetAttribute("failed")
    $skipped = $testSuite.GetAttribute("skipped")
    
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "PlayMode Results: Total: $total | Passed: $passed | Failed: $failed | Skipped: $skipped" -ForegroundColor Green
    Write-Host "========================================`n" -ForegroundColor Cyan
} elseif (Test-Path $logPath) {
    Write-Host "`n--- Unity Test Log Tail ---" -ForegroundColor Yellow
    Get-Content $logPath -Tail 50
}

exit $process.ExitCode
