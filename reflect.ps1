# Broader string search to understand the DLL's approach
$bytes = [System.IO.File]::ReadAllBytes("C:\Users\Owner\Documents\NinjaTrader 8\bin\Custom\OTMDeltaBarFree.dll")
$text = [System.Text.Encoding]::UTF8.GetString($bytes)

# Search for all relevant NinjaScript API references
$patterns = @(
    'OnBarUpdate', 'OnMarketData', 'OnStateChange', 'OnRender',
    'AddDataSeries', 'AddPlot', 'BarsPeriodType',
    'IsOverlay', 'DrawOnPricePanel', 'IsTickReplay',
    'OrderFlowCumulativeDelta',
    'MarketDataType', 'MarketDataEventArgs',
    'BarsInProgress', 'CurrentBar',
    'CumulativeDelta', 'cumDelta', 'cumulativeDelta',
    'DeltaOpen', 'DeltaHigh', 'DeltaLow', 'DeltaClose',
    'deltaOpen', 'deltaHigh', 'deltaLow', 'deltaClose',
    'OnEachTick', 'OnBarClose', 'OnPriceChange',
    'IsSuspendedWhileInactive',
    'Values', 'Value',
    'Tick', 'Volume',
    'GetBar',
    'chartScale', 'chartControl', 'ChartControl', 'ChartScale',
    'RenderTarget', 'SharpDX', 'Direct2D',
    'FillRectangle', 'DrawLine', 'DrawRectangle',
    'Brush', 'SolidColorBrush',
    'Series', 'MaximumBarsLookBack'
)

foreach ($pattern in $patterns) {
    $regex = [regex]::new($pattern, 'None')
    $m = $regex.Matches($text)
    if ($m.Count -gt 0) {
        Write-Output "  $($pattern): found $($m.Count) time(s)"
    }
}

# Also try to find the ildasm.exe
Write-Output ""
Write-Output "=== Looking for ildasm ==="
$ildasmPaths = @(
    "C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\ildasm.exe",
    "C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\x64\ildasm.exe",
    "C:\Program Files\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\ildasm.exe"
)
foreach ($p in $ildasmPaths) {
    if (Test-Path $p) {
        Write-Output "Found: $p"
        # Run ildasm and dump to text
        & $p "C:\Users\Owner\Documents\NinjaTrader 8\bin\Custom\OTMDeltaBarFree.dll" /out="C:\Users\Owner\.gemini\antigravity\scratch\Orca\otm_il.txt" /text
        Write-Output "IL dump saved to otm_il.txt"
        break
    }
}
