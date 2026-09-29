@echo off
:: Self-elevate to Administrator if not already elevated
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo Requesting Administrator privileges to modify Meta Horizon files...
    powershell -Command "Start-Process cmd -ArgumentList '/c \"\"%~f0\"\"' -Verb RunAs"
    exit /b
)

echo ========================================================
echo  Unlocking Meta Quest Link for AMD Radeon 860M Graphics
echo ========================================================
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Whitelist-GpuInOculus.ps1"
echo.
echo Press any key to exit...
pause >nul
