# Orca Journal — capture timing and recording recovery — 2026-09-09

## Objective
User requested spaced compact durations and investigation/fix of empty videos and Not captured: Out-of-order price/execution timestamps before another restart. Preserve all history, annotations, media and unrelated dirty Working_Suite work.

## Findings: facts vs limits
- Live DB read-only inspection: all 32 September 9 trades have the old combined out-of-order error (9 SIM, 5 other funded account, 18 account ending 2126). Old error does not distinguish price-stream regression, execution-stream regression or cross-stream interleaving; exact per-event timing is not persisted.
- RoundTripExcursion compared both callback streams to a single lastTime. TradeCapture serializes callbacks but market and account execution source timestamps need not increase together. This can discard a whole round trip on cross-stream interleaving. NinjaTrader documents event-sequence caveats: https://ninjatrader.com/support/helpguides/nt8/onexecutionupdate.htm and https://ninjatrader.com/support/helpguides/nt8/onmarketdata.htm . No claim that every observed trade failed for precisely the same interleaving.
- Recorder September 9 directory: seven cleanly stopped schema-3 captures, all Pending, 14 ledger trades. One unfinished schema-1 capture at 12:29 with no StoppedUtc/ledger. Raw MKVs exist.
- Recorder log: runtime disposed at 12:30:17 during final clip tail, recreated requiring manual arm; another recreation 16:09. No later capture/armed events found. Account ending 2126's 18 Journal trades run 12:29–16:01. No later footage exists in the configured recorder output directory. Not recoverable by changing Journal links.
- Journal importer explicitly skipped all schema>=2 manifests. Thus even correctly finalized schema-3 videos would not have linked. Ten of fourteen stopped-ledger trades have unique matching Journal identities; four require identity review (no guessed time fallback).

## Code changed (external OrcaJournal)
Core/RoundTripExcursion.cs: separate last execution and last market-price timestamps. Source-stream regressions still fail closed with stream/time-specific reason; disconnect, missing price coverage and invalid inputs remain unavailable. Cross-stream overlaps retain explicitly Partial observed callback-order estimates, with overlap count/max milliseconds in persisted quality. This is not an exact exchange-time reconstruction; already invalid historical extrema remain unavailable. Gross round-trip cash/net calculation and scaling/reversal grouping unchanged; no backfill.
Core/TradeBuilder.cs: consumes quality description from accumulator.
Data/TradeRecordingImporter.cs: finalized schema-3 shared identity can link only a unique exact candidate, validating both ledger and Journal provenance. Reconciliation stays read-only; schema-2 stays review-only; schema-1 compatibility retained. Respect detached tombstones, reject ambiguous/tampered/incomplete/invalid captures. Report pending/missing/unreadable/identity-review counts and unavailable root.
OrcaJournal.cs, UI/Windows/JournalWindow.cs, UI/Views/TagLibraryView.cs: explicit Refresh videos invokes importer and refreshes all Journal views; visible status and finalization hint. No new recursive polling.
Data/Models/Trade.cs: durations 1m 47s / 1h 11m 52s. Includes preceding staged open/close columns from deabe60.
UI/Views/TradesView.xaml: excursion status moved below MFE/MAE and prefixed MAE/MFE to avoid implying video/P&L capture failed.

## Performance/platform implications
No secondary series, AddDataSeries, Calculate or Tick Replay changes. Existing one Last feed per active full contract remains. O(1) scalar timing checks per fill/price callback, no rebuilds/allocations in normal price paths. Existing synchronization unchanged. No OnRender work added. Import scans only startup/open/explicit refresh; no background periodic full-tree scans. No new schema, cache or historical-load changes. No Working_Suite or Full_Suite source edits; automatic rearming after compilation intentionally not added.

## Offline validation
Release build 0 errors / 4 existing MSB3277 reference warnings.
Identity suite: 11 video import checks; 8 saved-view; 23 tag performance; 31 excursion/title (including cross-stream overlaps vs actual within-stream regressions); 13 risk/media; 22 review; 85 identity assertions. WPF suite passed including Refresh videos callback/status and prior gallery/review/saved views. No NinjaTrader runtime validation of this new build.

## Prepared video recovery — NOT published
Staging root: .codex-backups/journal-recording-recovery-20260909.
Seven MP4s produced using installed FFmpeg stream-copy concatenation of every listed raw segment. ffprobe confirmed nonzero duration plus audio and video on each. Original raw recordings and manifests remain unchanged. Original capture manifest bytes and hashes backed up inside staging. One unfinished 12:29 capture deliberately skipped.
recovery.json records original-manifest hashes, staged/destination video paths/hashes, publish manifests and durations. capture.json copies point to staged videos for dry-run; publish-capture.json points to intended final video in each original bundle.
On SQLite backup of live DB: actual new importer linked exactly 10 videos, second import zero; 4 ledger records need review. Compared all trades/reviews/history/annotations/tag rows with read-only production: unchanged (2058 trades, 1 review, 2 review revisions, 21 annotations, 7 annotation-tag rows, 4 trade-tag rows, 11 tags). Do not deploy the test database (its links point into staging).
Guarded publication script: .codex-backups/publish-journal-recovered-recordings.ps1. Requires NinjaTrader and FFmpeg stopped; validates original/staged hashes, backs up production manifests, copies seven validated videos, then publishes manifests. Does not delete/move raw recordings or write live DB. Hash mismatch must stop for review. Already-complete identical recovery is idempotent. Journal importer creates links on next startup/Refresh videos.

## Staged DLL / next deployment
Stage .codex-backups/journal-capture-recovery-stage/OrcaJournal.dll
SHA256 EB0B739C1FC99156BE6F301030D2612C58FFDDFCF49F5D2E6FD864F38B0AC275
Current live baseline verified: 5BC7CA068EF3FE908E92E52596D30B9C3979D8D15BE6A7BCA0A5442A63C8ECE2.
NinjaTrader PID6412 still running. Neither DLL nor recovered videos/manifests published this turn. On user closing: guarded DLL backup/copy plus guarded recovery publication, then reopen to import unique links. Do not copy old standalone trade-times stage; this combined stage supersedes it.
Manual gates: startup; video refresh/link and actual playback; next new round trip MAE/MFE partial-quality inspection with scale/partial behavior. Existing afternoon footage cannot be invented; re-arm Recorder after NinjaScript runtime recreation if recording desired. Runtime MAE/MFE fix remains unproven until new trade. Not eligible for Full_Suite promotion.
