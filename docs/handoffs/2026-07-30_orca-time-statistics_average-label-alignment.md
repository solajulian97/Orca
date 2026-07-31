# 2026-07-30 - Orca Time Statistics Average Label Alignment

## Objective

Keep the average-value column adjacent to the current/latest bar while restoring the metric row labels to the far-right edge of the Time Statistics panel.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaTimeStatistics.cs`
- `docs/handoffs/2026-07-30_orca-time-statistics_average-label-alignment.md`

## Behavior added, changed, or removed

- Average values retain the existing current-bar-adjacent placement calculated by `TryGetAverageColumnLayout` and drawn by `DrawAverageColumn`.
- Metric row labels now always render through `DrawRightLabel` at the right edge of the chart panel, independent of whether averages are visible.
- Removed the now-unused near-average `DrawRowLabels` and `GetCompactRowLabel` helpers.
- No calculation, delta, cumulative-delta, percentage, source-mode, or scale-mask behavior changed.

## User-facing settings added, changed, deprecated, or removed

- None.
- Existing `Show Averages` and average lookback behavior are unchanged.

## Secondary series added or changed

- None. No Tick, Second, Bid, Ask, Last, Volumetric, or custom series changed.

## Tick Replay implications

- None. The change affects SharpDX text placement only and does not alter event processing or historical classification.

## Historical-load implications

- None. Historical calculations, provider backfill, and bar access are unchanged.

## Cache implications

- None. Shared-provider and local state behavior are unchanged.

## Rendering implications

- The average column and labels are now positioned independently.
- Averages remain next to the current/latest bar.
- Full metric labels remain trailing-aligned at `ChartPanel.X + ChartPanel.W - 5f`.
- Existing render-target resource tracking, scale mask, diagnostics render sampling, and fail-soft guards are preserved.

## Performance implications

- Negligible. The same per-row right-label text layouts already used when averages were unavailable are now used consistently.
- Removing the unused compact-label helper does not alter runtime work.

## Tests performed in NinjaTrader

- Targeted deployment completed with `deploy_orca.ps1 -Target OrcaTimeStatistics`.
- Working_Suite and deployed live source match after line-ending normalization.
- Working_Suite and deployed live files each contain exactly one NinjaScript generated-code region.
- NinjaTrader F5 compile has not yet been performed in this change session.
- Visual confirmation on a live chart is pending Julian.

## Compile status

- `git diff --check` passed with only the existing LF-to-CRLF warning.
- Standalone Roslyn syntax parsing could not be completed because Windows PowerShell could not load NinjaTrader's Roslyn dependency version (`System.Collections.Immutable` 9.0.0.0).
- NinjaTrader F5 compile: pending Julian.

## Manual-validation status

- Pending Julian.
- Confirm that enabling averages leaves the average values adjacent to the current bar while all row labels remain aligned at the far-right panel edge.

## Known issues, risks, and follow-up work

- If the chart has an unusually narrow right margin, the existing average-column fit logic may still fall back based on available width; that behavior was not changed.
- The right-side scale mask and its interaction with NinjaTrader's native scale remain unchanged.

## Full_Suite promotion eligibility

- Not eligible yet.
- Promotion requires a successful NinjaTrader F5 compile and Julian's live manual validation.
