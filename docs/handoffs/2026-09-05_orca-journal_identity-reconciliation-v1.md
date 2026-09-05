# Orca Journal additive identity and read-only reconciliation

Date: 2026-09-05.

## Objective

Continue `2026-09-05_orca-journal_continuation-roadmap.md` from coordination HEAD `c9750b4`: establish external source history, add identity/provenance storage, align Journal reversal grouping with recorder schema 2, and report reconciliation proposals without applying them. Preserve annotations/media and unrelated dirty work.

## Files changed

External source root: `C:\Users\julia\projects\OrcaTrading\OrcaJournal\OrcaJournal`.

- New `Core/ExecutionIdentity.cs`: versioned signed execution-allocation contract.
- `Core/TradeBuilder.cs`: split crossing-flat fills and retain allocation provenance; UID remains unverified for live continuity.
- `Core/TradeCapture.cs`: serialize dedupe/delivery and use unambiguous account-scoped dedupe keys.
- `Data/DatabaseManager.cs`: additive schema 6 sidecars and optional read-only connection.
- `Data/Models/Trade.cs`, `Data/TradeRepository.cs`: provenance/status/UID persistence and readback; existing key/update behavior retained.
- `Data/ExecutionLineAnnotationRepository.cs`, `Data/ExecutionLineNotesImporter.cs`: optional evidence-only IDENTITY_V1 records, preserving legacy formats.
- `Data/TradeRecordingImporter.cs`: schema-2 DTO and review-only handling for new schema-2 associations; schema-1 compatibility retained.
- New `Data/TradeReconciliation.cs`, `UI/Views/ReconciliationView.cs`; `UI/Windows/JournalWindow.cs`: on-demand read-only report tab.
- External `.gitignore` and source baseline `0467d1b`; external copy of this handoff and identity contract documentation.

Coordination repository: `docs/ORCA_JOURNAL_IDENTITY_V1.md`, this handoff, and `tests/OrcaJournal.Identity/` (project, harness, NinjaTrader stubs, runner and generated-artifact ignore rules).

## Behavior and user-facing changes

Reconciliation tab with Run reconciliation, classification counts, candidate IDs/counts and reasons. No automatic merge, deduplication, rekey, note/grade/tag reassignment or new schema-2 media link is performed. Existing links remain, including missing-file links. Existing schema-1 auto-link behavior remains.

Journal now preserves both portions of a crossing-flat reversal instead of discarding it. Existing scaling/partial-close gross P&L matches the recorder fixture. Complete execution provenance can produce an account/full-contract/allocated-quantity UID. Current live callback provenance stays explicitly unverified because subscription/reconnect continuity is not established; no confident live UID is fabricated. Current chart TSV remains legacy, and existing schema-2 manifests lack allocated quantities for exact UIDs. See the contract document for limitations and subsequent producer work.

No NinjaScript properties/defaults or recorder settings changed. No live Journal database/TSV/media files were edited by this work. Migration was tested only on synthetic disposable databases.

## Secondary series, Tick Replay, historical load, cache and rendering

No Tick/Second/Bid/Ask/Last/Volumetric/custom series, AddDataSeries, OnMarketData or Calculate changes. No provider/cache access, historical chart rebuild or Tick Replay dependency. Schema migration adds tables/indexes without backfilling history. Existing trade/annotation keys remain. UI changes are WPF-only; no indicator/chart renderer changes. Report runs on demand in a background task using a separate enforced read-only connection.

## Performance

Adds execution allocation fields and serialization on trade completion, plus sidecar insert/read joins. Existing capture persistence remains synchronous under serialized delivery. Report complexity grows with trades and evidence files; no frequent timer or scan was added. No NinjaTrader performance measurements were performed.

## Tests, compilation and deployment

- Linked-source regression harness: **62 assertions passed**, including actual recorder ledger parity, an independent Python/C# golden vector, schema 5 -> 6/reopen, legacy annotations/tags/grades/media preservation, repeated imports, account/full-contract isolation, reversal allocation, duplicate delivery, missing IDs, unknown continuity, UTC/local/DST, schema 1/2/future compatibility, conflicting candidates, missing media, and enforced read-only queries.
- Test compile: 0 errors, 0 warnings on final run. Initial package audit request was network-blocked; the offline fixture runner now disables NuGet audit only for its local build.
- External Release compile: 0 errors, 4 pre-existing MSB3277 assembly-reference conflict warnings.
- External and scoped coordination whitespace checks passed.
- Built DLL SHA-256: `D2F0FCC20E50E7238D9BF70D37C69B0D4DA4B5D821E4125981B62F2859FC0A5A`.
- Deployed DLL remained `39C34CB93FAEDAF765E1D7A5AD19983BBA644FCF6AC4A0DCDABD6AF288FC1E39` (rechecked). **No deployment** occurred.
- No NinjaTrader F5, process restart, AddOn load, UI, SIM or playback test performed. Julian has not reported runtime validation results in this continuation. Earlier deployment offline checks remain distinct from runtime validation.

## Known risks and follow-up

This is the bounded additive foundation, not completed producer-wide durable identity rollout. Live continuity certification, chart/recorder quantity-allocation envelopes, UID-aware insert dedupe and exact navigation remain future work. Existing composite-key collision handling and historical duplicates are unchanged. No automatic reconciliation acceptance is implemented. Schema-2 newly finalized clips therefore await association work while pre-existing links and schema-1 import remain available.

Collect any runtime defects for the previously deployed build first. Review the new report UI, test scaling/reversals/account reconnects in SIM, and verify annotations/media after a separately authorized deployment. Large data sets may require report indexing/pagination. Do not infer runtime or performance success from offline fixtures.

## Commit and promotion state

The external source now has its own Git baseline and implementation commit; coordination tests/docs are committed separately. Check the latest commits in both roots. No unrelated dirty suite files were staged or modified. Working_Suite and Full_Suite were untouched. Full_Suite promotion is not applicable to the external DLL and remains unapproved for related suite changes; this build is not runtime validated.
