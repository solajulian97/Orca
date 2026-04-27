param (
    [string]$Target = "All",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

$OrcaTradesRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$WorkingIndicators = Join-Path $OrcaTradesRoot "Working_Suite\Indicators"
$WorkingAddOns = Join-Path $OrcaTradesRoot "Working_Suite\AddOns"
$FullIndicators = Join-Path $OrcaTradesRoot "Full_Suite\Indicators"
$FullAddOns = Join-Path $OrcaTradesRoot "Full_Suite\AddOns"

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

function Copy-PromotedFile {
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

    Copy-Item -LiteralPath $File.FullName -Destination $destination -Force
    Write-Host "[$Label] $($File.Name)"
}

$indicatorFiles = Get-SourceFiles -Directory $WorkingIndicators -RequestedTarget $Target
$addOnFiles = Get-SourceFiles -Directory $WorkingAddOns -RequestedTarget $Target

if ($Target -ne "All" -and $indicatorFiles.Count -eq 0 -and $addOnFiles.Count -eq 0) {
    throw "Target '$Target' was not found in Working_Suite Indicators or AddOns."
}

Write-Host "Promoting validated Working_Suite files into Full_Suite..."
foreach ($file in $indicatorFiles) {
    Copy-PromotedFile -File $file -DestinationDirectory $FullIndicators -Label "PROMOTE Indicator"
}
foreach ($file in $addOnFiles) {
    Copy-PromotedFile -File $file -DestinationDirectory $FullAddOns -Label "PROMOTE AddOn"
}

Write-Host ""
Write-Host "Promotion complete. Review git diff before committing."
