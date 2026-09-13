# Orca Journal and Recorder — capture reliability and review queue

## Objective and authorization
Julian authorized MAE/MFE and video fixes, changes to the Trade Recorder where needed, Markdown documentation, and starting selected BoostYourCharts-inspired features. Preserve trading history, annotations, existing attachments, raw recordings, and unrelated dirty repository work.

## Evidence
- September 10–11 read-only production inspection: 77 trades without MAE/MFE; 63 report regressing price timestamps, 14 report execution-position mismatches. The installed DLL matches the September 9 build. Thus the previous deployment did not establish working live capture.
- Active video links before this change: September 9 10/32 trades; September 10 7/63; September 11 3/14. A capture bundle may contain several trades; bundle counts are not trade counts.
- September 10 has 31 complete and two pending manifests. The two pending manifests have neither a valid stop time nor listed raw segments. They cannot be finalized safely from their manifests.
- Some schema-3 recordings and Journal rows contain identical observed execution allocations without complete-history UIDs. Direction labels differ (`SHORT` versus `Short`). Other ledger records genuinely lack corresponding Journal fill evidence. Schema-2 records lack quantity allocations and remain review-only.
- NinjaTrader documents Execution.Position as account quantity at execution: https://ninjatrader.com/support/helpguides/nt8/execution.htm . This does not prove the cause of the 14 observed mismatches. New code does not silently treat mismatches as verified continuity.

## Files changed
External source: `C:\Users\julia\projects\OrcaTrading\OrcaJournal\OrcaJournal`:
- `Core/RoundTripExcursion.cs`, `Core/TradeBuilder.cs`, `Core/TradeCapture.cs`
- `Data/TradeReconciliation.cs`, `Data/TradeRecordingImporter.cs`, `Data/TradeReviewRepository.cs`, `Data/Models/Trade.cs`
- `OrcaJournal.cs`, `UI/ViewModels/TradesViewModel.cs`, `UI/Views/TradesView.xaml`, `UI/Views/TagLibraryView.cs`

Coordination repository:
- `Orca Trades/Working_Suite/AddOns/OrcaTradeRecorderAddOn.cs`
- `docs/ORCA_TRADE_RECORDER.md`, this handoff
- `tests/OrcaJournal.Identity/ExcursionChecks.cs`, `tests/OrcaJournal.Identity/VideoImportChecks.cs`, `tests/OrcaJournal.ReviewUI/Program.cs`, `tests/OrcaTradeRecorder/Finalizer.ps1`

## MAE/MFE behavior
- Capture copies execution fields and Last price/time before waiting on the callback lock, avoiding later reads of mutable platform event data.
- A price older than either the latest accepted price or latest fill is skipped. It cannot mark newer inventory. The timestamp high-water mark stays intact. Previously accepted extrema survive; skipped count and maximum regression are reported as partial observations.
- Execution timestamp regressions, invalid values, absent market observations, disconnects, and mismatch before a verified flat boundary still prevent an asserted value.
- After an observed matching flat close, a later execution-position disagreement retains only an explicitly partial estimate from the fills received. Quality reports disagreement count and callback-order basis. Shared identity remains conservative and unchanged. A connection interruption clears that established boundary.
- Fill cash-flow/net-inventory calculation, scaled entries, partial exits, reversal allocation, and gross P&L are preserved. No historical MAE/MFE backfill is attempted. Some first-cycle or genuinely incomplete trades will still have no value; this is not a promise of complete capture under every failure.

## Recorder and video behavior
- Cleanly stopped, preserved clips finalize in a background worker while the recorder remains armed. Disarm still waits for finalization. Failed work retries after 60 seconds through the existing timer; unfinished captures are excluded. Startup can retry eligible pending stopped bundles.
- Finalization has a per-runtime semaphore plus an exclusive bundle file lease, including across overlapping AddOn lifetimes. The manifest is rechecked under the lease so an already-completed clip is not republished. These changes do not automatically re-arm OBS after restart/recompile.
- Every listed raw segment must exist. FFprobe must find video, audio, and a positive finite duration. Raw recordings are retained. Atomic manifest replacement prevents readers seeing partially copied JSON.
- FFmpeg stdout/stderr drain concurrently while the timeout runs; synchronous pipe reads previously could prevent reaching the timeout check. Helper processes remain hidden.
- Exact complete UID links retain their existing rules. A distinct `Observed fills` association can link one uniquely matching account/full-contract/direction/quantity and identical execution-ID/allocated-quantity set. Both observed sequences must independently form one structurally valid cycle. Conflicting UIDs, malformed or duplicate allocations, incomplete capture ledgers, and multiple candidates remain blocked. This does not assign or upgrade any UID/history provenance. Association captions explicitly say history is unverified. Detached links remain detached. Schema-2 matching rules are unchanged.
- A recording-folder watcher marks work pending; the existing dispatcher timer imports changes at most once every 15 seconds. Trade completion also requests a scan. File callbacks do not access SQLite or UI. Startup, menu open, and explicit Refresh videos still import. No per-tick scan is added.
- Trade rows and detail show video link/pending/failure/review status from an import snapshot. Unknown state says no linked video and directs the user to Recorder/Reconciliation; it does not claim footage never existed or that recording is currently armed.

