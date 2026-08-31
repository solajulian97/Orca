# Orca Candle Volume Profile

Updated: 2026-08-31. Working_Suite development source only.

## Ownership

- `Orca Trades/Working_Suite/Indicators/OrcaCandleVolumeProfile.cs`: indicator, serialized settings, contextual settings converter, accepted trade processing, legacy drawing, shared publication. Keep the `IndicatorBaseConverter` beside the concrete indicator declaration to avoid spurious NinjaScript indicator wrappers in a helper partial.
- `Orca Trades/Working_Suite/Indicators/OrcaFootprintCore.cs`: platform-independent strict evidence, integer price buckets, immutable snapshots, POC and normalization.
- `Orca Trades/Working_Suite/Indicators/OrcaCandleVolumeProfile.Rendering.cs`: enhanced viewport observer, preparation worker, text fitting, drawing, inspection, enum converters.
- `tests/OrcaFootprint.Tests`: isolated executable fixtures, no NinjaTrader dependency.
- `tests/OrcaFootprint.PlatformCheck`: installed-platform metadata check, backup parity assertions, extracted legacy classifier/attribution fixtures, converter placement guard and invalid-generated-wrapper regression. `--live-generated` includes the deployed wrappers with test-only host partial fields; it does not run NinjaTrader's generator or replace F5.

## Opt-In Clarity Release (P0-A/P0-B)

Select **Profile Display: Bid x Ask**, then enable **Enhanced Footprint**. This remains false by default. Selecting enhancement never changes Cluster to Histogram. All five legacy display choices remain available; disable enhancement and reload to restore legacy rendering.

Enhanced controls are contextual and ordered into Profile, Data Quality, Rows, Scale, Scaffold / POC, and Text. Hidden values are retained, not reset. Existing brush serialization, visibility migration, enum values, defaults and generated factory signatures remain intact. New options are ordinary public serialized properties, not new required factory parameters. `FootprintSettingsVersion` is serialized and currently 1. `FootprintAnalysisTicks = 0` initializes from the existing fixed order-flow row size when first enabled, then stores that independent analysis size.

| Setting | Contract |
| --- | --- |
| Enhanced Footprint | Bid x Ask only; false by default. |
| Analysis Row Size | Fixed POC calculation rows, independent of zoom/display compression. |
| Display Row Size / Dynamic Order Flow Aggregation | Existing presentation controls; do not change analysis rows. |
| Normalization | Per Bar, Visible Range, Session Global, Fixed. |
| Fixed Scale Volume | Enter a positive contract count before Fixed becomes a dropdown choice. Zero in a restored Fixed configuration reports N/A. |
| OHLC Scaffold | Off, OHLC Spine, OHLC Spine + Body. Neutral spine is the enhanced default. |
| POC Outline | Existing ShowPOC setting; neutral outline in enhanced mode. Lowest-price tie retained, tie count on hover. Developing POC is provisional. |
| Cell Values | Bid x Ask, Total Volume, Strict Delta, Strict Delta %. |
| Number Format | Full, Compact, Auto. Auto tries full then compact, otherwise hides text that cannot fit. |
| Center Gutter | Separates bid and ask number columns. |
| Show Data Health | Coverage, source limitations, clipping and failure status; hover retains detailed evidence. |

Histogram uses a common denominator for both sides, with bid left/red and ask right/blue by default. Existing custom colors are respected. Cluster is explicitly labeled **Cluster - Delta Heatmap**, with fixed-width cells, total-volume intensity and strict delta hue. Histogram uses maximum opacity; Cluster uses its opacity range. Unclassified activity has a neutral mark. Known zeros render as `0`; entirely unclassified sides display `N/A`, with observed zero classified quantities and the unclassified total disclosed on hover. Missing accepted rows are never manufactured into zero-volume rows.

Fonts retain the selected family/weight. DirectWrite tabular figures are requested and digit widths checked outside rendering. Missing/non-tabular numeral fonts fall back to Consolas with disclosure; Figtree remains selectable. Measured width and height govern text visibility, including dynamic sizing. Single-value views use the right text column to keep the central OHLC spine clear of digits. Exact row bounds, values, quote-quality flags/age, scale denominator, POC/ties, session coverage, event time, sequence and revision remain available on hover.

## Numerical And Data Contracts

