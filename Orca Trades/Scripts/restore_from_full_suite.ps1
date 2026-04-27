param (
    [string]$Target = "All",
    [string]$LiveCustomRoot = "$env:USERPROFILE\Documents\NinjaTrader 8\bin\Custom",
    [switch]$DryRun
)

$deployScript = Join-Path $PSScriptRoot "deploy_orca.ps1"

& $deployScript `
    -Target $Target `
    -SourceSuite "Full_Suite" `
    -LiveCustomRoot $LiveCustomRoot `
    -SyncLocalMirror `
    -DryRun:$DryRun
