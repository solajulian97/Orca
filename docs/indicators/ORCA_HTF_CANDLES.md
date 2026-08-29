# Orca HTF Candles

Last updated: 2026-08-25

## Component

- Display name: `Orca HTF Candles`
- Class: `OrcaHTFCandles`
- Active source: `Orca Trades/Working_Suite/Indicators/OrcaHTFCandles.cs`
- Status: Working_Suite implementation with Eastern session options; NinjaTrader runtime validation pending

## Pine Reference Behavior

The supplied Pine Script makes five `request.security()` requests for the selected timeframe: open, high, low, close, and the HTF bar's opening timestamp. `barmerge.gaps_off` carries the latest requested value across chart bars. `barmerge.lookahead_off` does not leak a completed HTF candle's final OHLC into earlier historical chart bars.

On historical chart bars, the requested HTF value becomes available at the end of the HTF interval. The script then creates the box retroactively from the HTF opening timestamp. On realtime bars, the same requests expose the developing HTF open/high/low/close, so the current body, wick, and direction can change until the HTF bar closes.

The Pine comment `pas de repaint` is imprecise. A developing higher-timeframe request without a one-bar expression offset can change during realtime and TradingView classifies that historical/realtime difference as repainting. The script does not use future information or `lookahead_on`, however, and the retained object for a completed candle ends with that candle's final OHLC. The expected visual behavior is therefore a changing active candle and stable completed objects.

Pine horizontal geometry is based on time rather than elapsed chart bars:

- Left edge: HTF opening timestamp `htf_t`.
- Right edge: `htf_t + timeframe.in_seconds(htf)`.
- Wick X: the time midpoint between those two boundaries.
- The developing body is projected through the full nominal HTF interval immediately.
- TradingView controls the HTF aggregation/session alignment that produces `htf_t`. The script itself does not create a custom session anchor.
- Because the right edge is a fixed nominal duration, a shortened session bar can be drawn past its actual trading end in Pine.

`Candle Lookback` is applied to the arrays of frozen objects. With a value of 200, Pine retains up to 200 completed bodies and wick pairs plus the separate current object.

Only the body fill receives `Transparency`. The border and both wick colors remain at their selected brush opacity. `close >= open` is bullish, so a doji uses the bull body and bull wick.

## NinjaTrader Data Architecture

The indicator adds exactly one same-instrument secondary series selected during `State.Configure`:

| Setting | Added series |
| --- | --- |
| 5 Minutes | Minute 5 |
| 15 Minutes | Minute 15 |
| 30 Minutes | Minute 30 |
| 1 Hour | Minute 60 |
| 2 Hours | Minute 120 |
| 4 Hours | Minute 240 |
| 1 Day | Day 1 |
| Weekly | Minute 30, aggregated Sunday 18:00 through Friday 17:00 Eastern |
| ETH / RTH | Minute 30, aggregated into 18:00-09:30 and 09:30-17:00 Eastern candles |
| Asia / London / New York | Minute 30, aggregated into 18:00-03:00, 03:00-09:30, and 09:30-17:00 Eastern candles |

`BarsInProgress == 1` is the only calculation path. Fixed timeframes use the secondary bar's native OHLC directly. Weekly and session-split modes aggregate native 30-minute secondary OHLC; this preserves exact open/high/low/close for boundaries that all align to 30 minutes without adding a Tick series. OHLC is never reconstructed from the primary chart. `Calculate.OnPriceChange` is sufficient because this version has no volume-dependent behavior. Tick Replay is not required.

For fixed timeframes, the latest secondary index is stored as the active candle. For custom modes, each 30-minute source bar updates the active scheduled window, and a changed window freezes the prior aggregate. Completed render data is published as an immutable array only at candle transitions, in bounded historical batches, and at `State.Transition`. The active candle is copied under a short lock. `OnRender` never traverses the mutable queue.

