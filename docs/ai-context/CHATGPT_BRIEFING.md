# ChatGPT Briefing For Future Codex Prompts

## What This Project Is

Orca is a NinjaTrader 8 suite for professional intraday trading. It provides order-flow visualization, volume/delta profiles, VWAP/session context, execution visualization, and on-chart trade/risk management.

ChatGPT's role should be product/architecture/prompt planning. Codex's role should be implementation in the local repo.

## Source Of Truth

Editable source:

- `Orca Trades/Working_Suite/Indicators`
- `Orca Trades/Working_Suite/AddOns`

Validated clean suite:

- `Orca Trades/Full_Suite`

Never ask Codex to copy from `Orca Trades/NinjaTrader`, `Stable_Release`, `decompiled`, or live NinjaTrader folders unless Julia explicitly says that exact source is newer and intended to replace current source.

Do not ask Codex to deploy, promote, commit, or refactor unless that is explicitly part of the task.

## Current Implemented Features

- Volume/delta profile family: rolling profiles, step profiles, leg-to-leg profiles, candle/footprint profiles, visible-range profile, shared profile core.
- VWAP/session tools: anchored VWAPs, time/rolling VWAPs, MGI daily structural levels.
- Order-flow tools: absorption candles, cumulative delta, large prints/clusters, tick direction index, time statistics.
- Execution/order tools: execution lines, visual orders, risk manager add-on, execution router add-on.
- Adjacent Orca Journal AddOn exists outside this repo for trade capture, SQLite persistence, tags, dashboards, and SkiaSharp charts.

## Major Files And Modules

- `OrcaVolumeProfileCore.cs`: shared row aggregation, POC, and value-area helper.
- `OrcaVisibleRangeVolumeProfile.cs`: visible viewport volume/delta profile; current dirty changes add locking/snapshots for live render safety.
- `OrcaCandleVolumeProfile.cs`: per-candle footprint/profile; current dirty changes add dynamic volume aggregation and width scaling.
- `OrcaRollingProfiles.cs`, `OrcaStepProfile.cs`, `OrcaLegtoLegProfile.cs`: profile family using volume/delta maps, dynamic aggregation, POC/value areas.
- `OrcaMGIDaily.cs`: daily/session levels, RTH/ETH/ON/IB/OR/VWAP/value area/open/close logic; current dirty changes alter session boundary handling.
- `OrcaTimeVWAPs.cs`: time/session/rolling VWAPs; current dirty changes add RTH end bounds/reset behavior.
- `OrcaTimeStatistics.cs`: bottom-panel per-bar metrics; current dirty changes add separators and better range/time formatting.
- `OrcaExecutionLines.cs`: execution matching, SQLite history, R-multiple analysis; current dirty changes add note/tag persistence/editor work.
- `OrcaVisualOrders.cs`, `OrcaRiskManagerAddOn.cs`, `OrcaPrints.Rendering.cs`: current dirty changes include DPI-safe chart coordinate fixes.
- `deploy_orca.ps1`: preferred deploy script; defaults to Working_Suite.
- `promote_working_to_full_suite.ps1`: preferred promotion script after Julia validates behavior.

## NinjaTrader Constraints

- NinjaTrader compiles the whole live Custom tree, so ghost files can cause misleading errors.
- After deployment, Julia still must press F5 in the NinjaScript Editor.
- Historical, Tick Replay, and realtime order-flow behavior can differ.
- Bid/ask state can be missing historically; delta code often needs uptick/downtick fallback.
- SharpDX resources must be disposed/recreated on render target changes.
- WPF brushes need `[XmlIgnore]` properties plus serializable string companions.
- Be careful with generated NinjaScript cache sections.

## Current Incomplete Areas / Technical Debt

