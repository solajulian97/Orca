$SourcePath = "c:\Users\Owner\.gemini\antigravity\scratch\Orca\NinjaTrader\Indicators\OrcaCandleVolumeProfile.cs"
$BasePath = "C:\Users\Owner"
$DestPath = "$BasePath\Documents\NinjaTrader 8\bin\Custom\Indicators\OrcaCandleVolumeProfile.cs"

Write-Host "Deploying OrcaCandleVolumeProfile to NinjaTrader 8..."
Copy-Item -Path $SourcePath -Destination $DestPath -Force
Write-Host "Deployment complete."
