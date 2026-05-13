# Architecture

## High-Level Layers

Orca has four practical layers:

- NinjaScript indicators: chart overlays or panels that collect market/order-flow state and render visual context.
- NinjaTrader add-ons: Control Center or ChartTrader integrations for routing, risk management, and trade controls.
- Deployment/promotion scripts: PowerShell tools that copy selected source files between `Working_Suite`, live NinjaTrader Custom folders, mirrors, and `Full_Suite`.
- Adjacent tooling: external Orca Journal project, experiments, ports, and decompiled NinjaTrader references.

## Canonical Source Flow

The intended source flow is:

1. Edit `Orca Trades/Working_Suite/Indicators` or `Orca Trades/Working_Suite/AddOns`.
2. Deploy from `Working_Suite` with `Orca Trades/Scripts/deploy_orca.ps1`.
3. Press `F5` in the NinjaScript Editor to compile.
4. Julia tests behavior in NinjaTrader.
5. Only after Julia approves, promote with `Orca Trades/Scripts/promote_working_to_full_suite.ps1 -Target <FileName>`.

`Full_Suite` is not an experiment lane. It is the clean validated lane.

## Indicator Patterns

Most indicators follow standard NinjaTrader lifecycle methods:

- `State.SetDefaults`: set display name, description, default properties, plots, and panel/overlay behavior.
- `State.Configure`: add secondary series such as `AddDataSeries(BarsPeriodType.Tick, 1)` or a 1-minute series.
- `State.DataLoaded`: initialize dictionaries, caches, brushes, sessions, trackers, and event state.
- `State.Terminated`: unhook events and dispose resources.
- `OnBarUpdate`: update bar/session/profile state.
- `OnMarketData`: capture tick, bid/ask, signed-volume, and same-price direction state when needed.
- `OnRender`: draw high-density visuals using SharpDX where native NinjaTrader drawing would be too heavy.
- `OnRenderTargetChanged`: dispose/recreate render-target-bound SharpDX resources.

Indicators that depend on true volume-at-price or delta often add a 1-tick secondary data series and classify prints with bid/ask data when available, with fallback logic for historical conditions.

## Rendering Architecture

SharpDX/Direct2D rendering is central for dense visuals:

- `OrcaCandleVolumeProfile`
- `OrcaCumulativeDelta`
- `OrcaExecutionLines`
- `OrcaLegtoLegProfile`
- `OrcaMGIDaily`
- `OrcaPrints.Rendering`
- `OrcaRollingProfiles`
- `OrcaStepProfile`
- `OrcaTickDirectionIndex`
- `OrcaTimeStatistics`
- `OrcaVisibleRangeVolumeProfile`
- `OrcaVisualOrders`

Critical rendering constraints:

- Render-target-bound brushes must be disposed when the render target changes.
- Avoid expensive allocations in hot render loops unless scoped in `using` and proven acceptable.
- Text layout/font caches should be invalidated when font, size, scale, or render conditions change.
- Chart zoom/pan paths can run frequently and expose expensive recalculations.

## Data Structures

Common data patterns:

- Dictionaries keyed by price for volume/delta maps.
- Per-bar maps for visible range and candle profile work.
- Per-period/per-leg blocks for rolling, step, and leg profiles.
- Session structs/classes for RTH/ETH/daily levels and VWAP accumulators.
- Account state dictionaries for execution matching and risk/order UI state.

`OrcaVolumeProfileCore.cs` is a shared helper for visible-range row aggregation, POC detection, dictionary price-bin value-area calculation, and value-area expansion from POC.

## Module Map

### Volume/Delta Profile Family

- `OrcaRollingProfiles.cs`: rolling time windows; dynamic delta aggregation; POC/value area.
- `OrcaStepProfile.cs`: fixed time interval blocks; volume/delta histograms; POC/value area.
- `OrcaLegtoLegProfile.cs`: profile per detected rotation/leg.
- `OrcaCandleVolumeProfile.cs`: per-candle footprint/profile rendering.
- `OrcaVisibleRangeVolumeProfile.cs`: profile recalculated from currently visible bars.
- `OrcaVolumeProfileCore.cs`: shared aggregation/value-area calculations.

### VWAP/Session Context

