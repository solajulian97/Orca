# Orca Journal shared producer identity

Date: 2026-09-05. Source implementation and offline verification complete; staged, not deployed.

## Objective

Connect Journal, Execution Lines and Recorder to the same versioned execution-allocation identity. Preserve annotations/media, read-only reconciliation, existing calculations and unrelated dirty work. Explain deployment status: the previous foundation was intentionally left at source/offline verification while prior runtime results were pending; this was not based on a process check. NinjaTrader is now confirmed open (PID 45252 at final staging check).

## Files changed

Coordination source:

- `Orca Trades/Working_Suite/AddOns/OrcaTradeIdentity.cs` (new): canonical internal hash/validation/JSON contract and per-account/full-contract continuity observer.
- `Orca Trades/Working_Suite/AddOns/OrcaTradeRecorderAddOn.cs`: shared tracking and schema-3 IdentityJson/TradeUid, connection invalidation.
- Narrow additions to `Orca Trades/Working_Suite/Indicators/OrcaExecutionLines.cs`: identity observer/provenance, connection/SOD invalidation, optional execution-position evidence, additive IDENTITY_V1 export during annotation saves.
- `tests/OrcaJournal.Identity/`: linked helper, extended stubs, cross-producer/continuity/schema-3/conflict checks.
- `tests/OrcaJournal.PlatformCheck/` (new): reproducible chart model/settings/render preservation and NinjaTrader-reference compilation.
- `tests/OrcaTradeRecorder/Verify.ps1`: include the new helper in its existing fixture and reference compile.
- `docs/ORCA_JOURNAL_IDENTITY_V1.md`, `docs/ORCA_TRADE_RECORDER.md`, this handoff, and `docs/patches/2026-09-05_orca-execution-lines_shared-identity.patch`.

External source `C:\Users\julia\projects\OrcaTrading\OrcaJournal\OrcaJournal`:

- `Core/ExecutionIdentity.cs`: delegates contract validation/hash to the canonical linked helper.
- `Core/TradeBuilder.cs`, `Core/TradeCapture.cs`: observed-boundary provenance and connection/account/SOD invalidation.
- `Data/ExecutionLineAnnotationRepository.cs`: weaker evidence cannot replace a trusted UID; conflicting complete UIDs remain ambiguous across repeated imports.
- `Data/TradeReconciliation.cs`, `Data/TradeRecordingImporter.cs`: verified schema-3 exact proposals, no uncertain schema-3 fallback.
- `OrcaJournal.csproj`: source link via configurable OrcaSuiteRoot.
- External copies of identity documentation and this handoff.

## Behavior/settings

All three producers use one canonical UID algorithm. The first observed cycle is unverified; a fill ending flat with consistent position arithmetic establishes the next boundary. Each subsequent fill must have an ID, valid/nondecreasing time and a consistent reported account-position magnitude. Reversals split allocations and inherit trust only from a trusted preceding cycle. Connection changes/missing evidence invalidate identity. Historical fills without position evidence stay unverified. No missing history is inferred. Existing recorder global uncertainty may continue requiring re-arm even after identity arithmetic recovers.

Execution Lines publishes identity only alongside explicitly saved annotations, preserving the legacy records and other identity records. Recorder adds identity fields in schema 3. Journal recomputes and verifies identities before reporting exact proposals. No automatic link, merge, rekey, legacy identity backfill or annotation reassignment was added. Schema 1 remains compatible; schema 2 remains review-only legacy evidence. UID-aware insertion/deduplication/navigation and association acceptance remain later work.

No user settings/defaults were added or removed. No live database, TSV or media was changed during testing. Existing gross-P&L and filename semantics remain.

## Secondary series, Tick Replay, historical load, cache, rendering, performance

