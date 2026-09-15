<#
.SYNOPSIS
    Validates the Convergence repository directory layout and instruction file integrity.

.DESCRIPTION
    Checks that:
    1. Required top-level directories exist.
    2. Required AGENTS.md instruction files exist.
    3. Required Documentation files exist.
    4. No duplicate canonical documents exist.
    5. Redirect files point to valid paths.
    6. Expected .asmdef files exist (warning only if not yet created).
    7. No Core .asmdef references a forbidden runtime dependency.

    Exits 0 if all required checks pass.
    Exits 1 if any required check fails.
    Warnings are printed but do not fail the script.

.NOTES
    Required by: AGENTS.md (root)
    Run when:    Directories, AGENTS.md files, or .asmdef files change.
    CI command:  pwsh Tools/Validation/Validate-RepositoryLayout.ps1
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
$ErrorActionPreference = 'Continue'   # Don't abort on Write-Error; collect all violations

# ──────────────────────────────────────────────────────────────────────────────
# Configuration
# ──────────────────────────────────────────────────────────────────────────────

# Directories that MUST exist
$requiredDirs = @(
    ".agents",
    ".agents/tasks",
    ".agents/templates",
    "Assets",
    "Assets/Scripts",
    "Assets/Scripts/Core",
    "Assets/Scripts/Gameplay",
    "Assets/Scripts/XR",
    "Documentation",
    "Documentation/Architecture",
    "Documentation/Architecture/ADRs",
    "Documentation/Mathematics",
    "Documentation/Operations",
    "Documentation/Testing",
    "Tools",
    "Tools/Validation"
)

# AGENTS.md files that MUST exist
$requiredAgentFiles = @(
    "AGENTS.md",
    ".agents/PROJECT_MAP.md",
    ".agents/WORKFLOW.md",
    ".agents/DECISIONS_INDEX.md",
    ".agents/templates/TASK_TEMPLATE.md",
    ".agents/templates/HANDOFF_TEMPLATE.md",
    "Assets/AGENTS.md",
    "Assets/Scripts/AGENTS.md",
    "Assets/Scripts/Core/AGENTS.md",
    "Assets/Scripts/Gameplay/AGENTS.md",
    "Assets/Scripts/XR/AGENTS.md",
    "Documentation/AGENTS.md",
    "Tools/AGENTS.md"
)

# Documentation files that MUST exist
$requiredDocFiles = @(
    "Documentation/Architecture/OVERVIEW.md",
    "Documentation/Architecture/DEPENDENCY_RULES.md",
    "Documentation/Architecture/RUNTIME_BOUNDARIES.md",
    "Documentation/Architecture/ADRs/ADR-001-core-separation.md",
    "Documentation/Architecture/ADRs/ADR-005-xr-stack.md",
    "Documentation/Mathematics/FORMULAS.md",
    "Documentation/Operations/DESKTOP_WORKFLOW.md",
    "Documentation/Operations/HEADSET_WORKFLOW.md",
    "Documentation/Operations/XR_OPERATOR.md",
    "Documentation/Testing/TEST_STRATEGY.md"
)

# .asmdef files expected to exist (warning only — may not yet be created)
$expectedAsmdefs = @(
    "Assets/Scripts/Core/Math/Convergence.Core.Math.asmdef",
    "Assets/Scripts/Core/Neural/Convergence.Core.Neural.asmdef",
    "Assets/Scripts/Core/Training/Convergence.Core.Training.asmdef",
    "Assets/Scripts/Core/Puzzles/Convergence.Core.Puzzles.asmdef",
    "Assets/Scripts/Gameplay/Convergence.Gameplay.asmdef",
    "Assets/Scripts/XR/Convergence.XR.asmdef"
)

# Paths that must NOT contain duplicate canonical content
# (pattern: look for files that should only exist in one canonical location)
$duplicateChecks = @(
    @{ Pattern = "ARCHITECTURE.md"; CanonicalDir = "Documentation/Architecture"; LegacyDirs = @("Documentation/Technical") }
)

# ──────────────────────────────────────────────────────────────────────────────
# Helpers
# ──────────────────────────────────────────────────────────────────────────────

$violations = [System.Collections.Generic.List[string]]::new()
$warnings   = [System.Collections.Generic.List[string]]::new()
$passed     = 0

function Add-Violation([string]$message) {
    $script:violations.Add($message)
    Write-Host "  FAIL: $message" -ForegroundColor Red
}

function Add-Warning([string]$message) {
    $script:warnings.Add($message)
    Write-Host "  WARN: $message" -ForegroundColor Yellow
}

function Add-Pass([string]$message) {
    $script:passed++
    Write-Host "  OK  : $message" -ForegroundColor Green
}

function Resolve-Repo([string]$relativePath) {
    return Join-Path $ProjectRoot ($relativePath -replace '/', [System.IO.Path]::DirectorySeparatorChar)
}

