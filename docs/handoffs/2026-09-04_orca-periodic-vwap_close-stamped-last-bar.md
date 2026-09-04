# Orca Periodic VWAP Close-Stamped Last Bar

Date: 2026-09-04

## Objective

Keep each completed Periodic or Session VWAP visible through its final owned close-stamped time bar while preserving visual separation from the next window.

## Files Changed

- `Orca Trades/Working_Suite/Indicators/OrcaPeriodicVWAP.cs`
- `docs/indicators/ORCA_PERIODIC_VWAP.md`
- `docs/handoffs/2026-09-04_orca-periodic-vwap_close-stamped-last-bar.md`

No existing VWAP, Step Profile, deployment script, source map, or `Full_Suite` file changed.

## Behavior Added, Changed, Or Removed

- Removed the boundary behavior that invalidated the completed window's final owned plot sample.
- The completed window now retains that last value on close-stamped time charts.
- The first native-plot segment entering a directly adjacent new window is transparent, preventing a diagonal bridge while retaining both endpoint values.
- A non-time primary bar containing trades from both windows still has one visual sample and belongs to the new window; the completed window ends at the prior primary bar.
- Window timestamps, accumulators, reset semantics, calculations, defaults, plot order, plot names, and `Values` indexes are unchanged.

## User-Facing Settings Added, Changed, Deprecated, Or Removed

None.

## Secondary Series Added Or Changed

None. The existing hidden `AddDataSeries(BarsPeriodType.Tick, 1)` Last series is unchanged.

## Tick Replay Implications

None. Tick Replay remains unnecessary.

## Historical-Load Implications

Historical and realtime plot publication use the same segment-separation rule. No history rebuild or additional data request was added.

## Cache Implications

None.

## Rendering Implications

- The same seven native plots and bounded per-window regions remain in use.
- `PlotBrushes` marks the first incoming segment transparent at a direct window transition.
- Transparent first samples remain available in the Data Box.
- No `OnRender` override, SharpDX resource, render-time calculation, collection mutation, cache access, or synchronization wait was added.

## Performance Implications

Seven constant-time plot-brush assignments or resets occur when a dirty primary-bar sample is published. No per-tick scan, allocation, or historical rebuild was added.

## Tests Performed In NinjaTrader

Targeted deployment completed with the existing script:

- Dry run selected only `OrcaPeriodicVWAP.cs`.
- Live copy completed with `deploy_orca.ps1 -Target OrcaPeriodicVWAP`.
- NinjaTrader's source watcher regenerated `NinjaTrader.Custom.dll` and `.pdb` after the live source write.
- The live file contains one generated NinjaScript region with the expected accessor/cache code.
- Normalized authored-source parity between Working_Suite and the live file passed.
- The current trace contains no `OrcaPeriodicVWAP`, C# compiler, or unable-to-compile error.

Offline verification completed:

- Roslyn semantic compilation against the installed NinjaTrader assemblies passed.
- Eleven source-invariant assertions passed, including seven plots, one hidden Tick-1 series, unchanged 30-minute default, retained Daily interval, transparent-segment publication, and no `OnRender` override.
- Three boundary-ownership assertions passed for adjacent close-stamped bars, a shared non-time bar, and a single shared bar.
- Scoped `git diff --check` passed.

Explicit NinjaScript Editor F5 was not performed because the running NinjaTrader process exposes no interactive editor window to this task. Automatic assembly regeneration is not F5 proof.

Required manual checks:

- Daily VWAP on a 60-minute chart reaches the last bar before the 6:00 PM boundary.
- Hourly VWAP on a 2-minute chart reaches the final bar of the hour.
- 30-minute VWAP on a 1-minute chart reaches the final bar of the period.
- No VWAP, deviation band, or enabled fill bridges diagonally into the next window.
- Minute, volume, range, and tick charts preserve correct shared-bar ownership.
- Historical load, realtime transition, reload, and two differently configured instances remain correct.

## Compile Status

Offline semantic compilation passed. NinjaTrader automatically regenerated its custom DLL and PDB after deployment, but explicit NinjaScript Editor F5 remains pending and is not claimed.

## Manual-Validation Status

Pending Julian's live chart confirmation.

## Known Issues, Risks, And Follow-Up Work

- Native plots have one value per primary bar. A range or volume bar spanning a logical boundary cannot display both completed- and new-window VWAP values; it remains assigned to the new window.
- The exact transparent-segment appearance requires live NinjaTrader confirmation with solid and dashed custom plot styles.

## Promotion Eligibility

Not eligible for promotion to `Orca Trades/Full_Suite` until NinjaTrader F5 succeeds and Julian validates the corrected endpoints and boundary separation.
