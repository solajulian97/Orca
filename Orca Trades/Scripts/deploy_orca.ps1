param (
    [string]$Target = "All",
    [ValidateSet("Working_Suite", "Full_Suite")]
    [string]$SourceSuite = "Working_Suite",
    [string]$LiveCustomRoot = "$env:USERPROFILE\Documents\NinjaTrader 8\bin\Custom",
    [switch]$SyncLocalMirror,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

$OrcaTradesRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$SourceSuiteRoot = Join-Path $OrcaTradesRoot $SourceSuite
$SourceIndicators = Join-Path $SourceSuiteRoot "Indicators"
$SourceAddOns = Join-Path $SourceSuiteRoot "AddOns"
$LiveIndicators = Join-Path $LiveCustomRoot "Indicators"
$LiveAddOns = Join-Path $LiveCustomRoot "AddOns"
$MirrorIndicators = Join-Path $OrcaTradesRoot "NinjaTrader\Indicators"
$MirrorAddOns = Join-Path $OrcaTradesRoot "NinjaTrader\AddOns"

function Assert-Directory {
    param ([string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        throw "Required directory not found: $Path"
    }
}

function Get-SourceFiles {
    param (
        [string]$Directory,
        [string]$RequestedTarget
    )

    if ($RequestedTarget -eq "All") {
        return @(Get-ChildItem -LiteralPath $Directory -Filter "*.cs" -File | Sort-Object Name)
    }

    $fileName = $RequestedTarget
    if ([System.IO.Path]::GetExtension($fileName) -eq "") {
        $fileName = "$fileName.cs"
    }

    $path = Join-Path $Directory $fileName
    if (Test-Path -LiteralPath $path -PathType Leaf) {
        return @(Get-Item -LiteralPath $path)
    }

    return @()
}

function Copy-OrcaFile {
    param (
        [System.IO.FileInfo]$File,
        [string]$DestinationDirectory,
        [string]$Label
    )

    $destination = Join-Path $DestinationDirectory $File.Name
    if ($DryRun) {
        Write-Host "[DRY RUN][$Label] $($File.FullName) -> $destination"
        return
    }

    Assert-Directory $DestinationDirectory
    Copy-Item -LiteralPath $File.FullName -Destination $destination -Force
    Write-Host "[$Label] $($File.Name)"
}

Assert-Directory $SourceIndicators
Assert-Directory $SourceAddOns

$indicatorFiles = Get-SourceFiles -Directory $SourceIndicators -RequestedTarget $Target
$addOnFiles = Get-SourceFiles -Directory $SourceAddOns -RequestedTarget $Target

if ($Target -ne "All" -and $indicatorFiles.Count -eq 0 -and $addOnFiles.Count -eq 0) {
    throw "Target '$Target' was not found in $SourceSuite Indicators or AddOns."
}

Write-Host "Orca deploy source: $SourceSuiteRoot"
Write-Host "NinjaTrader target: $LiveCustomRoot"
Write-Host ""

if ($SyncLocalMirror) {
    Write-Host "Syncing $SourceSuite to Orca Trades/NinjaTrader mirror..."
    foreach ($file in $indicatorFiles) {
        Copy-OrcaFile -File $file -DestinationDirectory $MirrorIndicators -Label "MIRROR Indicator"
    }
    foreach ($file in $addOnFiles) {
        Copy-OrcaFile -File $file -DestinationDirectory $MirrorAddOns -Label "MIRROR AddOn"
    }
    Write-Host ""
}

Write-Host "Deploying $SourceSuite to live NinjaTrader..."
foreach ($file in $indicatorFiles) {
    Copy-OrcaFile -File $file -DestinationDirectory $LiveIndicators -Label "LIVE Indicator"
}
foreach ($file in $addOnFiles) {
    Copy-OrcaFile -File $file -DestinationDirectory $LiveAddOns -Label "LIVE AddOn"
}

Write-Host ""
Write-Host "Deployment complete. Press F5 in the NinjaTrader 8 NinjaScript Editor to compile."
Write-Host "If NT8 reports removed or duplicate names, inspect the live Custom folders for ghost .cs files."