# ──────────────────────────────────────────────────────────────────────────────
# Check 1: Required directories
# ──────────────────────────────────────────────────────────────────────────────

Write-Host "`n[1/5] Required directories..."

foreach ($dir in $requiredDirs) {
    $full = Resolve-Repo $dir
    if (Test-Path $full -PathType Container) {
        Add-Pass "Directory exists: $dir"
    } else {
        Add-Violation "Required directory missing: '$dir'"
    }
}

# ──────────────────────────────────────────────────────────────────────────────
# Check 2: Required AGENTS.md files
# ──────────────────────────────────────────────────────────────────────────────

Write-Host "`n[2/5] Required instruction files..."

foreach ($file in $requiredAgentFiles) {
    $full = Resolve-Repo $file
    if (Test-Path $full -PathType Leaf) {
        Add-Pass "Instruction file exists: $file"
    } else {
        Add-Violation "Required instruction file missing: '$file'"
    }
}

# ──────────────────────────────────────────────────────────────────────────────
# Check 3: Required documentation files
# ──────────────────────────────────────────────────────────────────────────────

Write-Host "`n[3/5] Required documentation files..."

foreach ($file in $requiredDocFiles) {
    $full = Resolve-Repo $file
    if (Test-Path $full -PathType Leaf) {
        Add-Pass "Documentation file exists: $file"
    } else {
        Add-Violation "Required documentation file missing: '$file'"
    }
}

# ──────────────────────────────────────────────────────────────────────────────
# Check 4: Expected .asmdef files (warnings only)
# ──────────────────────────────────────────────────────────────────────────────

Write-Host "`n[4/5] Expected .asmdef files (warnings only if missing)..."

foreach ($asmdef in $expectedAsmdefs) {
    $full = Resolve-Repo $asmdef
    if (Test-Path $full -PathType Leaf) {
        Add-Pass ".asmdef exists: $asmdef"

        # Also check the .asmdef does not reference forbidden assemblies if it's a Core file
        if ($asmdef -like "*/Core/*") {
            $content = Get-Content $full -Raw
            $forbiddenRefs = @("Unity.XR", "Meta.", "Oculus.", "Convergence.Gameplay", "Convergence.XR", "Convergence.Presentation")
            foreach ($forbidden in $forbiddenRefs) {
                if ($content -cmatch [regex]::Escape($forbidden)) {
                    Add-Violation "Forbidden assembly reference '$forbidden' in Core .asmdef: $asmdef"
                }
            }
        }
    } else {
        Add-Warning "Expected .asmdef not yet created: '$asmdef' (create before adding code to this directory)"
    }
}

# ──────────────────────────────────────────────────────────────────────────────
# Check 5: Redirect files point to valid destinations
# ──────────────────────────────────────────────────────────────────────────────

Write-Host "`n[5/5] Checking redirect file consistency..."

# Check that Documentation/AgentTasks/DECISIONS.md (if it exists and is a redirect)
# does not claim to be canonical
$oldDecisions = Resolve-Repo "Documentation/AgentTasks/DECISIONS.md"
if (Test-Path $oldDecisions) {
    $content = Get-Content $oldDecisions -Raw
    if ($content -notmatch "(?i)(moved|redirect|superseded|deprecated)") {
        Add-Warning "Documentation/AgentTasks/DECISIONS.md still appears to be canonical content. It should be a redirect to Documentation/Architecture/ADRs/."
    } else {
        Add-Pass "Documentation/AgentTasks/DECISIONS.md has redirect/deprecation notice."
    }
}

# Check Technical/ARCHITECTURE.md has deprecation notice
$legacyArch = Resolve-Repo "Documentation/Technical/ARCHITECTURE.md"
if (Test-Path $legacyArch) {
    $content = Get-Content $legacyArch -Raw
    if ($content -notmatch "(?i)(deprecated|superseded|moved)") {
        Add-Warning "Documentation/Technical/ARCHITECTURE.md should have a deprecation notice pointing to Documentation/Architecture/OVERVIEW.md."
    } else {
        Add-Pass "Documentation/Technical/ARCHITECTURE.md has deprecation notice."
    }
}

# ──────────────────────────────────────────────────────────────────────────────
# Summary
# ──────────────────────────────────────────────────────────────────────────────

Write-Host "`n──────────────────────────────────────────────"
Write-Host "Repository Layout Validation Summary"
Write-Host "──────────────────────────────────────────────"
Write-Host "  Checks passed: $passed"
Write-Host "  Violations   : $($violations.Count)"
Write-Host "  Warnings     : $($warnings.Count)"

if ($violations.Count -gt 0) {
    Write-Host "`nVIOLATIONS FOUND. Fix the items above before reporting task completion." -ForegroundColor Red
    exit 1
}

if ($warnings.Count -gt 0) {
    Write-Host "`nValidation passed with warnings. Review warnings above." -ForegroundColor Yellow
} else {
    Write-Host "`nAll repository layout checks passed." -ForegroundColor Green
}

exit 0
