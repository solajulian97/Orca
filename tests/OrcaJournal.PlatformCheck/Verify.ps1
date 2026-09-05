$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$temp=Join-Path ([IO.Path]::GetTempPath()) ('orca-journal-chart-'+[Guid]::NewGuid().ToString('N'))
$relative='Orca Trades/Working_Suite/Indicators/OrcaExecutionLines.cs'
$before=Join-Path $temp $relative
New-Item -ItemType Directory -Path (Split-Path $before) -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $root $relative) -Destination $before
# Reconstruct the exact pre-edit authored snapshot in a disposable directory only.
git -C $temp apply --reverse (Join-Path $root 'docs/patches/2026-09-05_orca-execution-lines_shared-identity.patch')
if($LASTEXITCODE -ne 0) { throw 'Chart changed since the scoped patch; inspect current work before updating this check.' }
dotnet run --project (Join-Path $PSScriptRoot 'OrcaJournal.PlatformCheck.csproj') -p:NuGetAudit=false -- $root $before
if($LASTEXITCODE -ne 0) { throw 'Chart preservation or NinjaTrader-reference check failed' }
