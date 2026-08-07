# Orca Product State

Last updated: 2026-08-01

## Current Status

Orca is a commercial NinjaTrader 8 suite for futures/order-flow visualization, profile work, VWAP/session context, execution visualization, and risk/trade-management add-ons.

Active development source is `Orca Trades/Working_Suite`. `Orca Trades/Full_Suite` is the validated promotion target and should not be changed until Julian confirms NinjaTrader compile and manual behavior.

Current active development includes the live-tested first Phase 0 Discipline Guard monitoring slice. Other status entries remain evidence-specific snapshots and are not promoted to `Full_Suite` without Julian's validation.

## Current Suite Inventory

Working_Suite currently contains:

- Indicators: `OrcaAbsorptionCandles`, `OrcaAnchoredVWAPs`, `OrcaCandleVolumeProfile`, `OrcaCumulativeDelta`, `OrcaExecutionLines`, `OrcaExecutionLines2`, `OrcaLegtoLegProfile`, `OrcaMGIDaily`, `OrcaMGIStatistics`, `OrcaMGIWeekly`, `OrcaPrints`, `OrcaProfileDataProvider`, `OrcaRollingProfiles`, `OrcaSessionContextMap`, `OrcaStepProfile`, `OrcaTickDirectionIndex`, `OrcaTimeStatistics`, `OrcaTimeVWAPs`, `OrcaVisibleRangeVolumeProfile`, `OrcaVisualOrders`, `OrcaVolumeProfileCore`.
- OrcaPrints partials: `OrcaPrints.Engine`, `OrcaPrints.Models`, `OrcaPrints.Rendering`, `OrcaPrints.Scoring`.
- Drawing tools: `OrcaFixedRangeProfile`, `OrcaManualAnchoredVWAP`.
- Bars types: `OrcaAtrAdaptiveRangeBarsType`.
- Chart styles: `OrcaVolumeCandles` (`Orca Volume Candles`) in active `Working_Suite` development; NinjaTrader compile and live-chart validation are tracked separately in its dated handoff.
- Add-ons/support: `OrcaCopyAddOn`, `OrcaCopyEngine`, `OrcaCopyNetwork`, `OrcaDisciplineGuardAddOn`, `OrcaExecutionRouterAddOn`, `OrcaRiskManagerAddOn`, `OrcaTradeCopierAddOn`, `OrcaTradeCopierEngine`, `OrcaTradeCopierNetwork`.

## Development Versus Validated Status

Known current development state from Git status on 2026-06-25:

- Modified Working_Suite files: `OrcaRiskManagerAddOn.cs`, `OrcaLegtoLegProfile.cs`, `OrcaMGIDaily.cs`, `OrcaPrints.Rendering.cs`, `OrcaPrints.cs`, `OrcaRollingProfiles.cs`, `OrcaStepProfile.cs`.
- Untracked paths: `.codex-backups/`, `Orca Full Suite/`, `Orca_NinjaTrader_sync_55d9422.zip`.
- These dirty files were not edited in this pass.

Validated status remains unknown unless a handoff or Julian confirms NinjaTrader compile and behavior. Do not infer validation from repo state.

## Current Active Work

The active reliability concern is workspace startup, historical-load completeness, Tick Replay amplification, cache/state sequencing, and render/calculation cost across a distributed multi-chart workspace.

## Historical-Data And Startup Incident Summary

Facts reported by Julian:

- Some existing MNQ September-contract charts appeared to have roughly a ten-minute historical gap after NinjaTrader restart.
- A clean new MNQ one-minute chart loaded complete data immediately.
- A one-minute chart from an existing template also loaded complete data and included Orca Prints, MGI-related tools, and Rolling VWAP.
- Some affected charts had Tick Replay disabled.
- Enabling Tick Replay on a chart with about five days of MNQ data stayed in Calculating for more than fifteen minutes.
- Later, the complete workspace loaded successfully with all data and became usable in roughly two minutes.

Interpretation:

- The gap is not proven to be missing broker/NinjaTrader historical data.
- The current evidence fits startup sequencing, chart-instance state, hidden/secondary series hydration, shared cache availability, Tick Replay amplification, rendering, or stale chart state.
- Broad data reloads and broad cache clearing are not default recovery actions.

## Tick Replay Risk Summary

Tick Replay risk is highest in components that process every tick or maintain hidden 1-tick series:

