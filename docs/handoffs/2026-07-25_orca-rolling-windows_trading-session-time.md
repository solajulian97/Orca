# Orca Rolling Windows Use Trading-Session Time

Date: 2026-07-25

## Objective

Prevent closed weekends and other chart-session closures from consuming elapsed time in rolling VWAP and rolling profile windows. A five-day window should represent five configured trading sessions rather than five calendar days.

## Files Changed

- `Orca Trades/Working_Suite/Indicators/OrcaTimeVWAPs.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaRollingProfiles.cs`
- `docs/handoffs/2026-07-25_orca-rolling-windows_trading-session-time.md`

## Behavior Added, Changed, Or Removed

- Orca Time VWAP rolling periods no longer insert empty time buckets across a chart-session boundary. In-session elapsed gaps still count normally.
- Orca Rolling Profiles now calculates its cutoff across defined trading sessions from the chart Trading Hours template.
- Full Session profile mode uses NinjaTrader `SessionIterator` session begin/end times and skips undefined trading days, including weekends and template holidays.
- RTH Only profile mode uses the configured RTH start/end times while still skipping undefined trading days.
- The session cutoff is calculated once per source minute and advanced within that minute, avoiding a session-history walk on every hidden tick.
- All rolling periods use the new behavior, including intraday, 1-day, 2-day, 5-day, 10-day, and 20-day windows.
- VWAP formulas, profile volume/delta aggregation, value-area calculations, and fixed Weekly/Globex/RTH VWAP anchors are unchanged.

## User-Facing Settings Added, Changed, Deprecated, Or Removed

None. Existing `Minutes In Trading Day`, profile mode, RTH start/end, and rolling period settings continue to control window length.

## Secondary Series Added Or Changed

None.

- `OrcaTimeVWAPs` remains primary-series only.
- `OrcaRollingProfiles` retains its existing conditional hidden 1 Tick series when local tick cache mode is active.
- No Tick, Second, Bid, Ask, Last, Volumetric, or custom series declaration changed.

## Tick Replay Implications

No source-mode or Tick Replay requirement changed. Historical hidden-tick work in local Rolling Profiles mode remains as before.

## Historical-Load Implications

- Historical rolling windows now need enough loaded chart history to cover the requested number of actual trading sessions.
- Weekend, holiday, and maintenance closures defined by the chart Trading Hours template no longer shorten the effective data window.
- Existing saved chart settings remain compatible; no serialization migration is required.

## Cache Implications

None. Shared-provider defaults, local tick-cache defaults, provider backfill limits, and spike-compaction behavior are unchanged.

## Rendering Implications

- Rendering code and SharpDX resources are unchanged.
- Visible VWAP/profile values can change because older valid trading-session data remains in the rolling window instead of being displaced by closed calendar time.
- The five-day rolling VWAP and profile should approach the current weekly data set near the end of the fifth session, not around Wednesday after a weekend.

## Performance Implications

- Time VWAP adds one existing session-boundary check when advancing rolling buckets.
- Rolling Profiles resolves a bounded set of prior session intervals once per source minute, then advances the cached cutoff within that minute.
- No session traversal, cache access, model rebuild, or allocation-heavy work was added to `OnRender`.

## Tests Performed In NinjaTrader

- Deployed both edited indicators from Working_Suite to the live NinjaTrader Custom Indicators folder.
- Verified authored source equivalence between Working_Suite and both live files after line-ending normalization and before generated wrapper code.
- NinjaTrader F5 compile and live chart behavior are pending.

## Source Validation

- `git diff --check` passed for both source files.
- A deterministic session-window simulation retained prior-week data on Wednesday and moved the five-session cutoff to the current Monday session at Friday close.
- Simulated cutoffs were Sunday 18:00 for a five-session ETH window ending Friday 17:00 and Monday 09:30 for a five-session RTH window ending Friday 16:00.
- NinjaTrader built-in source confirms the used `SessionIterator` APIs: `GetTradingDay`, `GetTradingDayBeginLocal`, `GetTradingDayEndLocal`, and `IsTradingDayDefined`.

## Compile Status

NinjaTrader F5 compile is pending. Deployment and source checks are not compile validation.

## Manual-Validation Status

Pending Julian's chart validation.

- Confirm the five-day rolling VWAP remains distinct from Weekly during Wednesday and approaches it near Friday.
- Confirm five-day Rolling Profiles retains five actual sessions across a weekend.
- Check both Full Session and RTH Only profile modes if both are used.

## Known Issues, Risks, And Follow-Up Work

- A chart must load enough historical sessions for a multi-session window; the indicator cannot retain sessions NinjaTrader did not load.
- Rolling Profiles shared-provider historical backfill limits remain unchanged and may still be too short for long windows when that optional mode is enabled.
- `OrcaRollingProfiles.cs` already contained separate uncommitted diagnostics, spike-compaction, and delta-clipping work. Those edits were preserved and were already equivalent to the live authored file before this change.
- No unrelated dirty Working_Suite file was edited or deployed.

## Promotion Eligibility

Not eligible for promotion to `Full_Suite` until NinjaTrader F5 compile succeeds and Julian confirms the rolling behavior on live charts.