#!/usr/bin/env pwsh
# Generate Demo Lucene Index for NeoLuke
# This script runs the NeoLuke.DemoGenerator console app to create a sample index

Write-Host "NeoLuke Demo Index Generator" -ForegroundColor Cyan
Write-Host "=============================" -ForegroundColor Cyan
Write-Host ""

$scriptDir = $PSScriptRoot
$projectPath = Join-Path $scriptDir "NeoLuke.DemoGenerator"

# Check if the project exists
if (-not (Test-Path (Join-Path $projectPath "NeoLuke.DemoGenerator.csproj"))) {
    Write-Host "Error: NeoLuke.DemoGenerator project not found at $projectPath" -ForegroundColor Red
    exit 1
}

# Run the demo generator
Write-Host "Running demo generator..." -ForegroundColor Yellow
Push-Location $projectPath

try {
    dotnet run
    $exitCode = $LASTEXITCODE

    if ($exitCode -eq 0) {
        Write-Host ""
        Write-Host "Demo index generation completed successfully!" -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "Demo index generation failed with exit code: $exitCode" -ForegroundColor Red
        exit $exitCode
    }
} finally {
    Pop-Location
}