- `OrcaAbsorptionCandles`
- `OrcaCumulativeDelta`
- `OrcaCandleVolumeProfile`
- `OrcaLegtoLegProfile`
- `OrcaProfileDataProvider`
- `OrcaRollingProfiles`
- `OrcaStepProfile`
- `OrcaTickDirectionIndex`
- `OrcaVisibleRangeVolumeProfile`
- `OrcaPrints`

Tick Replay may be needed for historical per-tick accuracy in some order-flow tools, but this must be measured per component and configuration.

## Current Performance Evidence

Reported NinjaScript Utilization Monitor cumulative entries after workspace load included high bars-type totals for 1 Tick, 15 Second, 30 Second, 30 Minute, 930 Minute, range bars, 5 Minute, and smaller visible entries for Orca Manual Anchored VWAP, OrcaTimeVWAPs, PriceLine, and OrcaExecutionLines.

This is a ranking signal only. It is not startup-only timing and does not identify a specific source chart or indicator instance.

## Known Unknowns

- Which chart instances in the real workspace own each hidden 1-tick, 30-second, minute, range, or custom series.
- Whether the MNQ gap came from data availability, chart state, cache state, indicator model state, or rendering.
- Which Orca modules require Tick Replay for historical correctness versus live intrabar behavior only.
- Whether shared provider registration timing is delayed during workspace startup.
- Whether local tick-series hydration duplicates work that could use `OrcaProfileDataProvider`.
- Whether render work is significant during the startup window.
- Whether `OrcaProfileDataProvider` persistent cache is enabled in Julian's workspace.

## Root-Cause Hypothesis Matrix

This matrix ranks current hypotheses from code inspection and Julian's observations only. It is not a confirmed root-cause list.

| Hypothesis | Probability | User impact | Ease of measurement | Ease of remediation | Evidence needed |
| --- | --- | --- | --- | --- | --- |
| Multiple Orca modules hydrate independent hidden 1-tick series for the same MNQ range during startup. | High | High | High with series-map diagnostics | Medium; may use shared provider defaults or dedupe | Per-instance series map, Tick Replay event counts, provider availability timing |
| Tick Replay amplifies per-tick historical work across profile/prints/delta tools. | High | High | High with lifecycle/event counters | Medium; component-specific fast paths may be possible | Off/on benchmarks for one-day and five-day MNQ history |
| Shared provider/cache data is unavailable or late during workspace startup, forcing local fallbacks or empty models. | Medium | High | Medium with cache registration/snapshot telemetry | Medium | Provider registration timestamps, snapshot hit/miss, source age, fallback reason |
| Stale chart-instance state or startup sequencing caused the visible MNQ gap while raw historical data was complete. | Medium | High | Medium; needs workspace startup report and chart-state capture | Low to medium if isolated to reload/recreate chart workflow | Compare affected chart instance against clean chart/template with same series and tools |
| Render-triggered profile rebuild or snapshot refresh delays chart usability. | Medium | Medium | Medium with render sampling and profile rebuild counters | Medium | `OnRender` timing, rebuild count, snapshot age, cache-read-in-render detection |
| Persistent/local cache contains partial or stale tick data for a narrow range. | Low to medium | High | Medium with cache gap telemetry | Medium; recovery must be scoped | Cache key, returned tick count, first/last tick timestamp, largest gap |
| Broker/NinjaTrader historical data was genuinely missing. | Low based on clean-chart evidence | High | High with clean chart and raw historical comparison | Low only if platform-side reload is scoped | Same instrument/contract/time range comparison outside Orca paths |

## 2026-06-26 Orca Prints Tick Replay Chart Incident

Facts reported by Julian on 2026-06-26:

- A one-minute chart with three days of history and Tick Replay enabled took about ten minutes to finish loading/calculating.
- After calculation completed, the chart showed an approximately 30-minute no-data gap around 10:00 a.m. to 10:30 a.m.
- Chart indicators were `OrcaPrints`, `OrcaStepProfile`, `OrcaAbsorptionCandles`, `OrcaLegtoLegProfile`, and `Orca Time VWAPs`.
- Tick Replay was enabled specifically to inspect historical Orca Prints.

Chart-specific source-map interpretation:

