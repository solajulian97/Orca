param (
    [string]$Target = "All"
)

$WorkspaceRoot = "C:\Users\julia\.gemini\antigravity\scratch\Orca\Orca Trades"
$LiveCustomRoot = "C:\Users\julia\Documents\NinjaTrader 8\bin\Custom"

# Files to sync
$Indicators = @(
    "AutoLegProfile.cs", "AutoLegProfileNT.cs", "AutoLegProfileNT2.cs", "BarTimes.cs", 
    "FastCandleHighlight.cs", "LegToLegDeltaProfile.cs", "OrcaAbsorptionCandles.cs", 
    "OrcaAnchoredVWAPs.cs", "OrcaCandleVolumeProfile.cs", "OrcaCumulativeDelta.cs", 
    "OrcaExecutionLines.cs", "OrcaExecutionLines2.cs", "OrcaLegtoLegProfile.cs", 
    "OrcaRollingProfiles.cs", "OrcaStepProfile.cs", "OrcaTickDirectionIndex.cs", 
    "OrcaTimeStatistics.cs", "OrcaTimeVWAPs.cs", "OrcaVisualOrders.cs", 
    "PAX30OpeningRange.cs", "PassiveFlowSuite.cs", "VWAPx.cs"
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

Write-Host "Deploying Orca Suite Indicators to NinjaTrader 8..."
foreach ($file in $Indicators) {
    if (Test-Path "$WorkspaceRoot\NinjaTrader\Indicators\$file") {
        Write-Host "Copying $file..."
        Copy-Item -Path "$WorkspaceRoot\NinjaTrader\Indicators\$file" -Destination "$LiveCustomRoot\Indicators\$file" -Force
    }
}

Write-Host "`nDeploying Orca Suite AddOns to NinjaTrader 8..."
foreach ($file in $AddOns) {
    if (Test-Path "$WorkspaceRoot\NinjaTrader\AddOns\$file") {
        Write-Host "Copying $file..."
        Copy-Item -Path "$WorkspaceRoot\NinjaTrader\AddOns\$file" -Destination "$LiveCustomRoot\AddOns\$file" -Force
    }
}

Write-Host "`nDeployment complete. Please press F5 in the NinjaTrader editor to recompile."
