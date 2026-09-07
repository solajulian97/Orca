# Provider session capture — 2026-09-07

## Objective

Replace the private probe's template-name-only session descriptor with a content snapshot, as groundwork for shared source admission. Connection identity remains unverified; no shared service or consumer migration is enabled.

## Files changed

- Working_Suite/Indicators/OrcaProviderSessionCapture.cs (new)
- Working_Suite/Indicators/OrcaProviderProbe.cs
- tests/OrcaProviderStream.Tests/ProviderSessionCaptureTests.cs (new), Program.cs and project
- tests/OrcaProvider.PlatformCheck/Program.cs (optional installed metadata inspection)
- This handoff.

## Behavior

Capture includes ordered regular sessions (begin/end day and time and trading-day assignment), date-sorted holidays, partial-holiday flags/date/constraint/session lists, and the effective session timezone serialization. Event timestamp timezone is captured separately. Strings are immutable and length-prefixed; no chart, account or mutable TradingHours objects are retained. Template display name is not a compatibility criterion. Null/incomplete definitions and unresolved instrument/data-series template placeholders are rejected.

The capture runs twice and rejects a detected change. This does not establish atomicity against concurrent editing or detect ABA mutation. Caller must use stable owner-controlled initialization metadata. Subsequent edits require source invalidation/recreation; automatic monitoring is not implemented here. TimeZoneInfo serialization conservatively includes labels as well as rules, which can prevent otherwise safe sharing but avoids false equivalence.

The probe now uses the captured definition and event timezone in its still-private key. Its unique per-instance environment remains unchanged. No consumer can discover this registry.

## Platform evidence and limits

Official [PartialHolidays documentation](https://ninjatrader.com/support/helpGuides/nt8/partialholidays.htm) confirms holiday overrides are part of a Trading Hours definition. Installed metadata inspection additionally identified PartialHoliday.Constraint, Sessions and IsEarlyEnd; the offline build binds directly to those installed members rather than guessing their names from documentation. Public property inspection of Bars and MarketData did not establish a reliable originating feed connection. This is not proof that no suitable API exists; connection attribution and continuity ownership remain open work.

## Settings / series / historical / cache / rendering / performance

- No settings/defaults added, changed or removed. No existing consumer, trading or drawing output changed.
- No secondary Tick, Second, Bid, Ask, Last, Volumetric or custom series added/changed. No AddDataSeries calls added.
- Probe retains its existing OnMarketData Last lane and Calculate.OnEachTick; Tick Replay flags and historical event handling unchanged.
- Capture adds initialization-only serialization proportional to metadata size; no new per-tick or render work. No historical rebuild or persistence/database access.
- No subscriptions/timers added, no static platform references. No measured startup/runtime improvement claimed.

## Verification and rollout

- 278 core checks pass, including 16 new fixture-based session capture checks. Fixtures model installed property shapes; they do not run NinjaTrader scheduling or serialization of live templates.
- Installed-platform offline C# 7.3 semantic check: zero errors across seven provider sources.
- No deployment or NinjaTrader F5/manual validation for this slice. Previously deployed probe remains unchanged until a deliberate targeted rollout.
- Full_Suite untouched; not eligible for promotion. Existing unrelated work preserved.

## Next

Resolve feed attribution and connection/replay epoch ownership, plus invalidation on settings changes. Then integrate typed identities into a shared service and test one opt-in consumer in comparison mode. Do not remove existing hidden series or claim history completeness based on matching identities. Diagnostics restart recovery remains evidence of recovered coverage, not proof of compile/reload continuity.