- `OrcaPrints` processes replayed `OnMarketData` Last/Bid/Ask events and has `Calculate.OnEachTick`; it does not add its own secondary series.
- `OrcaStepProfile` adds a hidden 1-tick series and processes `BarsInProgress == 1` tick events.
- `OrcaAbsorptionCandles` adds a hidden 1-tick series and processes `BarsInProgress == 1` tick events.
- `OrcaLegtoLegProfile` defaults to `SecondaryTickSeries`, which adds a hidden 1-tick series; it also has a `TickReplayLastEvents` mode that avoids this secondary series path.
- `Orca Time VWAPs` does not add a secondary series, but it still updates on primary price changes and uses volume deltas by bar.

Current interpretation:

- The ten-minute load is plausible but not acceptable for this chart stack because Tick Replay plus three separate hidden 1-tick series can multiply historical tick work.
- The 30-minute gap is not expected and must be separated into either price-bar data absent, Orca model absent, or render output absent.
- Do not assume broker/NinjaTrader data loss until the same time window is checked on a clean chart with the same instrument, contract, trading-hours template, and Tick Replay setting.

Immediate measurement sequence:

1. Open a clean one-minute chart for the same instrument/contract, same trading-hours template, same three-day range, Tick Replay on, no Orca indicators. Confirm whether price bars exist from 10:00 to 10:30 and record load time.
2. Add only `OrcaPrints`. Record load time and whether historical prints appear before, during, and after 10:00 to 10:30.
3. Add `Orca Time VWAPs`. Record load time and whether price bars/prints remain intact.
4. Add `OrcaAbsorptionCandles` only, then test again.
5. Add `OrcaStepProfile` only, then test again.
6. Add `OrcaLegtoLegProfile` in default `SecondaryTickSeries` mode, then test again.
7. Repeat `OrcaLegtoLegProfile` with `Trade Source Mode = TickReplayLastEvents`, if available in the deployed build, to test whether removing one hidden 1-tick secondary series materially improves load time or gap behavior.

Updated evidence from Julian later on 2026-06-26:

- Screenshot shows a visible time-axis gap after reload.
- Julian removed all indicators and reloaded the same one-minute, three-day, Tick Replay chart; the chart loaded quickly, but the 10:00-10:30 gap remained.
- Julian then reloaded historical data; the gap remained.
- Julian observed that enabling Tick Replay or reloading history on one MNQ chart appears to trigger reload/calculation behavior across several other open MNQ charts.

Updated interpretation:

- Missing bars are now more likely a NinjaTrader chart/data-series/session/template/cache/provider issue than an Orca indicator issue, because the gap survived with all indicators removed.
- Orca indicators remain likely contributors to the earlier long calculation time when loaded on the chart, especially due to hidden 1-tick series and Tick Replay event volume.
- The immediate next test is a brand-new MNQ one-minute chart with the same contract, same trading-hours template, same three-day range, and Tick Replay on. If the new chart has complete bars, the old chart/template instance is suspect. If the new chart has the same gap, the missing span is likely upstream of Orca and should be checked against NinjaTrader historical data/session settings/data provider behavior.

Further evidence from Julian on 2026-06-26:

- Julian loaded the original one-minute chart template with Tick Replay off so it would load quickly before restarting NinjaTrader.
- With Tick Replay off, the same template/chart loaded complete one-minute data; the visible 10:00-10:30 missing region was no longer apparent.

Updated interpretation:

- Regular one-minute historical data appears to exist for the missing window.
- The gap is now most consistent with Tick Replay/tick-level historical data, replay cache state, or replay-specific chart construction/state rather than missing minute data or Orca indicator rendering.
- Next test should compare the same clean/template chart with Tick Replay off versus Tick Replay on after a full NinjaTrader restart, ideally with other MNQ charts closed.

Bare one-chart restart reproduction from Julian on 2026-06-26:

- After restarting NinjaTrader, Julian tested only one MNQ one-minute chart.
- With Tick Replay off, all one-minute data loaded correctly.
- With Tick Replay on, the large gap returned.

Current strongest conclusion:

- The missing region is no longer likely to be Orca indicator logic, chart template logic, or regular one-minute historical data loss.
- The reproducible failure path is Tick Replay/tick-level historical replay for MNQ on that window.
- Next troubleshooting should be scoped to NinjaTrader Tick Replay data/cache/provider behavior for the affected contract and date/time window, not broad Orca changes.

Confirmed tick-history evidence from Julian on 2026-06-26:

- Julian tested a 1-tick MNQ chart for the same contract/window.
- The 1-tick chart is also missing data, approximately 9:49 a.m. to 10:41 a.m.

