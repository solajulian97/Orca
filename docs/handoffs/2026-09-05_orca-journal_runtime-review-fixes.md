# Orca Journal runtime viewer fix and readable review fields — 2026-09-05

## Objective and observed runtime result
Julian opened the deployed Journal, pasted a screenshot, and reported Open Image Failed: "The calling thread cannot access this object because a different thread owns it." His screenshots establish Journal UI startup, a pasted image thumbnail, and a reconciliation report; they do not validate image opening, shared-identity SIM behavior, or every existing annotation/media link. Fix this reported defect before Journal-side annotation editing.

## Files and behavior
External source root: C:\Users\julia\projects\OrcaTrading\OrcaJournal\OrcaJournal.
- UI/ViewModels/TradesViewModel.cs and UI/Windows/TradeImageViewer.cs: remove Application.Current.Windows scan. Resolve the owner from the clicked button's visual tree, on the Journal UI dispatcher. Same-dispatcher helper tested.
- UI/Views/TradesView.xaml: pass the Open button as command context, retain its attachment DataContext; hold-time display, numeric sort and wrapped detail text.
- Data/Models/Trade.cs: display-only HoldDuration property. Unknown/negative is '-'; 60 seconds is '1 minute, 0 seconds'; 4330 is '1 hour, 12 minutes, 10 seconds'. Hours do not wrap at 24. Underlying seconds unchanged.
- UI/Controls/TradeRow.xaml: use the same duration formatting.
- UI/Views/ReconciliationView.cs: plain-language summary and collapsed technical details, Check trade links button. Matching algorithm, report rows and read-only behavior unchanged.
- Coordination tests/OrcaJournal.Screenshots/Program.cs and Verify.ps1: multi-STA regression and duration boundaries in addition to previous image tests.

Reconciliation checks identity/link confidence. Julian's displayed report had Exact 0, Legacy 2057, Ambiguous 39, Unmatched 95. These are diagnostic rows, not counts of lost trades or deleted media. Legacy matches lack shared execution-identity proof; ambiguous results require investigation if a link is missing or wrong. No merge, repair, deletion, or relinking was performed.

## Verification and staged deployment
24 offline WPF checks pass: old application-window access reproduces the cross-thread exception, new owner assignment succeeds on a secondary STA with its own shown window; duration tests include null/negative, seconds, minute/hour boundaries and more than 24 hours. Existing image navigation/error tests remain passing. The owner test window is offscreen and does not activate; no NinjaTrader UI was automated.
Release net48 x64 build passes with 0 errors and 4 existing MSB3277 warnings. SQLite reference remains 2.0.2.0 with installed public key token db937bc2d44ff139.
Staged DLL: C:\Users\julia\Documents\New project\.codex-backups\journal-runtime-fixes-stage\OrcaJournal.dll
SHA256: 6379428AE1E96D52A982A8D53259D1279C58283B134CDE67C34BE1F0C4E0E8D2.
NinjaTrader is running; no deployment performed. Live DLL remains E9E88E716DEE73B4153BAEBCE0459BEAF6D392645B97E78CDA0E20F1CB782C00. After Julian closes NinjaTrader, recheck process absence, source/stage hashes and live baseline, back up DLL and database/sidecars/annotation TSV, copy only DLL, verify parity. No data mutation required.

## Implications and pending validation
No persistent settings, schemas, secondary series, AddDataSeries, OnMarketData, Calculate modes, Tick Replay, historical-load, cache, trade calculations, or indicator changes. Rendering changes are ordinary WPF layout/text and window ownership only. No per-tick work added. No performance benchmark claimed. Existing annotations/media and unrelated dirty work preserved. Full_Suite untouched and not eligible for promotion.
NinjaTrader compile/load and runtime validation of this fix remain pending. Retry the existing pasted image, test zoom/pan and videos, inspect long hold duration and numeric sorting, run the summarized reconciliation and expand technical details. Prior shared identity/SIM validation remains pending. Journal-side editing deferred until this runtime defect is resolved.

## Deployment completed — 2026-09-05 19:34 local
Julian confirmed platform closure and authorized proceeding. NinjaTrader was absent before backup, immediately before copying, and after deployment. Source and stage matched the recorded tested SHA256, and the live prior DLL matched its expected baseline. SQLite metadata verified as 2.0.2.0.
Deployed only Custom\OrcaJournal.dll; live hash matches 6379428AE1E96D52A982A8D53259D1279C58283B134CDE67C34BE1F0C4E0E8D2.
Backup: C:\Users\julia\Documents\New project\.codex-backups\journal-runtime-fixes-deploy-20260905-193452. Prior DLL and existing database/sidecars/annotation TSV backed up; data backup hashes verified. Live database and annotation-file hashes remained unchanged. No media edits or unrelated source changes.
Deployment is complete. NinjaTrader restart/F5/load and screenshot-opening runtime confirmation remain pending; the 24 passing checks are offline evidence only.
