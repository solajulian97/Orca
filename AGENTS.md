# Agent Instructions

These instructions apply to Codex, Claude Code, and any other coding assistant working in this repo.

## Source Of Truth

`Orca Trades/Working_Suite` is the editable local working copy for normal edits.

Edit:

- `Orca Trades/Working_Suite/Indicators`
- `Orca Trades/Working_Suite/AddOns`

`Orca Trades/Full_Suite` is the clean validated suite. Promote into it only after Julia confirms the change compiled and behaved correctly in NinjaTrader.

### Live NinjaTrader freshness

Before editing a file that also exists in the live NinjaTrader Custom tree (`Documents/NinjaTrader 8/bin/Custom`), compare mtimes / freshness:

- If the live NinjaTrader copy is **newer** than `Working_Suite`, treat the live file as source of truth: copy it into `Working_Suite`, then continue all work from `Working_Suite`.
- If `Working_Suite` is newer (or equal), edit `Working_Suite`. Do not overwrite a newer live NinjaTrader file with an older `Working_Suite` version via reverse sync unless you are intentionally deploying.

Do not make retrograde copies from `Stable_Release`, `decompiled`, or stale mirrors such as `Orca Trades/NinjaTrader` into `Working_Suite` or `Full_Suite`. Those trees are not freshness sources for this rule.

## Collaboration Safety

- Run `git status --short --branch` before edits.
- Keep edits targeted.
- Avoid broad formatting.
- Do not overwrite uncommitted changes from Julia or another agent.
- If another agent changed a file, inspect the diff and continue from the latest content rather than reverting.
- If source-of-truth is ambiguous, pause and ask Julia.

## Deployment

Use `Orca Trades/Scripts/deploy_orca.ps1`. It deploys from `Working_Suite` by default.

After deployment, NinjaTrader still requires pressing `F5` in the NinjaScript Editor to compile.

After Julia approves the behavior, use `Orca Trades/Scripts/promote_working_to_full_suite.ps1 -Target <FileName>` to copy the validated file into `Full_Suite`.

## NinjaTrader Cautions

- Ghost files in `Documents/NinjaTrader 8/bin/Custom` can cause compiler errors unrelated to current repo source.
- SharpDX resources must be disposed correctly.
- WPF brushes need explicit serializable string companion properties.
- Historical and realtime order-flow behavior can differ.

See `docs/collaboration-workflow.md` and `docs/engineering-notes.md` before substantial work.
