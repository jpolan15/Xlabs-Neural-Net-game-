<#
.SYNOPSIS
    Runs Unity in batchmode to build Level 1 scene and broken presets.
#>

$unityExe = "C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe"
$projectPath = (Get-Item .).FullName
$logPath = Join-Path $projectPath "Temp\unity_build.log"

$unityArgs = "-batchmode -nographics -projectPath `"$projectPath`" -executeMethod Convergence.EditorTools.Level01SceneBuilder.BuildLevel01 -quit -logFile `"$logPath`""

Write-Host "Running Unity batchmode to execute Level01SceneBuilder.BuildLevel01..." -ForegroundColor Cyan
$process = Start-Process -FilePath $unityExe -ArgumentList $unityArgs -Wait -PassThru

Write-Host "Unity exited with code: $($process.ExitCode)"
if (Test-Path $logPath) {
    Write-Host "`n--- Unity Log Tail ---" -ForegroundColor Cyan
    Get-Content $logPath -Tail 50
}
exit $process.ExitCode
