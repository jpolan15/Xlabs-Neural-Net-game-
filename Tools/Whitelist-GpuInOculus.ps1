# Whitelists AMD Radeon 860M in Meta Horizon Compatibility.json to remove the red Link banner and enable Quest Link.
$ErrorActionPreference = "Stop"

$path = "C:\Program Files\Meta Horizon\Support\oculus-runtime\Compatibility.json"
if (-not (Test-Path $path)) {
    Write-Host "Meta Horizon Compatibility.json not found at: $path" -ForegroundColor Red
    exit 1
}

Write-Host "Reading $path..." -ForegroundColor Cyan
$jsonText = [System.IO.File]::ReadAllText($path)
$json = $jsonText | ConvertFrom-Json

$gpuEntry = [PSCustomObject]@{
    Name = "AMD Radeon(TM) 860M Graphics"
    Vendor = "AMD"
    PID = "1114"
    SubsysVID = "Any"
}

$alreadyInWhite = ($json.VideoCardWhiteList | Where-Object { $_.PID -eq "1114" }) -ne $null
$alreadyInMin = ($json.VideoCardMinSpecList | Where-Object { $_.PID -eq "1114" }) -ne $null

if (-not $alreadyInWhite) {
    Write-Host "Adding AMD Radeon 860M (PID: 1114) to VideoCardWhiteList..." -ForegroundColor Green
    $json.VideoCardWhiteList = @($json.VideoCardWhiteList) + $gpuEntry
}

if (-not $alreadyInMin) {
    Write-Host "Adding AMD Radeon 860M (PID: 1114) to VideoCardMinSpecList..." -ForegroundColor Green
    $json.VideoCardMinSpecList = @($json.VideoCardMinSpecList) + $gpuEntry
}

# Create backup
$backup = "C:\Program Files\Meta Horizon\Support\oculus-runtime\Compatibility.json.orig"
if (-not (Test-Path $backup)) {
    Copy-Item $path $backup -Force
    Write-Host "Created backup at $backup" -ForegroundColor Yellow
}

$newJson = $json | ConvertTo-Json -Depth 10
[System.IO.File]::WriteAllText($path, $newJson, [System.Text.Encoding]::UTF8)
Write-Host "Successfully updated Compatibility.json!" -ForegroundColor Green

Write-Host "Restarting OVRService..." -ForegroundColor Cyan
Restart-Service -Name "OVRService" -Force
Write-Host "OVRService restarted! Quest Link is now unlocked." -ForegroundColor Green
