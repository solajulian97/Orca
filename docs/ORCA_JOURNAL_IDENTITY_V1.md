# Orca Journal identity and reconciliation v1 foundation

Date: 2026-09-05. Source/build verified offline; NinjaTrader runtime validation pending Julian. No deployment in this slice.

## Ownership

Authoritative external solution: `C:\Users\julia\projects\OrcaTrading\OrcaJournal\OrcaJournal.sln`.
Source: sibling `OrcaJournal` directory. External Git was initialized on `main` with source-only baseline `0467d1b`; generated output and private databases/media are excluded. This coordination repository holds the integration documentation and linked-source tests. No source was copied into Working_Suite or Full_Suite.

## Grouping and execution identity contract

One trade is one account/full-contract flat-to-flat cycle. Scaling and partial exits stay together. A crossing-flat fill closes the old cycle for its remaining position quantity, then opens a new cycle for the remainder. Both cycles retain that execution ID with their respective signed allocations. The current Execution Lines FIFO path and recorder cash-flow ledger follow this grouping; Journal now splits the reversal too. Tests compare Journal against the actual recorder ledger source, including quantities, ordered IDs and gross P&L.

`Core/ExecutionIdentity.cs` defines a version-1 `ExecutionProvenance` envelope:

- Exact, ordinal account and full-contract names, without case-folding or short-symbol substitution.
- Explicit `CompleteHistory`, optional uncertainty reason, and execution allocations in producer execution order.
- Each allocation has a nonempty execution ID and nonzero signed quantity: buy positive, sell negative. An ID appears once within a cycle; a reversal ID may appear in both adjacent cycles.
- A valid cycle never crosses sign or reaches flat before its final allocation. At least two allocations and a final zero balance are required.
- Complete history means the producer has established the initial flat boundary, continuous delivery, reliable order, and the final flat boundary. Missing IDs, partial history or unverified reconnects must not be marked complete. Missing/unsupported envelopes yield no UID.

UID encoding: SHA-256 over .NET BinaryWriter UTF-8 strings (7-bit byte-length prefix), in order: `orca.trade.v1`, account, full contract; Int32 little-endian allocation count; then each execution ID string and its Int32 little-endian signed quantity. Render lower-case hex with `orca.trade.v1:` prefix. Prices, timestamps, commissions, local row IDs and composite keys are excluded. Producer ordering is authoritative; do not sort IDs lexically or infer ordering from tied timestamps. ID reuse across account resets would require a future execution namespace/version; do not manufacture complete provenance for such history.

Independent Python/C# golden vector: account `Sim`, contract `MNQ SEP26`, allocations `e0:+2,e1:-2` produces `orca.trade.v1:65428ff6137f4582a84ef92b2747bf6ed061344f0b87aff81a7efdab697e1e3a`.

**Current live capture deliberately does not assert complete history.** It stores observed allocations with `Live history continuity unverified`, and leaves UID null. The existing account subscription/SOD path lacks enough evidence to certify reconnect completeness. This foundation provides durable storage and a tested contract, not a claim that all producers now publish trusted UIDs.

## Additive persistence and compatibility

Schema 6 adds `trade_identity` (trade ID, nullable UID, provenance JSON, status, source) and `annotation_identity` (legacy key, nullable UID, JSON, status). Existing trade rows, composite keys, pending annotations, tags, grades and media links are not backfilled, merged or rewritten. New trade plus provenance insert is transactional. Ordinary annotation updates do not rewrite provenance. Existing explicit trade deletion remains compatible through the sidecar foreign key's cascade.

The existing exact-composite-key insertion guard remains. UID is not yet an authoritative insert/deduplication or navigation key. Historical duplicate keys, missing provenance and same-composite-key collisions remain review work; this slice does not resolve those records automatically.

Legacy `TRADE`, `TAG`, and `HIDDEN_TAG` TSV lines remain compatible. Optional new lines are:

`IDENTITY_V1<TAB>base64(legacy trade key)<TAB>base64(UTF-8 provenance JSON)`

These store evidence only. They never apply notes/tags/grades by a proposed UID match. The complete provenance must agree with the key's account, contract, direction and quantity. Current Execution Lines still writes legacy records; no producer-side TSV changes were made.

## Read-only reconciliation

Open the new Reconciliation tab and choose **Run reconciliation**. It uses a separate read-only SQLite connection and background task; no automatic scan/poll was added. Counts represent report rows grouped by Exact, Legacy, Ambiguous and Unmatched, with candidate IDs/counts and explanations.

- Exact: one complete UID candidate without conflicting composite-key evidence. Proposal only.
- Legacy: composite-key evidence, existing media link, or scoped recorder evidence without a proven UID.
- Ambiguous: duplicate candidates, UID/key conflict, conflicting execution IDs, incomplete ledger history or uncertain local-time conversion.
- Unmatched: no candidate, missing media, unsupported schema, incomplete/unreadable manifest or inaccessible scan root. Existing links remain.

Annotation proposals do not replace the pre-existing exact-key annotation importer. Running that existing importer still has its earlier behavior; the new report itself only reads.

Schema 2 is read per constituent trade: account/full contract/direction/entry quantity, ordered execution IDs, completeness and interval. It never falls back to the capture-wide account/instrument Cartesian combinations. Existing schema-2 IDs lack per-fill allocated quantities, so they **cannot produce a version-1 UID**. Matching ordered IDs is shown as legacy evidence; conflicting IDs remain ambiguous. Legacy candidates without comparable IDs use both entry and exit within five seconds, not broad capture overlap. Unspecified trade times use the explicit local zone; ambiguous/nonexistent DST times are flagged. UTC times need no conversion.

New schema-2 attachment creation is held for read-only review in this slice. Existing schema-2 links are untouched. Schema-1 auto-import compatibility remains, including its prior capture-overlap behavior and idempotent trade/path guard. The new report is stricter than that older importer and reports multiple schema-1 candidates as unproven pairings; legitimate many-trade clips remain supported as multiple proposals.

Future schemas, missing ledger entries, missing files and incomplete finalization fail closed in the report. No media files are moved, copied or renamed. A schema-2 clip can contain multiple ledger trades; there is no one-clip/one-trade restriction.

## Verification and limits

Run `tests/OrcaJournal.Identity/Verify.ps1` in the coordination checkout. It compiles actual external core/data sources, uses minimal NinjaTrader execution stubs, and extracts the current recorder ledger for cross-producer fixtures. Full external Release compilation separately checks the real NinjaTrader/WPF references. Tests use generated disposable databases/media placeholders, never the live Journal database.

Coverage: schema-5 migration and repeat initialization; notes/grades/tags/hidden tags/screenshots retained; identity serialization/reopen/golden vector; scope separation; scaling, partial closes, reversal allocations, duplicate delivery, missing IDs, unknown continuity, same time/price distinct identity; schema-1/schema-2 handling and repeated imports; UID ambiguity, missing files, UTC/local/DST; SELECT-only repeated reconciliation and enforced read-only connection.

Report cost grows with trade/annotation/manifest count, approximately O(trades × evidence rows); no live benchmark is claimed. A very large recording tree may need indexing/pagination later. Account execution capture and its existing persistence callback remain synchronous; this slice adds no market-data series, Tick Replay processing, provider/cache access or indicator rendering work.

Next steps: collect the deployed build's runtime results and address reported defects; validate this source build separately before deployment; establish trustworthy initial-flat/reconnect/order evidence; implement the same contract in chart/recorder producers with allocated quantities; then consider explicit association acceptance and UID-aware insert/navigation. Journal-side editing/conflict resolution and image viewer remain separate work.
