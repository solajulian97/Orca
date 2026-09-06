# Orca Journal — runtime excursion diagnosis, calendar navigation and review media — 2026-09-06

## Objective and runtime feedback
Julian requested calendar date clicks to open that day's trades, and screenshots/video inside the review reached from tag performance. He confirmed the tag-performance tab is useful and showed its running UI. He reported no MAE/MFE for a new18-quantity Sim101 MNQ round trip. This is runtime feedback, not validation of every feature.

## Evidence and diagnosis
Read-only SQLite query of September6 trades found:
- id1958 Sim101 MNQ SEP26,19:14:52–19:18:09, quantity18,P&L203,2155 price observations; first19:14:56,last19:18:05. Identity status Initial flat boundary or continuity unverified.
- id1957 on a different account,78 price observations, same initial-boundary status.
Thus price subscription did receive samples; the shared identity complete-history gate alone is sufficient to suppress excursions in the deployed code. The earlier trade did not establish Sim101 continuity. Previously stored data lacks raw extrema, so these values cannot be restored from sample counts. Actual prior accumulator timestamp validity is not persisted and cannot be inferred from this query. No historical backfill or live data edits performed.

## Files changed
External source C:\Users\julia\projects\OrcaTrading\OrcaJournal:
- Core/TradeBuilder.cs and RoundTripExcursion.cs.
- Data/Models/Trade.cs and TradeReviewRepository.cs.
- UI/ViewModels/DashboardViewModel.cs, TradesViewModel.cs.
- UI/Views/DashboardView.xaml, TradesView.xaml.
- UI/Windows/JournalWindow.cs, TradeReviewWindow.cs.
Coordination tests: tests/OrcaJournal.Identity/ExcursionChecks.cs, tests/OrcaJournal.ReviewUI/Program.cs. No Working_Suite/Full_Suite edits; unrelated dirty work preserved.

## Behavior and user-facing changes
Calendar selection retains selected-day dashboard/30-minute breakdown, refreshes Journal data, opens Trades with inclusive day+dashboard account, resets instrument/direction to All and selects first matching trade (or clears selection on empty day). Trades date filtering now uses SessionDate, same basis as dashboard. Calendar hint updated.
Review now shows Screenshots & video after tags, with saved titles, bounded unlocked thumbnails, Open screenshot using existing zoom/gallery on the review's dispatcher, and Play video using Windows associated player. Missing files show an error. No attachment edits or file mutation. Review title gets account, full instrument fallback, direction, entry time and P&L directly from the selected trade record; legacy short instruments no longer show blank context. Saving/draft/conflict behavior preserved.
Excursions no longer discard otherwise valid sampled observations solely because shared UID starting-flat history is unverified. Such future records explicitly say Partial observed: starting flat boundary unverified; before commissions and both values carry a Partial prefix. Complete-history measurements keep Live observed (sampled; before commissions). Shared identity policy/UIDs remain unchanged; partial observations are NOT proof of a complete round-trip price path.
Added independent execution.Position versus reconstructed quantity consistency check before each allocated fill, including reversals. Position mismatch, disconnect/identity invalidation or out-of-order/invalid price-time evidence still withholds numeric values. First failure reason now retained in excursion quality for diagnosis. No Last samples remains unavailable. Scale-ins/partials retain flat-to-flat accumulation; no per-order excursion rows.
Existing historical rows, media, annotations and reviews unchanged. No schema migration or backfill. Old missing extrema cannot be recovered by this build.

## Series, Tick Replay, history, cache, rendering and performance
No new subscriptions/secondary series, AddDataSeries, BarsRequest, Calculate settings or Tick Replay changes. Existing shared Last feed and active-bucket scalar calculation retained. One scalar position check per fill; no per-tick database work. No historical-data loads or new caches. Review images reuse 256px OnLoad decoder; native UI work only, no indicator OnRender work. No measured live performance claims.

## Verification
Release net48/x64 build0 errors,4 existing MSB3277 warnings.16 tag,25 excursion/title,13 risk/media,22 review,85 identity checks pass. Revised tests assert explicitly partial initial cycle and suppress startup-mid-position mismatch. Existing disconnect/out-of-order/scale/reversal tests remain passing.
Actual compiled WPF harness verifies matching-trade review screenshot and video buttons, decoded thumbnail/title, day filtering clearing stale instrument/direction and empty selection. Review/media render inspected at C:\Users\julia\AppData\Local\Temp\orca-review-ui-b8073180671146868791a069a58f6d9c\review-media.png. Native video player launch not exercised.15 dashboard checks/render pass. No NinjaTrader tests of this revised build. Diff whitespace checks pass.

## Stage and next steps
Not deployed: NinjaTrader observed running PID48420. Stage .codex-backups/journal-runtime-navigation-stage/OrcaJournal.dll SHA256 B5BB2011FC8F477F793BC9BD20D3DAA04F1FA6D60F60E6CEC409662C57B91D54. Expected current live61E56B3AE09312E2B358EFCAD54AE1C4420897684672F4CBB19B4E32765E3D2C.
After Julian closes: guarded metadata/hash checks, backup DLL/database/sidecars/TSV, copy only staged DLL and verify live parity. Then validate calendar click account/day navigation, both screenshot and real video open from tag matching reviews, first and subsequent SIM round-trip excursions, and any specific rejection reason. First-cycle partial figures may omit unobserved prices and must stay qualified. Initial account-flat snapshot reconciliation remains future work; no claim that complete-history identity has been solved. No Full_Suite promotion eligibility.
