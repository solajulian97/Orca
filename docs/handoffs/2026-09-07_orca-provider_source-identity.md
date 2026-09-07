# Provider source identity — 2026-09-07

## Objective / recovered state

Continue provider hardening without a workspace-wide switch. Julian's latest screenshot shows recovered diagnostics coverage: 24 open / 24 reporting, 105 instances and 30 hidden Tick 1 series, with 1.3 seconds reported backlog. Recovery does not establish why registrations disappeared or demonstrate a performance improvement. The historical probe observation remains 4,805,466 historical events, 100,000 retained, completed historical pass and 9.0 seconds elapsed for its load window.

Existing isolated core provides bounded storage, stable generation/sequence cursors, exclusive publisher ownership, explicit historical coverage, reader/batch budgets and serialized ingestion. The private probe exercises platform callbacks but is not shared with consumers. The legacy provider remains unchanged.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaProviderIdentityCore.cs` (new)
- `tests/OrcaProviderStream.Tests/ProviderIdentityTests.cs` (new)
- `tests/OrcaProviderStream.Tests/Program.cs` and project (wire new checks)
- This handoff.

## Behavior

Adds an immutable, platform-independent identity builder producing the existing exact-stream key. Inputs explicitly include full contract, live/market-replay/historical environment, connection/replay epoch, session-definition snapshot, timezone-rule snapshot, reset policy, classifier-origin scope, classification algorithm and supplied-quote semantics.

Connection epochs must change when continuity is lost or a replay run is replaced. Classifier origin identifies shared initialization/history lineage: independently initialized classifiers must not receive the same origin merely because their instrument and algorithm match. Initialization matters to tick-direction fallback and equal-price classification. Do not invent stable identities from account names, chart names, contract roots or connection display names.

Session snapshots must describe actual effective sessions/holidays, not just a template name; timezone snapshots must include effective adjustment rules, not just a label. These obligations are not yet implemented in a platform adapter. The pure builder cannot independently authenticate its inputs. Arbitrary strings are length-prefixed, with ordinal, culture-independent versioned encoding to avoid delimiter collisions.

The existing raw key constructor remains for existing tests/probe compatibility; this slice does not force existing callers through the new builder. Compatible identity is not proof of available history, strict exchange quote provenance or complete retained coverage. Existing coverage/eviction/fault gates remain necessary.

## Settings, series, history, cache, rendering and performance

- No user-facing settings/defaults added, changed or removed.
- No AddDataSeries, OnMarketData, Calculate mode, Tick Replay flag, subscription or chart/account lifecycle changed.
- No secondary Tick, Second, Bid, Ask, Last, Volumetric or custom series added/changed.
- No historical loading or cache persistence changes. No legacy cache or database modified.
- No rendering changes or per-tick identity construction. Builder is intended for source admission, not callback hot paths.
- No production consumer migration or measured performance gain. Full_Suite untouched.

## Verification

- 262 linked-source core checks pass (36 added checks): compatible equality/hash, 11 incompatible dimensions and rejected reader attachment, exclusive ownership, delimiter safety, culture independence, invalid/missing identity rejection, ingestion compatibility and no false historical-coverage claim.
- Offline C# 7.3 NinjaTrader-reference semantic check: zero errors across six provider sources; structural guards pass.
- No deployment, NinjaTrader F5, or manual runtime test for this new builder. Not eligible for Full_Suite promotion.
- Existing unrelated dirty changes, including diagnostics and Time Statistics, preserved.

## Next steps / risks

Implement and verify platform identity capture and service ownership before exposing a shared registry. Fail closed when feed identity, effective session rules, timestamp rules or classifier origin cannot be established. Then use one opt-in consumer in comparison/shadow mode, preserving its existing output and secondary series until parity and retention behavior are demonstrated. Actual hidden-series reduction and startup/runtime improvement are later measured gates, not accomplished by this disconnected identity helper.