Current conclusion:

- The root issue for the visible gap is missing or corrupt historical tick data for the affected MNQ contract/time range.
- Tick Replay on the one-minute chart exposes the same tick-history hole; Tick Replay off can still build minute bars from available minute historical data.
- Orca indicators can still add calculation load, but they are not the cause of the missing time window.
- Recovery should be a targeted historical tick-data repair/redownload for the exact MNQ contract and date/time range, not an Orca code change or broad workspace reset.

Mixed bar-type evidence from Julian on 2026-06-26:

- Julian downloaded MNQ September tick data for Ask, Bid, and Last from June 25 through June 26.
- After reloading, the missing window still did not appear on the 1-tick chart.
- The window did appear on 30-second and 5-second charts.
- The window did not appear on the 1-minute chart.

Updated interpretation:

- NinjaTrader appears to have inconsistent historical stores or cached aggregations by bar type/data type: second bars are available, while tick and one-minute chart paths still show the hole.
- This is still outside Orca indicator logic.
- Next safe recovery should target the specific MNQ September 2026 Tick and Minute data stores for June 26, not Second data and not the whole database.

Additional bar-period evidence from Julian on 2026-06-26:

- The 2-minute and 5-minute MNQ charts show the previously missing window.
- The 1-minute MNQ chart still does not show the window.

Updated interpretation:

- The problem is not all minute-style historical bars. It appears specific to the 1-minute historical/bar-cache path plus the 1-tick/Tick Replay path.
- NinjaTrader may be serving or caching 1-minute, 2-minute, 5-minute, second, and tick data through distinct historical requests or stored aggregations.
- Targeted cleanup should prioritize MNQ September June 26 `Last > 1 Minute` if visible in Historical Data Edit, otherwise the June 26 `Last > Minute` node, plus the affected Tick nodes needed for Tick Replay.

Historical Data Edit screenshot evidence from Julian on 2026-06-26:

- Historical Data Edit for `MNQ SEP26 > Last > Minute > June 2026 > 6/26/2026` shows minute rows through the suspected window, including 9:49 through 10:41, with OHLC and volume values present.

Updated interpretation:

- The NinjaTrader historical minute database contains the affected one-minute bars.
- The 1-minute chart gap is therefore more likely a chart/bar-cache construction or loaded-chart state issue than absent `Last > Minute` rows.
- Do not delete minute data based on current evidence. Next check should be `MNQ SEP26 > Last > Tick > 6/26/2026` for the same 9:49-10:41 window.
- If tick rows are present too, then recovery should focus on forcing chart/bar cache rebuild or opening a new chart/session rather than deleting valid historical rows.

Historical Data Edit tick screenshot evidence from Julian on 2026-06-26:

- Historical Data Edit for `MNQ SEP26 > Last > Tick > June 2026 > 6/26/2026` shows tick rows in the 10:00 a.m. hour, including rows at 10:00:00.xxx with price and volume values.
- The tree shows the 10:00 AM tick bucket has many items.

Updated interpretation:

- Both `Last > Minute` and `Last > Tick` appear to contain data for the affected period in Historical Data Edit.
- The chart gap is now more consistent with chart/bar-cache/session construction or loaded chart state than absent historical rows.
- Do not delete historical minute or tick rows based on current evidence. Next recovery should focus on forcing NinjaTrader to rebuild chart bars from existing data: close affected charts, restart NinjaTrader, open a fresh chart, and avoid reusing the suspect chart/tab/template until the fresh chart behavior is known.

Resolution evidence from Julian on 2026-06-26:

- Julian renamed `Documents\NinjaTrader 8\db\cache` to `cache_backup_2026-06-26` with NinjaTrader closed, leaving `day`, `minute`, `tick`, `replay`, and `NinjaTrader.sqlite` untouched.
- After reopening NinjaTrader, a one-minute MNQ September chart with Tick Replay on showed all data correctly.

Current resolution:

- The gap was caused by stale or corrupt NinjaTrader chart/bar cache state, not missing historical minute/tick rows and not Orca indicator logic.
- The reversible cache-folder rename was the successful recovery path.
- Future similar incidents should first verify Historical Data Edit rows, then rebuild `db\cache` before deleting real historical data.

Workspace validation after cache rebuild on 2026-06-26:

