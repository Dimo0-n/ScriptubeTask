#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Local test runner — mirrors the CI parameterization.

.PARAMETER Area
    Which test area to run: api | ui | webhook | all  (default: all)

.PARAMETER Suite
    Which suite to run: smoke | regression  (default: smoke)

.PARAMETER Threads
    Number of parallel test threads (default: 2)

.PARAMETER NoBuild
    Skip build step (use when solution is already built)

.PARAMETER LiveApi
    Set RUN_LIVE_API=true for live API execution (default: true)

.PARAMETER LiveUi
    Set RUN_LIVE_UI=true for live UI execution (default: false for local)

.PARAMETER LiveWebhooks
    Set RUN_LIVE_WEBHOOKS=true for live webhook tests (default: false for local)

.EXAMPLE
    .\scripts\run-tests.ps1
    .\scripts\run-tests.ps1 -Area api -Suite regression -Threads 4
    .\scripts\run-tests.ps1 -Area ui -Suite smoke -LiveUi
    .\scripts\run-tests.ps1 -Area all -Suite regression -Threads 4 -LiveApi -LiveUi -LiveWebhooks
#>

[CmdletBinding()]
param(
    [ValidateSet("all", "api", "ui", "webhook")]
    [string]$Area = "all",

    [ValidateSet("smoke", "regression")]
    [string]$Suite = "smoke",

    [ValidateSet("1", "2", "4")]
    [string]$Threads = "2",

    [switch]$NoBuild,
    [switch]$LiveApi,
    [switch]$LiveUi,
    [switch]$LiveWebhooks
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repoRoot "Scriptube.Tests.sln"

# ── Environment setup ────────────────────────────────────────────────────────
$dotEnvFile = Join-Path $repoRoot ".env"
if (Test-Path $dotEnvFile) {
    Get-Content $dotEnvFile | ForEach-Object {
        if ($_ -match "^\s*([^#=]+?)\s*=\s*(.*)$") {
            $name  = $Matches[1].Trim()
            $value = $Matches[2].Trim().Trim('"').Trim("'")
            [System.Environment]::SetEnvironmentVariable($name, $value, "Process")
        }
    }
    Write-Host "Loaded .env from $dotEnvFile" -ForegroundColor DarkGray
}

if ($LiveApi)      { $env:RUN_LIVE_API      = "true" }
if ($LiveUi)       { $env:RUN_LIVE_UI       = "true" }
if ($LiveWebhooks) { $env:RUN_LIVE_WEBHOOKS = "true" }

Write-Host ""
Write-Host "=== Scriptube Test Runner ===" -ForegroundColor Cyan
Write-Host "  Area    : $Area"
Write-Host "  Suite   : $Suite"
Write-Host "  Threads : $Threads"
Write-Host "  LiveApi : $($env:RUN_LIVE_API -eq 'true')"
Write-Host "  LiveUi  : $($env:RUN_LIVE_UI -eq 'true')"
Write-Host "  LiveWH  : $($env:RUN_LIVE_WEBHOOKS -eq 'true')"
Write-Host ""

# ── Build ────────────────────────────────────────────────────────────────────
if (-not $NoBuild) {
    Write-Host "=== [1/2] Build ===" -ForegroundColor Cyan
    dotnet build $solution --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { Write-Error "Build failed."; exit 1 }
}

# ── Compute NUnit filter ─────────────────────────────────────────────────────
$filterParts = @()

switch ($Area) {
    "api"     { $filterParts += "Category=API" }
    "ui"      { $filterParts += "Category=UI" }
    "webhook" { $filterParts += "Category=Webhook" }
}

switch ($Suite) {
    "smoke"      { $filterParts += "Category=Smoke" }
    "regression" { $filterParts += "Category=Regression" }
}

$filterArg = @()
if ($filterParts.Count -gt 0) {
    $filterExpr = $filterParts -join "&"
    $filterArg  = @("--filter", $filterExpr)
    Write-Host "  Filter  : $filterExpr" -ForegroundColor DarkGray
}

# ── Run tests ────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "=== [2/2] Run Tests ===" -ForegroundColor Cyan

$timestamp  = Get-Date -Format "yyyyMMdd-HHmmss"
$resultsDir = Join-Path $repoRoot "TestResults\$timestamp"

$dotnetArgs = @(
    "test", $solution,
    "--configuration", "Release",
    "--no-build",
    "--logger", "console;verbosity=normal",
    "--logger", "trx;LogFileName=results.trx",
    "--results-directory", $resultsDir,
    "-m:$Threads"
) + $filterArg

& dotnet @dotnetArgs
$exitCode = $LASTEXITCODE

Write-Host ""
if ($exitCode -eq 0) {
    Write-Host "✅ Test run completed — all tests passed/skipped." -ForegroundColor Green
}
else {
    Write-Host "❌ Test run completed — failures detected. Check output above." -ForegroundColor Red
}

Write-Host "Results: $resultsDir" -ForegroundColor DarkGray
Write-Host ""

exit $exitCode
