# Orca Time Statistics RTH Reset And Font Weight

## Objective

Correct the RTH cumulative-delta reset for NinjaTrader close-stamped chart bars and add a selectable text font weight directly below the installed font-family setting.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaTimeStatistics.cs`
- `docs/handoffs/2026-08-23_orca-time-statistics_rth-reset-font-weight.md`

## Behavior added, changed, or removed

- The RTH reset boundary now treats a bar stamped exactly 9:30 AM Eastern as the bar closing at the session boundary, not the first RTH bar.
- The first bar stamped strictly after 9:30 AM starts the new RTH cumulative-delta period. On a five-minute chart, the bar labeled 9:35 AM is therefore the first RTH bar.
- Full Day and Weekly cumulative-delta reset behavior is unchanged.
- Time Statistics text now uses the selected font weight in both the requested-family and Segoe UI fallback DirectWrite formats.

## User-facing settings added, changed, deprecated, or removed

- Added `16. Font Weight` under `15. Font Family` in the Visual group.
- Available weights are Light, Regular, Medium, SemiBold, Bold, and ExtraBold.
- Bold is the default to preserve existing appearance.
- Font Size moved from display order 16 to 17 so Font Weight appears directly beneath Font Family.

## Secondary series added or changed

- No secondary series were added or changed.
- Existing primary-series, shared-provider, and internal market-data paths remain unchanged.

## Tick Replay implications

- No Tick Replay behavior changed.
- The RTH correction only changes how an existing bar timestamp is assigned to a cumulative-delta reset period.

## Historical-load implications

- Historical RTH cumulative-delta values rebuild with the corrected close-stamped boundary assignment.
- No additional historical-data request or full-history pass was added.

## Cache implications

- No cache behavior changed.

## Rendering implications

- DirectWrite text formats now resolve their weight from the new setting.
- Existing render-target tracking, DirectX resource disposal/recreation, scale masking, and guarded render paths were preserved.

## Performance implications

- The RTH change remains a constant-time timestamp comparison per cumulative-delta bar.
- Font-weight resolution occurs only when DirectWrite resources are created or recreated.
- No per-tick allocation, render-time data access, polling, or synchronization was added.

## Tests performed in NinjaTrader

- Pending Julian's F5 compile and chart validation.

## Static tests performed

- Confirmed the file still contains exactly one NinjaScript generated-code region.
- Confirmed both DirectWrite text-format constructors use the resolved selected weight and no constructor remains hardcoded to Bold.
- Boundary simulations passed for 9:25 AM, 9:30 AM, and 9:35 AM on an ordinary weekday.
- Weekend simulation passed: Monday 9:30 AM remains assigned to Friday's RTH anchor, while Monday 9:35 AM starts Monday's RTH period.
- `git diff --check` passed for the scoped files; only the existing line-ending conversion warning was reported.
- Normalized authored-region parity between `Working_Suite` and the deployed NinjaTrader copy passed.
- The deployed copy contains one generated-code region, the strict RTH boundary comparison, and the Font Weight property.

## Compile status

- NinjaTrader F5 compile pending after deployment.

## Deployment status

- Targeted deployment completed with `deploy_orca.ps1 -Target OrcaTimeStatistics`.
- Live destination: `C:\Users\julia\Documents\NinjaTrader 8\bin\Custom\Indicators\OrcaTimeStatistics.cs`.

## Manual-validation status

- Pending validation on a five-minute RTH cumulative-delta chart.
- Confirm the 9:35 AM bar starts the RTH cumulative delta and initially equals that bar's delta, excluding the bar labeled 9:30 AM.
- Confirm each font-weight choice applies without a DirectX error and that Bold preserves the prior appearance.

## Known issues, risks, and follow-up work

- The corrected rule follows NinjaTrader's close-stamped time-bar convention. Non-time BarsTypes that stamp a bar exactly at 9:30 AM should be checked separately because their timestamp may not represent a completed pre-RTH interval.
- This change does not redesign out-of-RTH accumulation; it only corrects which bar triggers the next RTH reset.

## Promotion eligibility

- Not eligible for promotion to `Full_Suite` until NinjaTrader compiles and Julian confirms the RTH alignment and font-weight rendering on a live chart.
