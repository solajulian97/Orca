param (
    [string]$Target = "All"
)

$WorkspaceRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$WorkingIndicators = Join-Path $WorkspaceRoot "NinjaTrader\Indicators"
$WorkingAddOns     = Join-Path $WorkspaceRoot "NinjaTrader\AddOns"
$FullSuiteIndicators = Join-Path $WorkspaceRoot "Full_Suite\Indicators"
$FullSuiteAddOns     = Join-Path $WorkspaceRoot "Full_Suite\AddOns"

$Indicators = @(
    "OrcaAbsorptionCandles.cs",
    "OrcaAnchoredVWAPs.cs",
    "OrcaCandleVolumeProfile.cs",
    "OrcaCumulativeDelta.cs",
    "OrcaExecutionLines.cs",
    "OrcaExecutionLines2.cs",
    "OrcaLegtoLegProfile.cs",
    "OrcaPrints.cs",
    "OrcaPrints.Engine.cs",
    "OrcaPrints.Models.cs",
    "OrcaPrints.Rendering.cs",
    "OrcaPrints.Scoring.cs",
    "OrcaRollingProfiles.cs",
    "OrcaStepProfile.cs",
    "OrcaTickDirectionIndex.cs",
    "OrcaTimeStatistics.cs",
    "OrcaTimeVWAPs.cs",
    "OrcaVisualOrders.cs"
)
$AddOns = @("OrcaRiskManagerAddOn.cs")

if ($Target -ne "All") {
    if ($Target -match "AddOn") {
        $Indicators = @()
        $AddOns = @($Target)
    } else {
        $Indicators = @($Target)
        $AddOns = @()
    }
}

Write-Host "Promoting tested working files into Full_Suite..." -ForegroundColor Cyan

foreach ($file in $Indicators) {
    $source = Join-Path $WorkingIndicators $file
    $dest = Join-Path $FullSuiteIndicators $file
    if (Test-Path $source) {
        Write-Host "  [INDICATOR] $file"
        Copy-Item -Path $source -Destination $dest -Force
    } else {
        Write-Warning "Missing working indicator: $file"
    }
}

foreach ($file in $AddOns) {
    $source = Join-Path $WorkingAddOns $file
    $dest = Join-Path $FullSuiteAddOns $file
    if (Test-Path $source) {
        Write-Host "  [ADDON] $file"
        Copy-Item -Path $source -Destination $dest -Force
    } else {
        Write-Warning "Missing working add-on: $file"
    }
}

Write-Host ""
Write-Host "Promotion complete. Review git diff, then commit and push when ready." -ForegroundColor Green
