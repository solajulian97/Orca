# 2026-07-21 CVP Bid x Ask Footprint

## Objective

Add a true Bid x Ask display to `OrcaCandleVolumeProfile` with Cluster and Histogram render styles, while organizing the profile-selection settings and preserving existing chart templates.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaCandleVolumeProfile.cs`
- `docs/indicators/ORCA_SOURCE_MAP.md`
- `docs/handoffs/2026-07-21_cvp_bid-ask-footprint.md`

## Behavior added, changed, or removed

- Added one exclusive `Profile Display` choice: Off, Volume, Delta, Volume + Delta, or Bid x Ask.
- Added strict per-price Ask, Bid, and unclassified maps for each candle.
- Trades at or through the ask populate Ask; trades at or through the bid populate Bid.
- Inside-spread trades and trades without usable quotes remain available to the existing inferred Delta path, but are not mislabeled as Bid or Ask.
- Cluster style draws fixed-width cells. Cell hue follows strict row delta and cell intensity follows total row volume.
- Histogram style draws strict bid-side executions left and strict ask-side executions right on one per-candle scale.
- Unclassified volume receives a neutral treatment instead of being silently assigned to a side.
- Bid x Ask rows use the existing order-flow row aggregation controls.
- POC is outlined on the highest-total Bid x Ask row when `Show POC` is enabled.
- A thin candle spine is redrawn over centered footprint cells.
- Existing Volume, Delta, Volume + Delta, delta-only right-facing layout, volume text, delta text, and shared-cache publication paths remain intact.
- Value-area color and boundary rendering remain part of the Volume profile path; Bid x Ask does not add separate value-area shading.

## User-facing settings added, changed, deprecated, or removed

Added:

- `Profile Display`
- `Bid/Ask Style`
- `Bid x Ask Width (px)`
- `Show Bid x Ask Text`
- `Bid x Ask Text Min Threshold`
- `Bid x Ask Text Font Size`
- `Bid x Ask Text Color`
- `Bid x Ask Positive`
- `Bid x Ask Negative`
- `Bid x Ask Neutral`
- `Bid x Ask Min Opacity`
- `Bid x Ask Max Opacity`

Renamed for clarity without changing their internal property names:

- Volume and order-flow row-size/dynamic aggregation labels
- Volume profile width and Volume + Delta arrangement/scale labels
- Profile row spacing
- Shared text-family, text-weight, and dynamic-size descriptions now include Bid x Ask

Deprecated from the visible property grid:

- `Show Volume Profile`
- `Show Delta Profile`

Those two properties remain serialized and are resolved once during Configure so older templates retain their previous Volume/Delta visibility.

## Secondary series added or changed

None. No Tick, Second, Bid, Ask, Last, Volumetric, or custom series was added or removed.

`SecondaryTickSeries` still adds the existing hidden Tick 1 series. `TickReplayLastEvents` still adds no secondary series and processes replayed Last events.

## Tick Replay implications

- Historical strict Bid x Ask requires quote-bearing Last events. Use `Trade Source Mode = Tick Replay Last Events` on a chart with Tick Replay enabled.
- The existing Tick Replay Last-events volume and delta path is unchanged apart from recording strict Bid/Ask/unclassified maps alongside it.
- Tick-direction fallback still supports Delta but never populates strict Bid or Ask.

## Historical-load implications

- Bid x Ask instances maintain three additional sparse dictionaries per hydrated primary bar for strict Ask, strict Bid, and unclassified volume. Other display modes do not allocate those per-bar maps.
- Secondary Tick Series can rebuild historical Volume quickly, but historical quote context may be unavailable; Bid x Ask can therefore appear neutral or unclassified in that mode.
- No historical rebuild was added to `OnRender` or any per-frame path.

## Cache implications

- Shared `OrcaProfileDataCache` publication is unchanged and still publishes total/up/down maps when enabled.
- Strict Bid/Ask maps are instance-local and are not published to the shared cache in this change.
- No cache key, source-selection, persistence, or richer-source arbitration logic changed.

## Rendering implications

- Bid x Ask is exclusive from the side-by-side Volume/Delta display.
- Cluster cells are centered on the candle and capped by neighboring bar spacing.
- Histogram sides share one per-candle maximum so left and right widths are comparable.
- Direct2D intensity palettes and the Bid x Ask text brush are cached and disposed with the existing render-target lifecycle.
- Render-time work copies only the selected bar's finished maps under the existing short lock; no data mutation or synchronization wait was added to `OnRender`.

## Performance implications

- No additional hidden data series or market-data subscription was added.
- Tick processing adds up to one strict Bid/Ask/unclassified dictionary update per trade only while Bid x Ask is the selected display.
- Bid x Ask rendering allocates three map snapshots plus aggregate/price collections for each visible candle while that display is active, consistent with the indicator's existing snapshot rendering pattern.
- Dense Tick Replay history and very large visible bar counts should be observed in Orca Diagnostics before considering reusable render buffers.

## Tests performed in NinjaTrader

- Targeted deployment completed for `OrcaCandleVolumeProfile.cs` only.
- Working_Suite and live authored regions match after newline normalization: SHA256 `A6CA18C73880488BA447F427D9BFE5686A32A5932148A8AFB7ECC2A408549781`.
- NinjaTrader regenerated the live generated-code wrapper to include the new NinjaScript properties; the live file still contains exactly one generated-code region.
- Standalone Roslyn syntax parsing passed for both Working_Suite source and the deployed live file.
- Git whitespace validation passed for the source and source-map changes.
- NinjaTrader `F5` compile and live chart behavior testing remain pending Julian.

## Compile status

- Source syntax: passed.
- Deployed live-file syntax including the regenerated wrapper: passed.
- NinjaTrader semantic `F5` compile: pending Julian.

## Manual-validation status

Pending. Validate both styles on a Tick Replay chart with `Trade Source Mode = Tick Replay Last Events`:

- Cluster shows `Bid x Ask`, blue positive rows, red negative rows, and stronger opacity on higher-total rows.
- Histogram shows sells left and buys right with comparable widths.
- Neutral rows appear only where quote classification was unavailable or the row delta is exactly balanced.
- Reloaded historical values remain stable against live values.
- Volume, Delta, and Volume + Delta displays retain their prior behavior and old templates reopen with the same display selection.

## Known issues, risks, and follow-up work

- Secondary Tick Series may not provide enough historical quote context for a strict footprint; this is surfaced in the Trade Source Mode description.
- Bid x Ask value-area shading was intentionally not added; only the POC outline is shared with this display.
- The settings grid does not dynamically hide controls that are irrelevant to the selected display mode.
- Live compile and visual validation may expose platform-specific DirectWrite or generated-wrapper issues that standalone syntax parsing cannot detect.

## Full_Suite promotion eligibility

Not eligible. Promotion requires a successful NinjaTrader `F5` compile and Julian's manual approval of Cluster, Histogram, historical reload stability, and the legacy Volume/Delta displays.
