# Quantower Footprint Imbalance – project index

This folder is a full copy of the **Quantower Footprint Imbalance** indicator project, for use inside anti-gravity (or any context that needs the code and docs). All code and all MD files are here so they can be read and used together.

---

## Files in this project

| File | Purpose |
|------|--------|
| **FootprintImbalanceIndicator.cs** | Main indicator: C# source. Implements `IVolumeAnalysisIndicator`, uses `PriceLevels` per bar, draws buy/sell imbalance bands on the chart. |
| **QuantowerFootprintImbalance.csproj** | .NET 8 project file. References `TradingPlatform.BusinessLayer.dll` (Quantower API) and `System.Drawing.Common`. |
| **build-and-install.ps1** | PowerShell script: finds Quantower, builds the project with MSBuild, copies the output DLL to `Quantower\Settings\Scripts\Indicators\FootprintImbalance\`. |
| **README.md** | User-facing docs: what the indicator does, installation (script / VS / ScriptBuilder), parameters, usage, tips. |
| **LOGIC_AND_SOURCE.md** | Context for agents/readers: where data comes from (Quantower footprint), how imbalance is defined (ratio and min volume). |
| **INDEX.md** | This file: list of all project files and their roles. |

---

## Quick context

- **Platform:** Quantower (trading/charting).
- **Role:** Custom indicator that shows footprint-style buy/sell imbalances on the main chart without opening the cluster footprint panel.
- **Data:** Quantower volume-by-price (buy/sell per price) via `IVolumeAnalysisIndicator` and `PriceLevels`.
- **Logic:** At each price, mark buy imbalance if BuyVolume ≥ ratio × SellVolume, sell imbalance if SellVolume ≥ ratio × BuyVolume; optional min volume filter.

Use **README.md** for installation and usage, **LOGIC_AND_SOURCE.md** for data and logic details, and the `.cs` / `.csproj` / `.ps1` files for the actual code and build.
