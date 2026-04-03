$InstallPath = "C:\Quantower\Settings\Scripts\Indicators\DeltaMap"
$DllName = "DeltaMap.dll"

Write-Host "Building DeltaMap..."
dotnet build .\DeltaMap.csproj -c Release

if ($?) {
    if (-not (Test-Path $InstallPath)) {
        New-Item -ItemType Directory -Force -Path $InstallPath | Out-Null
    }
    
    Write-Host "Installing to Quantower..."
    Copy-Item ".\bin\Release\*" -Destination $InstallPath -Recurse -Force
    
    # Remove old .cs-based version if it exists (DLL takes precedence)
    $oldCs = "C:\Quantower\Settings\Scripts\Indicators\DeltaMap.cs"
    if (Test-Path $oldCs) {
        Remove-Item $oldCs -Force
        Write-Host "Removed old DeltaMap.cs (replaced by compiled DLL)" -ForegroundColor Yellow
    }
    
    Write-Host "Done! Please recompile the indicator in Quantower." -ForegroundColor Green
} else {
    Write-Host "Build failed. See output above." -ForegroundColor Red
}
