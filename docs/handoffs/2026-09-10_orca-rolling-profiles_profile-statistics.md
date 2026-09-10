# Rolling Profiles statistics — 2026-09-10

## Objective

Add Step Profile / Leg-to-Leg style statistics to the current Rolling Profile for both Time and Volume bases. Julian explicitly said the preceding volume-basis feature has not yet been tested; neither feature is manually validated.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaRollingProfiles.cs`
- `tests/OrcaRollingVolume.Tests/VolumeTests.cs`, `Harness.txt`
- `docs/indicators/ORCA_ROLLING_PROFILES.md`
- This handoff.

The indicator was clean at `bcf6ce8` before this change. Its pre-statistics copy is `.codex-backups/rolling-volume-20260910/OrcaRollingProfiles.before-statistics.cs`. Unrelated work was preserved.

## Behavior added or changed

- A compact `T <delta> | F <finish> | <percent>% | V <volume>` row for the retained rolling window, with Step Profile's exact Finish Delta and formatting methods.
- Total delta and volume are sums of retained trades; percentage includes unclassified volume in its denominator. Finish Delta subtracts the rolling cumulative maximum when final delta is nonnegative, or the rolling cumulative minimum when negative. Extrema start at zero at the current window's left edge, including partial-volume trimming.
- The ordered aggregate supports equal-time insertion order and out-of-order timestamps without scanning the retained history. It resets with the existing profile/source reset.
- With statistics enabled, original trade order/timestamps are retained rather than time/price spike coalescing. This avoids losing cumulative extrema and avoids timestamp reordering when entering/exiting coalescing. Existing Time coalescing remains when statistics are disabled. Volume mode already disabled coalescing.
- Optional provider batches now preserve equal-time source order when statistics are enabled, as Volume mode already did. Mixed provider records retain their existing proportional integer-delta limitation at a partial boundary.
- Corrected the existing Time-mode zero-net price-row expiry case: if the net row was removed at zero while opposing retained trades remained, subsequent expiry now recreates the remaining signed delta. This keeps the price map and window statistics consistent. Volume mode already had this correction.

## User-facing settings

Seven settings under `09 Profile Statistics`: Show Profile Statistics (false); Show Total Delta, Show Finish Delta, Show Delta Percent, Show Total Volume (all true); Statistics Font Size (12, range 8–30); Statistics Text Color (white, serialized brush). No separate historical/active switches for a single rolling profile. No previous setting removed, hidden or renamed. Reload when enabling statistics so loaded trades populate its state.

## Secondary series and event paths

No Tick, Second, Bid, Ask, Last, Volumetric or custom series added or changed. Uses the existing conditional local Tick 1 `AddDataSeries`, `Calculate.OnPriceChange`, `OnBarUpdate` ingestion, quote-only `OnMarketData`, or existing shared-provider batches. No `Calculate.OnEachTick` path added. Diagnostics callback/model/render reporting remains intact.

## Tick Replay and historical-load implications

No new Tick Replay requirement or historical request. Existing classified input supplies statistics. With statistics enabled, historical load also builds the ordered aggregate; source history and quote fidelity limitations remain. Local hydration continues to hide incomplete profiles/statistics until the existing readiness gate. The earlier volume-basis manual tests remain pending.

## Cache implications

No shared cache, database, file format or provider service changed. A private AVL aggregate retains original trade volume/delta and subtree sum/minimum/maximum prefixes only while statistics are enabled. It is cleared with profile state. No provider migration or data cleanup.

## Rendering implications

Model callbacks calculate the totals, Finish Delta and percentage. Rendering copies four scalar values under the existing snapshot lock and formats/draws one text row after the profile shapes. No new model calculation, history traversal, provider query or synchronization wait in OnRender. The row is trailing-aligned to the visible profile right edge (including scale-facing delta), clamped four pixels inside the panel, and allowed to extend left. It remains usable with either or both shapes hidden. Narrow panels clip text. Dedicated brush/TextFormat are reused and disposed on target change/termination; font-size changes recreate the format.

## Performance implications

Statistics off adds no trade nodes or statistics graphics resources. Enabled updates and removals cost O(log N) per inserted/expired trade; an expiry burst costs O(K log N) for K removed trades. Retained memory is O(N) in source records, in addition to the existing profile record storage. Time mode with statistics enabled no longer uses spike coalescing, so it can retain more records and expire at exact source timestamps rather than coalesced boundaries. No live CPU/memory improvement or measured chart performance is claimed.

## Tests and compile status

- Production methods tested against 20,000 randomized chronological/out-of-order volume streams and 3,000 Time-mode updates, including independent contract-level prefix extrema and map/statistics reconciliation.
- Partial oldest trade, oversized trade, equal-time order, subsecond burst ordering, Time coalescing preserved with statistics off, RTH filtering, session carry, reset, hydration rebuild, unclassified volume, zero-final-delta Finish rule, and mixed provider delta cases pass.
- All 16 metric visibility combinations and K/M boundaries pass. Exact formula/format source tokens match Step Profile.
- Two 30,000-node ascending/descending insertion cases confirm bounded tree height and correct aggregates after large time and volume expiry.
- Existing setting identities, time-window calculation and period mapping preservation checks pass.
- Authored source semantic compilation against installed NinjaTrader references: zero errors.
- Deployed authored parity and offline semantic compilation of installed source including generated wrappers: passed, zero errors. Installed wrappers include the statistics toggle, Finish Delta toggle and statistics font size; generation was observed, not treated as F5/load proof.
- Scoped diff whitespace checks passed.
- NinjaTrader F5 and UI tests: not performed; native app control unavailable. Offline compilation is not platform load or live validation.

## Deployment

Completed with `Orca Trades/Scripts/deploy_orca.ps1 -Target OrcaRollingProfiles.cs`. Only this indicator was copied. Installed pre-change source is backed up at `.codex-backups/rolling-volume-20260910/OrcaRollingProfiles.installed-before-statistics.cs`. Normalized authored parity passed. No mirror, dependency or Full_Suite deployment.

## Manual validation and known risks

Pending Julian: F5/load; enable statistics and reload; check both rolling bases, 5k/50k/200k saturation and Time expiry; compare Total Delta with the profile; inspect individual/all-off toggles, font/color persistence, both delta directions, volume-only/delta-only/both-hidden layouts, resize/pan/zoom/render-target recreation; monitor busy-market Diagnostics and memory. The current single compact row may clip on a narrow chart. Source quote/history/provider limitations still apply. No live or template validation is claimed.

## Full_Suite eligibility

Not eligible. Full_Suite remains untouched until F5/load and Julian's manual validation pass.