If the selected timeframe is equal to or lower than a time-based primary chart period, the indicator leaves the chart unchanged without throwing or printing. Tick, volume, range, and other non-time primary bars continue to use the selected HTF series because they have no fixed period duration that can be compared safely.

## Time And Session Boundaries

NinjaTrader intraday time bars are close-stamped. For Minute-based HTF series:

- The secondary bar timestamp is the scheduled/actual bar end.
- The start is the previous secondary close when it belongs to the same trading session.
- The first HTF bar after a session break starts at `SessionIterator`'s trading-day begin, not at the prior session's close.
- NinjaTrader's shortened final bar timestamp is retained, so an early-close candle stops at the actual native bar end rather than extending by a fixed number of minutes.

Daily bars use the selected Trading Hours trading-day begin and an explicit 17:00 Eastern close. With an ETH trading-day begin at 18:00 Eastern, the candle spans the evening open through the following day's 17:00 close. The native Day 1 secondary OHLC remains the source of the candle values.

The three custom modes use Eastern market time with daylight-saving transitions (`Eastern Standard Time` is the Windows timezone identifier, not a fixed UTC-5 offset):

- `Weekly`: Sunday 18:00 through Friday 17:00.
- `ETH / RTH`: overnight 18:00-09:30, then RTH 09:30-17:00.
- `Asia / London / New York`: Asia 18:00-03:00, London 03:00-09:30, then New York/RTH 09:30-17:00.
- The 17:00-18:00 maintenance interval is excluded.
- Overnight/Asia starts are allowed Sunday through Thursday only. RTH, London, and New York windows are allowed Monday through Friday only.
- Scheduled Eastern boundaries are converted to NinjaTrader's configured chart timezone before rendering.

The custom windows are drawn across their complete scheduled interval as soon as their first native 30-minute source bar begins. Their OHLC then develops from the source bars received inside that window. A prior window is frozen when the first source bar of the next valid window arrives.

Logical boundaries are mapped to the first primary bar whose timestamp is strictly later than the boundary. This matches NinjaTrader's close-stamped convention: on a five-minute chart, an hourly boundary at 05:00 maps to the 05:05 close-stamped bar that represents the 05:00-05:05 interval. Completed candles cache these primary indices. The active future edge falls back to `ChartControl.GetXByTime()` until a qualifying primary bar exists.

On tick, volume, and range charts, the same rule maps each boundary to the first bar that closes after it. A future time has no guaranteed bar slot on a non-time chart, so NinjaTrader's time-to-X projection controls how far the active right edge can be shown before additional primary bars exist.

## Rendering

- Overlay, price panel, no plots, no price markers, and no autoscale contribution.
- Z-order `-1000` places the entire HTF drawing behind primary price bars.
- SharpDX body fill, body border, and two wick segments.
- Wick position uses the time midpoint, matching the Pine calculation rather than averaging clipped screen coordinates.
- A doji receives a one-pixel visible body while its wick still terminates at the exact open/close price.
- The render pass clips to the chart panel, so partially visible bodies retain their true off-screen border geometry.
- Completed candles use cached primary indices; only unresolved/current time boundaries require binary-search/time projection during rendering.
- Direct2D brushes are cached per render target and disposed on render-target change and termination.
- When `Use Directional Borders` is enabled, bullish and bearish bodies use the separate `Bull Border` and `Bear Border` brushes; otherwise all bodies use the common `Border` brush.
- `1 Day` candles show their Monday-through-Friday trading-day name centered along the panel bottom.
- `Asia / London / New York` candles show `Asia`, `London`, or `RTH` centered along the panel bottom.
- Bottom labels use each candle's logical start/end coordinates and are omitted when that logical center is outside the viewport.
- Label brush and DirectWrite format resources are cached per render target and disposed with the candle resources.
- No LINQ, drawing objects, persistent tags, model mutation, cache access, or Output-window logging occurs in `OnRender`.

## Settings And Defaults

Defaults match the supplied settings/appearance screenshot:

