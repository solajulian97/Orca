# Orca Candle Volume Profile

## Instant Replay

`OrcaCandleVolumeProfile` supports completed-bar Orca Instant Replay. Its existing completed historical profile maps remain untouched while the coordinator's high-Z price-panel mask reveals bars one at a time. Developing tick replay is intentionally unavailable when this indicator is visible until a separate replay-only footprint model is implemented; this prevents the completed bar's final volume/delta profile from leaking into an earlier tick prefix. The adapter adds no secondary series, cache reads, or normal per-tick work.

Updated: 2026-09-12. Working_Suite development source only.

## Settings Organization

The current settings layout supersedes the original enhanced-only grouping below: **01 Display**, **02 Profile Layout**, **03 Candles**, **04 Rows & Scaling**, **05 Profile Colors**, **06 POC & Value Area**, **07 Text - General**, **08 Text - Volume**, **09 Text - Delta**, **10 Text - Bid x Ask**, and **11 Advanced - Data**. All 93 existing displayed properties retain their identities and have distinct positions. Toggles precede related controls, POC color sits alongside Show POC, and each text family has its own group.

Text groups appear in their applicable modes. Bid x Ask style, width and enhancement appear in Bid x Ask; combined arrangement appears in Volume + Delta. Existing enhanced applicability and Volume-only emphasis / combined-mode brightness conditions remain. Hidden settings retain their saved values. Native NinjaTrader properties remain platform-managed.

Labels spell out value area, clarify text thresholds, and use Auto-Size Text, Bid/Ask Column Gap and Minimum Dominance Ratio. The description summarizes per-candle volume/delta, display choices, POC, value area and appearance controls. Property names, enums, defaults, ranges, brush serialization and factory signatures are unchanged. See `docs/handoffs/2026-09-07_cvp_settings-organization.md` for verification and deployment status.

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
| Candle Display | Off, OHLC Spine, Full Candle, Hollow Body Delta. The default narrow spine preserves prior Bid x Ask appearance. Full Candle reserves Candle Width; Hollow Body Delta reserves a narrow full-height center read for signed strict delta. |
| POC Outline | Existing ShowPOC setting; neutral outline in enhanced mode. Lowest-price tie retained, tie count on hover. Developing POC is provisional. |
| Cell Values | Bid x Ask, Total Volume, Strict Delta, Strict Delta %. |
| Number Format | Full, Compact, Auto. Auto tries full then compact, otherwise hides text that cannot fit. |
| Center Gutter | Separates bid and ask number columns. |
| Show Data Health | Coverage, source limitations, clipping and failure status; hover retains detailed evidence. |
| Emphasize Row Winner | Uses a heavier font weight for the same-row Bid or Ask that meets Winner Ratio in enhanced and normal Bid x Ask modes. |
| Winner Ratio | Dominant side divided by opposing side; default 1.5. An unopposed positive side qualifies, while 0 x 0 does not. |

Histogram uses a common denominator for both sides, with bid left/red and ask right/blue by default. Existing custom colors are respected. Cluster is explicitly labeled **Cluster - Delta Heatmap**, with fixed-width cells, total-volume intensity and strict delta hue. Histogram uses maximum opacity; Cluster uses its opacity range. Unclassified activity has a neutral mark. Known zeros render as `0`; entirely unclassified sides display `N/A`, with observed zero classified quantities and the unclassified total disclosed on hover. Missing accepted rows are never manufactured into zero-volume rows.

Fonts retain the selected family/weight. DirectWrite tabular figures are requested and digit widths checked outside rendering. Missing/non-tabular numeral fonts fall back to Consolas with disclosure; Figtree remains selectable. Measured width governs text visibility; row height receives the same two-pixel vertical allowance as the established Bid x Ask renderer so practical 8-point labels do not disappear just before the next zoom step. Single-value views use the right text column to keep the central OHLC spine clear of digits.

The row hover is intentionally concise: bar/price bounds, Bid x Ask, total, strict delta and delta percentage, POC, and a qualifying winner. It adds only actionable exceptions for unclassified volume, unavailable sides, clipping or preparation failure. Session identity, font fallback, event/sequence/revision, quality enums, quote age and hidden-text narration remain retained in prepared evidence where applicable but are not repeated in every row tooltip. `Show Data Health` now defaults off for new instances; existing saved values remain intact.

## Volume Row Delta Emphasis

In `Volume + Delta`, `Scale Volume Text Brightness` optionally gives the white volume column its own per-candle hierarchy. The largest displayed volume row in each candle uses the full configured `Volume Text Color`; smaller rows fade toward `Volume Text Minimum Brightness`, which defaults to 35%. A square-root curve keeps lower-volume labels readable instead of making them nearly disappear. Scaling uses the same aggregated rows already rendered at the current compression and does not change the volume bars, delta labels, calculations, or data collection. The feature defaults off and is contextual to `Volume + Delta`.

