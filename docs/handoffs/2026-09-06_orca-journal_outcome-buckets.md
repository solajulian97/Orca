# Orca Journal — outcome buckets, breakeven band and trade sorting — 2026-09-06

## Objective
Julian requested clear winner/loser row backgrounds, clickable P&L/direction/instrument sorting, and a practical breakeven bucket for small +/-P&L trades. Default breakeven is -20 through +20 dollars inclusive; exclude it from meaningful-trade statistics, particularly tag performance. General review aesthetics deferred. User screenshot confirms current review screenshots display; video launch and revised excursion runtime still not confirmed.

## Files changed
External source C:\Users\julia\projects\OrcaTrading\OrcaJournal:
- Analytics/TradeOutcomes.cs (new): classification and shared threshold.
- UI/Controls/OutcomeRows.cs (new): frozen dark winner/loser/neutral brushes and selection override.
- Analytics/TagPerformance.cs: cohort RR.
- UI/ViewModels/TradesViewModel.cs; UI/Views/TradesView.xaml and .xaml.cs; UI/Views/TagPerformanceView.cs; UI/Windows/JournalWindow.cs.
Coordination: tests/OrcaJournal.Identity/{OrcaJournal.Identity.csproj,TagPerformanceChecks.cs}, tests/OrcaJournal.ReviewUI/Program.cs, this handoff.

## Behavior and user-facing controls
Trades and tag-performance matching trades use subtle green for P&L>threshold, red for P&L<-threshold, neutral for inclusive breakeven band. Blue selection preserved. No reliance on color alone: outcome dropdown identifies selected bucket.
Both views expose All trades, Exclude breakevens, Winners, Losers, Breakevens and adjustable Breakeven +/-$. Default20; finite nonnegative input, apply on focus loss/Tab. Shared threshold within the Journal window, independent bucket selections. Threshold and selections are session/window preferences, not persisted in this slice.
P&L, Direction and Instrument headers explicitly enable sorting with numeric PnlDollars/underlying fields. Other numeric fields retain their existing sort behavior. Filter changes clear details if selected trade becomes hidden. Calendar navigation resets outcome to All trades so a selected day is not silently incomplete.
Tag-performance outcome filtering occurs before all/any tag matching and overlap partitioning. Summary/counts/net/average/win rate/average R are computed from that selected cohort; excluded-scope count and inclusive band shown. RR added as mean positive P&L / absolute mean negative P&L; unknown if either side absent. RR sorts numerically. With Exclude breakevens, small results do not enter the win-rate denominator or RR means. All trades retains original sign-based win-rate/P&L behavior, including small dollar gains/losses. Dashboard KPIs unchanged. No synthetic tags or modifications to recorded P&L, user tags, annotations, reviews or media. Existing Trades export follows the filtered displayed trade collection.

## Verification
net48/x64 Release0 errors,4 existing MSB3277 warnings; diff whitespace checks pass.
23 tag-performance checks (7 new outcome checks): +/-20 boundaries, immediately outside, zero/small gain, exclusion cohort count/winrate/RR/net, unchanged raw P&L, threshold change notification, invalid negative threshold rejected. Existing25 excursion/title,13 risk/media,22 review,85 identity checks pass.
Actual compiled WPF harness: outcome filter, simulated real P&L header OnClick ascending/descending numeric ordering, winner/loser background difference, plus existing day/navigation/media/tag checks. Render inspected; dark threshold label contrast corrected. Artifact C:\Users\julia\AppData\Local\Temp\orca-review-ui-88571e4a8a31434aa905fa5425c04b88\outcomes.png. Instrument/direction explicit sort binding compiled but native-click behavior still needs manual validation. No NinjaTrader tests of this build.

## Implications
No schema migration, new data series/subscriptions, Tick Replay, AddDataSeries, OnMarketData, Calculate or historical-market-load changes. No new persistent cache. Ordinary WPF filtering/rendering over existing trade snapshot, scalar threshold classification and frozen shared brushes. No indicator OnRender work or per-tick analytics. No measured performance claim. Working_Suite/Full_Suite and unrelated dirty work untouched; not eligible for Full_Suite promotion.

## Staged, not deployed
NinjaTrader observed running PID40272. Stage .codex-backups/journal-outcomes-stage/OrcaJournal.dll SHA2561C5B8BE889D3A205420D5006286000AB7DBDE85FFB5213754D21912D2ADEDF9F. Expected live baseline B5BB2011FC8F477F793BC9BD20D3DAA04F1FA6D60F60E6CEC409662C57B91D54.
After closure: verify source/stage/live hashes/SQLite reference and process absence, back up DLL/data/sidecars/TSV, copy only DLL and verify parity. Manual checks: all outcome buckets, threshold shared between views, inclusive boundaries, all/any tags combined with bucket, numeric ascending/descending headers and blue selection. Runtime MAE/MFE remains a separate pending validation track. Preference persistence/dashboard outcome view can be considered later if requested.
