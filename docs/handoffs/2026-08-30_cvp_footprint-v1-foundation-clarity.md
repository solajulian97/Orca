# CVP P0-A/P0-B: Evidence Foundation And Clarity

Date: 2026-08-30
Status: source implemented and targeted live copies verified; local checks passed; NinjaTrader compile and manual acceptance pending.

## Objective

Implement only the approved opt-in Bid x Ask footprint foundation and clarity release. Preserve the established footprint silhouette and all legacy display/calculation contracts. Do not implement later signal formulas or alter unrelated indicators, Full_Suite, Figma, workspaces or market-data stores.

## Files Changed

- `Orca Trades/Working_Suite/Indicators/OrcaCandleVolumeProfile.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaFootprintCore.cs` (new)
- `Orca Trades/Working_Suite/Indicators/OrcaCandleVolumeProfile.Rendering.cs` (new)
- `tests/.gitignore`
- `tests/OrcaFootprint.Tests/OrcaFootprint.Tests.csproj`, `Program.cs`
- `tests/OrcaFootprint.PlatformCheck/OrcaFootprint.PlatformCheck.csproj`, `Program.cs`, `BaselineFixture.txt`
- `docs/indicators/ORCA_CANDLE_VOLUME_PROFILE.md` (new component reference)
- This handoff.

The dirty deployment script, other dirty indicators, global product/source-map docs and unrelated handoffs were not changed for this task. Existing backup checkpoint **34d7faf** remains intact; its archive is `.codex-backups/cvp-pre-footprint-2026-08-30_142958/CVP-current-version.zip`.

Dependency note: this slice uses the existing OrcaDiagnosticsCore API already available in the local compiled platform. Its source is currently untracked work owned by the diagnostics effort, and is intentionally not swept into this checkpoint. A clean checkout needs that diagnostics dependency checkpoint before compiling the complete suite.

## Behavior And Settings

- Enhancement defaults off and applies only to Bid x Ask. Legacy display migration, enum values, calculation methods, defaults, brushes and renderer are preserved.
- Typed integer-tick strict bid/ask/unclassified evidence; event sequence, time, quality/provenance limitations and observed quote age. No inferred delta substitution or identical-trade deduplication.
- Immutable dirty-bar snapshots, one coalesced preparation worker, viewport generation rejection, cancellation, bounded two-variant derived cache.
- Common bid/ask normalization: Per Bar (default), Visible Range, developing Session Global, configured Fixed. Cluster retains delta hue and total-row intensity.
- Independent fixed analysis rows, lowest-price POC ties and neutral POC outlines with provisional developing treatment.
- Optional neutral OHLC spine/body, measured full/compact/auto numeric layout, tabular figures/fallback disclosure, fixed gutter, strict cell-value selection, hover inspection and data-health strip.
- New serialized settings: EnhancedFootprint, FootprintSettingsVersion, FootprintAnalysisTicks, FootprintScale, FootprintFixedVolume, FootprintScaffold, FootprintValues, FootprintNumbers, FootprintGutterPx, FootprintShowHealth. Existing values are not reset when hidden. No settings removed.
- Enhanced property grid groups applicable controls into numbered Profile, Data Quality, Rows, Scale, Scaffold / POC and Text sections. Cluster label is explicit. Fixed is omitted from choices until a positive count is configured.

## Series And Event Implications

- **Added/changed series: none.** Existing conditional `AddDataSeries(BarsPeriodType.Tick, 1)` remains unchanged. No Bid/Ask/Second/Volumetric/provider series.
- `Calculate.OnPriceChange` unchanged. Existing secondary OnBarUpdate and Tick Replay Last-event ingestion preserved. OnMarketData additionally records quote timestamps only for enhancement.
- Core observes the already accepted/classified event. Legacy strict dictionaries are replaced by the enhanced typed evidence store only in opt-in Bid x Ask; other maps and shared publication retain their existing behavior.
- Legacy `ClassifySignedVolume`, `ResolvePrimaryBarIndex`, price/time helpers and NormalizeTradeVolume are token-identical to backup.

## Replay And Historical Load

- Trade-associated quotes are labeled provenance-unverified, never guaranteed native. Cached/secondary quote limitations, unknown quote age, unclassified volume, out-of-order events and attribution differences remain inspectable.
- Existing attribution search can choose a later neighboring bar in constructed fixtures. This is a characterized risk, not proof of a particular live-feed defect. The enhancement flags native-index disagreement/forward attribution without modifying the accepted trade assignment. Stop acceptance if actual equivalent-prefix replay fixtures show a mismatch; fix that separately after review.
- SessionIterator uses trading-day identity. First loaded day is conservatively partial; subsequent session maxima are independent. Realtime connection gaps latch a warning. No cache/database reset or tick-history edits performed.
- Historical Tick Replay, Market Replay forward data, Market Replay historical preload, and Historical Playback must be validated separately. No Orca Instant Replay dependency.

## Cache, Rendering And Performance

