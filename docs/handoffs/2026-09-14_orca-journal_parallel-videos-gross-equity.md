# Orca Journal: parallel videos, gross P&L, and equity intervals

## Objective and findings
Julian confirmed that the September 13 deployment now captures observed MAE/MFE and links most recordings. September 14 screenshots show partial MAE/MFE, not complete price coverage. Preserve that quality distinction.

The unlinked MES trades were not excluded simply because trades overlapped. Recorder seeded its cash/quantity ledger from positions already open when armed, but its identity tracker still started at zero. After the seeded position closed, later MES identity allocations could remain shifted by the original quantity. Examples included a 20-contract MES round trip with a 15-contract identity and earlier complete cash-ledger trades with no identity at all. Multi-instrument and post-roll re-entry clips remain intentional.

The screenshot P&L discrepancy has two separate causes:
- Journal accepted NQ/MNQ/ES/MES but silently omitted MGC. Before local noon on September 14, the selected account had $1,213.75 MES + $821.50 MNQ = $2,035.25. Six omitted MGC round trips total -$60.00, giving $1,975.25 gross, exactly the TPT screenshot.
- The dashboard called the sum of gross trade P&L “NET P&L.” TPT net $1,673.75 is $301.50 below gross. Stored NinjaTrader execution Commission and Fee fields are zero; actual per-trade deductions cannot be reconstructed from those fields. No invented commission schedule or net P&L is introduced.

Evidence: read-only NinjaTrader and Journal SQLite snapshots, allocated execution-ID/price comparisons, recorder capture manifests, and the supplied screenshots. Audit artifacts are under `C:\Users\julia\Documents\New project\.codex-backups\journal-dashboard-20260914`.

## Files changed
Working_Suite: `Orca Trades/Working_Suite/AddOns/OrcaTradeRecorderAddOn.cs`.

Authoritative external repository: `C:\Users\julia\projects\OrcaTrading\OrcaJournal`:
- `OrcaJournal/Core/InstrumentConfig.cs`: MGC .10 tick / $1 tick value.
- `OrcaJournal/Data/AttachmentRepository.cs`, `TradeRecordingImporter.cs`: explicit manual video attachment and immediate status.
- `OrcaJournal/UI/Windows/TradeMediaWindow.cs`, `UI/ViewModels/TradesViewModel.cs`, `UI/Views/TradesView.xaml`: Attach video from trade details or Manage; MGC instrument filter.
- `OrcaJournal/Analytics/EquitySeries.cs`, `ChartRenderer.cs`, `UI/ViewModels/DashboardViewModel.cs`, `UI/Views/DashboardView.xaml`, `DashboardView.xaml.cs`: ordered realized-gross series, interval selection, bitmap chart labels, pointer overlay, gross label with cents, chronological dashboard statistics.
- Corresponding handoff copied to external `docs/handoffs`.

Coordination tests: `tests/OrcaTradeRecorder/Verify.ps1`, `tests/OrcaJournal.Dashboard/Program.cs`, `tests/OrcaJournal.Identity/RiskMediaChecks.cs`; new `tests/OrcaJournal.MgcRecovery/Program.cs` and `apply_recovery.py`. Component documentation: `docs/ORCA_TRADE_RECORDER.md`.

## Behavior and settings
- Seeded recorder positions suppress identity until a flat boundary. A reversal starts the next identity with only its opening remainder. Unknown initial history remains unknown. Later fully observed round trips can match the Journal’s allocations.
- Manual Attach video references an existing nonempty supported video file. The same original can be attached to multiple trades. No file copying, moving, or deletion. Explicit reattachment restores a detached association; automatic imports still respect tombstones. Moving the original file later breaks its references.
- New equity selector: Trade by trade (default), 5 minutes, 15 minutes, 30 minutes. Realized gross P&L uses close-time order; buckets include their start and exclude their end. Empty intraday buckets carry the cumulative total; overnight inactive gaps are omitted. No mark-to-market or unsaved price history is implied. Selection is retained during view-model refresh, not persisted across application restarts.
- Hover shows trade/period P&L, date/time, and running gross P&L, using the same points and geometry as the frozen bitmap. Existing account/date filters apply to all modes.
- Dashboard label changes NET P&L to GROSS P&L and displays cents. Existing `NetPnlDollars` property identity is retained for compatibility; its value is gross.
- No recorder settings or post-roll duration changes.

## Historical recovery
The recovery harness feeds actual September 14 MGC database executions into the updated TradeBuilder and verifies contract specifications, execution IDs, position continuity, and flat completion. It reconstructed six MGC DEC26 trades: -12, +135, -8, -256, -198, +279 dollars. Candidates preserve execution provenance but mark historical continuity unverified and clear MAE/MFE observations. Candidate report: `.codex-backups/journal-dashboard-20260914/mgc-recovery-report.md`.