- Julian reopened the real workspace after the cache rebuild and reported that it completed, loaded fine, and everything appeared to function correctly.
- The cache incident is resolved for the live workspace, but it exposed a broader product need: workspace-level observability during startup, RTH open, news spikes, and MNQ high-tick-volume conditions.

Next product priority:

- Build Orca Diagnostics / Workspace Load Observatory as an internal live reliability surface, not only a startup log.
- First questions it must answer: which order-flow source each indicator uses, whether each indicator is receiving live real data, which modules add hidden secondary series, which modules are lagging behind realtime, and where tick/event/render work is concentrated.
- Optimization should follow measured evidence, with special focus on reducing redundant hidden 1-tick consumers and moving toward shared order-flow/provider paths where behavior remains correct.

## Open Decisions

- Standard diagnostic output location and retention policy.
- Whether diagnostics are per-indicator settings, a shared global setting, or both.
- Whether Phase 1 writes JSONL, CSV, or both.
- Whether shared-provider adoption should become default for profile consumers after measurement.
- Whether Tick Replay compatibility labels should be shown in UI or kept internal.

## Highest-Priority Next Steps

1. Add a disabled-by-default diagnostics core with `Off`, `StartupSummary`, and `Verbose`.
2. Instrument lifecycle, series map, shared cache registration/snapshot, and render timing without changing trading behavior.
3. Run a distributed workspace benchmark: clean launch, normal restart, Tick Replay off/on, one day/five days MNQ, cold/warm cache.
4. Use measurements to decide whether to optimize shared cache, local tick-series hydration, Tick Replay paths, profile rebuilds, or render snapshots.

## Recent Benchmark Records

No reproducible benchmark records exist in-repo yet. The utilization monitor table from the prompt is observational evidence, not a controlled benchmark.

## Files Requiring Audit Or Instrumentation

Highest priority:

- `Orca Trades/Working_Suite/Indicators/OrcaProfileDataProvider.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaVolumeProfileCore.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaRollingProfiles.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaVisibleRangeVolumeProfile.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaStepProfile.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaLegtoLegProfile.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaCandleVolumeProfile.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaPrints.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaPrints.Engine.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaPrints.Rendering.cs`
- `Orca Trades/Working_Suite/BarsTypes/OrcaAtrAdaptiveRangeBarsType.cs`

Second priority:

- `Orca Trades/Working_Suite/Indicators/OrcaCumulativeDelta.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaAbsorptionCandles.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaTickDirectionIndex.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaSessionContextMap.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaMGIDaily.cs`
- `Orca Trades/Working_Suite/DrawingTools/OrcaFixedRangeProfile.cs`
- `Orca Trades/Working_Suite/DrawingTools/OrcaManualAnchoredVWAP.cs`
## 2026-07-25 Discipline Guard, Rule Book, And Report Card Review

### Product Role And Current Stage

The intended product is one connected behavioral-development system:

- **Rule Book:** defines what the trader has committed to before the session.
- **Discipline Guard:** observes behavior during the session and delivers timely interventions.
- **Report Card:** turns completed sessions into evidence, reflection, and one concrete improvement focus.

The current `OrcaDisciplineGuardAddOn.cs` is a useful prototype of all three surfaces, but it is not yet a dependable always-on guard or a longitudinal trader-growth product. It can configure rules, detect several violations, score the open session, and export a report. The next work must make capture automatic, state durable, trade attribution trustworthy, and the review loop actionable.

### Verified Current Capability

The current Rule Book supports:

- Three persisted templates: `Prop Firm Discipline`, `Manual Discipline`, and `Conservative Scalping`.
- Add Rule, Delete Rule, Save Template, and Clone Template actions.
- Ten automated rule types and manual checklist rules.
- Account selection, instrument filtering, enabled state, severity, notes, and rule parameters.
- Mini/micro position-size normalization for the configured symbols, including a default limit of 2 NQ or 20 MNQ contracts.

The current Discipline Guard supports:

- Manual session start, pause/resume, end, and reset.
- Account `OrderUpdate`, `ExecutionUpdate`, `PositionUpdate`, and `AccountItemUpdate` subscriptions.
- One-second timer evaluation.
- Live score, grade, account realized P&L delta, completed trades, violations, current position, and loss streak.
- Automated checks for cooldown, position size, trade loss, session loss, trade count, loss streak, trading window, violation count, adding to a losing trade, and immediate reversal after a loss.

The current Report Card supports:

- A single-session summary.
- Rule and violation snapshots.
- JSON session export and CSV violation export.
- Copyable summary text.

