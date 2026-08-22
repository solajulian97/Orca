# Orca HTF Candles

Last updated: 2026-08-21

## Component

- Display name: `Orca HTF Candles`
- Class: `OrcaHTFCandles`
- Active source: `Orca Trades/Working_Suite/Indicators/OrcaHTFCandles.cs`
- Status: initial Working_Suite implementation; NinjaTrader runtime validation pending

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
| 1 Week | Week 1 |

`BarsInProgress == 1` is the only calculation path. OHLC comes directly from that native secondary series and is never reconstructed from the primary chart. `Calculate.OnPriceChange` is sufficient because this version has no volume-dependent behavior. Tick Replay is not required.

The latest secondary index is stored as the active candle. When the index advances, the former active candle is moved to a bounded completed-candle queue. Completed render data is published as an immutable array only at HTF transitions, in bounded historical batches, and at `State.Transition`. The active candle is copied under a short lock. `OnRender` never traverses the mutable queue.

If the selected timeframe is equal to or lower than a time-based primary chart period, the indicator leaves the chart unchanged without throwing or printing. Tick, volume, range, and other non-time primary bars continue to use the selected HTF series because they have no fixed period duration that can be compared safely.

## Time And Session Boundaries

NinjaTrader intraday time bars are close-stamped. For Minute-based HTF series:

- The secondary bar timestamp is the scheduled/actual bar end.
- The start is the previous secondary close when it belongs to the same trading session.
- The first HTF bar after a session break starts at `SessionIterator`'s trading-day begin, not at the prior session's close.
- NinjaTrader's shortened final bar timestamp is retained, so an early-close candle stops at the actual native bar end rather than extending by a fixed number of minutes.

Daily bars use the selected Trading Hours trading-day begin and a one-day scheduled interval. Weekly bars use the first defined trading-day begin in the native weekly bar's calendar week and the next week's first defined trading-day begin. These rules keep the overlay session-aligned while preserving a scheduled future edge for the active candle.

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
- No LINQ, drawing objects, persistent tags, model mutation, cache access, or Output-window logging occurs in `OnRender`.

## Settings And Defaults

Defaults match the supplied settings/appearance screenshot:

- Timeframe: 1 Hour
- Candle Lookback: 200 completed candles plus the active candle
- Bull Body: RGB `76,175,80` (`#4CAF50`)
- Bear Body: RGB `255,82,82` (`#FF5252`)
- Border: RGB `46,46,46` (`#2E2E2E`)
- Bull Wick: RGB `46,46,46` (`#2E2E2E`)
- Bear Wick: RGB `46,46,46` (`#2E2E2E`)
- Transparency: 85, converted to Direct2D opacity `0.15`
- Border Width: 1
- Wick Width: 1

Brushes use public `[XmlIgnore]` properties and hidden `Serialize.BrushToString`/`Serialize.StringToBrush` properties.

## TradingView Differences And Limitations

- Intraday shortened bars stop at NinjaTrader's actual native close rather than Pine's unconditional nominal-duration right edge.
- Daily and weekly OHLC availability can depend on the data provider. NinjaTrader native Day/Week bars may not reproduce every custom Trading Hours template exactly even though the display boundaries use the secondary series' `SessionIterator`.
- A non-time chart has no deterministic future bar spacing. Active future projection is therefore limited by NinjaTrader's `GetXByTime()` behavior until new tick/range/volume bars exist.
- Historical bars are derived from confirmed native secondary OHLC. The active bar develops in realtime, but completed snapshots are not revised by primary-chart reconstruction.
- Loaded history controls availability. A lookback larger than the available secondary data shows only the candles NinjaTrader loaded.

## Validation Status

- Isolated semantic compile against the installed NinjaTrader, WPF, and SharpDX assemblies: passed with zero C# errors.
- Targeted Working_Suite deployment: passed; the normalized authored region has source/live SHA-256 parity at `018F814ADA53F5AAF55A84B60E54245DC1182E869EC99D3E9B44A579334220D`. NinjaTrader appended its generated-code region only to the live copy.
- NinjaTrader automatic source-watcher compile: passed. `NinjaTrader.Custom.csproj` includes `Indicators\OrcaHTFCandles.cs`, and the Custom DLL/PDB regenerated at 2026-08-21 23:16:03 local time after the final live-source write at 23:15:59.
- Explicit NinjaTrader F5: not sent because Windows app control was not approved for NinjaTrader.
- Historical rendering: pending.
- Market Replay: pending.
- Live behavior: pending Julian validation.
- Full_Suite promotion: not eligible.
