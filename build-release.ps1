# 360 LocalShare Automated Release & Installer Build Script
# Run this script in PowerShell to build a self-contained x64 release and installer.

$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " 🚀 360 LocalShare - Stable Release & Installer Builder" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

$ProjectDir = Get-Location
$PublishDir = Join-Path $ProjectDir "dist\publish"
$InstallerOutputDir = Join-Path $ProjectDir "dist\installer"
$IssScript = Join-Path $ProjectDir "installer\installer.iss"

# 1. Clean previous build artifacts
Write-Host "[1/4] Cleaning previous build output folders..." -ForegroundColor Yellow
if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }
if (Test-Path $InstallerOutputDir) { Remove-Item $InstallerOutputDir -Recurse -Force }

New-Item -ItemType Directory -Path $PublishDir -Force | Out-Null
New-Item -ItemType Directory -Path $InstallerOutputDir -Force | Out-Null

# 2. Run dotnet publish for self-contained x64 Single File
Write-Host "[2/4] Publishing self-contained single-file x64 release..." -ForegroundColor Yellow
dotnet publish src/LocalShare.App/LocalShare.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $PublishDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "Dotnet publish failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Self-contained release published to: $PublishDir" -ForegroundColor Green

# 3. Create sample latest_version.json manifest for update server
Write-Host "[3/4] Generating update manifest (latest_version.json)..." -ForegroundColor Yellow
$ManifestObj = [PSCustomObject]@{
    version = "1.0.0"
    releaseDate = (Get-Date -Format "yyyy-MM-dd")
    downloadUrl = "https://github.com/Antigravity/360-LocalShare/releases/download/v1.0.0/360LocalShare_Setup_v1.0.0.exe"
    changelog = "Initial stable release with Obsidian Glass UI, dark high-visibility controls, Public Space browser, multi-peer streaming, and built-in auto-updater."
    sha256 = ""
    isMandatory = $false
}

$ManifestPath = Join-Path $ProjectDir "dist\latest_version.json"
$ManifestObj | ConvertTo-Json -Depth 4 | Out-File -FilePath $ManifestPath -Encoding utf8
Write-Host "Update manifest generated: $ManifestPath" -ForegroundColor Green

# 4. Search for Inno Setup Compiler (ISCC.exe) to generate installer .exe
Write-Host "[4/4] Searching for Inno Setup Compiler (ISCC.exe)..." -ForegroundColor Yellow
$IsccPaths = @(
    "ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)

$IsccCmd = $null
foreach ($p in $IsccPaths) {
    if (Get-Command $p -ErrorAction SilentlyContinue) {
        $IsccCmd = $p
        break
    }
    if (Test-Path $p) {
        $IsccCmd = $p
        break
    }
}

if ($IsccCmd) {
    Write-Host "Compiling setup installer using Inno Setup ($IsccCmd)..." -ForegroundColor Cyan
    & $IsccCmd $IssScript
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Installer generated at: $InstallerOutputDir\360LocalShare_Setup_v1.0.0.exe" -ForegroundColor Green
    } else {
        Write-Host "Inno Setup script compilation failed!" -ForegroundColor Red
    }
} else {
    Write-Host "Inno Setup (ISCC.exe) is not installed on this machine." -ForegroundColor Yellow
    Write-Host "Your standalone single-file release is ready in: $PublishDir" -ForegroundColor White
    Write-Host "To build the setup installer, install Inno Setup 6 from https://jrsoftware.org/isdl.php and rerun this script." -ForegroundColor White
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Release build process complete!" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
