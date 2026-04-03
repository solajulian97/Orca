$InstallPath = "C:\Quantower\Settings\Scripts\Indicators\PullAndStack"
$DllName = "PullAndStack.dll"

Write-Host "Building PullAndStack..."
dotnet build .\PullAndStack.csproj -c Release

if ($?) {
    if (-not (Test-Path $InstallPath)) {
        New-Item -ItemType Directory -Force -Path $InstallPath | Out-Null
    }
    
    Write-Host "Installing to Quantower..."
    Copy-Item ".\bin\Release\*" -Destination $InstallPath -Recurse -Force
    Write-Host "Done! Please recompile the indicator in Quantower." -ForegroundColor Green
} else {
    Write-Host "Build failed. See output above." -ForegroundColor Red
}
