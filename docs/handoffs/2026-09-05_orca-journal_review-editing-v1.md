# Orca Journal review editing v1 — 2026-09-05

## Objective and scope
Julian authorized proceeding through the roadmap. This is the next validation checkpoint: Journal-side review editing with explicit save feedback and conflict handling, bundled with the previously staged dark screenshot UI. Screenshot opening/zoom were confirmed by Julian on the preceding deployed runtime-fix build. Shared identity/SIM tests remain unconfirmed. Do not treat this checkpoint as completion of the whole roadmap.

## Files changed
Authoritative external repository C:\Users\julia\projects\OrcaTrading\OrcaJournal:
- OrcaJournal/Data/DatabaseManager.cs: additive schema 7 journal_reviews and journal_review_history tables.
- OrcaJournal/Data/TradeReviewRepository.cs: atomic versioned saves, imported baseline comparison, stale-editor rejection, explicit conflict acknowledgement, normalized tags and grade validation.
- OrcaJournal/Data/TradeRepository.cs: saved review notes/grade overlaid on display/export lists; raw GetById/GetByTradeKey remain capture/import data.
- OrcaJournal/Data/TagRepository.cs: saved review tag snapshot used for display; original source-specific links retained.
- OrcaJournal/Data/ExecutionLineAnnotationRepository.cs: explicit empty TRADE rows clear imported fields/source tags, making clear intent visible to review conflict detection. Missing rows are not interpreted as clears.
- OrcaJournal/UI/Windows/TradeReviewWindow.cs: modal, trade-bound editor, Save review, Check latest versions, Load saved review, Use imported version, unsaved-close warning and latest-imported comparison.
- OrcaJournal/UI/ViewModels/TradesViewModel.cs, UI/Windows/JournalWindow.cs, UI/Views/TradesView.xaml: Edit review action and refresh/import wiring.
- OrcaJournal/UI/Windows/TradeImageViewer.cs: share its native dark-caption request with the new editor.
Coordination tests: tests/OrcaJournal.Identity/ReviewChecks.cs, Program.cs; tests/OrcaJournal.ReviewUI/Program.cs and Verify.ps1. This handoff is copied to the external repository.

## Behavior and ownership
Select a trade and click Edit review. Notes, grade and one-tag-per-line text are edited in a dedicated window. Save is explicit; there is no autosave. Unsaved drafts stay in the editor during comparisons, and closing/replacing a dirty draft asks before discarding it. Grades use the existing A+ through F vocabulary, with blank for no grade.
Journal reviews are keyed by existing trade row ID, with a revision and imported baseline. The source chart annotation tables/TSV remain separate; Journal edits do not write back to charts. Imports cannot overwrite saved Journal reviews. Opening, checking, loading versions and saving refresh the chart TSV before comparing database values. A changed source or stale Journal revision blocks a save; Check latest versions preserves the draft and presents the current version. Keeping a draft over a detected conflict requires an explicit choice. Old saved Journal revisions and baseline snapshots remain in the history table for recovery; there is no history browser yet.
Tags are case-insensitively deduplicated, trimmed and registered in the existing library. Saved review tag names are versioned snapshots: subsequent library renames/deletions do not rewrite those snapshots. Imported/manual/automatic source tag links remain stored. Once a review is saved, its explicit tag set is the displayed set; later changes to the original source set are comparison evidence, not automatic additions to the review.

## Validation
- Release net48 x64 build passed: 0 errors, 4 existing MSB3277 reference-conflict warnings. SQLite metadata remains 2.0.2.0.
- 18 new disposable-database assertions pass: initial seed, normalization, raw source preservation, effective notes/tags, stale-editor rejection, imported conflict, explicit acknowledgement/rebase, idempotent reimport, empty review intent, history, attachment retention, invalid grades, explicit chart clear, reopen persistence.
- Existing 85 identity/reconciliation assertions pass with additive schema 5-to-7 migration preservation.
- Offline STA UI harness loaded real compiled theme/editor, edited all three fields, saved and refreshed, verified dirty/saved feedback and rendered a preview. Preview inspected. Final artifact directory: C:\Users\julia\AppData\Local\Temp\orca-review-ui-a4aeaaa9dca74c84b999a0e31125b53e.
- Existing screenshot regression harness: 24 checks pass after shared caption helper change.
- No NinjaTrader tests of this new batch. All database tests used disposable fixtures; no live database migration or edit was performed.

## Staged, not deployed
NinjaTrader remains open. Combined review/dark-viewer DLL:
C:\Users\julia\Documents\New project\.codex-backups\journal-review-stage\OrcaJournal.dll
SHA256: 00DD2E7EF1C4F387E54D6425108E8500FB72941A7BB30C77D5C3412A8A9D3114.
This supersedes the dark-viewer-only staging artifact. Current deployed baseline remains 6379428AE1E96D52A982A8D53259D1279C58283B134CDE67C34BE1F0C4E0E8D2. After Julian closes the platform, recheck absence and all hashes, back up DLL/database/sidecars/annotation TSV, copy only DLL and verify parity. Schema 7 initializes on next startup. Older DLLs do not display saved review overrides, so avoid rollback without consulting this ownership distinction; additive tables/data remain intact.

## Platform implications
No indicator, secondary-series, AddDataSeries, OnMarketData, Calculate mode, Tick Replay, historical-load or trading-calculation changes. No Full_Suite files changed. Ordinary WPF editor rendering and on-demand database operations only; no per-tick work added. Import refresh/save remain synchronous UI actions, and very large annotation files may delay the editor; no performance benchmark claimed. Existing media, IDs, provenance and unrelated dirty work preserved. No new persistent user settings. Full_Suite promotion not eligible.

## Manual validation checkpoint
1. Restart/F5, open Journal and confirm existing data remains present. Check screenshot buttons/title bar in dark mode.
2. Edit a trade's note, grade and tags; Save; confirm the detail panel and reopening Journal show them. Test clearing fields deliberately and canceling an unsaved close.
3. Change chart-side annotations, reopen the review/check latest versions, verify both versions and the blocked stale save. Explicitly keep the Journal version or use the imported version, then save. Confirm chart annotations remain unchanged by Journal edits.
4. Confirm existing images/videos still open. Complete outstanding shared identity/SIM validation separately. Reported defects take priority over the next roadmap slice.

## Continuing roadmap and captions note
After this checkpoint: screenshot captions/ordering and safe attachment removal; recorder metadata plus Play/Reveal/Relink/Unlink with precise identity-aware linking; chart-to-Journal exact selection, then Show on Chart; saved review filters and daily/weekly review; integrity/media maintenance and portable export. Screenshot captions remain a recorded follow-up, not an implemented feature. Chart write-back synchronization, autosave, advanced export and automatic capture are not included in this build. Implement these as separate reviewed slices; do not silently merge ambiguous histories or links.
