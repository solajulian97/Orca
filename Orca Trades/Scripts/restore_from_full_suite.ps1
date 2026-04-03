$WorkspaceRoot = "C:\Users\Owner\.gemini\antigravity\scratch\Orca"
$FullSuiteIndicators = "$WorkspaceRoot\Full_Suite\Indicators"
$FullSuiteAddOns     = "$WorkspaceRoot\Full_Suite\AddOns"
$LocalIndicators     = "$WorkspaceRoot\NinjaTrader\Indicators"
$LocalAddOns         = "$WorkspaceRoot\NinjaTrader\AddOns"
$LiveCustomRoot      = "C:\Users\Owner\Documents\NinjaTrader 8\bin\Custom"
$LiveIndicators      = "$LiveCustomRoot\Indicators"
$LiveAddOns          = "$LiveCustomRoot\AddOns"

Write-Host "=== Step 1: Git Pull ===" -ForegroundColor Cyan
Set-Location $WorkspaceRoot
git pull
Write-Host ""

Write-Host "=== Step 2: Copy Full_Suite -> Local NinjaTrader Folder ===" -ForegroundColor Cyan

# Copy Indicators
Write-Host "Syncing Indicators from Full_Suite..."
$files = Get-ChildItem -Path $FullSuiteIndicators -Filter *.cs
foreach ($file in $files) {
    Write-Host "  [LOCAL] $($file.Name)"
    Copy-Item -Path $file.FullName -Destination "$LocalIndicators\$($file.Name)" -Force
}

# Copy AddOns
Write-Host "Syncing AddOns from Full_Suite..."
$files = Get-ChildItem -Path $FullSuiteAddOns -Filter *.cs
foreach ($file in $files) {
    Write-Host "  [LOCAL] $($file.Name)"
    Copy-Item -Path $file.FullName -Destination "$LocalAddOns\$($file.Name)" -Force
}

Write-Host ""
Write-Host "=== Step 3: Deploy to Live NinjaTrader ===" -ForegroundColor Cyan

# Deploy Indicators to live
Write-Host "Deploying Indicators to live NinjaTrader..."
$files = Get-ChildItem -Path $FullSuiteIndicators -Filter *.cs
foreach ($file in $files) {
    Write-Host "  [LIVE] $($file.Name)"
    Copy-Item -Path $file.FullName -Destination "$LiveIndicators\$($file.Name)" -Force
}

# Deploy AddOns to live
Write-Host "Deploying AddOns to live NinjaTrader..."
$files = Get-ChildItem -Path $FullSuiteAddOns -Filter *.cs
foreach ($file in $files) {
    Write-Host "  [LIVE] $($file.Name)"
    Copy-Item -Path $file.FullName -Destination "$LiveAddOns\$($file.Name)" -Force
}

Write-Host ""
Write-Host "=== DONE ===" -ForegroundColor Green
Write-Host "All Full_Suite files have been deployed to local workspace and live NinjaTrader." -ForegroundColor Green
Write-Host ""
Write-Host "NEXT STEP: In NinjaTrader, press F5 in the NinjaScript Editor to recompile." -ForegroundColor Yellow
