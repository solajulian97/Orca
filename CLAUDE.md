# Claude Code Instructions

Read this before editing Orca.

`Orca Trades/Working_Suite` is the editable working copy for the core suite. `Orca Trades/Full_Suite` is the clean validated suite.

Do not edit `Full_Suite` directly for experiments.

### Live NinjaTrader freshness

Before editing a mirrored file, compare mtimes / freshness against the live NinjaTrader Custom copy (`Documents/NinjaTrader 8/bin/Custom`):

- If the live NinjaTrader file is **newer** than `Working_Suite`, copy it into `Working_Suite` and continue work from `Working_Suite`.
- If `Working_Suite` is newer (or equal), edit `Working_Suite`. Do not overwrite a newer live NinjaTrader file with an older `Working_Suite` version unless intentionally deploying.

Never make retrograde changes by copying `Stable_Release`, `decompiled`, or stale mirrors such as `Orca Trades/NinjaTrader` over newer `Working_Suite` or `Full_Suite` files.

Before editing:

1. Run `git status --short --branch`.
2. Identify the exact `Working_Suite` files you will edit.
3. Check for uncommitted changes.
4. Avoid touching files another agent is actively modifying.

After editing:

1. Run `git diff --check`.
2. If deploying, use `Orca Trades/Scripts/deploy_orca.ps1`.
3. Tell Julia which files changed and whether NinjaTrader compile was tested.
4. Promote to `Full_Suite` only after Julia approves the tested behavior.

Important docs:

- `AGENTS.md`
- `docs/collaboration-workflow.md`
- `docs/codex-onboarding.md`
- `docs/engineering-notes.md`
