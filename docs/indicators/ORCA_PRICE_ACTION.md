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
5. Pivot state is created only at the confirmation event. Once confirmed, its
   HH/LH/HL/LL label is rendered over the original pivot candle for visual
   alignment. Later Sponsor, Protected, Major, and Unprotected changes receive
   their own effective event bar rather than rewriting the original state.

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

After the third candle confirms the pattern, a standard FVG rectangle is
back-placed to the first candle so it begins at that candle's wick and spans the
entire three-candle formation. Detection remains third-candle-close causal.
iFVG rendering begins at the later close-through conversion bar rather than
being back-placed before inversion.

`Extension Bars` limits standard FVG, iFVG, and first-RTH zone geometry to 30
bars by default. This is a visual endpoint only: the model continues tracking
fills and close-through invalidation after the rectangle stops. A first-period
FVG using `Until Filled` or `Until Period End` follows that explicit timed
extension instead of the standard bar cap. For standard FVGs the bar count is
measured from the first pattern candle.

Timed FVGs use a DST-aware New York clock. The first qualifying FVG of either
direction claims a 15-minute, 30-minute, 1-hour, or 4-hour bucket. Four-hour
buckets begin at 00:00, 04:00, 08:00, 12:00, 16:00, and 20:00. The separate RTH
feature accepts timestamps strictly after the configured 09:30 open and before
the configured 16:00 close. Period-end rendering never extrapolates a synthetic
future bar on non-time charts.

## Volume imbalances

The zone price bounds remain the prior candle's close and the current candle's
open. Detection still waits for the second candle to close and pass Classic or
Advanced qualification. Once confirmed, the rectangle is back-placed to the
prior candle so its horizontal origin aligns with the close that formed the
first side of the imbalance.

## Structure and rejection blocks

Structure breaks require a strict close beyond the pivot plus the configured
tick buffer. Wick-only violations are sweeps. Bullish BOS promotes the latest
eligible confirmed low to Sponsor, Protected, External, and Major; bearish
behavior mirrors this. Only a close through the protected boundary creates
CHoCH/MSS. In a bearish trend, a strict completed close above the active
protected high is bullish CHoCH; in a bullish trend, a strict completed close
below the active protected low is bearish CHoCH. Equal closes are not breaks.

The newest BOS-eligible pivot on the crossed side is the immediate structure
level and is the only level that can publish BOS. A distinct protected boundary
may also publish CHoCH for the parent trend. Older same-side pivots lose future
BOS eligibility when immediate structure breaks; if price later closes through
them, they are retired silently rather than relabeled as new structure. No
stale-pivot `Liquidity` text or historical structure line is emitted.

Pivot labels combine the price relation with scope. `HH`, `LH`, `HL`, and `LL`
mean higher high, lower high, higher low, and lower low. The suffix `I` means
Internal and `E` means External, so `LH I` is an Internal lower high and `HL E`
is an External higher low.

The label is horizontally centered on the pivot candle. High labels sit just
above the wick and low labels sit just below it. This is a render-anchor choice:
the label does not exist until the configured right-side confirmation bars have
closed.

Protected-level dotted lines retain their effective promotion-to-current span,
but their text is not placed at the line endpoint. `Protected High` is centered
over the actual protected high candle and `Protected Low` under the actual
protected low candle. The text is offset by one row away from the wick so the
existing HH/LH/HL/LL relation label remains readable at the same pivot.

Visible sweeps use a separate quality filter so reducing sweep noise does not
change pivot, BOS, or CHoCH sensitivity. `Sweep Quality` provides:

- `Qualified Liquidity` (default): the pivot must be Standard or Major and must
  also be External, Protected, Sponsor, or the active trend-side target;
- `Major Only`: only Protected pivots, Sponsors, or External Major pivots; and
- `All Confirmed Pivots`: every confirmed, unbroken pivot, subject to the
  independent penetration and resting controls.

The default visible sweep must penetrate the pivot by at least one tick, close
back inside the actual level, and occur at least three bars after pivot
confirmation. `Minimum Sweep Penetration (Ticks)` and `Minimum Resting Bars`
can be set to zero, but an actual wick violation and completed close back inside
are still required. Structure break-buffer tolerance does not loosen the sweep
close-back-inside rule.

Each liquidity level receives one visible rolling episode. Resweeps inside
`Sweep Label Window Bars` move that level's event to the latest sweep; once the
window expires, that already-swept level is display-consumed. High-side and
low-side labels are still coalesced independently, so distinct same-side levels
swept close together publish only the latest label. The default window is five
bars, and each label is centered at the displayed sweep candle's wick extreme.

Rejection Blocks retain their own preset-driven liquidity checks. Additional
sweep events discovered only for rejection-block qualification remain internal
and do not automatically publish visible `Sweep` text.

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

## Zone style controls

FVG, iFVG, volume imbalance, rejection block, structural order block,
continuation order block, and propulsion block each have an independent style
group. Every group exposes:

- bullish fill color;
- bearish fill color;
- bullish border color;
- bearish border color; and
- opacity.

Brushes persist through XML string proxies. `FVG Filled-Portion Max Opacity`
caps the faded portion of a Two-Tone FVG, and `Completed-Zone Max Opacity` caps
terminal records without overriding a lower per-type opacity. Timed FVG
highlight borders continue to use the separately configurable timed-highlight
border color.

## Rendering and performance

Calculation code publishes immutable arrays of zone, line, and label geometry.
`OnRender` reads only those arrays and performs drawing; it does not traverse
calculation collections, query caches, rebuild history, or wait on locks.
SharpDX brushes, stroke styles, and text formats are recreated when the render
target changes and disposed at termination.

Fill and border brushes are separate SharpDX resources for each zone type and
direction. Render snapshots retain only immutable geometry, type, direction,
and opacity; `OnRender` routes those items to the precreated resources without
accessing mutable detector collections.

Pivot labels use a separate centered DirectWrite format that follows the same
render-target recreation and disposal lifecycle as the other text resources.
Protected labels use the same centered format and carry only a small immutable
vertical pixel offset to avoid relation-label overlap.

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
   Confirm HH/LH labels are centered above their pivot wick and HL/LL labels are
   centered below their pivot wick without appearing before confirmation.
   Confirm repeated same-side sweeps inside the configured window show only one
   label on the latest resweep wick.
   Exercise all three Sweep Quality modes. Confirm the default rejects Weak and
   ordinary Internal pivots, respects one-tick penetration and three resting
   bars, consumes a level after its episode, and does not print rejection-only
   sweep events.
   Confirm protected labels are centered on the actual protected pivot, not the
   current endpoint, and that wick/equal-close tests do not create CHoCH.
4. Exercise FVG partial fill, Two-Tone, completion, iFVG conversion, first-hour,
   and first-RTH behavior.
5. Exercise VI Classic/Advanced and true-gap exclusion.
6. Exercise rejection Strict/Balanced/Aggressive confirmation and failure.
7. Exercise S-OB, C-OB, PB parent linkage, display modes, mitigation, and
   invalidation.
8. Reload the same history and compare event bars and zone bounds.
9. Pan and zoom through dense history and inspect labels and zone endpoints.
10. Change every per-type fill, border, and opacity setting; save an indicator
    template, reload it, and confirm the values round-trip.
11. Repeat a sanity pass on 15-second and range charts.
