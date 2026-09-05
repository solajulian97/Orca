# Sequence handoff and pure ingestion adapter

## Objective

Continue the approved provider foundation. Remove timestamp partitioning as a prerequisite for a native lifecycle-based stream, while avoiding false claims of complete historical time coverage. Add an isolated, serialized trade-classification adapter. Julian need not restart or change settings for this source-only slice.

## Files changed

- Working_Suite/Indicators/OrcaProviderStreamCore.cs
- Working_Suite/Indicators/OrcaProviderRegistryCore.cs
- Working_Suite/Indicators/OrcaProviderIngestionCore.cs (new)
- tests/OrcaProviderStream.Tests/Program.cs and project
- This handoff.

## Coverage and lifecycle changes

The existing requested UTC half-open range contract remains available and its tests remain intact. New SourceLifecycle mode has no claimed UTC coverage interval. BeginHistoricalPass accepts an explicitly historical callback lane. CompleteHistoricalPassAndBeginLive atomically seals its accepted sequence prefix and changes phase. Historical and live trades may have identical timestamps, prices, quantities and direction; each callback is preserved as a separate event. No timestamp sorting, clamping or deduplication occurs.

SourceLifecycle requires AppendHistorical/AppendLive rather than generic Append. A historical callback arriving after the handoff is rejected, never silently relabeled. BeginLiveOnly makes no historical-pass or empty-history confirmation. Live source timestamp regression does not erase an event or reorder source arrival sequence.

Coverage distinguishes HistoricalPassCompleted / FullHistoricalPassRetained from ProducerConfirmedHistory / FullConfirmedHistoryRetained. The latter pair refers only to the explicit requested-time-range contract. A completed callback pass is not proof that all trades in a clock-time interval were supplied. Metadata and payload still share one atomic snapshot. Eviction and faults remain explicit.

This core cannot distinguish duplicated source delivery from distinct trades with identical visible attributes. It does not merge SQLite/persistent caches, two independent histories or connection streams. The future platform adapter must supply exactly one canonical callback lane and establish/drain its lifecycle boundary. Errors must be surfaced as source faults rather than caught and ignored. The race fixture exercises rejected late callbacks; it does not certify NinjaTrader's actual transition scheduling.

## Ingestion adapter

OrcaProviderIngestion is platform-independent and wraps one caller-owned publisher lease. Its lock serializes classification, publication and the lifecycle handoff. It accepts explicit UTC time, price, quantity, supplied trade-time quotes, quote-availability evidence, historical/live lane and an explicit classifier-reset flag. It does not poll live quotes or infer a session from local clock time.

Policies: tick direction, or usable trade-time quote classification with tick-direction fallback. Locked/crossed/nonfinite quotes do not count as usable bid/ask evidence. The first trade without usable quotes is Unknown; equal-price fallback carries the prior direction. Reset clears that prior classifier state for the current event. Rejected publication or invalid input does not mutate classifier state. Policy/version and UTC identity must match the publisher key before ingestion starts.

BidAsk denotes classification using supplied quotes, NOT independently verified exchange provenance. NinjaTrader may synthesize historical bid/ask depending on the data source; strict provenance remains an adapter/consumer contract. This is a new opt-in algorithm, not proof of parity with the existing provider classifier. Real session reset decisions, policy-key inclusion of all source settings, platform time conversion and callback wiring remain future work.

## Verification

226 linked-source checks pass: prior storage/ownership/coverage/budget checks plus same-time historical/live boundary preservation, explicit lane rejection, source-pass versus UTC-range truthfulness, retention, live-only mode, empty historical pass, 20 append/handoff concurrency trials, quote/fallback/unknown classification, reset, rejected-event state preservation, policy-identity mismatch and fault rejection. Race trials cover exercised interleavings, not exhaustive concurrency proof.

All four core source files compile with the installed standalone .NET Framework compiler. NinjaTrader F5/load/manual tests: not performed. No platform runtime, end-to-end startup or live-performance improvement claimed.

## Settings / series / cache / rendering implications

No existing indicator code, user settings/defaults, subscriptions, secondary series, Tick Replay flags, OnMarketData or Calculate modes changed. No history/cache/database files touched, no persistence, no rendering or trading modifications, no deployment. The adapter adds no timer, chart/account reference or NinjaTrader call. Full_Suite untouched; not eligible for promotion.

## Next and remaining risks

Next is a narrow opt-in NinjaTrader producer adapter with diagnostic source/phase reporting and no consumer migration initially. Verify canonical OnMarketData Last ingestion, platform timezone conversion, session reset rules and transition callback evidence. Then compare one consumer against local mode before removing any hidden series. The old provider remains unchanged and its known defects are not fixed by these currently disconnected core classes.
