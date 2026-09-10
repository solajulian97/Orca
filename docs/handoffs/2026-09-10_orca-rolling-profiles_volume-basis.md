# Rolling Profiles volume basis — 2026-09-10

## Objective

Add a rolling contract-volume basis analogous to Step Profile's volume amount, with continuous oldest-volume expiry rather than completed step blocks.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaRollingProfiles.cs`
- `tests/OrcaRollingVolume.Tests/OrcaRollingVolume.Tests.csproj`, `Program.cs`, `VolumeTests.cs`, `Harness.txt`
- `docs/indicators/ORCA_ROLLING_PROFILES.md`
- This handoff.

Pre-edit source was clean relative to HEAD and copied to `.codex-backups/rolling-volume-20260910/OrcaRollingProfiles.cs`. Unrelated dirty source, deployment script and shared provider changes were preserved.

## Behavior and user-facing settings

- Added Rolling Basis (Time, Volume), default Time, and editable Rolling Volume Amount (1 through int.MaxValue), default 100,000.
- Volume retains the latest N included contracts, partially trimming the oldest trade when needed. An oversized individual trade leaves exactly N contracts at its price. Before warm-up reaches N, all available included volume is shown.
- No daily reset. Existing FullSession/RthOnly inclusion remains. Time periods and Minutes in Trading Day do not limit volume mode.
- Signed delta expires with volume. Zero-net delta rows can reappear with the remaining side after an opposing trade expires.
- Exact local trades preserve signed delta; partially removed mixed provider records use proportional integer delta because internal ordering is unavailable.
- Equal timestamps preserve insertion order; provider batches use stable time ordering in Volume mode. Time-mode sorting is unchanged.
- Existing settings identities/defaults, enum values, period/session calculation and renderer are preserved. No settings removed or hidden. NinjaTrader must regenerate wrappers for the two new NinjaScript properties at F5.

## Secondary series and event paths

No Tick, Second, Bid, Ask, Last, Volumetric or custom series added or changed. Existing conditional `AddDataSeries(BarsPeriodType.Tick, 1)` remains local-only when the shared provider is off. Existing `Calculate.OnPriceChange`, secondary `OnBarUpdate` ingestion and quote-only `OnMarketData` classification are unchanged. No `Calculate.OnEachTick` path added.

## Tick Replay and historical load

No Tick Replay requirement changed. Historical local data uses the existing hidden tick series. Volume rolls through all included loaded data and carries across sessions. Historical quote/classification fidelity remains source-dependent. Hydration rebuild preserves partial retained records.

## Cache implications

Reuses the existing profile maps and active time-indexed records. Adds retained volume and per-timestamp head offsets, reset with model/source reset. No shared service, provider migration, cache deletion or persistence added. Volume mode initial provider lookback uses Maximum Provider Backfill rather than being shortened by the irrelevant Time period. Existing provider retention/cursor and historical coverage risks remain.

## Rendering and performance

Renderer and SharpDX code are unchanged. No new calculations, cache reads, mutation or waits in OnRender. Incremental add/subtract work is proportional to expired records with sorted-time lookup; no full historical rebuild per trade. Head offsets and amortized compaction avoid repeated equal-time list shifts. Volume mode disables spike time/price coalescing because it loses expiry order; retaining individual records can cost more CPU/memory than compacted Time mode during bursts. At most N positive-volume records remain active, plus bounded amortized list slack. No live performance claim is made.

## Tests, compile and manual validation

- Production-method harness: partial trim, oversized trades, equal timestamps, disabled compaction, late records, RTH filtering, cross-session carry, mixed provider delta, source reset, hydration rebuild, time expiry and amounts through int.MaxValue.
- 20,000 randomized chronological/out-of-order trades compared to an independent contract-level volume/delta reference, with periodic rebuilds.
- Existing property identities and unchanged renderer, time-window method and period mapping checked against the archived baseline.
- Offline authored C# 7.3/NinjaTrader semantic compile: zero errors.
- Deployed authored parity and installed generated-wrapper offline semantic compile: zero errors. Installed wrappers include both `rollingBasis` and `rollingVolumeAmount`; NinjaTrader regeneration was observed, but F5/load and chart validation remain separate.
- Scoped `git diff --check`: passed.
- NinjaTrader UI tests/F5: not performed; native app control unavailable. Offline checks do not run the NinjaScript generator or establish assembly load.
- Manual validation: pending Julian. Test 5k/50k/200k on an active chart, total-volume saturation, partial expiry, POC/value-area/delta updates, FullSession/RthOnly, reload/template preservation, same-price trades and a busy-period Diagnostics capture. Check Time mode on an existing template.

## Deployment

Completed with `Orca Trades/Scripts/deploy_orca.ps1 -Target OrcaRollingProfiles.cs`. Only the exact live indicator target was copied. Installed pre-edit backup: `.codex-backups/rolling-volume-20260910/OrcaRollingProfiles.installed-before.cs`. Authored source parity passed after newline normalization. No Full_Suite or mirror copy.

## Known risks and follow-up

Available historical data may not fill the requested volume. Optional provider lookback/retention may further limit coverage. Provider mixed records cannot supply exact constituent-trade delta order. F5 must regenerate settings wrappers. Native callback cadence and live performance need chart validation.

## Full_Suite promotion

Not eligible until NinjaTrader F5/load and Julian's live manual validation pass. Full_Suite untouched.