### Validation And Usage Evidence

| Gate | Current evidence | Status |
| --- | --- | --- |
| Working source | `Orca Trades/Working_Suite/AddOns/OrcaDisciplineGuardAddOn.cs` reviewed on 2026-07-25. | Confirmed |
| Source/deployed parity | Working_Suite, Full_Suite, and live NinjaTrader copies share SHA-256 `8A0DCA0376A35CA4023770E5E8F2FDFD4B6D3E7B8D411455C61CD1E77ACAA751`. | Confirmed parity only |
| Add-on load | `Diagnostics.log` shows repeated successful Control Center menu injection through 2026-07-25. | Confirmed |
| Window open/basic UI | Julian previously confirmed that the window opened and looked correct after the WPF binding fix. | User-confirmed |
| Add/delete/template workflow | Present in source and deployed copy. | Manual validation not recorded |
| 2 NQ / 20 MNQ sizing | Present in source and deployed copy. | Manual validation not recorded |
| Completed session archive | No `Documents/NinjaTrader 8/OrcaDisciplineGuard/Sessions` directory existed during the 2026-07-25 review. | No usage evidence yet |
| Current compile | No NinjaTrader `F5` compile was performed for this documentation review. | Pending for next code change |

Identical file hashes do not promote or validate the component. `Full_Suite` must remain unchanged until Julian confirms the required live behavior.

### Foundation Risks

These are the highest-priority issues because they can make a report look authoritative while its capture is incomplete:

1. **Monitoring lifetime is tied to the window.** The engine is created by the window view model and disposed when the window closes, which stops the timer and unsubscribes account events.
2. **Arming is manual.** A trader can forget to open the window, select an account, and start a session. The current saved settings contain no selected account.
3. **Active state is not durable.** There is no crash/restart recovery, periodic session checkpoint, or automatic finalization. Closing NinjaTrader can lose the active session.
4. **Paused and ended sessions can still mutate.** Position and realized-P&L event handlers do not consistently gate rule evaluation on `Active`.
5. **Default-rule deletion is not durable.** Template loading merges any missing built-in rule back into a built-in template, so a deleted default rule can reappear.
6. **The session report has no trade ledger.** Violations expose a `TradeId` field, but the current violation path does not populate it and completed trades are not serialized into the report.
7. **Trade P&L is gross fill-derived P&L.** Commissions and fees are excluded, seeded open positions have incomplete history, and late starts cannot reconstruct prior executions.
8. **The score is penalty-only.** It subtracts fixed severity penalties from 100, can over-weight repeated event counts, and does not measure applicable opportunities, positive adherence, recovery, or data confidence.
9. **Rule parameters are raw text.** `Key=Value` editing is not a durable product experience and has limited validation.
10. **Micro normalization is a fixed list and 10x ratio.** This handles the requested NQ/MNQ case, but a general solution needs instrument-family or per-symbol risk-unit mappings.

### Product Principles

- **Automatic capture:** the normal workflow should require almost no administrative work during trading.
- **Process before outcome:** a profitable rule-breaking trade should not receive a good discipline grade.
- **Context before judgment:** every violation should identify the trade, rule opportunity, observed value, limit, and surrounding state.
- **One improvement at a time:** the weekly review should recommend one measurable behavior, not produce a wall of criticism.
- **Trust is visible:** monitoring state, event freshness, reconciliation status, and report confidence must be shown.
- **Observation before enforcement:** coaching mode should be the default; blocking or flattening must be explicit, staged, and Sim-tested.
- **Local-first privacy:** behavioral and account data should remain local unless the trader explicitly chooses otherwise.

### Implementation Roadmap

#### Phase 0: Trustworthy Capture

Primary file: `Orca Trades/Working_Suite/AddOns/OrcaDisciplineGuardAddOn.cs`.

Required work:

1. Move monitoring ownership from `OrcaDisciplineGuardWindow`/view model to an add-on-lifetime service.
2. Add explicit `NotStarted`, `Active`, `Paused`, and `Ended` event gates and idempotent event handling.
3. Add account/template assignment, auto-arm options, session scheduling, periodic checkpoints, restart recovery, and automatic finalization.
4. Persist a normalized trade ledger with execution IDs, fills, round trips, rule opportunities, violations, and manual review state.
5. Reconcile session/trade totals against NinjaTrader account data and expose a data-confidence status.
6. Fix default-rule deletion semantics and replace raw parameter strings with typed rule settings.
7. Add focused tests for partial fills, scale-ins, partial exits, reversals, seeded positions, duplicate events, pause/resume, restart recovery, and mini/micro normalization.

