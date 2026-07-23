# OrcaSessionContextMap Render Snapshot And Label Clarity

## Objective

Prevent the reported `OnRender` index-out-of-range failure from blanking the chart and apply the first label-clarity pass approved by Julian.

## Files Changed

- `Orca Trades/Working_Suite/Indicators/OrcaSessionContextMap.cs`
- `docs/ORCA_ARCHITECTURE.md`
- `docs/indicators/ORCA_SOURCE_MAP.md`
- `docs/handoffs/2026-07-23_orca-session-context-map_render-snapshot-label-clarity.md`

## Behavior Added, Changed, Or Removed

- Rendering now reads an immutable snapshot of sessions, profile rows, VWAP points, display events, and prior-session levels.
- The reported failure is consistent with `OnRender` indexing mutable collections while the hidden tick series updates them. The snapshot boundary removes that concurrent collection traversal; live NinjaTrader reproduction is still required to confirm the diagnosis.
- A render exception is throttled and logged, and only that frame is skipped instead of allowing the indicator render path to fail continuously.
- The chart keeps only the latest displayed state for each related level. Reclaim or acceptance supersedes an earlier sweep label for that level.
- Open-location context is shown once in a session header instead of as a price-level event label.
- Event labels are shorter, prioritized, placed using rectangle collision checks, and connected to the source price with a leader and anchor mark.
- Structural level labels remain lower priority than reclaim and acceptance context.

## User-Facing Settings

No settings were added, renamed, deprecated, or removed. Existing label, stats-panel, session, and profile settings continue to control visibility.

## Secondary Series

- The existing 30-second series is unchanged and continues to drive the opening range independently of the primary chart timeframe.
- The existing 1-tick series remains conditional on the session volume profile being enabled.
- No Bid, Ask, Volumetric, or custom data series was added.

## Tick Replay Implications

Tick Replay requirements are unchanged. True traded volume comes from the hidden 1-tick series. Historical delta precision still depends on available replayed quote context and should not be described as strict historical Bid x Ask without that evidence.

## Historical-Load Implications

- Historical 1-tick hydration remains the dominant cost when the volume profile is enabled.
- Full POC/value-area rescans are no longer performed after every tick. They are refreshed when the render snapshot is published, which occurs on primary historical updates.
- Historical chart load time must be compared in NinjaTrader before claiming a measured improvement.

## Cache Implications

No shared cache or local persistent cache was added or changed. The indicator still owns its session profile data.

## Rendering Implications

- `OnRender` performs no live session-model traversal and no profile calculation.
- Snapshot data is published from the calculation thread and consumed as arrays by SharpDX rendering.
- Label placement preserves the source price vertically, fans labels horizontally when needed, and may suppress a lower-priority label when no non-overlapping horizontal position is available.
- Open-location headers avoid the configured stats-panel rectangle.

## Performance Implications

- Realtime snapshot publication is capped at approximately four times per second.
- Historical snapshots publish on primary-series updates rather than on each hidden tick.
- Snapshot publication copies profile and VWAP rows, so live timing should confirm that the reduced profile-summary work outweighs copy cost on large sessions.
- No locks, waits, historical rebuilds, or collection mutation were added to `OnRender`.

## Tests Performed In NinjaTrader

None in this task yet.

## Compile Status

A local sanitized C# compile against the installed NinjaTrader and SharpDX assemblies passed. NinjaTrader `F5` compile remains pending.

## Manual-Validation Status

Pending Julian's chart validation. Required checks are:

1. Reload NinjaScript and confirm no `OnRender` index error.
2. Confirm the chart remains populated during and after historical calculation.
3. Inspect overlapping same-price labels and leader-line alignment on both compact and zoomed chart views.
4. Confirm only the latest sweep/reclaim/acceptance state appears for each related level.
5. Compare reload duration with the previous build when the true-volume profile is enabled.

## Known Issues, Risks, And Follow-Up Work

- The exact original collection and index cannot be proven from NinjaTrader's message alone; the snapshot fix removes all known mutable render inputs.
- Hidden 1-tick hydration can still be slow across long chart histories.
- Label priority and suppression need visual tuning after Julian reviews the first live pass.
- The render-error guard is a recovery boundary, not a substitute for reviewing any new logged exception.

## Full_Suite Promotion

Not eligible. Promotion requires NinjaTrader `F5` success and Julian's manual runtime validation.
