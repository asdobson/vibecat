# VibeCat Portable Release Build Script
# Creates a single-file portable executable for distribution

$ErrorActionPreference = "Stop"

Write-Host "`n=== VibeCat Portable Release Builder ===" -ForegroundColor Cyan
Write-Host "Building self-contained single-file executable..." -ForegroundColor Yellow

$dotnetVersion = dotnet --version 2>$null
if (-not $dotnetVersion -or -not $dotnetVersion.StartsWith("8.")) {
    Write-Host "Error: .NET 8 SDK is not installed!" -ForegroundColor Red
    Write-Host "Install it with: winget install Microsoft.DotNet.SDK.8" -ForegroundColor Yellow
    exit 1
}

Write-Host "Using .NET SDK: $dotnetVersion" -ForegroundColor Green

if (Test-Path "./publish") {
    Write-Host "`nCleaning previous publish directory..." -ForegroundColor Yellow
    Remove-Item -Path "./publish" -Recurse -Force
}

Write-Host "`nRestoring NuGet packages..." -ForegroundColor Yellow
dotnet restore VibeCat/VibeCat.csproj --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to restore packages!" -ForegroundColor Red
    exit 1
}

Write-Host "Building portable executable..." -ForegroundColor Yellow
Write-Host "This may take a minute as it bundles the .NET runtime..." -ForegroundColor DarkGray

$publishArgs = @(
    "publish",
    "VibeCat/VibeCat.csproj",
    "-c", "Release",
    "-r", "win-x64",
    "--self-contained", "true",
    "-p:PublishSingleFile=true",
    "-p:PublishReadyToRun=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:IncludeAllContentForSelfExtract=true",
    "-p:EnableCompressionInSingleFile=true",
    "-p:DebugType=embedded",
    "-o", "./publish",
    "--nologo",
    "-v", "minimal"
)

& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

$exePath = "./publish/VibeCat.exe"
if (-not (Test-Path $exePath)) {
    Write-Host "Error: Expected output file not found!" -ForegroundColor Red
    exit 1
}

$fileInfo = Get-Item $exePath
$fileSizeMB = [math]::Round($fileInfo.Length / 1MB, 1)

Write-Host "`nGenerating SHA256 checksum..." -ForegroundColor Yellow
$hash = Get-FileHash -Path $exePath -Algorithm SHA256
$hashFile = "$exePath.sha256"
"$($hash.Hash)  VibeCat.exe" | Out-File -FilePath $hashFile -Encoding UTF8 -NoNewline

Write-Host "`n=== Build Complete! ===" -ForegroundColor Green
Write-Host "Output file: " -NoNewline
Write-Host $exePath -ForegroundColor Cyan
Write-Host "File size: " -NoNewline
Write-Host "$fileSizeMB MB" -ForegroundColor Cyan
Write-Host "SHA256: " -NoNewline
Write-Host $hash.Hash -ForegroundColor DarkGray

Write-Host "`n=== GitHub Release Instructions ===" -ForegroundColor Yellow
Write-Host "1. Test the executable: " -NoNewline -ForegroundColor White
Write-Host ".\publish\VibeCat.exe" -ForegroundColor Cyan
Write-Host "2. Create and push a git tag:" -ForegroundColor White
Write-Host "   git tag v1.0.0" -ForegroundColor Cyan
Write-Host "   git push origin v1.0.0" -ForegroundColor Cyan
Write-Host "3. Go to: " -NoNewline -ForegroundColor White
Write-Host "https://github.com/[your-username]/vibecat/releases/new" -ForegroundColor Cyan
Write-Host "4. Select your tag and upload:" -ForegroundColor White
Write-Host "   - VibeCat.exe" -ForegroundColor Cyan
Write-Host "   - VibeCat.exe.sha256" -ForegroundColor Cyan

$response = Read-Host "`nDo you want to test the executable now? (y/n)"
if ($response -eq 'y') {
    Write-Host "`nStarting VibeCat..." -ForegroundColor Green
    Start-Process $exePath
}