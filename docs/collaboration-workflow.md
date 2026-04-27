# Collaboration Workflow

This repo may be edited by Julia, Claude Code, and Codex. The goal is to prevent overlapping edits and retrograde copies from stale folders.

## Working Copy And Validated Source

For the core suite, edit only:

- `Orca Trades/Working_Suite/Indicators`
- `Orca Trades/Working_Suite/AddOns`

`Orca Trades/Full_Suite` is the validated suite. Only promote into it after Julia approves the deployed/tested behavior.

Do not treat `Orca Trades/NinjaTrader`, `Stable_Release`, or `decompiled` as source of truth for overlapping files.

## Before Editing

1. Run `git status --short --branch`.
2. Identify the exact files you intend to edit.
3. Check whether the file also exists in `Orca Trades/NinjaTrader`.
4. If the same file exists in multiple places, edit the `Working_Suite` copy unless Julia says otherwise.
5. Do not overwrite another agent's uncommitted work.

## During Editing

- Keep changes scoped to the task.
- Do not run broad formatters across the suite unless Julia explicitly asks.
- Do not copy an entire folder over another folder.
- Do not restore from `Stable_Release` unless explicitly requested.
- Do not update generated NinjaScript cache sections by hand unless the change requires it and is understood.

## After Editing

1. Run `git diff --check`.
2. Run targeted searches for renamed symbols if the task involved a rename.
3. Deploy from `Working_Suite` with `Orca Trades/Scripts/deploy_orca.ps1` when ready to test.
4. Compile in NinjaTrader with `F5`.
5. Capture and share compiler errors before making follow-up changes.
6. Promote the approved file into `Full_Suite` only after Julia confirms the behavior is good.

## Handoff Between Claude And Codex

If Claude edits a file and Codex continues, or vice versa:

- State which files changed.
- State whether the changes were deployed to NinjaTrader.
- State whether NT8 compile passed.
- Do not assume the mirror folder is current.
- Do not assume `Full_Suite` should be changed until the working copy has been approved.

## Conflict Rule

If there are unexpected changes in a file you are about to edit, stop and ask Julia how to proceed unless the changes are clearly from your own current task.