- Timeframe: 1 Hour
- Timeframe options: 5 Minutes, 15 Minutes, 30 Minutes, 1 Hour, 2 Hours, 4 Hours, 1 Day, Weekly, ETH / RTH, and Asia / London / New York
- Candle Lookback: 200 completed candles plus the active candle
- Bull Body: RGB `76,175,80` (`#4CAF50`)
- Bear Body: RGB `255,82,82` (`#FF5252`)
- Border: RGB `46,46,46` (`#2E2E2E`)
- Use Directional Borders: disabled (backward-compatible common border behavior)
- Bull Border: RGB `76,175,80` (`#4CAF50`)
- Bear Border: RGB `255,82,82` (`#FF5252`)
- Bull Wick: RGB `46,46,46` (`#2E2E2E`)
- Bear Wick: RGB `46,46,46` (`#2E2E2E`)
- Transparency: 85, converted to Direct2D opacity `0.15`
- Border Width: 1
- Wick Width: 1

Brushes use public `[XmlIgnore]` properties and hidden `Serialize.BrushToString`/`Serialize.StringToBrush` properties.

For split modes, Candle Lookback counts individual session candles rather than trading days. For example, `ETH / RTH` produces up to two candles per trading day and `Asia / London / New York` produces up to three.

## TradingView Differences And Limitations

- Intraday shortened bars stop at NinjaTrader's actual native close rather than Pine's unconditional nominal-duration right edge.
- `1 Day` candle geometry now closes at 17:00 Eastern even when the chart's display timezone differs; the underlying native Day 1 OHLC still depends on the selected Trading Hours template and provider.
- Daily OHLC availability can depend on the data provider and selected Trading Hours template.
- Weekly and session-split modes can aggregate only the 30-minute bars made available by the chart's Trading Hours template. An RTH-only template cannot supply overnight/Asia/London OHLC, and missing provider history cannot be reconstructed.
- Holiday and early-close windows retain the requested scheduled right edge even when the final available 30-minute source bar ends earlier.
- A non-time chart has no deterministic future bar spacing. Active future projection is therefore limited by NinjaTrader's `GetXByTime()` behavior until new tick/range/volume bars exist.
- Historical bars are derived from confirmed native secondary OHLC. The active bar develops in realtime, but completed snapshots are not revised by primary-chart reconstruction.
- Loaded history controls availability. A lookback larger than the available secondary data shows only the candles NinjaTrader loaded.

## Validation Status

- Initial and 2026-08-22 session-extension isolated semantic compiles against the installed NinjaTrader, WPF, and SharpDX assemblies: passed with zero C# errors.
- Fourteen exact-source boundary assertions passed for Weekly, ETH/RTH, Asia, London, New York, the 17:00 maintenance boundary, Friday-night exclusion, and winter/summer Eastern offsets.
- Targeted Working_Suite deployment: passed; the normalized authored region has source/live SHA-256 parity at `018F814ADA53F5AAF55A84B60E54245DC1182E869EC99D3E9B44A579334220D`. NinjaTrader appended its generated-code region only to the live copy.
- Prior NinjaTrader assembly generation completed, but Windows Smart App Control blocked NinjaTrader from loading the unsigned temporary Custom assembly. The platform compile/load gate therefore remains failed/pending even though no C# compiler diagnostic was recorded.
- The 2026-08-22 session extension was targeted-deployed at 20:29 local time. The live authored region matches `Working_Suite` at SHA-256 `9BD0113627DA4477E515E8DD93005D4F91F2970D89F61C33A440CAEBD8CDCC5E`, NinjaTrader appended its generated-code region, and `NinjaTrader.Custom.dll` regenerated at 20:29:06.
- No new Code Integrity 3033/3077 event appeared after this deployment. That does not independently prove a successful Custom-assembly load.
- Explicit NinjaTrader F5: not sent; Windows app inspection/control was not approved for NinjaTrader.
- Historical rendering: pending.
- Market Replay: pending.
- Live behavior: pending Julian validation.
- Full_Suite promotion: not eligible.
