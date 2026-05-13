# Current State

## Git Status

Observed command:

```powershell
git status --short --branch
```

Result summary:

- Branch: `main...origin/main`
- Dirty working tree.
- No code changes were made while preparing this AI context documentation.

Dirty files observed:

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

`git diff --stat` reported 10 files changed with 1221 insertions and 141 deletions. Git also warned that LF will be replaced by CRLF the next time Git touches the dirty files.

## Working_Suite Versus Full_Suite

Same files:

- `AddOns/OrcaExecutionRouterAddOn.cs`
- `Indicators/OrcaAbsorptionCandles.cs`
- `Indicators/OrcaAnchoredVWAPs.cs`
- `Indicators/OrcaCumulativeDelta.cs`
- `Indicators/OrcaExecutionLines2.cs`
- `Indicators/OrcaLegtoLegProfile.cs`
- `Indicators/OrcaPrints.cs`
- `Indicators/OrcaPrints.Engine.cs`
- `Indicators/OrcaPrints.Models.cs`
- `Indicators/OrcaPrints.Scoring.cs`
- `Indicators/OrcaRollingProfiles.cs`
- `Indicators/OrcaStepProfile.cs`
- `Indicators/OrcaTickDirectionIndex.cs`
- `Indicators/OrcaVolumeProfileCore.cs`

Different files:

- `AddOns/OrcaRiskManagerAddOn.cs`
- `Indicators/OrcaCandleVolumeProfile.cs`
- `Indicators/OrcaExecutionLines.cs`
- `Indicators/OrcaMGIDaily.cs`
- `Indicators/OrcaPrints.Rendering.cs`
- `Indicators/OrcaTimeStatistics.cs`
- `Indicators/OrcaTimeVWAPs.cs`
- `Indicators/OrcaVisibleRangeVolumeProfile.cs`
- `Indicators/OrcaVisualOrders.cs`

Only-in-Working or only-in-Full C# files: none observed in the core suite comparison.

## Current Dirty Change Themes

These are observed from diffs and `SESSION_NOTES.md`; they still need Julia/NT8 validation unless already confirmed outside this document.

- `OrcaRiskManagerAddOn.cs`: DPI-safe chart drag overlay changes. Drag/routed order canvases are aligned to the chart control grid slot and price conversion reads overlay-local coordinates.
- `OrcaPrints.Rendering.cs`: hover tracking changed from panel-relative to chart-control coordinates.
- `OrcaVisualOrders.cs`: drag/hover price conversion changed from panel-relative to chart-control coordinates.
- `OrcaVisibleRangeVolumeProfile.cs`: true volume/delta map updates are protected by a lock; render-time profile builds snapshot the visible maps before aggregation; delta label alignment changed to draw within the actual delta bar rectangle.
- `OrcaCandleVolumeProfile.cs`: dynamic volume aggregation settings and helpers added; profile width scaling added; value-area cache tracks compression ticks.
- `OrcaMGIDaily.cs`: daily/RTH/ETH session bookkeeping changes, 1-minute secondary series, RTH history tracking, updated PDC behavior, true/current open timing, IB range handling, label migration, and latest close before maintenance handling.
- `OrcaTimeVWAPs.cs`: RTH end time added; RTH VWAP plots reset outside RTH window; crossing logic changed to avoid carrying RTH values outside bounds.
- `OrcaTimeStatistics.cs`: cell separators added; range formatting and bar-duration calculation changed.
- `OrcaExecutionLines.cs`: note/tag persistence for round trips added under `Documents/OrcaJournal/execution_line_notes.tsv`; right-click/context-menu editor work is present in dirty diff; round trips now carry `Note` and `Tags`.
- `SESSION_NOTES.md`: updated with current visible-range, MGI, and DPI coordinate test plans.

## Validation State

No deployment, promotion, commit, or NT8 compile was performed while creating these docs.

Known validation states from inspected local docs:

- `SESSION_NOTES.md` contains multiple test plans and notes that deployed files still need NinjaTrader F5 compile validation.
- `docs/roadmap.md` still lists validating current NT8 compile and finishing/validating `FinishDelta` naming as priorities.
- Search showed current source uses `ShowFinishDelta`/`FinishDelta*` in `OrcaTimeStatistics`; `DeltaEfficiency` appears only in docs as historical context.

Unknown:

- Whether the current dirty Working_Suite changes have already been compiled in NinjaTrader.
- Whether Julia approved any of the dirty Working_Suite changes for promotion.
- Whether live NinjaTrader Custom folders match Working_Suite, Full_Suite, or another intermediate state.

## Active Scripts

Use:

- `Orca Trades/Scripts/deploy_orca.ps1`
- `Orca Trades/Scripts/promote_working_to_full_suite.ps1`

Avoid unless explicitly requested:

- `Orca Trades/Scripts/promote_to_full_suite.ps1` because it copies from the `NinjaTrader` mirror into `Full_Suite`.
- `Orca Trades/Scripts/deploy_nt8.ps1` because it has stale hard-coded `C:\Users\Owner` paths.

## External Orca Journal State

Accessible:

- `C:\Users\julia\projects\OrcaTrading\OrcaJournal\OrcaJournal\`

Observed source files include:

- `OrcaJournal.cs`
- `Core/TradeCapture.cs`
- `Core/TradeBuilder.cs`
- `Core/InstrumentConfig.cs`
- `Data/DatabaseManager.cs`
- `Data/TradeRepository.cs`
- `Data/TagRepository.cs`
- `Data/SessionRepository.cs`
- `Analytics/KpiCalculator.cs`
- `Analytics/ChartRenderer.cs`
- WPF UI windows/views/view models/controls.

Project notes:

- Target framework: `net48`.
- Output type: library.
- References NinjaTrader core/gui assemblies and `System.Data.SQLite` from the NT8 install.
- Uses SkiaSharp 2.88.9.
- Contains `bin` and `obj` build outputs; treat source files as canonical, not generated outputs.