- Current Working_Suite has dirty changes in nine source files plus `SESSION_NOTES.md`; compile/behavior approval is Unknown unless Julia confirms.
- Working_Suite differs from Full_Suite for those nine source files. Do not promote until validated.
- Some scripts are stale or unsafe under current workflow: avoid `deploy_nt8.ps1` and `promote_to_full_suite.ps1`.
- `OrcaExecutionLines2.cs` is a deprecated stub, but deletion needs confirmation.
- Dynamic aggregation flicker/performance remains a theme in profile rendering.
- Active module set outside Full/Working, especially mirror-only MGI Weekly/Statistics and legacy modules, needs confirmation.
- Orca Journal is separate from this repo; integration with execution-line notes/tags needs product decisions.

## Known Fragile Areas

- Visual order/risk price conversion on high-DPI or multi-monitor setups.
- MGI Daily session boundary handling: ETH open, midnight/TDO, RTH open, IB end, PDC close boundary.
- Visible-range true volume/delta during live rendering and multi-chart layouts.
- SharpDX text/brush allocation and disposal.
- Delta classification for same-price prints and missing historical bid/ask.
- Any change that touches live order placement, account event hooks, or execution matching.

## Things Future Agents Should Not Break

- Working_Suite as the only edit lane.
- Full_Suite as validated-only.
- Deployment from Working_Suite by default.
- Manual NT8 F5 compile step.
- Brush serialization patterns.
- SharpDX lifecycle cleanup.
- Existing NinjaScript property names/templates unless intentionally migrating them.
- Profile value-area convention: start at POC and expand toward the larger next-volume row until target VA percent is covered.
- Current dirty work from Julia/other agents.

## Recommended Next Development Priorities

1. Validate dirty Working_Suite changes in NT8 and decide per file whether to promote.
2. Test DPI-safe coordinate fixes in SIM across laptop/external displays.
3. Validate MGI Daily session/open/PDC changes across minute and tick charts.
4. Stress-test visible-range and candle profile aggregation on multi-chart layouts.
5. Decide how `OrcaExecutionLines` notes/tags should align with external Orca Journal before expanding journaling features.

## How ChatGPT Should Write Codex Prompts

Use precise implementation prompts:

- Tell Codex to read `AGENTS.md`, `docs/collaboration-workflow.md`, and `docs/engineering-notes.md`.
- Include `git status --short --branch` as the first step.
- Name exact target files.
- State "do not deploy/promote/commit" unless Julia wants that.
- State source-of-truth rules.
- Provide acceptance criteria and a manual NT8 test plan.
- Ask Codex to report files changed, verification run, remaining unknowns, and whether NT8 compile was actually tested.

Prompt Codex like this:

```text
We need to change [specific behavior] in [exact module].

Edit only Orca Trades/Working_Suite/[Indicators|AddOns]/[file].cs.
Do not copy from NinjaTrader, Stable_Release, decompiled, or live NT8 folders.
Do not deploy, promote, commit, or refactor unrelated code.

Before editing, run git status and inspect any dirty target file.

Implement [exact behavior].
Preserve [important existing behavior].

Verification:
- Run git diff --check.
- Run targeted rg searches for renamed symbols/properties.
- Provide an NT8 manual test plan. If deployment is requested, use deploy_orca.ps1 -Target [target] and remind Julia to press F5.

Return changed files, summary, verification, and unknowns.
```

## External/Adjacent Projects

Orca Journal project is accessible at:

- `C:\Users\julia\projects\OrcaTrading\OrcaJournal\OrcaJournal\`

Main entry:

- `C:\Users\julia\projects\OrcaTrading\OrcaJournal\OrcaJournal\OrcaJournal.cs`

Summary:

- NinjaTrader AddOn named `Orca Journal`.
- Captures account executions through `TradeCapture`.
- Builds open-to-flat trades with `TradeBuilder`.
- Saves to SQLite at `Documents/OrcaJournal/orca_journal.db`.
- Uses repositories for trades, sessions, tags, and tag rules.
- UI is WPF/NTWindow with Dashboard, Trades, and Tags tabs.
- Charts are static SkiaSharp-rendered bitmap images. Source comments say LiveCharts2 WPF controls were removed due to NT8 D2D visual-tree conflicts.
- Known limitation: V1 `TradeBuilder` does not split reversal fills.