`Total = Bid + Ask + Unclassified`; strict `Delta = Ask - Bid`; `Delta % = 100 * Delta / Total` for positive total. The enhanced model consumes the existing accepted trade and classification result. An inferred tick-rule direction is never promoted to strict bid/ask. Repeated identical timestamp/price/size events are preserved. Negative tick prices use floor bucketing. No raw trade tape is retained.

The existing hidden Last Tick 1 series and TickReplayLastEvents source choices are unchanged. There are no added series. `Calculate.OnPriceChange`, secondary `OnBarUpdate`, and Last-event `OnMarketData` processing remain as before. Quote timestamps are observational metadata only; no stale-quote threshold or classifier correction is silently introduced. Valid-looking trade-associated quotes have unverified provider provenance. Secondary historical ticks cannot promise native bid/ask evidence or event-sequence fidelity.

Session identity comes from the chart's `SessionIterator.ActualTradingDayExchange`, not calendar midnight. The first loaded trading day is conservatively marked partial/coverage-unverified; subsequent days share their own developing denominators. A realtime connection loss latches a gap warning until reload; no missing events are invented. Reconnect does not deduplicate matching trades.

Per Bar uses each candle's maximum displayed side row. Visible Range uses the maximum across all price rows of horizontally visible candles. Session Global uses the maximum candle-side row across that trading day's available prefix, including offscreen candles, not cumulative session volume-at-price. Cluster uses the analogous maximum total-row volume. Fixed saturates geometry at the entered positive count without changing exact values; edge marks and health/hover disclose clipping. Session Global can rescale earlier footprints as data develops.

Shared profile registration/publication remains the existing inferred total/up/down contract. Strict evidence and provenance are private to CVP v1, not published as an upgraded shared-cache contract. No new Time Statistics source is introduced.

## Preparation, Rendering And Bounds

One chart-dispatcher observer samples the actual indicator scale, viewport, DPI and dimensions every 100 ms, even without trades. It coalesces one preparation worker per instance. Lifecycle and viewport generations reject obsolete output. Cancellation stops outstanding work on termination. Dirty raw bars are copied under short per-bar locks; aggregation, sorting, analysis POC, scale summaries and numeric text preparation happen outside `OnRender`. Completed unchanged bars are reused; active dirty bars are refreshed on the coalesced interval. Snapshots carry per-bar revisions/sequences; they are not a globally atomic exchange-tape snapshot during concurrent ingestion.

The enhanced render branch reads the prepared frame, projects/culls rows, draws cached DirectWrite layouts and RenderTarget-owned brushes, and records elapsed time through an atomic counter. No authored per-row managed object creation, dictionary aggregation, shared-cache registration or locks occur there. The legacy renderer remains unchanged behind the opt-in branch. Enhanced neutral scaffolding does not call the legacy absorption-color allocation path.

At most two compression variants are retained. Row snapshots cover visible bars plus one screen on each side; offscreen rows of those bars remain included in normalization. Summary/row cache accounting is limited to 16 MB, reserving the other half of the 32 MB derived budget for current/retiring presentation frames. Each presentation frame is capped at an estimated 8 MB; label fitting stops near 7 MB. Excessive viewports report N/A and retain source evidence. These are explicit allocation estimates and retention caps, not measured total process/native DirectWrite memory. Raw evidence, legacy maps and transient preparation are outside the derived-cache budget; actual native memory still needs profiling.

RenderTarget resources are disposed/recreated on target change. Layouts are frame-owned and retired after readers release them. Observers/tooltips detach and pending preparation is cancelled on termination. Diagnostics use existing OrcaDiagnosticsCore registration/source/series declarations, sampled observation work, preparation, model/cache health and render work-phase samples. Render samples are coalesced to the latest elapsed render per observer interval, not a complete frame trace.

## Validation Boundary

See `docs/handoffs/2026-08-30_cvp_footprint-v1-foundation-clarity.md` for exact local results and deployment status. NinjaTrader F5, saved-template round trips, replay equivalence, visual acceptance, native memory and full performance budgets are pending Julian's validation. No Full_Suite promotion is authorized.

The reported F5 failure generated `EqualsInput`/`CacheIndicator` calls for the settings converter, not CVP. The 2026-08-31 two-file corrective deployment removes that invalid wrapper and relocates the unchanged converter to the main indicator file. Authored and deployed-wrapper offline checks pass; fresh F5 generation and chart acceptance remain pending. See `docs/handoffs/2026-08-31_cvp_settings-converter-generation.md`.

Later imbalances, auctions, statistics integration, semantic zoom states, observations, levels, reset anchors and interpretation formulas remain unimplemented approval gates.
