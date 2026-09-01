# Orca Periodic VWAP Plot Settings UX

Date: 2026-09-01

## Objective

Replace the anonymous `[0]` through `[6]` plot-style presentation and unrelated Misc descriptors in the `Orca Periodic VWAP` settings grid with clear, named controls without changing calculations or the seven-plot contract.

## Files Changed

- `Orca Trades/Working_Suite/Indicators/OrcaPeriodicVWAP.cs`
- `docs/indicators/ORCA_PERIODIC_VWAP.md`
- `docs/handoffs/2026-09-01_orca-periodic-vwap_plot-settings-ux.md`

No existing VWAP indicator, Step Profile source, deployment script, source map, or `Full_Suite` file changed.

## Behavior Added, Changed, Or Removed

- Added seven named plot-style controls: VWAP Line and independent upper/lower styles for deviations 1, 2, and 3.
- Each control uses NinjaTrader's normal `Stroke` editor for color, dash style, opacity, and width.
- Disabled VWAP or deviation lines hide their corresponding style controls.
- Hid the raw `Plots`, `BarsPeriod`, `InputPlot`, and `SelectedValueSeries` descriptors from this indicator's property grid.
- Renumbered the visual groups to `3. Plot Styling`, `4. Region Fills`, and `5. Display`.
- Preserved the seven `AddPlot` calls, names, order, default styles, calculations, public series, and `Values` indexes.
- Kept the seven visual `Stroke` properties out of the NinjaScript generated cache parameters so programmatic calculation signatures remain unchanged.

## User-Facing Settings Added, Changed, Deprecated, Or Removed

Added under `3. Plot Styling`:

- `VWAP Line`
- `Deviation 1 Upper`
- `Deviation 1 Lower`
- `Deviation 2 Upper`
- `Deviation 2 Lower`
- `Deviation 3 Upper`
- `Deviation 3 Lower`

The anonymous raw plot collection is no longer intended to appear. No calculation setting was added, deprecated, or removed.

## Secondary Series Added Or Changed

None. The existing single hidden `AddDataSeries(BarsPeriodType.Tick, 1)` Last series is unchanged.

## Tick Replay Implications

None. Tick Replay remains unnecessary; this change only affects property-grid presentation and plot styling.

## Historical-Load Implications

None. Historical tick hydration, window resolution, accumulation, and publication paths are unchanged.

## Cache Implications

None. No shared or local cache behavior changed.

## Rendering Implications

- The same seven native plots render the same values at the same indexes.
- Named `Stroke` settings are copied onto those plots during configuration.
- Region drawing and the absence of an `OnRender` override are unchanged.

## Performance Implications

No per-tick or render-path work was added. Seven constant-size style copies occur during configuration only.

## Tests Performed In NinjaTrader

- Targeted deployment passed with `deploy_orca.ps1 -Target OrcaPeriodicVWAP`; no deployment-script change was required.
- NinjaTrader regenerated the live `OrcaPeriodicVWAP` accessor/cache region and the Custom DLL/PDB after deployment.
- The generated accessor retains the existing calculation parameters and contains no visual `Stroke` parameters.
- Normalized authored-source parity between Working_Suite and the live Custom copy passed.
- The current NinjaTrader trace contains no `OrcaPeriodicVWAP` compiler error after deployment.
- Live-assembly reflection confirmed seven named `Stroke` properties in `3. Plot Styling`, no `NinjaScriptProperty` cache attributes, seven native plots, unchanged plot names/default styles, and `ArePlotsConfigurable=false`.
- Focused source assertions confirmed the four raw descriptor hides; critical calculation, boundary-resolution, and region methods are byte-for-byte unchanged from the prior commit.
- Scoped `git diff --check` passed, apart from Git's existing LF-to-CRLF working-copy notices.
- Explicit NinjaScript Editor F5 and Julian's property-grid confirmation remain pending.

Required manual checks:

- Re-add or reset `Orca Periodic VWAP` so class-level plot configurability changes are applied.
- Confirm `3. Plot Styling` shows seven descriptive names rather than `[0]` through `[6]`.
- Confirm the raw Misc entries are absent.
- Change color, dash style, and width on several independent lines and confirm chart rendering.
- Save and reload a chart template/workspace and confirm the styles persist.
- Disable VWAP or individual deviation bands and confirm irrelevant style settings hide.

## Compile Status

Pending NinjaTrader F5. Direct Roslyn semantic compilation against the installed NinjaTrader and .NET Framework assemblies passed with zero errors. NinjaTrader's source watcher regenerated the Custom assembly, but neither result is being reported as an explicit F5 compile.

## Manual-Validation Status

Pending Julian's confirmation of the revised settings grid and persistence behavior.

## Known Issues, Risks, And Follow-Up Work

- NinjaTrader can retain a previously applied indicator instance's class-level property descriptors. Remove/re-add the indicator or reset its settings before judging the new layout.
- Property-grid presentation and plot-style persistence still require Julian's live confirmation.
- Plot-style persistence must be confirmed through an actual template/workspace reload.

## Promotion Eligibility

Not eligible for promotion to `Orca Trades/Full_Suite` until NinjaTrader F5 succeeds and Julian validates the property-grid layout, styling behavior, and persistence.
