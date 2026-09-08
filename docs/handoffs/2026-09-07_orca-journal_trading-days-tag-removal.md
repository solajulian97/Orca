# Orca Journal — rolling trading days and tag removal — 2026-09-07

## Objective / behavior
Fix user-reported calendar-day rolling ranges and month arrows resetting selected period. Last 5/10/20 Trading Days count Monday-Friday inclusively; weekend anchors use preceding weekdays. Trades in full inclusive date range retained, including Sunday futures activity. Explicit limitation: no exchange holiday calendar/session-template mapping; existing trade SessionDate semantics unchanged. Monday Sep 7 five-day start Sep 1; ten-day start Aug 25; twenty-day start Aug 11. Period labels updated; old internal strings accepted for compatibility.
Month browsing changes visible month without changing rolling/day selection or totals. Explicit Calendar month selection retains month filtering/navigation. Refresh and account changes preserve browsed month; selecting a new non-month period returns view to its end month. Help text updated.

Visible Remove from library action confirms hiding from library/new tag choices while preserving historical assignments, saved reviews/revisions, rules and media. Uses existing hidden-tag table, no migration. Existing review selections/performance membership retain historical tag. Add same name again restores it. Legacy Advanced delete semantics unchanged. This is reversible removal, not global deletion of trade history.

## Files
External OrcaJournal/UI/ViewModels/DashboardViewModel.cs, UI/Views/DashboardView.xaml, UI/Views/TagLibraryView.cs, Data/TradeReviewRepository.cs.
Coordination tests/OrcaJournal.Dashboard/Program.cs and tests/OrcaJournal.ReviewUI/Program.cs.

## Platform and performance
No secondary series, AddDataSeries, OnMarketData, Tick Replay, data-cache or historical-load changes. UI date-range loop bounded by 20 weekdays; filtering/rendering otherwise unchanged. No trade recalculation/persistence mutation from date navigation. No Working_Suite/Full_Suite changes.

## Verification
Release build 0 errors / 4 existing assembly warnings. 26 dashboard checks: Monday/weekend, month/year/leap boundaries, inclusive cutoff, browsing/refresh preserving period and totals, explicit calendar month behavior. WPF checks cover removal hiding new choices without altering review tags/revision, restoration, gallery/saved combinations/review/media behavior. Disposable fixtures only; live DB untouched.
User confirmed prior Tag Library/open screenshot and Tag Performance layout at runtime. These new fixes are offline-verified only; no NinjaTrader runtime tests. MAE/MFE live capture remains pending. Not eligible for Full_Suite promotion.

## Stage
.codex-backups/journal-trading-days-stage/OrcaJournal.dll
SHA256 5BC7CA068EF3FE908E92E52596D30B9C3979D8D15BE6A7BCA0A5442A63C8ECE2
Not deployed. Current expected live baseline 6E4B98AAFB51FAAABE3357C786C37DB99AD269C4CD01AAA15E27BB78C4889C94. Guarded backup/copy after NinjaTrader closes.

## Deployment completed — 2026-09-08 00:05 EDT
NinjaTrader confirmed closed before and after deployment. Source/stage/live SHA256 matched 5BC7CA068EF3FE908E92E52596D30B9C3979D8D15BE6A7BCA0A5442A63C8ECE2; SQLite reference 2.0.2.0 verified. Backup/manifest: .codex-backups/journal-trading-days-deploy-20260908-000524. Only DLL copied; database/annotation TSV hashes unchanged. Supersedes not-deployed status above. Custom Dashboard date ranges remain proposed, not implemented or included. Startup and manual validation of this build remain pending.
