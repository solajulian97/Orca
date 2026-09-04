# Orca Periodic VWAP Daily Interval

Date: 2026-09-04

## Objective

Add a Daily choice to the `Orca Periodic VWAP` Periodic Interval setting while preserving all existing intraday intervals, Session mode, calculations, plots, fills, and property-grid organization.

## Files Changed

- `Orca Trades/Working_Suite/Indicators/OrcaPeriodicVWAP.cs`
- `docs/indicators/ORCA_PERIODIC_VWAP.md`
- `docs/handoffs/2026-09-04_orca-periodic-vwap_daily-interval.md`

No other indicator, deployment script, source map, or `Full_Suite` file changed.

## Behavior Added, Changed, Or Removed

- Added `Daily = 1440` to `OrcaPeriodicVwapInterval`.
- Added the user-facing `Daily` interval label.
- Daily windows use Orca's established Eastern-aligned trading-day boundary: `[6:00 PM, 6:00 PM)` the following calendar day.
- Missing weekends, maintenance gaps, and other periods still create no synthetic volume; the next trade maps directly to its current Daily window.
- Existing 5-, 15-, and 30-minute, 1-hour, 4-hour, and Session-mode behavior is unchanged.

## User-Facing Settings Added, Changed, Deprecated, Or Removed

- Added `Daily` to `Periodic Interval` when `Window Basis` is `Periodic`.
- The default remains `30 Minutes`.
- Updated the Periodic Interval description to state that both 4 Hour and Daily windows are anchored to 6:00 PM.
- No setting was removed or deprecated.

## Secondary Series Added Or Changed

None. The existing single hidden `AddDataSeries(BarsPeriodType.Tick, 1)` Last series is unchanged.

## Tick Replay Implications

None. Tick Replay remains unnecessary because the existing hidden Last tick series supplies historical and realtime trades.

## Historical-Load Implications

- Daily VWAP accuracy depends on available historical Last ticks and Trading Hours coverage across the 6:00 PM-to-6:00 PM window.
- Missing data contributes no volume and does not create synthetic callbacks or windows.
- Historical and realtime processing continue through the same resolver and accumulator path.

## Cache Implications

None. No shared or local cache is read or written.

## Rendering Implications

None. The same seven plots and bounded per-window regions are used. Boundary separation and the absence of an `OnRender` override are unchanged.

## Performance Implications

Negligible. Daily selection uses the existing constant-time trading-day resolver and changes no per-tick allocation, series count, history rebuild, or rendering path.

## Tests Performed In NinjaTrader

Targeted deployment completed with the existing script:

- Dry run: `deploy_orca.ps1 -Target OrcaPeriodicVWAP -DryRun` selected only `OrcaPeriodicVWAP.cs`.
- Live copy: `deploy_orca.ps1 -Target OrcaPeriodicVWAP` completed successfully.
- NinjaTrader's source watcher regenerated `NinjaTrader.Custom.dll` and `.pdb` after the live source write.
- The live file contains exactly one generated NinjaScript region, including the expected `OrcaPeriodicVWAP` accessor/cache signatures.
- The live authored region matches the Working_Suite source after normalizing line endings and excluding generated code.

Offline verification completed:

- Roslyn semantic compilation against the installed NinjaTrader assemblies passed.
- A 20-assertion boundary/static harness passed Daily pre-boundary, exact-boundary, midnight, and end-of-window cases; retained 4 Hour and 5 Minute alignment cases; and confirmed the unchanged default, seven plots, one secondary series, and absence of `OnRender`.
- Scoped `git diff --check` passed.

No explicit NinjaScript Editor F5 was performed because the running NinjaTrader process exposes no interactive editor window to this task. Automatic assembly regeneration is not recorded as F5 proof.

Required manual checks:

- Confirm `Daily` appears after `4 Hours` in Periodic Interval.
- Confirm a Daily VWAP resets at exactly 6:00 PM Eastern-aligned chart time, not midnight.
- Confirm data on both sides of midnight remains in one Daily window.
- Confirm no line or fill bridges across the 6:00 PM reset.
- Confirm historical-to-realtime transition, reload, and template/workspace persistence.
- Confirm existing 4 Hour and Session selections remain unchanged.

## Compile Status

Offline semantic compilation passed. NinjaTrader automatically regenerated the custom DLL and PDB after deployment, but explicit NinjaScript Editor F5 remains pending and is not claimed.

## Manual-Validation Status

Pending Julian's Daily-window chart confirmation.

## Known Issues, Risks, And Follow-Up Work

- A Trading Hours template that omits parts of the overnight session cannot reconstruct missing trade volume.
- A non-time primary bar spanning 6:00 PM retains the indicator's existing ownership rule: the shared bar displays the new window and an older sample is cleared to prevent bridging.
- The settings grid may require removing/re-adding or resetting an existing indicator instance before a newly added enum choice is visible.

## Promotion Eligibility

Not eligible for promotion to `Orca Trades/Full_Suite` until NinjaTrader F5 succeeds and Julian validates Daily behavior on live charts.
