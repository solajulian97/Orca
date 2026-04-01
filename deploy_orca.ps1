$WorkspaceRoot = "C:\Users\Owner\.gemini\antigravity\scratch\Orca"
$LiveCustomRoot = "C:\Users\Owner\Documents\NinjaTrader 8\bin\Custom"

# Files to sync
$Indicators = @("OrcaLegtoLegProfile.cs", "OrcaExecutionLines.cs", "OrcaStepProfile.cs", "OrcaCandleVolumeProfile.cs", "OrcaAnchoredVWAPs.cs", "OrcaCumulativeDelta.cs", "OrcaTimeVWAPs.cs", "OrcaTickDirectionIndex.cs")
$AddOns = @("OrcaRiskManagerAddOn.cs")

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