Acceptance gate: close the UI while a Sim session remains monitored, restart NinjaTrader during a checkpointed session, and finish with one non-duplicated report whose trade/P&L totals reconcile to NinjaTrader.

#### Phase 1: Rule Book That Traders Will Maintain

Primary file: `Orca Trades/Working_Suite/AddOns/OrcaDisciplineGuardAddOn.cs`, with extraction into focused add-on support files if NinjaTrader compilation permits.

Required work:

1. Create typed editors for each rule type with plain-language definitions and examples.
2. Support template rename, delete, version, duplicate, and assignment by account/instrument family.
3. Replace the universal 10x micro assumption with explicit instrument-family risk units.
4. Add pre-session intentions: allowed setups, maximum risk, trading window, daily focus, and stop conditions.
5. Distinguish automated, per-trade confirmation, post-trade reflection, and session-level review rules.

Acceptance gate: a trader can build and assign a complete rule book without editing `Key=Value` text, and deleted/disabled rules remain deleted/disabled after restart.

#### Phase 2: In-Session Coaching

Primary integration points:

- `Orca Trades/Working_Suite/AddOns/OrcaDisciplineGuardAddOn.cs`
- `Orca Trades/Working_Suite/AddOns/OrcaRiskManagerAddOn.cs`
- `Orca Trades/Working_Suite/AddOns/OrcaExecutionRouterAddOn.cs`

Required work:

1. Add a compact always-visible armed/healthy/paused indicator.
2. Add an intervention ladder: record, notify, require acknowledgment, enforce cooldown, soft-block, and hard-block.
3. Introduce one shared discipline-policy API so Orca order-entry paths can ask for a decision before submission.
4. Keep observe-only mode as the default and validate enforcement on Sim accounts first.
5. Define explicit behavior when monitoring data is stale or unavailable.

Acceptance gate: all Orca-owned order-entry paths use the same policy result, while non-enforcement mode cannot interfere with order submission.

#### Phase 3: Growth Report Card

Persistence and integration candidates:

- Discipline Guard's current JSON session store.
- External Orca Journal source at `C:\Users\julia\projects\OrcaTrading\OrcaJournal\OrcaJournal`, which already has add-on-lifetime trade capture, SQLite repositories, sessions, attachments, tags, and KPI calculation.
- `Orca Trades/Working_Suite/Indicators/OrcaExecutionLines.cs` annotations imported by Orca Journal.

Required work:

1. Choose one canonical trade/session identity and persistence boundary so Discipline Guard and Orca Journal do not create competing records.
2. Link every applicable rule result and violation to a trade or session opportunity.
3. Add daily, weekly, and rolling views for adherence rate, violation recurrence, recovery, and data confidence.
4. Compare performance when a rule was followed versus broken, including expectancy, average loss, drawdown, and the P&L cost after the first violation.
5. Add setup, screenshot, note, emotion, and post-session reflection context through Journal integration.
6. Generate one evidence-backed weekly focus with a measurable target and show whether it improved.

Acceptance gate: a trader can answer, from trusted records, which behavior is costing the most, whether it is improving, and what single behavior to practice next.

### Report Card Metric Model

The first durable report card should separate:

- **Process score:** followed opportunities divided by applicable opportunities, weighted by rule importance.
- **Risk discipline:** position sizing, stop/risk adherence, max loss, and stop-after-limit behavior.
- **Execution discipline:** entry timing, cooldown, reversal, scale-in, and trading-window behavior.
- **Plan discipline:** valid setup and pre-session-plan adherence.
- **Recovery:** behavior after a loss or first violation.
- **Outcome context:** P&L and expectancy shown beside, but not used to excuse, process violations.
- **Data confidence:** complete, partial, reconciled, or needs review.

### Product Success Measures

- At least 95% of expected sessions are captured and finalized without manual export.
- Session trade counts and realized P&L reconcile to the selected source within a documented tolerance.
- At least 95% of automated violations are linked to a specific trade or session opportunity.
- The weekly review can be completed in under five minutes.
- The trader can identify one highest-cost behavior and track its four-week trend.
- False-positive and duplicate-violation rates are measurable and remain below an agreed threshold.