- `OrcaAnchoredVWAPs.cs`: anchored VWAP with reversal thresholds and bands.
- `OrcaTimeVWAPs.cs`: time/session anchored VWAPs and rolling VWAPs.
- `OrcaMGIDaily.cs`: structural daily session levels and value areas.

### Order Flow/Signal Context

- `OrcaAbsorptionCandles.cs`: candle coloring from absorption/delta intensity.
- `OrcaCumulativeDelta.cs`: cumulative delta OHLC panel.
- `OrcaPrints*.cs`: large prints and clustered aggressive participation.
- `OrcaTickDirectionIndex.cs`: tick-direction based effort/volume classification.
- `OrcaTimeStatistics.cs`: bottom-panel per-bar statistics.

### Execution And Trade Tools

- `OrcaExecutionLines.cs`: account execution events, FIFO round-trip matching, SQLite history loading, R-multiple tracking, chart rendering, and current dirty note/tag work.
- `OrcaVisualOrders.cs`: chart visual order controls and routed order overlays.
- `OrcaRiskManagerAddOn.cs`: ChartTrader risk/trade UI and chart drag overlays.
- `OrcaExecutionRouterAddOn.cs`: instrument routing settings used by visual order workflows.

## Add-On Architecture

`OrcaRiskManagerAddOn.cs`:

- Inherits `NinjaTrader.NinjaScript.AddOnBase`.
- Hooks window creation to inject a risk panel into ChartTrader/chart UI.
- Uses WPF controls/canvases for interaction and overlays.
- Hooks account execution events.
- Current dirty changes align overlay canvases with chart grid slots and use overlay-local mouse coordinates for DPI-safe price conversion.

`OrcaExecutionRouterAddOn.cs`:

- Inherits `AddOnBase`.
- Adds routing settings and a settings window.
- Provides `OrcaExecutionRouterSettings` and static `OrcaExecutionRouter` state consumed by visual order functionality.

## Deployment Scripts

Preferred:

- `Orca Trades/Scripts/deploy_orca.ps1`
  - Defaults to `-SourceSuite Working_Suite`.
  - Supports `-Target <FileName|All>`.
  - Supports `-SourceSuite Working_Suite|Full_Suite`.
  - Copies to live NinjaTrader Custom Indicators/AddOns.
  - Optional `-SyncLocalMirror` copies to `Orca Trades/NinjaTrader`.
  - Supports `-DryRun`.

- `Orca Trades/Scripts/promote_working_to_full_suite.ps1`
  - Copies validated files from `Working_Suite` to `Full_Suite`.
  - Supports `-Target <FileName|All>` and `-DryRun`.
  - Use only after Julia approves deployed/compiled behavior.

Use with care:

- `Orca Trades/Scripts/restore_from_full_suite.ps1`
  - Calls `deploy_orca.ps1 -SourceSuite Full_Suite -SyncLocalMirror`.
  - This restores validated Full_Suite into live NinjaTrader and mirror. Use only when intentionally restoring validated code.

Avoid unless explicitly confirmed:

- `Orca Trades/Scripts/promote_to_full_suite.ps1`
  - Older script that copies from `Orca Trades/NinjaTrader` mirror to `Full_Suite`.
  - This conflicts with the current source-of-truth rule unless Julia explicitly asks.

- `Orca Trades/Scripts/deploy_nt8.ps1`
  - Stale hard-coded path under `C:\Users\Owner\.gemini\antigravity\scratch`.
  - Do not use for current workflow.

Other scripts such as `gen_csharp.ps1`, `generate_vwaps_csharp.py`, `reflect.ps1`, and `fix_ambiguity.ps1` are support/generation/research tools. Confirm intent before using them.

## Adjacent Orca Journal Architecture

External path:

- `C:\Users\julia\projects\OrcaTrading\OrcaJournal\OrcaJournal\`

Architecture summary:

- .NET Framework 4.8 WPF library AddOn for NinjaTrader.
- Main entry: `OrcaJournal.cs`.
- Captures executions through `Account.ExecutionUpdate`.
- Builds completed open-to-flat trades with `TradeBuilder`.
- Persists trades, tags, tag rules, and sessions into SQLite.
- UI uses `NTWindow`, WPF views/view models, and SkiaSharp-rendered bitmap charts.
- Project comments state LiveCharts2 WPF controls were removed to avoid NT8 D2D visual-tree conflicts.

