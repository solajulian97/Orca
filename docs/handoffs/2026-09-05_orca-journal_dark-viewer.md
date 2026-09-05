# Orca Journal dark screenshot viewer — 2026-09-05

## Objective and validation received
Julian confirmed screenshot opening and zoom work well in NinjaTrader after the dispatcher fix. He requested dark buttons and a dark title bar. This confirms those viewer functions only; unrelated shared identity/SIM validation remains pending.

## Change
External UI/Windows/TradeImageViewer.cs only: dark button template with light text, dark hover/pressed states and dimmed disabled buttons. Request native dark caption on SourceInitialized using DwmSetWindowAttribute 20/19, following the existing Working_Suite ExecutionLines note-editor pattern. Native window controls retained. Unsupported native dark-caption requests fall back to OS styling. No new viewer features or settings.

## Verification and staging
Release build: 0 errors, 4 existing MSB3277 warnings. Existing 24 offline checks pass. Dark-button synthetic render visually inspected; native title bar is outside that render and requires platform confirmation. No extra tests added for this visual-only change.
Stage: C:\Users\julia\Documents\New project\.codex-backups\journal-dark-viewer-stage\OrcaJournal.dll
SHA256: F653C45777916ECD0003165E9A353F9A65C2262382A8C96B2503AA6735BF9A58
NinjaTrader running (PID 29972); no deployment. Before eventual DLL copy, verify process absence, stage/source parity and installed baseline; back up existing DLL and Journal database/sidecars/annotation TSV. Current installed runtime-fix hash: 6379428AE1E96D52A982A8D53259D1279C58283B134CDE67C34BE1F0C4E0E8D2.

## Implications and follow-up
No data, annotation, media, schema, dependency, series, AddDataSeries, OnMarketData, Calculate, Tick Replay, historical-load or cache changes. Rendering is local WPF button styling and one native caption request per viewer window; no per-tick work or performance claim. Other source and unrelated dirty work preserved. Full_Suite untouched; no promotion eligibility. Updated build F5/load and dark UI confirmation pending. Future captions can accompany trade-review editing; additional screenshot features are not needed now.
