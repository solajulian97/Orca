# Orca Periodic VWAP Initial Implementation

Date: 2026-09-01

## Objective

Add the standalone `OrcaPeriodicVWAP` NinjaTrader indicator with independently resetting periodic or session VWAPs, population-deviation bands, bounded historical regions, and exact trade-time allocation from a hidden Last tick series.

## Files Changed

- `Orca Trades/Working_Suite/Indicators/OrcaPeriodicVWAP.cs`
- `docs/indicators/ORCA_PERIODIC_VWAP.md`
- `docs/indicators/ORCA_SOURCE_MAP.md` (narrow additions within a pre-existing dirty file)
- `docs/handoffs/2026-09-01_orca-periodic-vwap_initial-implementation.md`

No existing indicator, deployment-script, or `Full_Suite` file was changed for this implementation.

## Behavior Added, Changed, Or Removed

- Added Periodic windows at 5, 15, and 30 minutes, 1 hour, and 4 hours.
- Added a 6:00 PM-anchored four-hour cadence.
- Added Overnight/RTH and Asia/London/RTH session configurations with fixed Eastern-aligned, half-open boundaries.
- Excluded `[5:00 PM, 6:00 PM)` from Session mode.
- Added cumulative trade volume, price-volume, and price-squared-volume with population standard deviation.
- Added seven stable plots, boundary gaps, ownership-aware historical pruning, and bounded instance-specific fill regions.
- Added direct mapping from every hidden Last tick to the corresponding primary chart bar for time, tick, range, and volume charts.
- No existing behavior was removed or changed.

## User-Facing Settings Added, Changed, Deprecated, Or Removed

Added four property groups:

- `1. Window Configuration`: Window Basis, Periodic Interval, Session Configuration.
- `2. VWAP and Bands`: Show VWAP, Show Deviation Bands, Show Deviation 1/2/3, and multipliers.
- `3. Region Fills`: XML-serializable brushes and zero-default opacity controls for the three fill tiers.
- `4. Display`: Show Historical Windows and Max Historical Windows.

The type converter hides the interval or session selector that does not apply, disabled band controls and fills, and the historical-window limit when history is disabled. No setting was deprecated or removed.

## Secondary Series Added Or Changed

- Added one same-instrument `AddDataSeries(BarsPeriodType.Tick, 1)` series.
- It is consumed as Last trade time, price, and volume through `Times[1][0]`, `Closes[1][0]`, and `Volumes[1][0]`.
- No Bid, Ask, Second, Minute, Volumetric, or custom series was added.
- No existing component's series declarations changed.

## Tick Replay Implications

Tick Replay is not required. Historical and realtime VWAP calculations consume the hidden Last tick series. Enabling chart Tick Replay is not necessary for this indicator and may add unrelated workspace load.

## Historical-Load Implications

- Historical load hydrates one additional Tick-1 Last series for every indicator instance.
- Results depend on available historical Last ticks and the selected Trading Hours template.
- Missing periods do not generate empty windows or synthetic volume; resumed data maps directly to the aligned current window.
- Historical and realtime transitions use the same window resolver and accumulator path.

## Cache Implications

No shared or local profile cache is read or written. The existing shared order-flow provider was inspected but not reused because its normal one-second bucket can combine different trade prices and therefore cannot always preserve exact price-volume and price-squared-volume inputs.

## Rendering Implications

- The indicator uses seven normal NinjaTrader plots.
- Every reset inserts an invalid separating sample so VWAP and deviation plots cannot bridge windows.
- Each fill uses a bounded, per-window, instance-specific `Draw.Region` tag.
- Region removal and historical pruning occur only during boundary processing.
- No `OnRender` override, SharpDX resource, render-time calculation, collection mutation, cache access, or synchronization wait was added.

## Performance Implications

- Each valid Last tick performs constant-time window resolution, primary-bar lookup, three accumulator additions, and one sample update.
- Accumulation is incremental; no per-tick historical rebuild or scan occurs.
- Plot publication and active-region endpoint updates occur on primary-series callbacks.
- Historical plot cleanup can scan one expired window's primary-bar span, but only at a boundary and only after the configured history limit is exceeded.
- Multiple instances each add their own hidden Tick-1 series, which can increase historical hydration and callback load in a large workspace.

## Tests Performed In NinjaTrader

- Targeted deployment passed with `deploy_orca.ps1 -Target OrcaPeriodicVWAP`; only the new indicator was copied into NinjaTrader's live Custom indicators folder.
- NinjaTrader's source watcher added `Indicators\OrcaPeriodicVWAP.cs` to `NinjaTrader.Custom.csproj` and regenerated `NinjaTrader.Custom.dll`, `.pdb`, and `.xml` at 2:43:59 PM.
- The deployed authored source matches the Working_Suite source after normalizing line endings and the absent generated region; both authored bodies hash to `FA79F8DA3270BC9A84DAB5D53BD622BF3FD05B8C76D008DC02458BD835038B63`.
- The current NinjaTrader trace contains no `OrcaPeriodicVWAP` compiler error after deployment.
- Explicit NinjaScript Editor F5 could not be run because desktop control of NinjaTrader was not approved. Watcher regeneration is not being reported as F5 compilation.
- No generated `OrcaPeriodicVWAP` accessor/cache region is present yet, so generated-signature inspection remains pending F5.
- Julian's chart validation is pending.

Required manual matrix:

- Minute chart and 5,000-Volume chart.
- Five-minute resets and 6:00 PM-anchored four-hour cadence.
- Both session configurations across 6:00 PM, 3:00 AM, 9:30 AM, 5:00 PM, and maintenance.
- Historical load, transition to realtime, reload, and new incoming bars.
- No line or cloud bridges.
- Two instances with different intervals.
- Template and workspace persistence.

## Compile Status

Pending NinjaTrader F5. A direct Roslyn semantic check against the installed NinjaTrader and .NET Framework assemblies completed with zero errors, and NinjaTrader's watcher regenerated the Custom assembly, but neither result is being reported as an F5 compile.

## Manual-Validation Status

Pending Julian's live chart confirmation.

## Known Issues, Risks, And Follow-Up Work

- A non-time primary bar can contain trades from both sides of a logical boundary. Calculation assigns each trade to the correct accumulator; because a normal NinjaTrader plot has one sample per primary bar, the shared bar displays the new window and the latest older-window plot sample is cleared to preserve separation.
- A Trading Hours template that omits overnight data cannot reconstruct Asia, London, or Overnight VWAPs.
- Default-off fills require explicit opacity values greater than zero.
- Optional period/session labels were intentionally omitted from the initial implementation.
- Run NinjaScript Editor F5, then inspect the generated `OrcaPeriodicVWAP` accessor/cache signatures before chart validation.
- Manual settings persistence, multi-instance drawing isolation, and live boundary rendering remain unvalidated until Julian completes the chart matrix.

## Promotion Eligibility

Not eligible for promotion to `Orca Trades/Full_Suite` until NinjaTrader F5 succeeds and Julian confirms the required live behavior.
