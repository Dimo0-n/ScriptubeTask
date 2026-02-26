#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Pre-commit checks: format verification and build (warnings as errors).

.DESCRIPTION
    Run this script before committing to ensure code quality gates pass.
    Intended for local use and as a reference for CI gate steps.

.EXAMPLE
    .\scripts\precommit.ps1
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repoRoot "Scriptube.Tests.sln"

Write-Host "`n=== [1/3] Restore ===" -ForegroundColor Cyan
dotnet restore $solution
if ($LASTEXITCODE -ne 0) { Write-Error "dotnet restore failed."; exit 1 }

Write-Host "`n=== [2/3] Format check ===" -ForegroundColor Cyan
dotnet format $solution --verify-no-changes --verbosity diagnostic
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "Format violations found. Run the following to auto-fix:" -ForegroundColor Yellow
    Write-Host "  dotnet format $solution" -ForegroundColor Yellow
    exit 1
}

Write-Host "`n=== [3/3] Build (--warnaserror) ===" -ForegroundColor Cyan
dotnet build $solution --configuration Release --no-restore --warnaserror
if ($LASTEXITCODE -ne 0) { Write-Error "Build failed."; exit 1 }

Write-Host "`n✅ All pre-commit checks passed." -ForegroundColor Green