On a COPY of production, inserting six candidates then repeating inserted zero. The separate additive application test likewise inserted six then zero, verified every pre-existing row across all non-internal tables, and passed SQLite integrity checking. Production data has not been changed. Do not replace production with a test database. `apply_recovery.py` adds reviewed records transactionally, rejects overlapping execution evidence, and checks NinjaTrader closure. Its explicit copy-test mode is restricted to a `recovery-test-*` file beside the reviewed candidate database.

## Series, data, rendering, and performance implications
- No AddDataSeries, secondary Tick/Second/Bid/Ask/Last/Volumetric series, Calculate mode, OnMarketData subscription, Tick Replay behavior, or shared historical cache changes.
- MGC joins the existing supported execution/price path; no new provider or historical rebuild is introduced.
- Equity points are precomputed on filter/size/interval changes. Charts remain frozen Skia bitmaps; WPF line/dot/card hover overlays avoid the prior LiveCharts/NinjaTrader D2D issue. Hover geometry currently scans the selected points for bounds; very large histories may benefit from cached extrema later.
- Historical recovery is an explicit offline operation. It does not fabricate excursions, fees, or old video links. Existing malformed recorder identities still require manual association.

## Verification
- Release Journal build: 0 errors, 4 existing MSB3277 reference warnings.
- Dashboard: 35 checks, including chronological ordering, 5/15/30-minute totals, empty buckets, exact boundaries, hover endpoints/coordinates, and filter-aware view-model updates. Actual WPF dashboard rendered and visually inspected.
- Identity suite: 18 video import, 8 saved-view, 23 tag-performance, 35 excursion/title, 16 risk/media, 22 review, and 85 identity assertions passed. Manual sharing, duplicate attachment, detach/reattach, original-file preservation, and invalid-type rejection tested on disposable files/database.
- Review UI suite passed, including actual WPF thumbnail lifetime and editor persistence.
- Recorder: 21 ledger assertions and full AddOn semantic compile against installed NinjaTrader/WPF passed. Seeded close/restart and reversal identities covered.
- Recovery: actual evidence reproduces -$60; candidate insertion and additive application both idempotent; existing rows preserved.
- Relevant `git diff --check` passed (line-ending notices only).

## Deployment and manual-validation status
Source/build/testing complete; **NOT DEPLOYED**. NinjaTrader was running as PID 76704 during final preparation. Previous installed Journal/recorder hashes still match the September 13 deployment.

Staging directory: `.codex-backups/journal-dashboard-20260914`.
- Journal DLL SHA256: `32EB770375F923CDEE1B0B836816190D3F6D2719DD598E11527E3AEFD66822BB`.
- Recorder source SHA256: `2808895C9509E27AC497C34E0E54D23AA69D79DDA9530517B1A6EE0048CD93DD`.
- `Deploy.ps1` pins source/stage/prior-live hashes, requires NinjaTrader closed, verifies SQLite 2.0.2.0 reference, backs up live targets and Journal data, copies only the two targets, and verifies data unchanged. Run in Windows PowerShell 5.1. Deployment itself does not apply historical recovery.
- Apply reviewed MGC recovery only after deployment backups exist, while NinjaTrader remains closed. Recheck candidate/source evidence before applying. The copied recovery database is input evidence only, never a production replacement.

NinjaTrader F5 compile/load: pending. Tests performed inside NinjaTrader for this change: none. Manual validation: pending. Julian's earlier MAE/MFE/video confirmation applies to the previous deployment only. Full_Suite untouched and not eligible for promotion.

Acceptance: close after Recorder Disarm/Off; deploy; apply reviewed recovery; reopen and F5; inspect gross P&L and six recovered MGC trades; confirm Attach video shares a playable original across MES/MNQ; select every equity mode and hover positive/negative/baseline values; manually Arm Recorder and test an existing-position start, subsequent round trip, reversal, parallel MES/MNQ, and post-roll re-entry. Confirm identity matching and playback before claiming reliable capture.

## Deployment and production recovery completed — September 14, 2026 20:56 local
Julian confirmed NinjaTrader closed and explicitly requested deployment. The guarded installer deployed both staged targets; source/stage/live hashes matched the values above, and SQLite reference 2.0.2.0 was verified. Backups: `.codex-backups/journal-dashboard-20260914/deploy-20260914-205604`, including `backups.json` with original file hashes.

All 26 MGC recovery executions were rechecked against the current NinjaTrader database and matched the reviewed snapshot. The additive production transaction inserted six MGC trades and six unverified identity rows. Independent comparison against the deployment backup confirmed every pre-existing row in every application table remained unchanged and no other rows were added. SQLite integrity check returned `ok`. The selected account now has 22 completed trades before local noon on September 14 totaling $1,975.25 gross, matching the screenshot's TPT gross. No commission values, historical excursions, or video links were fabricated.

This supersedes the staging-only status above. Deployment and historical recovery are complete. NinjaTrader remained closed during verification. Reopen NinjaTrader and compile with F5; successful assembly load and manual runtime validation remain pending. Manually Arm Recorder before the next capture. Full_Suite remains untouched and ineligible for promotion.
