# Project Overview

## Product

Orca is a NinjaTrader 8 trading suite for order-flow visualization, volume and delta profiling, VWAP context, execution visualization, and on-chart trade/risk tooling.

The project currently mixes the active Orca NinjaScript suite, validated snapshots, local/live NinjaTrader mirrors, legacy or experimental projects, decompiled NinjaTrader references, and an adjacent Orca Journal project outside this repo.

## Core User Problem

Orca helps Julia read market structure and execution context inside NinjaTrader with fewer separate tools:

- See volume-at-price, delta, value areas, POC, session levels, VWAP bands, and large prints directly on charts.
- Manage and visualize orders, TP/SL levels, and position/risk context from chart surfaces.
- Capture execution history and trade context for review.

## Source Of Truth

Editable local source of truth:

- `Orca Trades/Working_Suite/Indicators`
- `Orca Trades/Working_Suite/AddOns`

Validated clean suite:

- `Orca Trades/Full_Suite/Indicators`
- `Orca Trades/Full_Suite/AddOns`

Do not copy from these locations into `Working_Suite` or `Full_Suite` unless Julia explicitly confirms that the source file is newer and intended to replace current code:

- `Orca Trades/NinjaTrader`
- `Orca Trades/Stable_Release`
- `decompiled`
- local NinjaTrader live folders under `Documents/NinjaTrader 8/bin/Custom`

## Main Repo Areas

- `AGENTS.md`: source-of-truth, deployment, and collaboration safety rules for all coding agents.
- `CLAUDE.md`: equivalent collaboration rules for Claude Code.
- `README.md`: product summary, included modules, and workflow.
- `docs/`: existing workflow, architecture, engineering, roadmap, and onboarding notes.
- `Orca Trades/Working_Suite`: active editable NinjaScript suite.
- `Orca Trades/Full_Suite`: validated suite. Promote into it only after NT8 compile and behavior are approved by Julia.
- `Orca Trades/Scripts`: deployment, promotion, restore, reflection, and generator scripts.
- `Orca Trades/NinjaTrader`: local/live mirror and older staging area. Not canonical for overlapping files.
- `Orca Trades/Stable_Release`: older stable release snapshot. Not canonical for current work.
- `decompiled`: NinjaTrader/decompiled reference source. Research only.
- `ControlIndex`, `TempCheck`, `DeltaMap`, `VolumeRPM`, `PullAndStack`, `AutoLegProfile`, `QuantowerFootprintImbalance`, and similar folders: experiments, support projects, ports, or legacy/prototype areas unless Julia says otherwise.

## Current Worktree Snapshot

`git status --short --branch` reported:

- Branch: `main...origin/main`
- Dirty files:
  - `Orca Trades/Working_Suite/AddOns/OrcaRiskManagerAddOn.cs`
  - `Orca Trades/Working_Suite/Indicators/OrcaCandleVolumeProfile.cs`
  - `Orca Trades/Working_Suite/Indicators/OrcaExecutionLines.cs`
  - `Orca Trades/Working_Suite/Indicators/OrcaMGIDaily.cs`
  - `Orca Trades/Working_Suite/Indicators/OrcaPrints.Rendering.cs`
  - `Orca Trades/Working_Suite/Indicators/OrcaTimeStatistics.cs`
  - `Orca Trades/Working_Suite/Indicators/OrcaTimeVWAPs.cs`
  - `Orca Trades/Working_Suite/Indicators/OrcaVisibleRangeVolumeProfile.cs`
  - `Orca Trades/Working_Suite/Indicators/OrcaVisualOrders.cs`
  - `SESSION_NOTES.md`

These dirty changes were not reverted or overwritten. Their compile/runtime status is Unknown unless Julia has separately validated them in NinjaTrader.

## Working_Suite Indicator Inventory

- `OrcaAbsorptionCandles.cs`: paints candles by absorption/delta intensity using tick data.
- `OrcaAnchoredVWAPs.cs`: anchored VWAPs with reversal thresholds, standard deviation bands, and region fills.
- `OrcaCandleVolumeProfile.cs`: footprint-style candles with per-candle volume/delta profiles and value area.
- `OrcaCumulativeDelta.cs`: cumulative delta OHLC panel.
- `OrcaExecutionLines.cs`: FIFO execution matching, round-trip visualization, SQLite history, R-multiple tracking, and current dirty note/tag journaling work.
- `OrcaExecutionLines2.cs`: deprecated stub pointing to `OrcaExecutionLines.cs`.
- `OrcaLegtoLegProfile.cs`: rotation/leg-based volume and delta profiles.
- `OrcaMGIDaily.cs`: daily structural market levels including current/prior RTH/ETH, overnight, IB/OR, VWAP/value area, and opens.
- `OrcaPrints*.cs`: partial indicator for large prints and clustered aggressive participation.
- `OrcaRollingProfiles.cs`: rolling volume/delta profiles over configurable periods.
- `OrcaStepProfile.cs`: time-sliced profile blocks with POC/value areas and volume/delta histograms.
- `OrcaTickDirectionIndex.cs`: tick-direction based effort/volume classification.
- `OrcaTimeStatistics.cs`: bottom-panel per-bar statistics such as volume, delta, finish delta, range, and time.
- `OrcaTimeVWAPs.cs`: time/session anchored and rolling VWAPs with deviation bands.
- `OrcaVisibleRangeVolumeProfile.cs`: viewport-driven visible-range volume/delta profile using `OrcaVolumeProfileCore`.
- `OrcaVisualOrders.cs`: chart-canvas TP/SL/order visualization and interaction.
- `OrcaVolumeProfileCore.cs`: shared volume profile calculation helper.

## Working_Suite Add-On Inventory

- `OrcaRiskManagerAddOn.cs`: ChartTrader-injected risk/trade manager UI with account/execution handling and chart drag/order overlays.
- `OrcaExecutionRouterAddOn.cs`: execution instrument routing controls and settings window used by visual order tooling.

## External/Adjacent Projects

### Orca Journal

Accessible path:

- `C:\Users\julia\projects\OrcaTrading\OrcaJournal\OrcaJournal\`

Main entry:

- `C:\Users\julia\projects\OrcaTrading\OrcaJournal\OrcaJournal\OrcaJournal.cs`

Observed architecture:

- NinjaTrader AddOn named `Orca Journal`.
- `OrcaJournal.cs` creates `DatabaseManager`, repositories, `TradeBuilder`, and `TradeCapture`, attaches to `Account.ExecutionUpdate`, and adds a Control Center menu item.
- SQLite database path: `Documents/OrcaJournal/orca_journal.db`.
- `Core/TradeCapture.cs`: subscribes to account execution updates, skips SOD events, forwards fills to `TradeBuilder`.
- `Core/TradeBuilder.cs`: assembles fills into one open-to-flat trade record. Reversal fills are explicitly V1-limited and not split.
- `Data/DatabaseManager.cs`: owns SQLite connection, creates schema, uses WAL, enables foreign keys, and has additive migrations.
- `UI/Windows/JournalWindow.cs`: NTWindow with Dashboard, Trades, and Tags tabs.
- `Analytics/ChartRenderer.cs`: uses SkiaSharp to render static chart bitmap images. Comments state LiveCharts2 WPF controls were removed to avoid NT8 D2D visual-tree conflicts.

Relationship to this repo: adjacent/separate source, not part of the current Orca Codex repo tree. Treat it as context only unless Julia asks to work in that path.

