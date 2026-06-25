# Orca Decisions

Last updated: 2026-06-25

## 2026-06-25: Distributed Multi-Chart Workspace Is Intended

Decision: Orca users intentionally distribute specialized tools across multiple charts. A 10-12 chart workspace with different instruments, timeframes, bar types, trading-hours templates, Tick Replay states, and Orca combinations is normal product usage.

Implication: Performance work must measure realistic distributed workspaces, not only single-chart toy cases.

## 2026-06-25: Do Not Test Suite Performance Solely By Placing All Modules On One Chart

Decision: "All Orca tools on one chart" is not the primary benchmark.

Implication: It may be useful as a stress test, but acceptance requires realistic chart layouts and native combinations.

## 2026-06-25: Broad Historical-Data Reloads Are Not A Default Recovery Action

Decision: Broad historical reloads, cache clears, database resets, or workspace-destructive actions require a narrow reason and explicit Julian approval.

Implication: Diagnostics should first identify chart, series, cache, or indicator scope before any recovery action.

## 2026-06-25: Tick Replay Must Be Measured Per Component And Configuration

Decision: Tick Replay is neither globally guilty nor globally required.

Implication: Each component needs compatibility/performance evidence for Tick Replay off/on, historical versus realtime, one-day versus five-day ranges, and shared-provider versus local tick-series modes.

## 2026-06-25: Performance Conclusions Require Measurable Evidence

Decision: NinjaScript Utilization Monitor totals are useful ranking signals but are not sufficient root-cause evidence.

Implication: Root-cause claims must connect to reproducible diagnostics, benchmark records, or code paths that directly explain measured behavior.

## 2026-06-25: Repository Docs And Handoffs Are The Durable Coordination Layer

Decision: Agents should not rely on chat memory as the source of truth.

Implication: Product state, architecture, diagnostics spec, decisions, source maps, and per-change handoffs must stay current in the repo.