- Shared cache format unchanged: total/up/down inferred maps only; no strict/provenance export.
- Immutable rows are prepared outside OnRender. Dirty-bar copy locks are short and per-bar; aggregation/sorting and scale/POC work run on one worker. UI dispatcher prepares measured DirectWrite layouts outside drawing.
- Visible rows plus one-screen overscan on both sides, at most two compression variants. Estimated row/summary cache cap 16 MB plus estimated current/retiring presentation caps up to 16 MB. Native DirectWrite/process memory must still be measured; source evidence and transient preparation are not this derived-cache budget.
- No authored per-row managed allocations, locks, full profile rebuilds or shared registration inside the enhanced renderer. Legacy rendering remains untouched. Enhanced scaffold does not allocate absorption brushes per bar.
- Target brushes recreate on target change; retired layouts wait for active readers without synchronous waits. Termination cancels preparation and removes timer/mouse/tooltip handlers.
- Existing diagnostics: source/series/state declarations, sampled accepted-event work, snapshot preparation, model/cache health, latest render work sample per 100 ms observer interval. This is not a full frame-time distribution.

## Local Checks

Commands from repository root:

```powershell
dotnet run --project tests/OrcaFootprint.Tests -c Release
dotnet run --project tests/OrcaFootprint.PlatformCheck -c Release
```

- Model fixtures cover conservation, strict vs inferred separation, large/repeated trades, stable event prefixes, aggregation, negatives, POC ties/zoom independence, all denominators, zeros/unknowns, quote age, immutable revisions, cancellation, eviction and budget recovery.
- Offline platform semantic compilation uses installed NinjaTrader/SharpDX metadata and authored regions only; no C# errors.
- Baseline parity assertions cover legacy defaults, enums, migration, classifier/attribution methods, shared registration and complete legacy render body after removing only the opt-in early branch.
- Thirteen fixtures execute extracted baseline classifier/attribution methods against explicit stubbed bar queries. They do not replace native minute/range/tick/volume bar attribution tests.
- AST check finds no authored reference-type object creation, locks or preparation/cache calls in RenderEnhancedFootprint.
- Final synthetic model-only run: **393 checks passed**; 200,000 observations in 46 ms; warm preparation with 200 visible/600 cached bars p50 0.041 ms, p95 0.054 ms; estimated derived model bytes 1,443,616. This is not an old/new NinjaTrader benchmark or render measurement.

## Deployment And Compile Status

- Source implementation: complete for this development slice.
- Before deployment: live CVP authored region matched backup 34d7faf; neither new helper file existed in live Indicators.
- Targeted live copy: **complete** for exactly OrcaFootprintCore.cs, OrcaCandleVolumeProfile.Rendering.cs and OrcaCandleVolumeProfile.cs through the existing targeted deploy script. Source/live authored parity passed for all three after LF/CRLF normalization. Generated-region counts: core 0/0, partial 0/0, main 1/1. No local mirror or Full_Suite copy.
- NinjaTrader F5 / assembly-load success: **not performed or claimed**.
- Tests performed in NinjaTrader: **none this turn**. The running platform was not reloaded and no chart settings/orders/accounts were changed by this task.
- Julian's manual validation: **pending**.
- Eligible for Full_Suite: **no**.

## Acceptance Checklist And Known Limits

1. F5; confirm successful Custom assembly load. Preserve old-template screenshots before enabling enhancement.
2. Verify all legacy displays and saved workspace round trips, then both enhanced styles, colors, new settings persistence and contextual dropdowns. New public serialized options intentionally do not alter existing factory parameter signatures; test strategy-created instances explicitly if using programmatic configuration.
3. ES/MES/NQ/MNQ: equivalent supplied-event prefixes, same-price/repeated timestamps, minute/range/tick/volume bar boundaries, last incomplete bar, historical-to-live transitions and reload. Inspect attribution warnings before accepting values.
4. Full/partial trading days, overnight/RTH, DST/holidays, contract change, reconnect gaps. First loaded trading day is conservatively marked partial even when coverage may be complete.
5. Known zero, completely/mixed unclassified rows, missing history, tied POC, developing POC, fixed clipping, all cell views. Hover must reconcile exact quantities and explain hidden text.
6. Figtree and fallback, tiny/large fonts, 100/125/150/200% DPI, resize, stationary-market zoom/pan, price clipping, RenderTarget recreation, multiple CVPs, repeated remove/re-add.
7. Compare identical two-day ES/MNQ and longer/multi-instance inputs against backup. Required budgets remain unproven: <=10% historical/callback regression, warm render p95 <=8 ms at 200 visible bars, native allocation/memory stability and no task/dispatcher backlog. UI text preparation currently recreates a bounded frame on changed data, so measure that cost separately from pure model preparation.

Publication is per-bar revisioned evidence, not a globally atomic exchange-tape prefix while concurrent ingestion is active. Cancellation/viewport rejection use lifecycle and viewport generations; market progression is allowed between preparation and display, with up to the coalescing delay. Explicit full-process/native-memory, saved-template and live-render verification remain release gates.

## Rollback And Follow-Up

First rollback: disable Enhanced Footprint and reload. No new feature is enabled automatically. Source rollback should restore only this CVP checkpoint and remove its two new helper sources if returning to pre-partial CVP; never bulk-restore shared dependencies or unrelated work. Preserve archive/checkpoint 34d7faf.

P0-C and all later milestones remain unimplemented: imbalance/stack rules, semantic zoom states, auctions/clusters/divergence, Time Statistics integration, interpretation/levels, reset anchors, sequence analytics and alerts require their separate approved definitions and checkpoints.