No AddDataSeries, Tick/Second/Bid/Ask/Last/Volumetric/custom series, Calculate or market-data callback changes. No shared market-data cache access or historical rebuild. Identity observer work is account-execution driven and O(1) per allocation; validation/hash/serialization is at cycle completion. Allocations are capped at 100,000 per cycle with unverified identity on overflow, without capping trading calculations. Connection notifications invalidate only identity state. No new chart renderer work or callback disk writes. Existing synchronous trade persistence and TSV save concurrency behavior remain. No measured live performance claim.

## Tests and compile status

- `tests/OrcaJournal.Identity/Verify.ps1`: 85 assertions, 0 build errors/warnings. Cross-producer parity uses actual Journal and Recorder source plus the canonical chart observer. Includes trusted reversal UIDs, startup/restart uncertainty, connection interruption/recovery, position mismatch, missing IDs, out-of-order time, JSON escaping, schema-3 verified proposals and persistent annotation identity conflicts, in addition to the prior migration/preservation suite.
- `tests/OrcaTradeRecorder/Verify.ps1`: existing 18 ledger assertions and full AddOn/helper NinjaTrader-reference compile pass.
- `tests/OrcaJournal.PlatformCheck/Verify.ps1`: chart model preserved after removing exactly two identity statements; unchanged settings/render methods and unrelated methods; full semantic check 0 errors.
- Journal Release build: 0 errors, 4 pre-existing MSB3277 reference warnings.
- .NET Framework metadata check: System.Data.SQLite 2.0.2.0, public key token db937bc2d44ff139.
- Source whitespace checks pass. Pre-edit Working_Suite/live authored parity passed for both chart and recorder.
- NinjaTrader F5/load, SIM/manual validation and performance testing: **not performed; pending Julian**. No new runtime defect was reported in this turn.

## Deployment package and remaining gate

Staged package: `C:\Users\julia\Documents\New project\.codex-backups\journal-shared-identity\stage`.

- `OrcaJournal.dll`: SHA256 `F0B0D73DE50BF758604710D01B3E4F40069B7DA953625654A448414F289C104E`.
- `OrcaTradeIdentity.cs` -> live Custom/AddOns (new required dependency).
- `OrcaTradeRecorderAddOn.cs` -> live Custom/AddOns.
- `OrcaExecutionLines.cs` -> live Custom/Indicators, preserving the pre-existing working changes already matching the deployed authored baseline.

Per-file hashes are in the sibling `stage-hashes.json`. Before copying: recheck staged/source hashes and pre-edit/live parity, confirm NinjaTrader has exited, and back up the current DLL, affected sources, Journal database with WAL/SHM sidecars, and annotation TSV. Copy the exact four-file set; do not replace SQLite or unrelated suite files. Then restart NinjaTrader/F5 and test Journal startup/reconciliation, a warm-up flat cycle followed by a scaled/reversal SIM trade, reconnect uncertainty/recovery, annotation preservation and recorder schema-3 identity/playback. Prior deployed DLL remains unchanged; no shutdown was requested or performed by tools.

## Git, risks and promotion

Recorder/helper/tests/docs are committed in the coordination repository; external changes are committed in its own repository. The chart file already contains substantial unrelated uncommitted changes, including execution-ID/routed-contract prerequisites absent from HEAD. **Its new edits remain in the working file; the exact focused patch is committed instead of whole-file staging unrelated work.** The platform runner reverses that patch only in a disposable copy to reconstruct the pre-edit snapshot. Normalized authored pre-edit SHA256: `cf0bc7940e901cddf0cc0ccf690e72ce310065d00e8bc659d37e95b66a7cd6fa`.

Provider delivery/position semantics require SIM/runtime validation; consistent callbacks cannot prove an upstream provider never lost offsetting fills. Starting mid-position may remain unverified until restart/re-arm with complete observation. Execution-ID reuse across account resets still needs a namespace contract. The shared source link means both repositories are build inputs; preserve the recorded coordination state. Full_Suite is untouched and no promotion is eligible without Julian's validation.
