# Codex Onboarding

Orca is a commercial-grade NinjaTrader 8 ecosystem focused on order-flow visibility, volume/delta profiles, VWAPs, execution visualization, and trade/risk tooling.

## Read First

1. `AGENTS.md` and `CLAUDE.md` for collaboration and source-of-truth rules.
2. `README.md` for repo layout and canonical paths.
3. `docs/architecture.md` for the design model.
4. `docs/engineering-notes.md` for NinjaTrader and SharpDX gotchas.
5. `Orca Trades/Full_Suite/Indicators/OrcaRollingProfiles.cs` as the primary rendering/data-pattern example.
6. `Orca Trades/Scripts/deploy_orca.ps1` for deployment.

## Current Workflow

`Orca Trades/Working_Suite` is the editable working copy for the core suite.

`Orca Trades/Full_Suite` is the clean validated suite. Promote into it only after a change has been deployed to NinjaTrader, compiled, and approved by Julia.

Do not use `Orca Trades/NinjaTrader` as the source for overlapping files unless Julia confirms that a specific file in that tree is newer and should replace the current version.

## Most Important Concepts

- Orca stores profile and order-flow state in custom dictionaries/objects instead of relying only on NinjaTrader bar series.
- Complex visuals use SharpDX/Direct2D rendering rather than heavy native drawing calls.
- NinjaTrader compiles the entire `Documents/NinjaTrader 8/bin/Custom` tree, so duplicate or stale files can cause confusing compiler errors.
- Historical order-flow data can differ from realtime behavior. Tick Replay and bid/ask availability matter.
- WPF brushes and NT8 properties need explicit serialization patterns.

## Immediate Priorities

- Keep `Working_Suite` as the edit lane and `Full_Suite` as the validated lane.
- Validate the `ShowDeltaEfficiency` to `ShowFinishDelta` migration in `OrcaTimeStatistics`.
- Stabilize dynamic aggregation in `OrcaRollingProfiles`.
- Modernize deployment scripts so they point at this repo and deploy from `Full_Suite`.
- Add or locate OrcaJournal sources if journal work resumes; the current checkout does not include the files described in the handoff.

## Verification Checklist

- Check `git status --short --branch` before editing.
- Confirm the target file lives under `Orca Trades/Working_Suite` for core suite work.
- After edits, compare against any mirror file only intentionally, never automatically.
- Deploy with `Orca Trades/Scripts/deploy_orca.ps1`.
- Compile inside NinjaTrader with `F5`.
- If NT8 errors reference removed names, inspect the live Custom folder for ghost files.
- Promote to `Full_Suite` only after Julia approves the tested behavior.
