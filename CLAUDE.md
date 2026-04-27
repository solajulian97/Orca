# Claude Code Instructions

Read this before editing Orca.

`Orca Trades/Working_Suite` is the editable working copy for the core suite. `Orca Trades/Full_Suite` is the clean validated suite.

Do not edit `Full_Suite` directly for experiments. Do not overwrite `Working_Suite` or `Full_Suite` with files from `Orca Trades/NinjaTrader`, `Stable_Release`, `decompiled`, or live NinjaTrader folders unless Julia explicitly asks.

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

Important rule: never make retrograde changes by copying stale mirror files over newer `Working_Suite` or `Full_Suite` files.
