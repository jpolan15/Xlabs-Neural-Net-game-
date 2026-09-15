<#
.SYNOPSIS
    Validates that Core assemblies contain no forbidden Unity or Meta SDK dependencies.

.DESCRIPTION
    Scans Assets/Scripts/Core/**/*.cs for forbidden using directives.
    Also validates that Core .asmdef files do not reference forbidden assemblies.

    Exits 0 if all checks pass.
    Exits 1 if any violation is found or if required Core structure is missing.

.NOTES
    Required by: AGENTS.md (root), Assets/Scripts/Core/AGENTS.md
    Run when:    Any Core assembly file or .asmdef changes.
    CI command:  pwsh Tools/Validation/Validate-CoreBoundaries.ps1
#>

[CmdletBinding()]
param (
    [string]$ProjectRoot = "",
    [switch]$WhatIf
)

# Resolve ProjectRoot — $PSScriptRoot may be empty when invoked via -File from another directory
if (-not $ProjectRoot) {
    $scriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
    $ProjectRoot = Split-Path -Parent (Split-Path -Parent $scriptDir)
}

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ──────────────────────────────────────────────────────────────────────────────
# Configuration
# ──────────────────────────────────────────────────────────────────────────────

$coreRoot = Join-Path $ProjectRoot "Assets/Scripts/Core"

# Forbidden using directives in source files
$forbiddenUsings = @(
    "using UnityEngine",
    "using UnityEngine.UI",
    "using UnityEngine.XR",
    "using Unity.XR",
    "using UnityEngine.Audio",
    "using UnityEngine.AddressableAssets",
    "using Meta.",
    "using Oculus."
)

# Forbidden assembly references in .asmdef files (partial match)
$forbiddenAsmRefs = @(
    "Unity.XR",
    "UnityEngine.UI",
    "Meta.",
    "Oculus.",
    "Convergence.Gameplay",
    "Convergence.XR",
    "Convergence.Presentation"
)

# Expected Core assemblies (warns if missing, fails on bad content)
$expectedAssemblies = @(
    "Convergence.Core.Math",
    "Convergence.Core.Neural",
    "Convergence.Core.Training",
    "Convergence.Core.Puzzles"
)

# ──────────────────────────────────────────────────────────────────────────────
# Helpers
# ──────────────────────────────────────────────────────────────────────────────

$violations = [System.Collections.Generic.List[string]]::new()
$warnings   = [System.Collections.Generic.List[string]]::new()

function Add-Violation([string]$message) {
    $violations.Add($message)
    Write-Error "VIOLATION: $message"
}

function Add-Warning([string]$message) {
    $warnings.Add($message)
    Write-Warning "WARNING: $message"
}

# ──────────────────────────────────────────────────────────────────────────────
# Guard: Core directory must exist
# ──────────────────────────────────────────────────────────────────────────────

if (-not (Test-Path $coreRoot)) {
    Add-Violation "Core directory not found: '$coreRoot'. Cannot validate an absent Core layer."
    exit 1
}

# ──────────────────────────────────────────────────────────────────────────────
# Guard: Core directory must not be unexpectedly empty of .cs files
# ──────────────────────────────────────────────────────────────────────────────

$csFiles = @(Get-ChildItem $coreRoot -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue)
if ($csFiles.Count -eq 0) {
    Add-Warning "Core directory contains no .cs files. Source scan is skipped (nothing to check)."
    Add-Warning "Add Core source files and re-run this validator."
}

# ──────────────────────────────────────────────────────────────────────────────
# Check 1: Scan .cs source files for forbidden using directives
# ──────────────────────────────────────────────────────────────────────────────

Write-Host "`n[1/3] Scanning Core source files for forbidden using directives..."
Write-Host "      Path: $coreRoot"
Write-Host "      Files: $($csFiles.Count)"

foreach ($file in $csFiles) {
    $lines = Get-Content $file.FullName
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i].Trim()
        foreach ($forbidden in $forbiddenUsings) {
            if ($line.StartsWith($forbidden)) {
                Add-Violation "Forbidden directive '$forbidden' in $($file.FullName):$($i + 1)"
            }
        }
    }
}

# ──────────────────────────────────────────────────────────────────────────────
# Check 2: Scan .asmdef files for forbidden assembly references
# ──────────────────────────────────────────────────────────────────────────────

Write-Host "`n[2/3] Scanning Core .asmdef files for forbidden assembly references..."

$asmdefFiles = @(Get-ChildItem $coreRoot -Recurse -Filter "*.asmdef" -ErrorAction SilentlyContinue)
Write-Host "      .asmdef files found: $($asmdefFiles.Count)"

foreach ($asmdef in $asmdefFiles) {
    $content = Get-Content $asmdef.FullName -Raw
    foreach ($forbidden in $forbiddenAsmRefs) {
        # Use case-sensitive search; assembly names are case-sensitive in Unity
        if ($content -cmatch [regex]::Escape($forbidden)) {
            Add-Violation "Forbidden assembly reference '$forbidden' found in $($asmdef.FullName)"
        }
    }
}

# ──────────────────────────────────────────────────────────────────────────────
# Check 3: Warn if expected Core assemblies are missing
# ──────────────────────────────────────────────────────────────────────────────

Write-Host "`n[3/3] Checking for expected Core assembly definitions..."

foreach ($expectedName in $expectedAssemblies) {
    $found = $asmdefFiles | Where-Object { $_.BaseName -eq $expectedName }
    if (-not $found) {
        Add-Warning "Expected assembly definition not found: '$expectedName.asmdef' (may not yet be created)"
    } else {
        Write-Host "      Found: $($found.FullName)"
    }
}

# ──────────────────────────────────────────────────────────────────────────────
# Summary
# ──────────────────────────────────────────────────────────────────────────────

Write-Host "`n──────────────────────────────────────────────"
Write-Host "Core Boundary Validation Summary"
Write-Host "──────────────────────────────────────────────"
Write-Host "  Source files scanned : $($csFiles.Count)"
Write-Host "  .asmdef files scanned: $($asmdefFiles.Count)"
Write-Host "  Violations           : $($violations.Count)"
Write-Host "  Warnings             : $($warnings.Count)"

if ($violations.Count -gt 0) {
    Write-Host "`nVIOLATIONS FOUND. Fix the items above before reporting task completion." -ForegroundColor Red
    exit 1
}

if ($warnings.Count -gt 0) {
    Write-Host "`nValidation passed with warnings. Review warnings above." -ForegroundColor Yellow
} else {
    Write-Host "`nAll Core boundary checks passed." -ForegroundColor Green
}

exit 0