### Performance And Benchmark Status

Discipline Guard is account-event driven. It adds no secondary series, does not use Tick Replay, does not use SharpDX rendering, and does not access shared profile caches. Current runtime work consists mainly of account-event processing and a one-second UI-dispatcher timer.

No controlled Discipline Guard performance benchmark exists. Phase 0 should add bounded counters for received events, duplicate/rejected events, handler duration, checkpoint duration, reconciliation drift, and last-event age. Performance optimization should follow those measurements.

### Immediate Priority

Do not begin with charts or a more elaborate grade screen. The next code change should be **Phase 0 trustworthy capture**, starting with an add-on-lifetime monitoring service, correct state gates, and a durable trade/session schema. Once one complete Sim session survives window close/reopen and produces a reconciled report, the Rule Book and Report Card can safely become richer.

## 2026-08-01 Discipline Guard Background Monitoring And Runtime Health

### Current Development State

The first Phase 0 trustworthy-capture slice is implemented in `Orca Trades/Working_Suite/AddOns/OrcaDisciplineGuardAddOn.cs` and deployed to the live NinjaTrader AddOns folder. `Full_Suite` was not changed.

Completed in this slice:

- The AddOn owns one background `OrcaDisciplineGuardEngine` for its lifetime instead of creating and disposing monitoring with the window view model.
- Closing and reopening the window reattaches the UI to the existing runtime and preserves its account, template, instrument, session, heartbeat, and event-count state.
- Session mutation now requires `Active`; `NotStarted`, `Paused`, and `Ended` sessions are frozen. Duplicate position-rule evaluation after tracker synchronization was removed.
- The UI exposes `READY`, `ARMED`, `PAUSED`, `ENDED`, and `OFFLINE` health states, heartbeat freshness, last account event time, and received event count.
- The former WPF `TabControl` was replaced with a compact Session/Summary segmented switcher so NinjaTrader does not interpret the two internal views as multiple workspace tabs.
- Session actions and Rule Book actions are visually grouped, the header is denser, and neutral status styling avoids implying healthy monitoring before the runtime proves it.
- The existing mini/micro position normalization remains in place, including the 2 NQ / 20 MNQ default relationship.

### Validation Evidence

| Gate | Evidence | Status |
| --- | --- | --- |
| Source syntax | Roslyn parsed the complete source with no syntax diagnostics. | Passed |
| Source hygiene | `git diff --check` passed for the source. | Passed |
| Targeted deployment | Working_Suite was copied only to `Documents/NinjaTrader 8/bin/Custom/AddOns/OrcaDisciplineGuardAddOn.cs`. | Passed |
| Source/live parity | Both files have SHA-256 `CD61E7789F9B8477BA1AE0054A486936B1D9F77CBC9E334364D94746B8A6500B`. | Passed |
| NinjaTrader compile | F5 completed in the NinjaScript Editor with no error rows or error dialog. | Passed |
| Live UI | The Guard opened with `READY`, a healthy heartbeat, last-event time, and live event count; Session/Summary switching worked. | Passed |
| Window lifecycle | The repaired window closed with NinjaTrader's normal single-window confirmation, raised no error, and reopened with the same runtime; the event counter advanced from 64 to 760 while the UI was closed. | Passed for `NotStarted` state |
| Active Sim session | No session was started and no order was submitted during this pass. | Pending Julian Sim validation |
| NinjaTrader restart | No durable checkpoint/recovery exists yet. | Pending Phase 0 |

### Runtime And Performance Impact

This change adds no secondary series, Tick Replay dependency, SharpDX rendering, profile cache access, or historical chart processing. The runtime retains the existing one-second dispatcher timer and account-event subscriptions while the window is closed. Health tracking stores timestamps and counters only; it performs no per-event disk I/O. Lifecycle diagnostics are low frequency.

### Next Phase 0 Priorities

1. Add periodic session checkpoints, startup recovery, and automatic finalization.
2. Add an idempotent execution/fill ledger and duplicate-event rejection.
3. Persist completed trades and link violations to trade or session opportunity IDs.
4. Reconcile trade count and realized P&L against NinjaTrader and expose confidence/drift.
5. Prove an active Sim session survives close/reopen, then prove restart recovery before any enforcement or richer report-card grading.

This slice is not eligible for `Full_Suite` promotion until Julian validates active-session behavior in Sim and the remaining Phase 0 recovery/reconciliation work is complete.
