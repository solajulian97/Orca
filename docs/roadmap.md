# Roadmap

## Highest Priority

- **Protect the Working_Suite -> Full_Suite promotion flow**
  - Risk: High
  - Why: Prevents Claude/Codex or scripts from overwriting validated files with untested or stale mirror files.
  - Next step: Edit `Working_Suite`, deploy/test from there, and promote to `Full_Suite` only after approval.

- **Validate current NT8 compile**
  - Risk: High
  - Why: NT8 ghost files can hide real source status.
  - Next step: Deploy from `Full_Suite`, press `F5` in NinjaTrader, and capture compiler output.

- **Finish/verify `FinishDelta` naming migration**
  - Risk: Medium/High
  - Why: Handoff identified this as a recent build-break risk.
  - Next step: Keep searching for `DeltaEfficiency` and compile in NT8.

## Near-Term

- **Stabilize dynamic delta aggregation**
  - Risk: Medium
  - Why: Zoom/pan flicker and text-density jumps can hurt usability.
  - Next step: Review `OrcaRollingProfiles` aggregation/hysteresis logic with chart-scale edge cases.

- **Clarify active module set**
  - Risk: Medium
  - Why: MGI and other indicators exist under `NinjaTrader` but not `Full_Suite`.
  - Next step: Decide whether each non-`Full_Suite` module is active, legacy, or separate.

- **Modernize deployment**
  - Risk: Medium
  - Why: Old scripts point to Anti-Gravity and `C:\Users\Owner` paths.
  - Next step: Use repo-relative paths, `Working_Suite` by default, and optional dry-run behavior.

## Medium-Term

- Add shared SharpDX resource helpers if repeated rendering patterns continue to diverge.
- Add a lightweight compile/check workflow that captures NT8 errors into the repo for review.
- Add OrcaJournal source or link its repo if journal work resumes.

## Nice To Have

- Organize experiments and legacy snapshots so active product files are easier to identify.
- Add per-indicator documentation for settings, expected behavior, and validation notes.
