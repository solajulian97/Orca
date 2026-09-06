param([string]$JournalRoot='C:\Users\julia\projects\OrcaTrading\OrcaJournal\OrcaJournal')
$ErrorActionPreference='Stop'
$output=Join-Path $env:TEMP ('orca-review-ui-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $output | Out-Null
Copy-Item -LiteralPath "$JournalRoot\bin\Release\net48\OrcaJournal.dll" -Destination $output
foreach($name in @('System.Data.SQLite.dll','e_sqlite3.dll')) {Copy-Item -LiteralPath "C:\Program Files\NinjaTrader 8\bin\$name" -Destination $output}
$framework='C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
& "$framework\csc.exe" /nologo /target:exe /platform:x64 "/out:$output\Verify.exe" "/r:$output\OrcaJournal.dll" "/r:$output\System.Data.SQLite.dll" "/r:$framework\WPF\WindowsBase.dll" "/r:$framework\WPF\PresentationCore.dll" "/r:$framework\WPF\PresentationFramework.dll" "/r:$framework\System.Xaml.dll" "$PSScriptRoot\Program.cs"
if($LASTEXITCODE -ne 0){throw 'UI harness compile failed'}
& "$output\Verify.exe" $output
if($LASTEXITCODE -ne 0){throw 'UI checks failed'}
Write-Output "Artifacts: $output"
