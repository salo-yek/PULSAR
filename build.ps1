#!/usr/bin/env pwsh
# Build script for PULSAR on Windows
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$Publish
)

Write-Host "=== PULSAR Build Script ===" -ForegroundColor Cyan
Write-Host ""

# Restore NuGet packages
Write-Host "[1/3] Restoring packages..." -ForegroundColor Yellow
dotnet restore src/PULSAR/PULSAR.csproj
if ($LASTEXITCODE -ne 0) {
    Write-Host "Restore failed!" -ForegroundColor Red
    exit 1
}

# Build
Write-Host "[2/3] Building ($Configuration)..." -ForegroundColor Yellow
dotnet build src/PULSAR/PULSAR.csproj -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Optional publish
if ($Publish) {
    Write-Host "[3/3] Publishing ($Configuration)..." -ForegroundColor Yellow
    dotnet publish src/PULSAR/PULSAR.csproj -c $Configuration --no-build -o ./publish
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Publish failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "Published to ./publish" -ForegroundColor Green
}

Write-Host ""
Write-Host "=== Build Complete ===" -ForegroundColor Green