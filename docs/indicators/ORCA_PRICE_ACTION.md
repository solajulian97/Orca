# Orca Price Action

Last updated: 2026-08-17

## Status

`OrcaPriceAction.cs` is active development in
`Orca Trades/Working_Suite/Indicators`.

The source passes a local .NET Framework semantic compile against the installed
NinjaTrader assemblies and the current Working_Suite diagnostics source.
NinjaTrader `F5` compilation and live chart behavior are not yet confirmed.
The indicator is not eligible for `Full_Suite`.

The 2026-08-17 historical-load fast path defers immutable snapshot publication
until the historical boundary, skips developing-bar FVG work during historical
processing, and republishes realtime snapshots only for a completed-bar model
change or an actual intrabar FVG geometry change. Terminal collections are
sorted only when their removable record count exceeds the configured cap.

## Product intent

Orca Price Action combines causal price-action context in one overlay:

- standard FVG and inverted FVG;
- classic and advanced volume imbalance;
- close-confirmed HH/LH/HL/LL structure, BOS, CHoCH/MSS, liquidity sweeps,
  protected pivots, and role changes;
- pivot-qualified rejection blocks;
- structural order blocks, continuation order blocks, and parent-linked
  propulsion blocks.

All engines consume primary-chart OHLC only. The indicator adds no hidden data
series, does not use `OnMarketData`, and has no order-flow, Tick Replay, or
profile-cache dependency.

## Calculation contract

The indicator uses `Calculate.OnPriceChange`, but completed bars own all causal
state changes:

1. The first callback of a new primary bar processes the just-closed bar once.
2. FVG/VI creation, pivot confirmation, BOS/CHoCH, sweep and rejection
   confirmation, block creation, mitigation, and invalidation use that completed
   bar.
3. Developing-bar highs and lows may shrink only an existing FVG's remaining
   geometry. Completion and inversion state are committed on the closed bar.
4. Strength-N pivots are unavailable until N right-side bars have closed.
5. Pivot labels are placed at the confirmation event. Later Sponsor, Protected,
   Major, and Unprotected changes receive their own effective event bar rather
   than rewriting the original confirmation state.

This gives identical structural decisions for the same completed OHLC sequence
regardless of how many price-change callbacks occur inside a bar.

## Fair value gaps

A bullish FVG requires the third candle's low to be strictly above the first
candle's high. A bearish FVG uses the inverse. Optional point-size,
middle-candle direction, and ATR-normalized middle-body displacement filters run
before the FVG can claim a timed slot.

The model retains original and remaining bounds. `RemainingOnly` shrinks the
displayed zone. `TwoTone` renders the traded portion with lower opacity and the
remaining portion with active opacity. A strict close through the original far
edge can create an opposite iFVG over the original range.

Timed FVGs use a DST-aware New York clock. The first qualifying FVG of either
direction claims a 15-minute, 30-minute, 1-hour, or 4-hour bucket. Four-hour
buckets begin at 00:00, 04:00, 08:00, 12:00, 16:00, and 20:00. The separate RTH
feature accepts timestamps strictly after the configured 09:30 open and before
the configured 16:00 close. Period-end rendering never extrapolates a synthetic
future bar on non-time charts.

## Structure and rejection blocks

Structure breaks require a strict close beyond the pivot plus the configured
tick buffer. Wick-only violations are sweeps. Bullish BOS promotes the latest
eligible confirmed low to Sponsor, Protected, External, and Major; bearish
behavior mirrors this. Only a close through the protected boundary creates
CHoCH/MSS.

Each pivot stores independent Scope, Function, Protection, Target, and
Importance dimensions plus its ATR-normalized confirmation reversal. Equal
price clusters keep the earliest pivot as the canonical level.

Rejection blocks require an eligible pivot sweep, a close back inside, and the
configured wick ratio. Follow-through mode confirms on a later close through
the rejection candle's opposite extreme within the window. The rejection zone
is the swept wick from the extreme to the nearest body edge.

## Order-block types

- `S-OB`: the nearest opposing candle before a close-confirmed external BOS or
  CHoCH impulse.
- `C-OB`: the nearest opposing candle before an aligned internal break or a
  sufficiently displaced continuation extension.
- `PB`: an opposing candle formed while price overlaps a still-valid S-OB or
  C-OB, followed by displacement away in the parent's direction.

Displacement is normalized by ATR at the source candle. Direction-matched FVG
confluence must occur inside the same origin-to-confirmation impulse. The
Standard, Strict, Broad, and Custom presets control the thresholds, scope,
confluence requirement, and weak break-only allowance.

Block display geometry does not change lifecycle logic. The source body is the
canonical touch area, a close through the source open is mitigation, and the
full-candle distal extreme controls invalidation. Display can use body, full
range, or open/body-midpoint lines.

## Rendering and performance

Calculation code publishes immutable arrays of zone, line, and label geometry.
`OnRender` reads only those arrays and performs drawing; it does not traverse
calculation collections, query caches, rebuild history, or wait on locks.
SharpDX brushes, stroke styles, and text formats are recreated when the render
target changes and disposed at termination.

The default historical discovery window is 3,000 bars. Active records are not
age-evicted. Terminal records are capped per type, while completed original
FVGs that can still invert are retained until inversion or feature disablement.
Diagnostics declare one primary series and report bar-update, model, active
state, and render timing through the existing Orca diagnostics core.

Historical callbacks before the discovery cutoff do not build render arrays.
During the discovery window, completed bars update the causal model without
publishing a full snapshot after every bar. The chart-ready immutable snapshot
is published at the last historical callback and again on the realtime state
transition. This preserves the completed-bar model while avoiding historical
`OnPriceChange` or Tick Replay amplification of render-snapshot allocation.

## Default presentation

`BlocksFocused` is the default display preset: structure, standard FVG,
confirmed rejection blocks, and S-OB are visible. `CleanCore` shows structure
and FVG only. `FullContext` enables every detector type plus first-period and
first-RTH FVG highlighting. Display presets change visibility only; rejection
and order-block detector presets remain independent.

## Required live validation

1. Press NinjaTrader `F5` and record compiler output.
2. Add Orca Price Action to a clean MNQ 1-minute chart with defaults.
3. Verify close-confirmed pivot, BOS, CHoCH, protected-level, and sweep timing.
4. Exercise FVG partial fill, Two-Tone, completion, iFVG conversion, first-hour,
   and first-RTH behavior.
5. Exercise VI Classic/Advanced and true-gap exclusion.
6. Exercise rejection Strict/Balanced/Aggressive confirmation and failure.
7. Exercise S-OB, C-OB, PB parent linkage, display modes, mitigation, and
   invalidation.
8. Reload the same history and compare event bars and zone bounds.
9. Pan and zoom through dense history and inspect labels and zone endpoints.
10. Repeat a sanity pass on 15-second and range charts.
