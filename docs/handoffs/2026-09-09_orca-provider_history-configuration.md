# Owned history configuration and identity — 2026-09-09

## Objective / recovered boundary

Continue after the normal historical/live and deliberate-disconnect probe gates passed. The fresh post-disconnect instances supplied live events but no historical callbacks; callback-pass completion is not history coverage. Add explicit configuration identity for a future owned historical request without retroactively authenticating chart/cache history or changing the validated probe.

## Verified platform constraints

- [BarsRequest](https://docs.ninjatrader.com/ninjascript/barsrequest) exposes per-request merge, split/dividend, lookup, session and request-range inputs. Date inputs represent whole local trading days; count-back requests do not by themselves establish a fixed UTC range. It is independent of a chart's primary series. Its example warns the final returned bar may still be developing.
- [MergePolicy](https://docs.ninjatrader.com/ninjascript/mergepolicy) distinguishes unmerged, merged/back-adjusted and merged/non-back-adjusted history from inherited global settings.
- [RolloverCollection](https://docs.ninjatrader.com/ninjascript/rollovercollection) supplies contract month, rollover date and offset.
- Installed public metadata confirms the used APIs and additionally exposes MergePolicy.UseDefault plus Provider/Repository lookup flags. Metadata binding is not platform dispatch or request-completeness proof.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaProviderHistoryConfigurationCapture.cs` (new)
- `Orca Trades/Working_Suite/Indicators/OrcaProviderIdentityCore.cs`
- `tests/OrcaProviderStream.Tests/ProviderHistoryConfigurationTests.cs` (new)
- `tests/OrcaProviderStream.Tests/{OrcaProviderStream.Tests.csproj,Program.cs}`
- `tests/OrcaProvider.PlatformCheck/Program.cs`
- `docs/indicators/ORCA_PROVIDER_EXPERIMENTS.md`
- This handoff.

## Behavior

The capture accepts an already configured request and explicitly supplied event/request-local clock definitions. It retains only immutable strings: full contract, encoded request definition, session definition and event timezone. It makes no Request call, adds no subscription, reads no payload and asserts no coverage.

The initial supported scope is futures with explicit Last Tick-1 bars, no split/dividend adjustment, and one of the three resolved merge policies. Other instruments or enabled corporate-action adjustments need their own schedule contract and are rejected; no settings are silently changed to fit. Lookup flags record request intent, not the actual provider/cache origin of a returned tick.

The definition includes exact contract/expiry, tick size/point value, merge/lookup policy, adjustment/reset flags, raw local/count-back inputs including DateTime kind, caller-declared request-clock rules, and captured session/event-clock rules. For merged requests it copies at most 4,096 rollover rows, validates finite/non-extreme offsets and nonempty dates, rejects duplicate contract months, sorts by month and records dates/kinds plus exact double bits. Unmerged requests do not apply rollover tables. Capturing twice detects observed changes; RequireUnchanged permits later comparison. Neither establishes an atomic platform snapshot or protects against unobserved ABA edits. The owner must serialize initialization and hold its configuration stable through loading.

An additive SourceIdentity overload requires a nonempty historical snapshot/lineage GUID and the configuration definition. Different owned history loads or configurations cannot alias. The capture's CreateIdentity uses its own captured contract/session/clock fields. The GUID must represent actual owned history lineage, not a current connection name or a chart label. Configuration remains a caller assertion, not authentication. History-aware keys have a distinct versioned prefix from legacy keys; the original constructor/key remains unchanged, with HasHistoricalConfiguration=false.

## Settings / series / history / cache / rendering / performance

- User-facing settings/defaults: none added, changed, deprecated or removed.
- Secondary Tick/Second/Bid/Ask/Last/Volumetric/custom series: none added or changed. No AddDataSeries or market-data subscription.
- OnMarketData, Calculate modes, Tick Replay and existing ingestion/reset paths: unchanged.
- Historical requests/loading: none initiated. No inferred UTC confirmation, overlap reconciliation, automatic reload, data deletion or changed chart merge policy.
- Cache/persistence: unchanged; no payload or database access.
- Rendering: none; no chart references or render-path work.
- Performance: initialization-only bounded rollover copying/sorting and string encoding. No per-tick capture, global locking, measured speedup or hidden-series reduction.

## Verification / deployment / manual validation

473 core checks passed, including 116 new configuration/identity checks. The unchanged probe regression passed 243 checks. Offline C# 7.3 installed-platform semantic compilation passed with zero errors across thirteen sources; structural guards confirm immutable string retention and no data request/subscription/coverage assertion from capture. Tests cover equivalent/culture-independent snapshots, reordered schedules, nineteen incompatible dimensions and rejected reader attachment, independent historical lineage, mutable configuration rejection, invalid/inherited/unsupported settings, duplicate/oversized schedules, exact offsets, clock separation, and unchanged unverified coverage despite captured identity.

Source-only, not deployed. NinjaTrader F5/load and actual platform configuration-capture/request behavior remain untested for this slice. Existing live probe files are unchanged; no new chart test is requested merely for an unused helper. Full_Suite untouched and not eligible for promotion. Existing unrelated dirty edits preserved.

## Next gate / risks

Integrate capture into a bounded, opt-in owned historical request observation before enabling publication. Inspect returned Tick-1 data and exact request lifecycle separately from chart callbacks; retain incomplete/error/zero-event outcomes. A successful request/configuration capture must not be promoted to authenticated historical coverage or a historical/live stitch without evidence. Continue to keep standalone snapshot/incremental ingestion, shared discovery and first Rolling Profiles shadow comparison gated. Local series removal is not authorized by this configuration slice.
