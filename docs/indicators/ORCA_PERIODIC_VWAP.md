# Orca Periodic VWAP

## Purpose

`OrcaPeriodicVWAP` is a standalone NinjaTrader indicator that builds one independently resetting VWAP at a time. Windows are selected from clock-aligned periods or fixed Eastern-aligned market sessions. Completed windows remain confined to their own historical spans instead of becoming one continuous VWAP.

Source: `Orca Trades/Working_Suite/Indicators/OrcaPeriodicVWAP.cs`.

## Window Modes

### Periodic

- 5, 15, and 30 minutes and 1 hour align to ordinary clock boundaries.
- Four-hour windows use the trading-day anchor at 6:00 PM: `[6:00 PM, 10:00 PM)`, `[10:00 PM, 2:00 AM)`, `[2:00 AM, 6:00 AM)`, `[6:00 AM, 10:00 AM)`, `[10:00 AM, 2:00 PM)`, and `[2:00 PM, 6:00 PM)`.
- Daily windows use the same trading-day anchor and run `[6:00 PM, 6:00 PM)` on the following calendar day.
- Missing periods, weekends, and maintenance gaps do not create placeholder volume. The next valid trade maps directly to its currently aligned window.

### Session

`Overnight / RTH`:

- Overnight: `[6:00 PM, 9:30 AM)`
- RTH: `[9:30 AM, 5:00 PM)`

`Asia / London / RTH`:

- Asia: `[6:00 PM, 3:00 AM)`
- London: `[3:00 AM, 9:30 AM)`
- RTH: `[9:30 AM, 5:00 PM)`

The `[5:00 PM, 6:00 PM)` maintenance period is excluded in Session mode. Window timestamps follow Orca's established Eastern-aligned chart timestamp convention and use date-aware, half-open comparisons.

## Calculation And Data Source

The indicator adds one hidden `1 Tick` Last series and processes each valid trade incrementally:

```text
sum volume += volume
sum price-volume += price * volume
sum price-squared-volume += price * price * volume
VWAP = sum price-volume / sum volume
variance = max(0, sum price-squared-volume / sum volume - VWAP * VWAP)
standard deviation = sqrt(variance)
```

Zero, negative, NaN, or infinite volume and NaN or infinite price are ignored. The accumulator resets once when the resolved window token changes. Historical and realtime callbacks use the same resolver.

The existing shared order-flow provider is not used. Its normal one-second bucket can aggregate multiple trade prices and mark the bucket price unavailable, which cannot reproduce exact price-volume or price-squared-volume. The hidden Last series preserves trade timestamps and prices on time, tick, range, and volume charts and does not require Tick Replay.

Historical accuracy depends on the chart's Trading Hours coverage and available historical Last tick data. A session omitted by the selected Trading Hours template cannot be reconstructed by the indicator.

## Plots And Fills

Stable plot order:

1. VWAP
2. Deviation 1 Upper
3. Deviation 1 Lower
4. Deviation 2 Upper
5. Deviation 2 Lower
6. Deviation 3 Upper
7. Deviation 3 Lower

Each boundary invalidates a separating primary-bar sample. A range or volume bar that contains trades from both sides of a boundary belongs visually to the new window, while the latest older-window sample is cleared to prevent a diagonal connection.

Fills are separate upper and lower regions for VWAP-to-Dev1, Dev1-to-Dev2, and Dev2-to-Dev3. Every region tag contains the indicator-instance ID and window start. Regions are bounded to one window, and expired objects are removed only during boundary processing. All fill opacities default to zero.

Plot styling is exposed as seven clearly named `Stroke` settings rather than NinjaTrader's anonymous `[0]` through `[6]` plot-array entries. `VWAP Line` and each independent upper/lower deviation line retain the normal color, dash-style, and width editor. The raw `Plots`, `BarsPeriod`, `InputPlot`, and `SelectedValueSeries` descriptors are hidden from this indicator's settings grid. This is a presentation change only: plot order, plot names, calculations, and `Values` indexes remain unchanged.

`Show Historical Windows` defaults on. `Max Historical Windows` defaults to 100 and is enforced only when a new boundary is processed. Plot cleanup checks window ownership so pruning an old window cannot erase a newer sample that shares a non-time primary bar.

The indicator has no `OnRender` override. Calculation, collection mutation, pruning, and drawing-object maintenance occur in data callbacks rather than SharpDX rendering.

## Settings

- `1. Window Configuration`: Window Basis, conditional Periodic Interval, conditional Session Configuration.
- `2. VWAP and Bands`: Show VWAP, Show Deviation Bands, independent Dev1/2/3 toggles and multipliers.
- `3. Plot Styling`: named VWAP and independent upper/lower deviation `Stroke` controls.
- `4. Region Fills`: three XML-serializable brushes and opacity controls.
- `5. Display`: Show Historical Windows and conditional Max Historical Windows.

Plot-style controls are hidden when their corresponding VWAP or deviation line is disabled. The custom strokes persist with chart templates and workspaces and are reapplied to the same seven native plots during indicator configuration.

Periodic interval choices are 5 Minutes, 15 Minutes, 30 Minutes, 1 Hour, 4 Hours, and Daily. Defaults remain Periodic, 30 Minutes, Overnight/RTH, VWAP and all three deviations visible, deviation multipliers 1.0/2.0/3.0, fill opacities zero, historical windows visible, and a 100-window limit.

## Validation Status

Source, deployment, NinjaTrader F5 compilation, and manual chart validation are tracked as separate gates in `docs/handoffs/2026-09-01_orca-periodic-vwap_initial-implementation.md`. This component is not eligible for `Full_Suite` promotion until Julian confirms live chart behavior.
