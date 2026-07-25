# Orca Time VWAP Period Reset Plot Breaks

Date: 2026-07-25

## Objective

Stop fixed-period Orca Time VWAP lines and deviation bands from drawing diagonal connections between the completed period and the next period's opening anchor.

## Files Changed

- `Orca Trades/Working_Suite/Indicators/OrcaTimeVWAPs.cs`
- `docs/handoffs/2026-07-25_orca-time-vwaps_period-reset-plot-breaks.md`

## Behavior Added, Changed, Or Removed

- Globex VWAP now resets plot indices 0 through 6 one bar back when its daily start boundary is crossed.
- Weekly VWAP now resets plot indices 21 through 27 one bar back when its weekly start boundary is crossed.
- The plot resets break the VWAP and all three upper/lower deviation pairs before the next period begins, preventing NinjaTrader from drawing a line between periods.
- Existing VWAP, variance, and standard-deviation calculations are unchanged.
- Existing RTH behavior is unchanged; RTH already resets its plot range at its start boundary and remains blank outside its configured time window.
- Rolling VWAP behavior is unchanged because it is a continuous moving window rather than a fixed-period reset series.

## User-Facing Settings Added, Changed, Deprecated, Or Removed

None.

## Secondary Series Added Or Changed

None. No Tick, Second, Bid, Ask, Last, Volumetric, or custom series were added or changed. `OrcaTimeVWAPs` remains primary-series only.

## Tick Replay Implications

None. The change only clears existing plot values at detected period boundaries and does not alter Tick Replay requirements or event sources.

## Historical-Load Implications

Historical processing now reconstructs a plot gap at every detected Globex and Weekly reset boundary. No additional historical scan or rebuild path was added.

## Cache Implications

None. No cache or shared provider behavior changed.

## Rendering Implications

- NinjaTrader line plots and deviation-region inputs now contain a missing prior-bar sample at Globex and Weekly boundaries, matching the existing RTH plot-break pattern.
- This prevents VWAP lines, deviation lines, and enabled fills from bridging into the next period.
- No SharpDX resources or custom rendering paths were added.

## Performance Implications

Negligible. Each fixed reset runs the existing seven-series `ResetPlotRange` loop once at the detected boundary. No per-render work or historical rescans were added.

## Tests Performed In NinjaTrader

- Deployed `OrcaTimeVWAPs.cs` from `Working_Suite` to `Documents\NinjaTrader 8\bin\Custom\Indicators`.
- Verified the deployed source is text-equivalent to the Working_Suite source after line-ending normalization.
- NinjaTrader chart behavior has not yet been manually tested.

## Compile Status

NinjaTrader F5 compile is pending. Deployment alone is not compile validation.

## Manual-Validation Status

Pending Julian's chart validation. Confirm that Weekly and Globex VWAP/deviation lines stop before the next reset and that enabled region fills do not bridge periods.

## Known Issues, Risks, And Follow-Up Work

- Verify both a weekly boundary and at least one daily Globex boundary on the intended chart types.
- Rolling VWAP remains intentionally continuous.
- No unrelated dirty Working_Suite files were modified or deployed.

## Promotion Eligibility

Not eligible for promotion to `Full_Suite` until NinjaTrader F5 compile succeeds and Julian confirms the fixed reset rendering on charts.