# Orca

Orca is a professional NinjaTrader 8 suite for order-flow visualization, volume/delta profiling, VWAPs, execution visualization, and on-chart risk/trade management.

## Working And Source Of Truth

Day-to-day edits happen in the local working copy:

- `Orca Trades/Working_Suite/Indicators`
- `Orca Trades/Working_Suite/AddOns`

After a change is deployed into NinjaTrader, compiled, and approved, promote it into the clean validated suite:

- `Orca Trades/Full_Suite/Indicators`
- `Orca Trades/Full_Suite/AddOns`

Treat `Orca Trades/Full_Suite` as validated code only. Do not experiment directly in it. The older `Orca Trades/NinjaTrader` tree is a local/live mirror and legacy staging area. Do not copy files from `NinjaTrader` back into `Working_Suite` or `Full_Suite` unless you first confirm the file is newer and intended to replace the current version.

## Included Core Suite

### Indicators

- **Orca Anchored VWAPs**: Anchored VWAP with standard deviation bands and reversal tracking.
- **Orca Rolling Profiles**: Rolling volume and delta profiles over configurable time windows.
- **Orca Time VWAPs**: Time/session-anchored VWAPs with deviation bands.
- **Orca Absorption Candles**: Candle coloring based on absorption/delta intensity.
- **Orca Candle Volume Profile**: Per-candle footprint/profile rendering.
- **Orca Cumulative Delta**: Cumulative delta OHLC rendering in a panel.
- **Orca Execution Lines**: FIFO execution matching and on-chart execution visualization.
- **Orca Leg-to-Leg Profile**: Profile rendering between rotation/leg points.
- **Orca Step Profile**: Time-sliced profile rendering with POC/value areas.
- **Orca Tick Direction Index**: Tick-direction based effort/volume classification.
- **Orca Time Statistics**: On-chart volume/delta/time statistics.
- **Orca Visual Orders**: Chart-canvas TP/SL visualization and order interaction.

### Add-Ons

- **Orca Risk Manager**: On-chart risk management, position sizing, and trade controls.

## Related And Legacy Areas

- `Orca Trades/NinjaTrader`: larger working mirror containing additional indicators such as MGI, PassiveFlowSuite, PAX30, and VWAPx.
- `Orca Trades/Stable_Release`: older stable release snapshot.
- `decompiled`: NinjaTrader reference/decompiled source used for platform behavior research. Do not treat this as Orca product code.
- `ControlIndex`, `TempCheck`, and standalone folders: experiments, prototypes, or support projects.

## Development Workflow

1. Edit files in `Orca Trades/Working_Suite`.
2. Use `Orca Trades/Scripts/deploy_orca.ps1` to deploy from `Working_Suite`.
3. Press `F5` in the NinjaTrader 8 NinjaScript Editor to compile.
4. If NinjaTrader reports stale names or duplicate definitions, check for ghost files in `Documents/NinjaTrader 8/bin/Custom`.
5. After Julia approves the tested behavior, run `Orca Trades/Scripts/promote_working_to_full_suite.ps1 -Target <FileName>` to promote the validated file.

## Documentation

Start here:

- `docs/codex-onboarding.md`
- `docs/architecture.md`
- `docs/engineering-notes.md`
- `docs/collaboration-workflow.md`
- `docs/known-issues.md`
- `docs/roadmap.md`

## Agent Safety

Claude Code and Codex may both work on this repo. Before editing, every agent should read `AGENTS.md` and `CLAUDE.md`, check `git status`, and avoid editing the same file as another active tool unless Julia explicitly coordinates the handoff.