## First reference-inspired feature slice
Reference: https://journal.boostyourcharts.com/#features (reviewed September 12; website claims are not independent runtime validation).
- Saved-review progress in the Trades filter area.
- `Without saved review` filter and `Next to review` navigation within current date/account/instrument/direction/outcome filters.
- Per-trade `Review` and `Video` columns.
- A saved Journal review revision defines saved review status. Imported tags/notes alone do not falsely mark a review saved.

This starts the review workflow without introducing unrelated analytical formulas. Daily plans/takeaways, mistake-tag analysis enhancements, automatic entry/exit screenshots, and broader filtered comparisons remain follow-up slices. What-if simulations remain dependent on adequate execution/price coverage.

## Settings, series, historical load, caches, rendering, performance
- No recorder settings or defaults changed; automatic stopped-clip finalization replaces the Disarm-only behavior. New Journal controls are view filters/actions, not new persisted settings.
- No AddDataSeries, Tick/Bid/Ask/Second/Volumetric series, Tick Replay, OnBarUpdate, Calculate mode, or shared market-cache changes. Recorder still adds no price subscription. Journal retains one Last subscription per active full contract across accounts.
- No historical rebuild or indicator OnRender changes. Last-event work remains scalar checks; execution snapshot allocates once per fill. WPF displays cached status and filtered review progress.
- FFmpeg stream copy now overlaps normal recording and can consume disk/CPU; live recording/performance needs validation. Import remains a full folder scan when requested, rate-limited for watcher events, not a periodic unconditional scan. Very large libraries may need an indexed incremental importer later.
- No schema migration, historical trade rewrite, cleanup, deletion, or live database write was performed by this development turn. Existing database concurrency/lifecycle behavior is not comprehensively redesigned here.

## Validation performed
- Journal Release build: 0 errors, 4 existing MSB3277 reference warnings.
- Identity suite: 18 video import, 35 excursion/title, 8 saved-view, 23 tag-performance, 13 risk/media, 22 review, and 85 identity assertions passed.
- WPF suite passed, including saved-review persistence/progress, next-review navigation, account filtering, and existing review/media/tag workflows. Trades screen rendered and inspected; checkbox contrast corrected.
- Recorder: 18 ledger assertions and full AddOn semantic compile against installed NinjaTrader/WPF passed.
- Real synthetic FFmpeg/ffprobe test passed: missing-segment rejection, missing-stop rejection, final MP4/manifest publication, and raw preservation.
- Actual importer on a SQLite backup of production added 14 media rows: September 8 +6, September 9 +4, September 10 +2, September 11 +2. Repeat import added zero. Every non-media table and every existing attachment row matched production. This does not claim production has those new links yet.
- The copied database and rendered image remain in `.codex-backups/journal-reliability-20260912`; do not deploy the test database.

## Staging and manual gates
- Source backup: `.codex-backups/journal-reliability-20260912/source` plus original recorder source.
- Staged Journal DLL: `.codex-backups/journal-reliability-20260912/OrcaJournal.dll`.
- Current staging hash and both source/live recorder baselines are recorded in the adjacent deployment script, which requires NinjaTrader closed and backs up both targets and Journal data before copying only the two targets.
- NinjaTrader was running (PID 66080). No new DLL or recorder source has been deployed this turn. Full_Suite remains untouched.
- NinjaTrader F5/load: pending. Manual validation: pending. Not eligible for Full_Suite promotion.
- Acceptance after deployment: Journal opens; existing reviews/media preserved; expected additional links import and play; Recorder is manually armed; a short SIM trade produces a finalized playable clip without Disarm; Journal attaches it after finalization; MAE/MFE shows observed values or an explicit quality reason. Also verify scale/partial/reversal, overlapping accounts, stop/re-arm, reload, and failure/retry behavior before claiming reliable live capture.

Final staged SHA256: Journal DLL `C15FD2BA9E7E912A5F8D11EAA1FC77E0274161169942C3B1351983853E985F34`; recorder source `AE1905B78DCDE15DBF6213F3D6B7B02F126AD9E74731FFD607DFDFA512CC5FE0`. Guarded installer: `.codex-backups/journal-reliability-20260912/Deploy.ps1` (Windows PowerShell 5.1). Production database and recorder files remain unchanged during staging.
