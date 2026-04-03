# Build Footprint Imbalance indicator and copy DLL to Quantower's Scripts\Indicators folder.
# Usage:
#   .\build-and-install.ps1
#   .\build-and-install.ps1 -QuantowerPath "D:\Quantower"
#   .\build-and-install.ps1 -QuantowerPath "C:\Program Files\Quantower"

param(
    [Parameter(Mandatory=$false)]
    [string]$QuantowerPath
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

# --- Find Quantower install folder ---
if (-not $QuantowerPath) {
    $candidates = @(
        $env:QUANTOWER_PATH,
        "C:\Quantower",
        "D:\Quantower",
        "E:\Quantower",
        (Join-Path $env:ProgramFiles "Quantower"),
        (Join-Path ${env:ProgramFiles(x86)} "Quantower"),
        (Join-Path $env:LOCALAPPDATA "Quantower")
    )
    foreach ($c in $candidates) {
        if (-not $c) { continue }
        $exe = Join-Path $c "Quantower.exe"
        if (Test-Path $exe) {
            $QuantowerPath = $c
            break
        }
        if (Test-Path $c) {
            $exeInSub = Get-ChildItem -Path $c -Filter "Quantower.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
            if ($exeInSub) {
                $QuantowerPath = Split-Path -Parent $exeInSub.FullName
                break
            }
        }
    }
}

if (-not $QuantowerPath -or -not (Test-Path $QuantowerPath)) {
    Write-Host "Quantower path not found. Please run:" -ForegroundColor Yellow
    Write-Host '  .\build-and-install.ps1 -QuantowerPath "C:\Path\To\Quantower"' -ForegroundColor Cyan
    Write-Host ""
    Write-Host "To find it: right-click Quantower shortcut -> Open file location. Use that folder." -ForegroundColor Gray
    exit 1
}

$QuantowerRoot = $QuantowerPath
$apiDll = Join-Path $QuantowerPath "TradingPlatform.BusinessLayer.dll"
if (-not (Test-Path $apiDll)) {
    $found = Get-ChildItem -Path $QuantowerPath -Filter "TradingPlatform*.dll" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $found) {
        Write-Host "Quantower API DLL not found in: $QuantowerPath" -ForegroundColor Red
        Write-Host "Expected: TradingPlatform.BusinessLayer.dll in the Quantower folder." -ForegroundColor Gray
        exit 1
    }
    $apiDll = $found.FullName
    $QuantowerPath = Split-Path -Parent $apiDll
}

Write-Host "Quantower root: $QuantowerRoot" -ForegroundColor Green
Write-Host "API DLL: $apiDll" -ForegroundColor Gray

# --- Build with MSBuild or dotnet ---
$msbuild = $null
if (Get-Command msbuild -ErrorAction SilentlyContinue) {
    $msbuild = "msbuild"
}
if (-not $msbuild) {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $vsPath = & $vswhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1
        if ($vsPath) { $msbuild = $vsPath }
    }
}

if (-not $msbuild) {
    Write-Host "MSBuild not found. Install Visual Studio with .NET desktop development workload, or run from Developer Command Prompt." -ForegroundColor Red
    exit 1
}

Write-Host "Building..." -ForegroundColor Cyan
& $msbuild "QuantowerFootprintImbalance.csproj" /p:Configuration=Release /p:QuantowerPath="$QuantowerPath" /v:minimal
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed." -ForegroundColor Red
    exit 1
}

$outDll = Join-Path $scriptDir "bin\Release\QuantowerFootprintImbalance.dll"
if (-not (Test-Path $outDll)) {
    Write-Host "Output DLL not found: $outDll" -ForegroundColor Red
    exit 1
}

# --- Copy to Quantower Settings\Scripts\Indicators ---
$indicatorsFolder = Join-Path $QuantowerRoot "Settings\Scripts\Indicators"
$targetFolder = Join-Path $indicatorsFolder "FootprintImbalance"

if (-not (Test-Path $indicatorsFolder)) {
    New-Item -ItemType Directory -Path $indicatorsFolder -Force | Out-Null
}
if (-not (Test-Path $targetFolder)) {
    New-Item -ItemType Directory -Path $targetFolder -Force | Out-Null
}

Copy-Item -Path $outDll -Destination $targetFolder -Force
Write-Host "Installed: $targetFolder\QuantowerFootprintImbalance.dll" -ForegroundColor Green
Write-Host "In Quantower, add the indicator via: Indicators -> Custom -> FootprintImbalance -> Footprint Imbalance" -ForegroundColor Cyan
