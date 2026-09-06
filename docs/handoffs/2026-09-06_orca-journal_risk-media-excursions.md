# Orca Journal planned risk, media controls and excursion roadmap — 2026-09-06

## Objective and scope
Julian approved manual planned risk with automatic realized R, and the previously proposed captions/order/detach/reveal media controls. He linked https://www.tradezella.com/trading-journal for tag and MAE/MFE inspiration and asked why Journal excursions are blank. Implement the bounded editing/media improvements; investigate and document excursion data rather than fabricate measurements.

## Findings and product direction
TradeZella's linked page describes categorized custom tags, risk/actual-return comparisons, running P&L and excursion analysis. Useful direction for Orca: retain the two requested tag groups now; later add performance filtering/comparison by tags, and optional user-defined categories (setups/mistakes/timeframe) when needed. No competitor layout clone or new unrelated feature suite.
Journal Core/TradeBuilder.cs constructs trades from fills and assigns PnlDollars, times and quantities, but never assigns Mfe/Mae. Data/Models/Trade.cs and repository/export/UI carry nullable fields, not a populated measurement pipeline. ExecutionLineNotesImporter accepts notes/tags/grade/identity only. Therefore blanks are uncaptured values, not evidence of a binding failure. No live database investigation/mutation was necessary to establish this source gap.
Working_Suite/Indicators/OrcaExecutionLines.cs has a separate CalculateMAEMFE path: execution lots, realized plus unrealized P&L and chart high/low samples. Historical bar extrema are not equivalent to a complete tick price path and may include intrabar timing uncertainty. Do not directly copy these into Journal as unqualified exact measurements. RiskManager currently exposes mutable executionRiskDollars; this is not by itself durable proof of risk at entry.

## Changes (external Journal source)
- Data/DatabaseManager.cs: additive schema 9 planned_risk columns on journal_reviews/history; detached and sort_order columns on trade_attachments. Defaults preserve existing visible attachments/order.
- Data/TradeReviewRepository.cs and UI/Windows/TradeReviewWindow.cs: planned-risk numeric input, blank/positive validation, immediate realized-R preview; planned risk saved with review revision/history and included in conflict comparison text. Source P&L is unchanged.
- Data/Models/Trade.cs and Data/TradeRepository.cs: display-list overlay computes stored trade P&L / manual planned risk. 275/200 is retained as 1.375, displayed 1.4R. Unknown manual risk uses existing source PnlR if available, otherwise stays unknown; no zero-risk division. Raw import/capture lookup remains unchanged.
- Analytics/ExportService.cs: append planned_risk to CSV and include it in JSON alongside computed pnl_r.
- Data/AttachmentRepository.cs: captions, persisted custom order, soft detach (row and original file preserved). Exists includes detached rows so recorder autoimport does not recreate a deliberately removed link. Existing hard Delete method is not used by new UI.
- UI/Windows/TradeMediaWindow.cs: Manage media window with caption fields, Move up/down, Show in folder, Remove from trade. Captions save when window closes or an action is used; failed caption close cancels closure. Files are never deleted. No attachment undo/restore UI in this slice; detached rows remain recoverable in storage.
- UI/ViewModels/TradesViewModel.cs and UI/Views/TradesView.xaml: Manage button, planned risk/Realized R detail labels, unavailable excursions read 'Not captured'. Existing gallery displays saved captions on reopening and follows attachment ordering.
- Data/TradeReconciliation.cs: ignore deliberately detached media in active-link diagnostics.
Coordination tests: tests/OrcaJournal.Identity/RiskMediaChecks.cs, Program.cs, tests/OrcaJournal.ReviewUI/Program.cs. Other staged grouped-tag/editor/dark changes remain included.

## Verification
Release net48 x64 build: 0 errors, 4 existing MSB3277 warnings. SQLite reference metadata remains 2.0.2.0. Passing disposable checks: 13 new risk/media assertions (exact/display R, persistence, invalid risks, unknown risk, captions, initial/custom order, preserved file, retained row, reimport tombstone), 22 review assertions, 85 identity/reconciliation assertions. Offline compiled WPF editor test includes risk input/save, existing conflicts, and caption-save-on-close in the media window. Dashboard's 15 checks remain passing. No live data edits and no NinjaTrader tests of this pending batch. The UI harness uses offscreen nonactivating windows and explicit application lifetime to test sequential dialogs.

## Stage, not deployment
C:\Users\julia\Documents\New project\.codex-backups\journal-risk-media-stage\OrcaJournal.dll
SHA256 598091F1D4396E3777970FC5693B0BE353328891B44467117A5F6ED072EE4AAD.
Includes and supersedes grouped-tag stage. NinjaTrader observed running PID 64884. Expected live baseline remains D02966A1B116E207F04C9969E41A676F082D67B476581DFF3A363C84F1830C32. After authorized closure: verify hashes/metadata/process absence, back up DLL/database/sidecars/TSV, copy only exact staged DLL and verify parity. Schema 9 applies on next startup. Rollback DLLs will not honor planned-risk/media-detachment overlays; consult these data semantics before rollback.

## Implications and manual checks
No indicator/Full_Suite changes; no additional data series, AddDataSeries, OnMarketData, Calculate changes, Tick Replay, historical-load or trading-cache changes. Ordinary on-demand UI/database work only, no per-tick or OnRender analytics. Existing annotations/IDs/provenance/media preserved. New manual risk and media metadata are per-trade values, not platform settings. No performance claims or Full_Suite eligibility.
Manual validation pending: enter 200 against 275 and see 1.4R before/after saving/reopening; test blank, invalid and loss cases; inspect grouped tags and simple Save flow; caption/order two images and reopen gallery; detach one and verify original file plus no auto-reappearance; Show in folder; exports; MAE/MFE 'Not captured'. Native focus/close behavior needs NinjaTrader confirmation.

## MAE/MFE and automatic-risk next slice
1. Define excursions as maximum/minimum realized-plus-open-position P&L over the exact flat-to-flat trade, account/full-contract scoped. Record currency/sign, commissions basis, observation start/end, and coverage/quality.
2. Reuse compatible existing ExecutionLines observation data rather than adding redundant series; preserve tick-observed vs bar-estimated vs unavailable provenance. Validate scaling, partial exits, reversals, startup-mid-trade, disconnects and routed instruments.
3. Transport an additive versioned measurement payload with shared trade UID; accept only unambiguous compatible matches. Never infer excursions from entry/exit prices alone or fill old trades with zero.
4. Add a quality-aware Journal display and importer tests before scatterplots/tag-based excursion reports. Historical reconstruction remains explicitly estimated unless complete underlying evidence exists.
5. Automatic planned risk needs an immutable order-time snapshot tied to executions/UID, with clear treatment of resized orders/stops, partial fills and reversals. Manual risk stays available and authoritative until that contract is validated.
