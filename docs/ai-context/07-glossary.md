# Glossary

## Source And Workflow

- `Working_Suite`: Editable source of truth for the active Orca NinjaTrader suite.
- `Full_Suite`: Clean validated suite. Receives files only after Julia confirms compile and behavior.
- `NinjaTrader mirror`: `Orca Trades/NinjaTrader`; not canonical for overlapping active files.
- `Stable_Release`: Older stable snapshot; not current source of truth.
- `decompiled`: Reference/decompiled NinjaTrader source; research only.
- Deploy: Copy source into live NinjaTrader Custom folders.
- Promote: Copy validated source from Working_Suite into Full_Suite.
- F5 compile: Pressing `F5` in the NinjaScript Editor after deployment so NinjaTrader compiles the whole Custom tree.
- Ghost file: Stale `.cs` file in live NT8 Custom folders that can cause compile errors unrelated to current repo source.

## NinjaTrader Lifecycle

- `State.SetDefaults`: Initial defaults, names, descriptions, plots, user properties.
- `State.Configure`: Configure series, including `AddDataSeries`.
- `State.DataLoaded`: Initialize runtime state after data is ready.
- `State.Terminated`: Cleanup/unhook/dispose.
- `OnBarUpdate`: Per-bar or per-series update method.
- `OnMarketData`: Market-data event handler used for tick/bid/ask/order-flow classification.
- `OnRender`: Chart rendering method, often SharpDX-based in Orca.
- `OnRenderTargetChanged`: Render-target lifecycle hook for disposing/recreating Direct2D resources.
- `BarsInProgress`: NinjaTrader index for primary versus secondary data series.
- `CurrentBars`: Per-series current bar index array.
- Tick Replay: NinjaTrader mode needed for accurate historical tick/order-flow replay in some indicators.

## Market And Session Terms

- RTH: Regular Trading Hours.
- ETH: Electronic/extended Trading Hours.
- ON: Overnight session/range.
- IB: Initial Balance, commonly the first hour of RTH.
- OR: Opening Range.
- PDC: Prior Day Close.
- PDO: Prior Day Open.
- PDH/PDL: Prior Day High/Low.
- TDO: True Daily Open, captured around midnight in current MGI context.
- POC: Point of Control, the price row with highest volume.
- VA: Value Area.
- VAH/VAL: Value Area High/Low.
- VWAP: Volume Weighted Average Price.
- Deviation bands: Standard-deviation-style bands around VWAP.

## Order Flow And Profiles

- Delta: Buy/aggressive volume minus sell/aggressive volume.
- Finish Delta: Current name used in `OrcaTimeStatistics` for the finish-delta metric; older docs mention `DeltaEfficiency`.
- Volume-at-price: Volume bucketed by traded price.
- True volume-at-price: Tick-derived price maps instead of OHLC range estimates.
- Dynamic aggregation: Increasing ticks-per-row or row grouping as charts zoom out so profiles remain legible.
- Tick compression: Combining multiple ticks into a rendered price row.
- Same-price prints: Consecutive trades at the same price; classification may need the last known direction.
- Bid/ask fallback: Using uptick/downtick classification when historical bid/ask state is unavailable.

## Rendering And UI

- SharpDX: Direct2D/DirectWrite rendering layer used for dense chart visuals.
- Render target: Direct2D destination tied to the chart; resources bound to it must be recreated after target changes.
- WPF brush serialization: NinjaScript property pattern using `[XmlIgnore]` brush plus hidden string companion property.
- ChartControl coordinates: Coordinates relative to the full chart control.
- ChartPanel coordinates: Coordinates relative to a chart panel. Recent DPI work moves some mouse reads to ChartControl/overlay coordinates.
- DPI-safe: Coordinate handling that remains accurate across scaling and multi-monitor layouts.

## Adjacent Orca Journal Terms

- Orca Journal: External NinjaTrader AddOn at `C:\Users\julia\projects\OrcaTrading\OrcaJournal\OrcaJournal\`.
- TradeCapture: Journal component that subscribes to `Account.ExecutionUpdate`.
- TradeBuilder: Journal component that builds open-to-flat trade records from fills.
- SQLite WAL: Write-ahead logging mode used by the journal database.
- SkiaSharp: Bitmap rendering library used by Orca Journal charts instead of LiveCharts2 WPF controls.

