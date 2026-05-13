# Codex Prompting Rules

## Role Split

ChatGPT should act as product, architecture, and prompt-planning layer.

Codex should act as implementation agent:

- inspect local files,
- edit only the requested scope,
- run targeted checks,
- report exact files changed and verification status.

## Every Codex Prompt Should Include

- The exact product goal and user-facing behavior.
- The exact source-of-truth reminder: edit `Orca Trades/Working_Suite/Indicators` or `Orca Trades/Working_Suite/AddOns`.
- The target file(s), if known.
- A warning not to copy from `NinjaTrader`, `Stable_Release`, `decompiled`, or live NT8 folders unless explicitly approved.
- Whether Codex may deploy. Default should be "do not deploy" unless the prompt asks for deployment.
- Whether Codex may promote to Full_Suite. Default should be "do not promote" unless Julia has confirmed NT8 compile and behavior.
- Acceptance criteria that can be checked by reading source and, when allowed, by NT8 compile/manual behavior tests.
- Any known dirty files or concurrent agent work.

## Default Safety Instructions

Use these rules in implementation prompts:

- Run `git status --short --branch` before edits.
- Inspect any dirty target file before changing it.
- Keep edits targeted.
- Do not run broad formatters.
- Do not refactor unrelated code.
- Do not touch generated NinjaScript cache sections unless required and understood.
- Do not deploy, promote, or commit unless explicitly asked.
- If behavior depends on live NT8 state, state what remains Unknown and what Julia must test.

## Prompt Shape

Good prompt structure:

1. Context: one or two paragraphs explaining the product behavior and why it matters.
2. Target: exact files/classes/functions if known.
3. Constraints: source-of-truth, no deployment/promotion unless asked, no broad refactor.
4. Implementation task: concrete code change.
5. Verification: exact searches/builds/manual NT8 test plan.
6. Output: what Codex should report back.

## Example Codex Prompt Template

```text
We need to fix [specific behavior] in Orca.

Read AGENTS.md, docs/collaboration-workflow.md, docs/engineering-notes.md, and the relevant source first.

Source of truth:
- Edit only Orca Trades/Working_Suite/[Indicators|AddOns]/[FileName].cs.
- Do not copy from Orca Trades/NinjaTrader, Stable_Release, decompiled, or live NinjaTrader folders.
- Do not deploy, promote, commit, or refactor unrelated code.

Current known state:
- [List dirty files or say to inspect git status.]
- [Known bug/context.]

Task:
- [Specific behavior change.]
- Preserve [important behavior].
- Unknowns should be marked Unknown or Needs confirmation.

Verification:
- Run git diff --check.
- Run targeted rg searches for [symbols].
- If deployment is allowed: use Orca Trades/Scripts/deploy_orca.ps1 -Target [Target], then Julia will press F5 in NinjaTrader.
- Provide a manual NT8 test plan.

Return:
- Files changed.
- Summary of the fix.
- Verification performed.
- Remaining risks/unknowns.
```

## What ChatGPT Should Avoid Asking Codex To Do

- "Clean up the whole suite."
- "Sync everything from NinjaTrader."
- "Promote all current files" without compile/behavior approval.
- "Use the decompiled version as source."
- "Just deploy everything" when target scope is unclear.
- "Fix all rendering issues" without naming a module and acceptance criteria.

## NinjaTrader-Specific Prompt Notes

For rendering tasks:

- Mention SharpDX resource disposal.
- Mention `OnRenderTargetChanged`.
- Ask for chart zoom/pan and multi-monitor/high-DPI test considerations.

For order-flow/delta tasks:

- Mention historical versus realtime behavior.
- Mention Tick Replay and bid/ask availability.
- Ask for tests on same-price prints and missing bid/ask fallback when relevant.

For WPF/NinjaScript properties:

- Mention brush serialization companions.
- Ask Codex to preserve property names when template compatibility matters.

For execution/order tasks:

- Mention account event hooks and unhooks.
- Ask Codex to avoid changing live order placement semantics unless that is the explicit task.
- Require a SIM/manual test plan before any live use.

