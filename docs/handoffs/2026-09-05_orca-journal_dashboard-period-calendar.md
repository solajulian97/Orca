# Orca Journal dashboard RR and P&L calendar — 2026-09-05

## Objective
Julian requested RR between Win Rate and Profit Factor, half-hour P&L for a single-day view, and calendar-style daily P&L. This extends the pending review-editor/dark-viewer batch; it does not replace those changes.

## Definitions and behavior
RR is realized average positive trade P&L divided by average absolute negative trade P&L. Breakeven trades are excluded from both averages. Show an em dash if there are no winners or no losers. It is not planned stop/target RR or the separate PnlR field. Existing WinRate/ProfitFactor/Expectancy calculations are unchanged. Optional clarification was requested; these defaults were stated while proceeding without an answer.
Thirty-minute intervals use stored trade ExitTime, with inclusive start and exclusive end: 11:00 <= close < 11:30. Each completed trade's full stored PnlDollars belongs to its closing period; this is not mark-to-market P&L or partial-execution attribution. Empty intervals between the first and last active periods show zero. Signed horizontal bars distinguish gains/losses, with dollar labels and precise two-decimal/count tooltips. No timezone conversion or new session convention is introduced.
Calendar totals use the existing SessionDate and filtered account/range. The current producer derives SessionDate from exit date. Day cells show P&L/trade count, distinguish no trades from flat trades, and dim dates outside the selected range. Calendar has Sunday-first weekday columns and six weeks; adjacent dates are clickable. Click a day for its equity/half-hour breakdown; month arrows select a whole month. Added Selected day and Calendar month period choices. Today/Yesterday are bounded to exact days; future-dated trades are excluded from rolling ranges. Refresh preserves period/account selection. Existing equity curve rendering remains SkiaSharp; new period/calendar views use WPF controls without LiveCharts.

## Files changed
External C:\Users\julia\projects\OrcaTrading\OrcaJournal\OrcaJournal:
- Analytics/KpiCalculator.cs: RR value/format.
- Analytics/DashboardBreakdown.cs: pure half-hour and calendar aggregation/view rows.
- UI/ViewModels/DashboardViewModel.cs: date/account filtering, calendar navigation, selected-day drilldown and preserved selection on refresh. Old daily bitmap is no longer rendered.
- UI/Views/DashboardView.xaml: RR card and new right-hand period/calendar panel, replacing daily bars.
Coordination tests/OrcaJournal.Dashboard/Program.cs and Verify.ps1; this handoff copied into external docs/handoffs.

## Verification
Release net48 x64 build: 0 errors, 4 existing MSB3277 warnings. 15 offline checks pass: half-hour sums, exact 11:30 boundary, zero interval, total conservation, RR excluding flat trades, missing-side RR, leap day, calendar alignment/totals, account/day filtering, refresh selection retention, month navigation, day drilldown, Today date bounds. Actual compiled DashboardView rendered with synthetic data and visually inspected. Final artifacts: C:\Users\julia\AppData\Local\Temp\orca-dashboard-7bcf94faff75485cb1e9fcd0a15f2b9b.
No live data modifications or NinjaTrader testing of this batch. No new dependencies, schema changes in this slice, indicator/secondary-series changes, AddDataSeries, OnMarketData, Calculate modes, Tick Replay, historical load or cache changes. Aggregations run on filter/load changes, not per-tick or OnRender. Period/calendar rows are bounded by selected data/month; original equity resize behavior retained. No performance benchmark claimed. Unrelated dirty work, annotations/media, Full_Suite preserved; no promotion eligible.

## Combined stage and remaining gates
Staged DLL includes these dashboard changes, review editing schema 7 and dark viewer changes:
C:\Users\julia\Documents\New project\.codex-backups\journal-dashboard-stage\OrcaJournal.dll
SHA256 D02966A1B116E207F04C9969E41A676F082D67B476581DFF3A363C84F1830C32.
Supersedes review-only/dark-only stage. NinjaTrader running PID 29972; no deployment. Expected current live baseline: 6379428AE1E96D52A982A8D53259D1279C58283B134CDE67C34BE1F0C4E0E8D2. Before copy: confirm closed, verify hashes and SQLite metadata, back up DLL/database/sidecars/TSV, copy only exact staged DLL, verify parity. Startup schema-7 implications and review ownership remain as documented in 2026-09-05_orca-journal_review-editing-v1.md.
Manual checks: RR placement/value, exact-period attribution for known trades, account selection, calendar navigation and day click, zero/no-trade dates, narrow window/high-DPI appearance, plus pending review editing and dark title-bar validation. Runtime defects take priority over advancing the roadmap. Screenshot captions remain a backlog item.

## Deployment completed — 2026-09-05 20:25 local
Julian confirmed closure and authorized deployment. NinjaTrader was absent before backup, before copy and after copy. Source/stage hashes matched the tested combined build; prior live hash matched the expected runtime-fix baseline. SQLite metadata verified as 2.0.2.0. Copied only Custom\OrcaJournal.dll; live SHA256 now D02966A1B116E207F04C9969E41A676F082D67B476581DFF3A363C84F1830C32.
Backup: C:\Users\julia\Documents\New project\.codex-backups\journal-dashboard-deploy-20260905-202509. Prior DLL/database and available sidecars/annotation TSV copied and hash-verified. Live data hashes unchanged during deployment; no media or source files changed. Schema 7 review tables initialize on next startup, not during this copy.
Combined dashboard/review editor/dark-viewer deployment complete. Restart/F5/load and runtime validation remain pending; offline tests do not establish platform success.
