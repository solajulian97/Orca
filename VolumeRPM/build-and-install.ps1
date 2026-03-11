$ErrorActionPreference = "Stop"

$ProjectDir = $PSScriptRoot
$ProjectName = "VolumeRPM"
$QuantowerDir = "C:\Quantower"
$SettingsDir = "$QuantowerDir\Settings\Scripts\Indicators\$ProjectName"

Write-Host "Building $ProjectName..."
dotnet build "$ProjectDir\$ProjectName.csproj" -c Release

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed!"
    exit 1
}

Write-Host "Deploying to Quantower..."
if (-not (Test-Path $SettingsDir)) {
    New-Item -ItemType Directory -Path $SettingsDir | Out-Null
}

$DllPath = "$ProjectDir\bin\Release\$ProjectName.dll"

Copy-Item -Path $DllPath -Destination "$SettingsDir\$ProjectName.dll" -Force

Write-Host "Deployed successfully to $SettingsDir. Please recompile the indicator in Quantower."