When `Profile Display` is `Volume`, `Emphasize Volume Rows by Delta` can highlight meaningful directional rows while leaving every volume number in the configured `Volume Text Color`. The setting defaults off. A row qualifies only when it meets both `Minimum Absolute Delta` and `Minimum Delta %`; defaults are 150 and 15%. Qualifying positive/negative profile bars use the existing Positive Delta or Negative Delta hue. POC retains its existing fill priority when it is also a qualifying row.

The dual gate prevents tiny 100%-delta rows from dominating and prevents high-volume range rows from qualifying on accumulated absolute delta alone. Delta and volume are aggregated into the same displayed row before qualification. Exact-zero, unavailable and nonqualifying rows retain the ordinary volume-profile fill selected by gradient/value-area settings. `Volume Text Min Threshold` remains the separate noise filter for small labels. The feature is inactive in Delta, Volume + Delta, Bid x Ask and Off modes, and it is independent from `Color Volume By Delta`, which continues to color all volume bars by delta. This is presentation-only and uses the established inferred CVP delta; it does not promote that value to strict Bid x Ask evidence. The former continuous percentage-intensity and qualified-text-weight properties remain serialized but hidden for template compatibility.

Hovering within a qualified Volume row shows two compact unlabeled lines: `Bid x Ask` values first, then strict `Delta = Ask - Bid`. Unclassified volume is intentionally omitted. When neither strict side is available, the tooltip displays `N/A x N/A` and `N/A` instead of manufactured values. Strict hover evidence is retained only while this feature is enabled. Enabling it on existing historical bars therefore requires the normal NinjaScript reload so those rows can be rebuilt from the selected trade source.

Winner emphasis also applies when Enhanced Footprint is off and Profile Display is Bid x Ask. Histogram preserves its bid-left/ask-right columns. Cluster preserves the `Bid x Ask` reading order while weighting only the qualifying side. The same settings and strict same-row ratio are shared across both renderers; Volume, Delta and combined modes remain unaffected.

`Candle Display` also applies to both Bid x Ask renderers. `Full Candle` uses the configured bullish/bearish body colors and Candle Width, with Bid shifted left and Ask shifted right. Normal Histogram and Cluster leave the center lane open and redraw the actual candle in the foreground. Enhanced Histogram and Cluster reserve the same center lane in their prepared geometry and render the full OHLC candle there. POC outlines split around that lane so they do not cover the candle.

`Hollow Body Delta` is the opt-in quick-read alternative. It reserves a narrow center column through every populated high-to-low footprint row; no wick or OHLC spine is drawn through the delta labels. Every row shows centered strict `Ask - Bid` and receives a faint neutral separator beneath the value. One continuous transparent outline spans the open-to-close body using the configured bullish or bearish body color; individual body rows are not boxed. Positive, negative and zero/unknown delta labels reuse Bid x Ask Positive, Negative and Neutral colors. Opacity is normalized across the candle's displayed rows from Bid x Ask Min Opacity to Max Opacity, so the strongest absolute row delta is brightest.

The center width is measured independently for each candle from its widest displayed delta label, with two pixels of padding on each side. Small values therefore keep a compact center lane while an occasional four-digit value expands only its own candle. Candle Width remains the user-selected minimum, Candle/Profile Gap remains outside the column, and Auto number formatting compacts values of five digits or more before measurement.

The center-delta label uses the Bid x Ask font, font size and number-format fallback, but it does not depend on `Show Bid x Ask Text` or the side-text minimum-volume threshold. This allows the left/right Bid and Ask numbers to be hidden while retaining every available center delta. When strict Bid/Ask is unavailable for a row, the center displays `N/A`; no inferred delta is substituted. Both normal and Enhanced Bid x Ask split POC outlines around the reserved center column.

## Enhanced Versus Normal Bid x Ask

The two Histogram styles are intentionally visually related; enhanced mode is not meant to advertise itself through decoration. Their operational differences are:

| Capability | Normal Bid x Ask | Enhanced Footprint |
| --- | --- | --- |
| Evidence owner | Existing per-bar strict Bid/Ask/unclassified maps | Typed integer-tick evidence book and immutable revisions |
| Row calculation | Display compression is also the rendered calculation row | Fixed analysis rows are independent from display/zoom rows |
| Normalization | Per-candle maximum | Per Bar, Visible Range, developing Session Global, or Fixed |
| Preparation | Copies/aggregates/sorts legacy maps during the existing render path | Aggregates and prepares bounded snapshots outside `OnRender` |
| Missing/unknown handling | Basic unclassified rendering | Explicit zero, unavailable, unclassified, clipping and coverage states |
| POC inspection | Visual POC outline | Fixed-row POC, tie count, provisional state and concise row hover |
| Technical status | None | Optional, default off for new instances |

Shared controls now include Bid x Ask style/colors, text/font, winner emphasis/ratio, width, row display settings, POC and Candle Display. Keep enhanced off when the normal footprint is sufficient; enable it when fixed analysis rows, cross-bar/session scaling, hover reconciliation or snapshot-based preparation matter.

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
