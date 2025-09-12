# VibeCat Build Script for Windows PowerShell

Write-Host "Building VibeCat..." -ForegroundColor Green

# Check if .NET 8 is installed
$dotnetVersion = dotnet --version 2>$null
if (-not $dotnetVersion -or -not $dotnetVersion.StartsWith("8.")) {
    Write-Host "Error: .NET 8 SDK is not installed!" -ForegroundColor Red
    Write-Host "Install it with: winget install Microsoft.DotNet.SDK.8" -ForegroundColor Yellow
    exit 1
}

Write-Host "Found .NET SDK: $dotnetVersion" -ForegroundColor Cyan

# Restore packages
Write-Host "`nRestoring NuGet packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to restore packages!" -ForegroundColor Red
    exit 1
}

# Clean previous builds
Write-Host "`nCleaning previous builds..." -ForegroundColor Yellow
dotnet clean -c Release

# Build the project
Write-Host "`nBuilding VibeCat in Release mode..." -ForegroundColor Yellow
dotnet build -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "`nBuild completed successfully!" -ForegroundColor Green
Write-Host "Output: VibeCat\bin\Release\net8.0-windows\" -ForegroundColor Cyan

# Ask if user wants to run
$response = Read-Host "`nDo you want to run VibeCat now? (y/n)"
if ($response -eq 'y') {
    Write-Host "`nStarting VibeCat..." -ForegroundColor Green
    Start-Process "VibeCat\bin\Release\net8.0-windows\VibeCat.exe"
}