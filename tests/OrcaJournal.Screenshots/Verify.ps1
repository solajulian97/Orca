param([string]$JournalRoot = 'C:\Users\julia\projects\OrcaTrading\OrcaJournal\OrcaJournal')
$ErrorActionPreference = 'Stop'
$output = Join-Path $env:TEMP ('orca-screenshots-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $output | Out-Null
$framework = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$refs = Join-Path $framework 'WPF'
& "$framework\csc.exe" /nologo /target:exe /platform:x64 "/out:$output\Verify.exe" "/r:$refs\WindowsBase.dll" "/r:$refs\PresentationCore.dll" "/r:$refs\PresentationFramework.dll" "/r:$framework\System.Xaml.dll" "$PSScriptRoot\Program.cs" "$JournalRoot\UI\Windows\TradeImageViewer.cs" "$JournalRoot\Data\Models\TradeAttachment.cs" "$JournalRoot\Data\Models\Trade.cs"
if ($LASTEXITCODE -ne 0) { throw 'Screenshot harness compile failed' }
& "$output\Verify.exe" $output
if ($LASTEXITCODE -ne 0) { throw 'Screenshot checks failed' }
Write-Output "Artifacts: $output"
