# Orca Journal — round-trip excursions and readable media titles — 2026-09-06

## Objective
Julian requested readable image titles, the exact groups Risk & Trade Management and Analysis & Execution, and MAE/MFE for full round trips. He reported Manage opens and exposes caption/order actions, plus a Windows image-file-in-use error. This confirms window visibility, not all prior save/order/risk behavior.

## Files changed
External authoritative repository: C:\Users\julia\projects\OrcaTrading\OrcaJournal.
- Core/RoundTripExcursion.cs (new), TradeBuilder.cs, TradeCapture.cs.
- Data/DatabaseManager.cs, TradeRepository.cs, Models/Trade.cs, Models/TradeAttachment.cs, ReviewTagOption.cs, TradeReviewRepository.cs.
- UI/Controls/AttachmentThumbnailConverter.cs (new), Views/TradesView.xaml, Windows/TradeMediaWindow.cs, TradeImageViewer.cs.
- Analytics/ExportService.cs.
Coordination tests: tests/OrcaJournal.Identity/{ExcursionChecks.cs,Program.cs,Stubs.cs}, tests/OrcaJournal.ReviewUI/Program.cs.
No Working_Suite, Full_Suite or unrelated dirty work edited.

## Behavior and user-facing changes
- Existing caption becomes the visible image title in Manage, thumbnails and gallery. Empty captions retain filename fallback. Filename available as tooltip. No schema change or disk rename for titles; existing captions preserved. Renaming linked files in Explorer can break paths; use the Journal title/caption field.
- Thumbnail binding previously used implicit WPF file decoding. It now decodes a bounded 256px bitmap OnLoad and closes the stream, using shared read/write/delete access while decoding. Frozen bitmap avoids keeping a file handle. Gallery already used OnLoad.
- Exact tag category labels requested; existing saved legacy category strings normalized on read without changing assignments or rewriting the tag database.
- Schema 10 adds nullable excursion quality/start/end and sample count default0 to trades. Existing MFE/MAE columns hold new values. Historical rows unchanged; no backfill or zero fabrication.
- Excursions are extrema of gross realized-plus-unrealized P&L for account/full-contract flat-to-flat cycles: (signed fill cash flow + net quantity * marked price) * point value. Scale-ins and partials stay one cycle. Reversal splits the fill and starts fresh extrema at the virtual flat boundary. MFE >=0; MAE <=0; baseline zero. Same configured dollar point value as Journal P&L, before commissions.
- Live Last callbacks and execution prices update the observation; Bid/Ask ignored. Tick observations counted separately from fills; first/last observed timestamps retained. Price/time invalidity or regression invalidates the cycle.
- Only shared identity with verified continuous execution history and at least one Last observation can publish numbers. Initial startup cycle is unavailable until an observed flat boundary; connection changes/detach invalidate continuity. Quality explicitly says Live observed (sampled; before commissions). This is observed extrema, not a guarantee of every market tick or complete coverage. First subscription is scheduled after entry; missed prices cannot be reconstructed. No cross-source matching/import or recorder schema changes are needed: values are assembled with that same Journal cycle and persisted with existing shared UID provenance.
- UI shows coverage text; CSV/JSON carry quality, sample count and timestamps.

## Architecture, series, historical, cache, rendering and performance
Source review found ExecutionLines chart-local high/low reconstruction and live P&L but no exported coverage-qualified measurement payload. Copying its history as exact would be misleading, and that source has unrelated dirty changes. Existing profile provider is chart/history oriented. Journal reuses NinjaTrader Instrument.MarketData instead, one subscription per active full contract shared across accounts, registered/unregistered on the instrument dispatcher. Official API reference: https://docs.ninjatrader.com/ninjascript/marketdata.
No AddDataSeries, BarsRequest, secondary Tick/Second/Bid/Ask/Last/Volumetric series, Calculate mode changes, or Tick Replay dependencies. Historical load unchanged. Playback is not validated and never assumed complete; connection/time discontinuities fail closed. No new persistent price cache or retained tick tape. Price updates inspect only active buckets for the matching full contract, with constant scalar work per active account; no database, render, historical rebuild or per-tick allocation introduced. Feed removed at flat, account detach and shutdown. Existing close-trade database write remains on the execution path. Subscription failures invalidate continuity and emit Trace error. No measured live performance claim.

## Verification and status
- Release net48/x64 build passed: 0 errors, 4 existing MSB3277 warnings.
- 24 new excursion/title checks: scaled round-trip extrema, realized partials, short reversal, initial-flat/gap/no-price/out-of-order/wrong-contract suppression, persistence/timestamps, Last-only capture, shared subscription and release at flat/detach/shutdown, labels/title fallback.
- Existing 13 risk/media, 22 review and 85 identity/reconciliation checks pass; disposable schema5-to10 migration tested twice.
- Actual compiled WPF UI harness passes. Thumbnail remains alive while source permits exclusive read/write open and rename round-trip; preview frozen and256px wide. Existing editor save/conflict/risk/media caption checks pass.
- Source diff whitespace checks passed. No live database/media edits, no NinjaTrader runtime checks of this new build. All test databases/files disposable.
- Not deployed: NinjaTrader running PID16896. Stage .codex-backups/journal-excursions-stage/OrcaJournal.dll, SHA256 A5FC0BCB67A3A491E4820507E2E903E1860D47D61A51CD8F02D8A22468B0ED8C.
- Expected live baseline remains 598091F1D4396E3777970FC5693B0BE353328891B44467117A5F6ED072EE4AAD.

## Next manual validation and risks
After Julian closes NinjaTrader: guarded backup DLL/database/sidecars/TSV, source/stage/hash and SQLite metadata checks, copy only DLL, verify live parity. No Full_Suite promotion eligibility.
On restart check title persistence/thumbnail/gallery and category assignments. In SIM establish a flat boundary first, then a complete long and short round trip with scale-in/partial exits; compare observed gross excursion values, quantity and final P&L. Check simultaneous accounts/contracts, routed micro contract, reconnect/mid-trade launch, flat/subscription teardown, and startup/runtime responsiveness. Runtime event timing, dispatcher lifecycle and provider clock compatibility remain unverified by offline stubs. Sampled live values are not historical/exact excursion reconstruction. Historical quality-aware reconstruction and recorder/chart measurement transport remain future slices. Existing legacy duplicate trade-key insertion does not overwrite earlier rows to attach newer observations.

## Deployment completed — 2026-09-06 12:44 local
Julian confirmed NinjaTrader closure. Process absence verified before backups, immediately before copy, and afterward. Source/stage matched expected hash; prior live baseline matched. SQLite assembly reference2.0.2.0 verified using Windows PowerShell/.NET Framework reflection (initial PowerShell Core metadata attempt stopped before mutation).
Only OrcaJournal.dll copied. Live SHA256 A5FC0BCB67A3A491E4820507E2E903E1860D47D61A51CD8F02D8A22468B0ED8C.
Backup: C:\Users\julia\Documents\New project\.codex-backups\journal-excursions-deploy-20260906-124441. Prior DLL and available database/sidecars/annotation TSV copied and hash-verified. Live data hashes unchanged; original media untouched. Deployment evidence in deployment.json. Schema10 initializes at next startup.
Deployment complete; NinjaTrader restart/load and SIM excursion/manual UI validation remain pending. No Full_Suite promotion.
