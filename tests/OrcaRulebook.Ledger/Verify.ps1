$ErrorActionPreference = 'Stop'
dotnet build (Join-Path $PSScriptRoot 'OrcaRulebook.Ledger.csproj') --configuration Release -v:quiet
if ($LASTEXITCODE -ne 0) { throw 'Orca Rulebook ledger test compile failed' }
& (Join-Path $PSScriptRoot 'bin\Release\net48\OrcaRulebook.Ledger.exe')
if ($LASTEXITCODE -ne 0) { throw 'Orca Rulebook ledger verification failed' }
