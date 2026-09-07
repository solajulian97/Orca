# Provider feed lifetime — 2026-09-07

## Objective / evidence

Implement terminal ownership of a feed continuity interval without claiming verified platform connection attribution. Installed metadata exposes Instrument.GetMarketDataConnection() and GetDataConnection(), but MarketDataEventArgs carries no connection property. A current routing query is not proof of the origin of an already-delivered callback or historical cache. Do not wire this API as authoritative provenance without further validation. The metadata inspection tool now includes these types/methods for reproducible investigation.

## Files changed

- Working_Suite/Indicators/OrcaProviderFeedLifetimeCore.cs (new)
- Working_Suite/Indicators/OrcaProviderIdentityCore.cs
- tests/OrcaProviderStream.Tests/ProviderFeedLifetimeTests.cs (new), Program.cs and project
- tests/OrcaProvider.PlatformCheck/Program.cs
- This handoff.

## Behavior

An explicitly constructed feed owner allocates a fresh epoch and owns one bounded registry. Only typed identities matching its epoch and environment can acquire publishers or open readers through it. Identity now exposes those immutable fields alongside its key. Disposal terminally closes all streams through the registry and rejects later acquisition. Reconnection requires a new owner; old handles do not attach to the replacement. Already-delivered immutable batch leases remain consumer-owned until released. This is continuity isolation, not an authentication boundary against code that fabricates matching identities.

No platform Connection, account or chart references, event handlers, timers or global discovery are retained. The platform adapter still must invoke closure when disconnect, replay replacement, route changes or relevant configuration changes invalidate continuity. This class does not itself subscribe to those events. No automatic failover or recovery is added. Existing raw registry/probe behavior is unchanged.

## Settings / data / rendering / performance

- No user settings/defaults added, changed or removed.
- No secondary Tick, Second, Bid, Ask, Last, Volumetric or custom series changed; no AddDataSeries or OnMarketData hooks added. Calculate modes and Tick Replay flags unchanged.
- No historical load, cache persistence, database, trading, account or drawing changes.
- No OnRender work. Admission validation occurs when obtaining handles, not per tick. Closure work is bounded by the registry stream limit.
- No measured performance or startup gain; this disconnected core does not reduce existing hidden series.

## Verification / rollout

309 linked-source checks passed. Offline C# 7.3 semantic check against installed NinjaTrader references: zero errors across eight provider files. Structural probe guards also passed.

Core checks cover epoch separation, identity/environment rejection, terminal reader closure, held-batch ownership, late callback rejection, independent replacement, stale publisher release, and 20 acquisition/closure race trials. Tests do not exhaust all interleavings or validate NinjaTrader callback scheduling.

No deployment or NinjaTrader F5/manual runtime testing for this source-only slice. Full_Suite untouched; not eligible for promotion. Unrelated dirty work preserved.

## Next

Establish authoritative feed attribution or adopt an explicitly isolated producer service whose own subscriptions define provenance. Historical source, merge/adjustment settings, session/config invalidation and reconnect event ordering still need platform integration and testing. Do not conflate a current connection object with complete historical provenance. Existing indicators remain on their current data paths until an opt-in comparison demonstrates parity.
