# Orca Journal — multi-tag performance and overlaps — 2026-09-06

## Objective
Julian approved performance by any number of selected tags, including their overlap, to explore useful setup/management combinations. Build this while the market is closed; MAE/MFE live validation remains deferred. Retain existing annotations/media and unrelated dirty suite work.

## Files changed
External source C:\Users\julia\projects\OrcaTrading\OrcaJournal:
- OrcaJournal/Analytics/TagPerformance.cs: selection/intersection/union, observed overlap partitions and summary metrics.
- OrcaJournal/Data/TagPerformanceRepository.cs: effective trade/tag snapshot with saved-review precedence.
- OrcaJournal/UI/Views/TagPerformanceView.cs: new dark Tag performance tab UI.
- OrcaJournal/UI/Windows/JournalWindow.cs: add tab and refresh alongside existing views.
Coordination: tests/OrcaJournal.Identity/{TagPerformanceChecks.cs,Program.cs,OrcaJournal.Identity.csproj}, tests/OrcaJournal.ReviewUI/Program.cs, this handoff. Working_Suite and Full_Suite untouched.

## Behavior and settings
New Tag performance tab; existing Tags tab remains tag management. Choose assigned tags across Risk & Trade Management, Analysis & Execution, Other. Search narrows visible choices while retaining selection. Clear tags restores all scoped trades, including untagged trades.
All selected tags is intersection (additional tags allowed). Any selected tag is union; every matching trade counted once. No hard tag-count limit; no bitmask/64-tag cutoff. Overlap rows show only memberships actually observed, not all possible combinations. Within selected tags rows are disjoint; unselected tags ignored. The table is more readable than overlapping circles for many tags. No automatic claim to discover the best strategy.
Filters: account, full-contract instrument, inclusive SessionDate From/To, All/Any. Refresh retains selections/filter scope where account/instrument values remain available. Invalid date range displays an error/empty grids. Double-click a matching trade row opens review; saving refreshes all Journal views.
Stats: count, winners/all matching trades (breakeven stays in denominator), net P&L, average P&L, mean realized R only for positive finite explicitly entered PlannedRisk. Missing risk is excluded, never zero-filled; risk-covered count displayed. Empty results show unknown rates/averages. Numeric columns sort by numeric properties, not formatted currency strings. Long tag text wraps. Matching trades include close time/account/contract/direction/P&L/tags.
No schema changes or settings persistence in this slice. No annotation/media mutation by analysis. Existing review editor writes only on explicit user Save. Base trade_tags loaded in bulk; saved journal_reviews tag sets replace base tags, including intentional empty sets. TradeRepository.GetAll supplies existing planned-risk overlay. Existing ambiguous/duplicate historical trade rows remain distinct as in Journal; no automatic deduplication.

## Series, Tick Replay, history, cache, rendering and performance
No data subscriptions, AddDataSeries, OnMarketData, Calculate changes, Tick/Second/Bid/Ask/Last/Volumetric series, or Tick Replay behavior changes. Historical trade analytics only; no historical market-data requests. Bulk database snapshot loaded on view creation/Refresh/review save, no per-trade query loop. Selection/search uses cached snapshots/options. Runtime work is proportional to loaded trade count times selected tag count; observed groups only, no exponential subset enumeration. All ordinary WPF UI work; no indicator OnRender work, price caches or per-tick allocations. No live performance benchmark claim.

## Verification
- net48/x64 Release build: 0 errors, 4 existing MSB3277 warnings. SQLite reference2.0.2.0 retained.
- 16 new deterministic assertions: all/any, case-insensitive selection, duplicate selection, extra tags, disjoint overlap/count totals, P&L/rates, missing/invalid risk, empty/untagged/breakeven,70 selections, database tag precedence and explicitly cleared tags.
- Existing24 excursion/title,13 risk/media,22 review,85 identity assertions pass.
- Actual compiled WPF harness passes all/any selection, date scope, preserved selection on refresh, plus earlier review/media checks. Render inspected: dark readable tag text, numeric tables, auto-height wrapping; UI artifact C:\Users\julia\AppData\Local\Temp\orca-review-ui-913f06156e1c4d4fa7f58705e96a235f\tag-performance.png.
- Live NinjaTrader behavior of this tab and the earlier MAE/MFE batch remains unverified. Julian explicitly deferred market/Playback excursion checks. No live data used or changed by test fixtures.

## Deployment completed — 2026-09-06 13:00 local
NinjaTrader remained closed from Julian's authorized restart batch; absence rechecked before backup/copy and after deployment. Source/stage parity, prior live baseline and SQLite reference verified. Copied only OrcaJournal.dll.
Live SHA25661E56B3AE09312E2B358EFCAD54AE1C4420897684672F4CBB19B4E32765E3D2C.
Prior live A5FC0BCB67A3A491E4820507E2E903E1860D47D61A51CD8F02D8A22468B0ED8C.
Backup C:\Users\julia\Documents\New project\.codex-backups\journal-tag-performance-deploy-20260906-130021 contains prior DLL, database and available sidecars/annotation TSV plus deployment.json. Backups hash verified and live data hashes unchanged. Original media untouched. Includes previous excursion/title changes.

## Manual validation, limitations and follow-up
On restart open Tag performance, select two known tags and compare All vs Any, inspect matching trade IDs/context, dates/accounts/contracts, numeric sorting, category visibility and review-save refresh. Check empty/no-tag and missing-risk cases. No Full_Suite promotion eligibility. Runtime validation remains separate from build/deployment.
Comparisons are descriptive, with explicit sample counts; tags are not mutually exclusive outside the selected-membership partition. No saved filter presets, baseline comparison, statistical confidence estimates, automated best-subset search, or export of this filtered analysis yet. Market-open SIM validation of round-trip excursions remains the next capture check, not required to use historical tag analysis.
