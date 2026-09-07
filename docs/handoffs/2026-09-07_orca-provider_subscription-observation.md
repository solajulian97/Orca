# Subscription-boundary observation probe — 2026-09-07

## Objective / recovered evidence

Julian supplied four historical multi-reader probe runs, each showing readerComparison=PASS with approximately 2.95 million verified events and live=0. This supports transport consistency for those runs, not live subscription snapshot discrimination, reconnect behavior, or production consumer parity.

The next probe observes the actual instrument subscription boundary without feeding any provider. Official [MarketData documentation](https://docs.ninjatrader.com/ninjascript/marketdata) says a snapshot is supplied on subscription. [MarketDataEventArgs documentation](https://ninjatrader.com/support/helpGuides/nt8/marketdataeventargs.htm) defines IsReset as a disconnect/UI-reset indication, not a snapshot flag. Therefore no events are promoted to trades based on IsReset, callback order, subscribe-return timing or timestamp equality.

## Files changed

- Working_Suite/Indicators/OrcaProviderSubscriptionProbe.cs (new)
- tests/OrcaProviderSubscription.Tests/ project, PlatformStubs.cs and Program.cs (new)
- tests/OrcaProvider.PlatformCheck/Program.cs (structural guards)
- This handoff.

## Behavior and ownership

Explicitly adding OrcaProviderSubscriptionProbe schedules one Instrument.MarketData.Update attachment on the instrument dispatcher when the indicator reaches Realtime. It records aggregate callback counts during and after the add accessor, Last/reset counts and at most eight callback detail strings. A five-second dispatcher timer detaches its handler and emits a bounded summary. Delay in dispatcher scheduling may extend the measured interval; elapsed time is reported.

The observer is separate from OrcaProviderProbe. It does not override OnMarketData, ingest historical Tick Replay, publish to a registry, or claim after-add callbacks are fresh trades. It creates no production consumer or shared source. It need not be on a Tick Replay chart.

Removal before attachment cancels the pending operation. Removal reentered during the add accessor sets a stop flag and defers detachment until that accessor exits, preventing a late-installed handler from surviving cleanup. ShutdownStarted performs cleanup on the instrument dispatcher. The observer retains no chart/indicator reference; it holds only its instrument and bounded observation state. A failed add accessor triggers removal of its exact possibly-installed handler; failed detachment is reported explicitly, retains the shutdown hook, and can be retried during later removal/shutdown. Cleanup is not claimed successful when removal throws. This conservative exception path cannot certify the internal atomicity of NinjaTrader event accessors.

## Settings / series / cache / history / rendering / performance

- No configurable user settings added or altered; the indicator itself is an opt-in diagnostic tool.
- No additional Tick, Second, Bid, Ask, Last, Volumetric or custom secondary series. No AddDataSeries.
- One temporary instrument-level market-data event subscription and one dispatcher timer are introduced by this probe only. No global connection handler is necessary because no events are published or joined across connections.
- No Calculate mode override, automatic OnMarketData override, provider/cache read/write, persistence, database, trading, account or order operations.
- Tick Replay flags and existing indicators remain unchanged. Historical replay is not consumed by this observer. Adding an indicator may still cause NinjaTrader to reload that chart normally.
- No OnRender, drawing output, chart overlay or render-time work. Callback detail formatting stops after eight events; aggregate counters continue for the observation window. This is instrumentation overhead, not an optimization or performance benchmark.

## Verification / deployment

- 14 linked-source lifecycle fixture checks pass: dispatcher scheduling, one handler, timer detachment, during/after-add observation, bounded details, no publication, idempotent cleanup, removal before/during attachment (including before actual handler installation), shutdown, partially successful add failure, and failed-detach reporting/retry.
- Fixtures do not establish actual NinjaTrader callback timing or snapshot semantics. The separate installed-platform semantic check binds actual API types.
- Offline C# 7.3 installed-platform compile: zero errors across ten provider sources. Structural guards passed; they prohibit provider publication, added series, OnRender and an automatic OnMarketData override in the observer.
- Preflight confirmed the new live target was absent. Targeted deployment copied only OrcaProviderSubscriptionProbe.cs; normalized authored-source parity passed. No existing live source was overwritten. NinjaTrader was running; no F5 action or successful assembly load was claimed.
- NinjaTrader F5 and runtime observation pending Julian; no actual observation run performed by the agent. Full_Suite untouched; not eligible for promotion.

## User check / next gate

Compile with F5, then add OrcaProviderSubscriptionProbe to one existing chart, preferably a non-Tick-Replay chart to minimize reload work. Keep all existing settings. After about five seconds in Realtime, collect the summary and up to eight following callback lines. Expected cleanup evidence is detached=True and published=0. This is an observation, so no particular callback count or snapshot-marker value is assumed. Remove the indicator after collecting output. No forced restart, trade placement or stopwatch required.

Use the observations to refine the snapshot boundary; one run alone does not prove that all providers or asynchronous interleavings behave identically. Do not wire a production live subscriber until snapshot discrimination and historical/live handoff have a defensible contract.
