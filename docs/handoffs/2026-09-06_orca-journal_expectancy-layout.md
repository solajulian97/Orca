# Orca Journal expectancy labels and compact performance context — 2026-09-06

## Objective and changes
Julian confirmed row colors/breakeven filtering useful in NinjaTrader. Requested renaming average P&L to expectancy, adding average winner/loser and reducing explanatory text.
External files: Analytics/TagPerformance.cs and UI/Views/TagPerformanceView.cs under C:\Users\julia\projects\OrcaTrading\OrcaJournal\OrcaJournal.
Expectancy remains mean recorded P&L for the included cohort, equivalent to historical dollar expectancy. Average winner is mean strictly positive P&L; average loser is mean strictly negative P&L (signed). Missing side displays dash, not zero. Existing outcome filters apply before aggregation. All-trades includes small signed results; Exclude breakevens removes the chosen band. Underlying P&L and original filters unchanged.
Summary adds separate winner/loser line; overlap table adds numerically sorted average winner/loser columns and renames Avg P&L to Expectancy. Avg R label clarified. Paragraph replaced with selected-tag mode, excluded count and risk coverage. Formulas/instructions moved to collapsed How these numbers work; full selected tags and band available as tooltip.

## Implications
No new settings persistence/schema/series/market subscriptions/AddDataSeries/Calculate/Tick Replay/historical load/cache changes. No indicator OnRender work. Additional averages reuse existing winning/losing lists during user-driven filtering. No live performance claim. Tags/reviews/media preserved; Working_Suite/Full_Suite untouched.

## Verification and deployment state
Release net48/x64 build0 errors,4 existing warnings. Existing23 tag,25 excursion/title,13 risk/media,22 review,85 identity checks pass; actual compiled WPF harness passes and render inspected. No NinjaTrader test of this new batch. Earlier user feedback is not validation of these new labels/calculations.
Stage .codex-backups/journal-expectancy-stage/OrcaJournal.dll SHA2564913E2423D4B83FDCF409D0341FBCCD4D29FC5EB5B9C3E964B65B49F8B7EC9CC. NinjaTrader running PID71880; not deployed. Expected live1C5B8BE889D3A205420D5006286000AB7DBDE85FFB5213754D21912D2ADEDF9F.
After closure use guarded deployment/backups. Validate mixed/winner-only/loser-only cohorts, expandable help, horizontal table scrolling at narrow widths. MAE/MFE runtime validation separate and pending. Not eligible for Full_Suite promotion.
