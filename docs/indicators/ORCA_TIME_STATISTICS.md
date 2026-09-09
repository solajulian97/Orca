# Orca Time Statistics

Updated: 2026-09-09. Current source and offline checks; live acceptance of the event-assignment correction is pending.

Source: `Orca Trades/Working_Suite/Indicators/OrcaTimeStatistics.cs`.

## Event and bar ownership

Time Statistics adds no secondary series. Its default is Internal source with Calculate.OnEachTick. OnMarketData tracks Bid/Ask and classifies Last-event volume using trade-time quotes where available, then remembered quotes/tick direction as before. Historical per-trade Internal data requires NinjaTrader Tick Replay.

With Calculate.OnEachTick, the local Last-event path now uses CurrentBar and checks that it is inside the primary series. A timestamp lookup cannot distinguish several range/volume bars with the same timestamp. Unavailable or out-of-range current indexes are skipped rather than assigned to an edge bar. The existing timestamp path is retained for OnBarClose/OnPriceChange; those modes have not acquired a new per-trade accuracy guarantee.

The assignment applies to Internal and the hybrid mode's existing local callback path. SharedProvider snapshot reconstruction still uses its existing timestamp-based bucket mapping and was not changed by this correction.

IsSuspendedWhileInactive=false remains the default and is now enforced in Configure after saved settings. This adds no event subscriptions or work loop. An instance previously overriding suspension to true can now do more background work; normal default instances already processed while inactive.

## Preserved behavior and known limits

- Delta classification, per-bar maxima/minima, Finish Delta, reset modes, row order, styling, property identities and all exposed defaults are preserved. Cumulative Delta remains after Finish Delta.
- Existing DirectX target recreation and Orca completed-bar replay horizon remain unchanged. This correction does not make the indicator an Orca developing-tick replay participant.
- Existing render-time cumulative scans and shared-provider refresh/rebuild paths remain separate architectural debt. They were not expanded or relocated onto every tick during this Internal-mode correction.
- A single trade split across multiple volume bars, custom BarsType removal/replacement behavior, and exact platform event ordering require separate validation; the fixture does not construct NinjaTrader bars.
- This is a proven defect under modeled same-timestamp bar inputs, not yet the proven cause of Julian's inactive-tab symptom.

See [the handoff](../handoffs/2026-09-09_orca-time-statistics_event-bar-assignment.md) and [the event check](../../tests/OrcaTimeStatistics.EventCheck/README.md).
