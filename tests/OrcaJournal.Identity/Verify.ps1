param([string]$JournalRoot='C:\Users\julia\projects\OrcaTrading\OrcaJournal')
$ErrorActionPreference='Stop'
$baseline = git -C $JournalRoot show '0467d1b:OrcaJournal/Data/DatabaseManager.cs'
if ($LASTEXITCODE -ne 0) { throw 'External baseline unavailable' }
$baseline -replace 'namespace OrcaJournal.Data','namespace Baseline' | Set-Content (Join-Path $PSScriptRoot 'Baseline.g.cs')
$recorder = Get-Content (Join-Path $PSScriptRoot '../../Orca Trades/Working_Suite/AddOns/OrcaTradeRecorderAddOn.cs') -Raw
$start = $recorder.IndexOf('public sealed class OrcaRecorderRoundTrip')
$end = $recorder.IndexOf('public sealed class OrcaRecorderCaptureManifest')
('using System; using System.IO; using System.Linq; using System.Collections.Generic; using System.Globalization; namespace RecorderFixture {' + $recorder.Substring($start,$end-$start) + '}') | Set-Content (Join-Path $PSScriptRoot 'Recorder.g.cs')
dotnet build (Join-Path $PSScriptRoot 'OrcaJournal.Identity.csproj') --configuration Release -v:quiet -p:NuGetAudit=false "-p:JournalRoot=$JournalRoot\OrcaJournal"
if ($LASTEXITCODE -ne 0) { throw 'Test compile failed' }
& (Join-Path $PSScriptRoot 'bin/Release/net48/OrcaJournal.Identity.exe')
if ($LASTEXITCODE -ne 0) { throw 'Identity verification failed' }
