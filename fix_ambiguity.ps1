$IndicatorsPath = "C:\Users\Owner\.gemini\antigravity\scratch\Orca\NinjaTrader\Indicators"
$Files = Get-ChildItem -Path $IndicatorsPath -Filter *.cs

$AliasBlock = @"
using WpfBrush = System.Windows.Media.Brush;
using DxBrush = SharpDX.Direct2D1.Brush;
using WpfColor = System.Windows.Media.Color;
using DxColor = SharpDX.Color;
using WpfPoint = System.Windows.Point;
using DxPoint = SharpDX.Point;
using DxSolidColorBrush = SharpDX.Direct2D1.SolidColorBrush;

"@

foreach ($File in $Files) {
    $Content = Get-Content $File.FullName -Raw
    if ($Content -notmatch "using WpfBrush") {
        Write-Host "Adding aliases to $($File.Name)..."
        # Insert after the last using statement or at the top
        $NewContent = $Content -replace "(?m)^(using\s+.*?;)(\r?\n)(?!using)", "$1`r`n$AliasBlock"
        if ($NewContent -eq $Content) {
            # Fallback: Insert at the very top
            $NewContent = $AliasBlock + $Content
        }
        Set-Content $File.FullName $NewContent
    }
}
