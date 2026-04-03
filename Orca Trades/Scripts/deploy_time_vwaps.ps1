$SourcePath = "c:\Users\Owner\.gemini\antigravity\scratch\Orca\Orca Time VWAPs\OrcaTimeVWAPs.cs"
$BasePath = "C:\Users\Owner"
$DestPath = "$BasePath\Documents\NinjaTrader 8\bin\Custom\Indicators\OrcaTimeVWAPs.cs"

Write-Host "Deploying OrcaTimeVWAPs to NinjaTrader 8..."
Copy-Item -Path $SourcePath -Destination $DestPath -Force
Write-Host "Deployment complete."
