# Orca Agent Coordination Protocol

## Source of truth

- Active development source: `Orca Trades/Working_Suite`
- Validated promotion target: `Orca Trades/Full_Suite`
- Do not modify or promote files into `Full_Suite` until Julian has compiled and manually validated the change in NinjaTrader.
- Do not copy from `Orca Trades/NinjaTrader`, `Stable_Release`, `decompiled`, or local NinjaTrader folders into `Working_Suite` or `Full_Suite` unless Julian explicitly confirms that the source file is newer and intended to replace the current version.

## Required reading before any task

Read, in order:

1. `AGENTS.md`
2. `docs/ORCA_PRODUCT_STATE.md`
3. `docs/ORCA_ARCHITECTURE.md`
4. `docs/ORCA_DIAGNOSTICS_SPEC.md`
5. Relevant component documentation in `docs/indicators/`
6. Recent relevant files in `docs/handoffs/`
7. Current Git status, recent Git log, and relevant diffs

Also read `docs/collaboration-workflow.md` and `docs/engineering-notes.md` before substantial implementation work.

## Required handoff after every completed logical change

Every agent must create or update:

`docs/handoffs/YYYY-MM-DD_<component>_<short-topic>.md`

Each handoff must include:

- Objective
- Files changed
- Behavior added, changed, or removed
- User-facing settings added, changed, deprecated, or removed
- Secondary series added or changed, including Tick, Second, Bid, Ask, Last, Volumetric, or custom series
- Tick Replay implications
- Historical-load implications
- Cache implications
- Rendering implications
- Performance implications
- Tests performed in NinjaTrader
- Compile status
- Manual-validation status
- Known issues, risks, and follow-up work
- Whether the change is eligible for promotion to `Full_Suite`

Do not treat "compiled" as "validated." Code is validated only after Julian confirms live NinjaTrader behavior.

Every completed logical change should be committed with its corresponding handoff documentation when Git is available.

## Performance-sensitive rules

Before adding or changing profile, prints, VWAP, execution, session-context, MGI, or data-driven features:

- Search for existing shared data/cache services before adding new data access or secondary series.
- Do not add redundant Tick, Bid, Ask, Second, Volumetric, or custom series without documenting why.
- Document all uses of `AddDataSeries`, `OnMarketData`, `Calculate.OnEachTick`, `Calculate.OnPriceChange`, Tick Replay, and shared cache access.
- Do not perform full historical rebuilds inside per-tick paths.
- Do not perform profile calculation, cache reads, allocation-heavy work, mutation, or synchronization waits inside `OnRender`.
- Keep rendering snapshot-based and precomputed.
- Add or preserve diagnostics around historical warm-up, series maps, cache wait time, profile-build time, and render time where relevant.
- Do not make destructive historical-data, cache, database, or workspace changes without explicit user approval.

## Product-manager review routine

When asked for a project review, the product manager must:

1. Review Git status, recent commits, diffs, handoff files, component documentation, and current source code.
2. Update `docs/ORCA_PRODUCT_STATE.md` with validated work, active development, open risks, benchmark findings, and next priorities.
3. Identify cross-indicator conflicts, duplicated data series, duplicated caches, inconsistent settings, lifecycle risks, Tick Replay risks, historical-load risks, and performance risks.
4. Convert cross-cutting findings into file-aware implementation plans.
5. Do not write indicator logic unless explicitly asked. Produce a concrete plan first.

## Documentation quality

- Prefer precise, dated, file-aware documentation.
- Record facts separately from hypotheses.
- Link each performance conclusion to a reproducible test or measurement.
- Do not claim a root cause without evidence.
- Keep module documentation current when behavior or settings change.
