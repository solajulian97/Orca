# 2026-08-07 - Orca Time Statistics Volume Per Second

## Objective

Add an optional per-bar volume-per-second measurement to Orca Time Statistics, including the existing average-column behavior, without changing order-flow sourcing or rendering-resource ownership.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaTimeStatistics.cs`
- `docs/handoffs/2026-08-07_orca-time-statistics_volume-per-second.md`

## Behavior added, changed, or removed

- Added a `Volume / Sec` row immediately below Volume when enabled.
- The value is bar volume divided by the guarded elapsed bar duration in seconds.
- Bars with unavailable or zero duration leave the rate cell empty instead of displaying a fabricated value.
- The row participates in visible-range opacity scaling and in the existing average column.
- The average uses the configured completed-bar lookback and excludes invalid-duration samples.
- Existing Volume, Delta, cumulative delta, delta percent, Finish Delta, Range, Time, label placement, scale mask, and fail-soft behavior remain unchanged.

## User-facing settings added, changed, deprecated, or removed

- Added `Show Volume Per Second` under Rows.
- Default: `false`.
- No settings were removed or deprecated.

## Secondary series added or changed

- None.
- No Tick, Second, Bid, Ask, Last, Volumetric, or custom series were added or changed.

## Tick Replay implications

- None beyond the indicator's existing behavior.
- Volume per second uses primary-bar volume and guarded primary-bar timestamps; it does not add market-data classification or replay processing.

## Historical-load implications

- Completed historical bars use the existing safe bar-duration helper.
- A bar with unavailable timestamps or a nonpositive duration is omitted from the rate average.
- No historical rebuild or additional data request was added.

## Cache implications

- None.
- Shared-provider and local order-flow cache behavior are unchanged.

## Rendering implications

- The new row reuses the existing volume SharpDX brush and text resources.
- No new render-target-bound resources were introduced.
- Existing render-target tracking, disposal/recreation, right-edge labels, average-column placement, scale mask, and guarded bar-access paths were preserved.

## Performance implications

- The setting is disabled by default.
- When enabled, visible-bar scaling and cell rendering each perform one bounded duration lookup per visible bar; average calculation performs one lookup per configured lookback bar.
- When disabled, the average collector skips the new rate calculation.
- No full-history scan, allocation-heavy model, synchronization wait, or file/cache I/O was added.

## Tests performed in NinjaTrader

- Targeted deployment completed with `deploy_orca.ps1 -Target OrcaTimeStatistics`.
- Working_Suite and deployed live source match after line-ending normalization.
- Working_Suite and deployed live files each contain exactly one NinjaScript generated-code region.
- NinjaTrader F5 compile has not yet been performed for this change.
- Live chart validation of the new row is pending Julian.

## Compile status

- `git diff --check` passed; Git reported only the existing LF-to-CRLF conversion warning.
- Targeted symbol checks confirmed the setting, row, average fields, brush mapping, and guarded rate helper.
- No direct `Bars.GetOpen` or `Bars.GetClose` access was introduced.
- NinjaTrader F5 compile: pending Julian.

## Manual-validation status

- Pending Julian.
- Enable `Show Volume Per Second`, confirm the row appears below Volume, values update on the forming bar, completed bars show plausible rates, the average appears beside the current bar when averages are enabled, and the row label stays aligned at the far-right edge.

## Known issues, risks, and follow-up work

- The forming bar's value is intentionally dynamic because elapsed time increases until the bar completes.
- Non-time bars measure throughput over their actual elapsed wall-clock duration between bar timestamps.
- Candle body and displacement-style measurements were discussed but intentionally not included in this change.

## Full_Suite promotion eligibility

- Not eligible yet.
- Promotion requires a successful NinjaTrader F5 compile and Julian's live manual validation.
