# Multi-reader provider probe — 2026-09-07

## Objective and files

Exercise one canonical producer with two independent readers in the existing opt-in platform probe, without adding a second market-data subscription.

Changed: Working_Suite/Indicators/OrcaProviderReaderComparisonCore.cs (new), OrcaProviderProbe.cs; tests/OrcaProviderStream.Tests/ProviderReaderComparisonTests.cs (new), Program.cs and project; this handoff.

## Behavior

The probe now owns an OrcaProviderFeedLifetime and constructs a typed identity from captured sessions/timezones, a private epoch and private classifier origin. Bars.IsInReplayMode selects the private live/replay environment; this does not authenticate feed provenance or enable cross-chart sharing.

Two comparison readers maintain independent generation/sequence cursors. Each comparison call reads at most 256 events per reader, verifies exact timestamp ticks/kind, price bits, volume, signed volume and classification, then atomically commits its observer totals/cursors. It reports cumulative verified count, volume/signed-volume totals and an order-sensitive diagnostic digest. Payload comparison is exact; digest equality is not relied upon as a collision-free proof. This checks transport consistency between readers, not producer correctness or indicator calculation parity.

Comparison runs every 256 accepted events and flushes a bounded remainder for status/realtime handoff. Missing publication, eviction, closure, generation mismatch or payload mismatch is a terminal comparison failure. No cursor skipping or rebuilding history. Empty input reports NO_EVENTS, not PASS. Probe errors close its private feed lifetime and dispose the comparison; termination releases all remaining handles and diagnostics registration.

## Settings, series and historical implications

No settings/defaults or existing consumer code changed. Adding the probe remains opt-in. No secondary Tick, Second, Bid, Ask, Last, Volumetric or custom series added/changed. No AddDataSeries call, new market subscription, timer, account hook or order action.

Existing OnMarketData Last ingestion, Calculate.OnEachTick, Tick Replay flags and native session reset handling remain. The new comparison adds test-only callback work and snapshot allocations. It is not a startup optimization or production aggregation path. No OnRender or drawing changes. No persistent cache/database writes or reads. Chart/model historical correctness and historical UTC completeness are not established by matching readers.

The ring remains bounded at 100,000 events. Reader quota is now three (one status, two comparison); max batch 256 and outstanding-batch quota two. At most two comparison payload batches coexist, disposed at the end of each comparison. Exact retained-history flags may become false while streaming comparison still passes, because already-consumed events can be evicted safely.

## Verification and deployment

- 322 core checks passed, including 13 new checks for bounded drain, remainder, repeated caught-up calls, same-time duplicates, cumulative volume, continued reads across consumed-prefix eviction, unread eviction failure, closure, and cleanup after partial reader admission.
- Offline installed NinjaTrader-reference C# 7.3 semantic check: zero errors across nine provider files; structural probe guards passed.
- Pre-deployment live probe matched commit a8fe8f0's authored source. Four unchanged live core dependencies matched Working_Suite. Four new target files were absent.
- Previous live probe backed up at `.codex-backups/provider-multireader-20260907-132002/OrcaProviderProbe.cs`.
- Targeted deployment copied OrcaProviderIdentityCore, OrcaProviderSessionCapture, OrcaProviderFeedLifetimeCore, OrcaProviderReaderComparisonCore and OrcaProviderProbe only. All five normalized authored-source parity checks passed. No running NinjaTrader process was found in the preflight process query.
- NinjaTrader F5/load and actual multi-reader chart run: pending Julian. No runtime/performance gain claimed. Full_Suite untouched; not eligible for promotion.

## Manual check and next steps

Compile with F5 and add one OrcaProviderProbe to one existing Tick Replay chart, preserving other indicators/settings. After historical load, collect its Output summary: readerComparison=PASS, verified=historical+live, and no FAULT. A fullPassRetained=False flag is expected after more than 100,000 events; it does not itself indicate a comparison failure. No full-platform restart or stopwatch required.

Then remove the probe and check diagnostics cleanup. Actual callback scheduling, feed reconnect, session boundaries and repeated reload/disposal remain platform tests. Cross-chart sharing, feed attribution, owned-subscription snapshot handling, merge/adjustment identity and consumer parity remain gates before removing hidden series.
