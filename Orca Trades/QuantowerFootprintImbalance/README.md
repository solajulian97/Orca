# Quantower Footprint Imbalance Indicator

Shows **footprint-style buy/sell imbalances** directly on your chart so you can see where aggressive buying or selling occurred at each price level—without switching to the cluster footprint chart.

## What it does

- Uses the same volume-by-price data as the cluster footprint (bid/ask, delta).
- For each bar, scans every price level and marks **buy imbalance** (green) or **sell imbalance** (red) when one side dominates by your chosen ratio.
- Draws colored bands on the chart at those price levels so you can spot absorption, exhaustion, and order flow at a glance on candles or any chart type.

## Requirements

- **Quantower** with **Quantower Algo** (Visual Studio extension) or ScriptBuilder.
- Chart must load **volume analysis data** (the indicator implements `IVolumeAnalysisIndicator` so Quantower will load it when you add the indicator).
- Symbol must provide trade/volume data (works with futures, crypto, etc. that have footprint-style data).

## Installation

### Option A: Build and install with the script (no VS connection needed)

If Quantower Algo in Visual Studio won't connect to the platform, you can build and install the indicator from the project folder:

1. Open **PowerShell** and go to the indicator folder:
   ```powershell
   cd C:\Users\Owner\.antigravity\QuantowerFootprintImbalance
   ```
   (Or the original folder: `C:\Users\Owner\QuantowerFootprintImbalance`)

2. Run the install script. Use your Quantower install folder (if it's not `C:\Quantower`, pass it):
   ```powershell
   .\build-and-install.ps1
   # or, if Quantower is elsewhere:
   .\build-and-install.ps1 -QuantowerPath "D:\Quantower"
   ```
   The script will build the DLL and copy it to  
   `Quantower\Settings\Scripts\Indicators\FootprintImbalance\`.

3. In Quantower, add the indicator: **Indicators** → **Custom** → **FootprintImbalance** → **Footprint Imbalance**.

**Requirements:** Visual Studio (or Build Tools) with MSBuild, and .NET 8 SDK (or the one that matches your Quantower version). The script will search common locations for Quantower if you don't pass `-QuantowerPath`.

### Option B: Using Quantower Algo (Visual Studio)

1. Install the [Quantower Algo extension](https://help.quantower.com/quantower-algo/installing-visual-studio) and open/create an Indicator project.  
2. Add `FootprintImbalanceIndicator.cs` to the project (or create a new file and paste the code).  
3. Build the solution (F6). The extension will copy the indicator into Quantower.

### Option C: Using ScriptBuilder

If your Quantower version supports custom C# scripts, create a new indicator script and paste the contents of `FootprintImbalanceIndicator.cs`.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| **Imbalance ratio** | Buy volume ≥ ratio × Sell volume → buy imbalance; same for sell. (e.g. 2.0 = 2:1) | 2.0 |
| **Min volume at level** | Only show imbalance if total volume at that price is above this. | 0 |
| **Buy imbalance color** | Color for buy imbalance bands. | Semi-transparent green |
| **Sell imbalance color** | Color for sell imbalance bands. | Semi-transparent red |
| **Line width** | Thickness of the imbalance bands in pixels. | 2 |

If color parameters don't appear in the indicator settings (depends on Quantower version), edit the default values in the constructor for `BuyImbalanceColor` and `SellImbalanceColor`.

## Usage

- Add the indicator to any chart (candles, range, etc.).  
- Wait for volume analysis to finish loading (first time may take a moment).  
- Green bands = price levels with buy imbalance; red bands = sell imbalance.  
- Adjust **Imbalance ratio** to be stricter (e.g. 3.0) or looser (e.g. 1.5).  
- Use **Min volume at level** to filter out low-volume levels.

## Tips

- **Stacked imbalances** (several consecutive levels same color) often act as support/resistance.  
- **Unfinished auctions** (both sides at high/low) are not drawn as imbalance; the indicator only highlights one-sided dominance.  
- Works best on symbols and timeframes where footprint data is available and meaningful (e.g. liquid futures, tick or volume bars).

## License

Use and modify freely for personal or commercial use with Quantower.
