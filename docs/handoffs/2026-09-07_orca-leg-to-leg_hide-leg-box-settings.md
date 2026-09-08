# Leg-to-Leg: hide leg box settings

## Objective and changes

Remove Show Current Leg Box and Leg Box Color from the settings panel as requested. Added Browsable(false) to ShowCurrentLegBox and LegBoxBrush. Retained their property identities, NinjaScriptProperty/XmlIgnore attributes, defaults, brush serializer and existing rendering behavior for template compatibility. Templates with the box enabled retain that behavior. There are now 75 visible settings in 11 groups.

## Files changed

- Orca Trades/Working_Suite/Indicators/OrcaLegtoLegProfile.cs
- tests/OrcaLegToLegProfile.PlatformCheck/Program.cs
- docs/indicators/ORCA_LEG_TO_LEG_PROFILE.md
- This handoff.

## Secondary series, Tick Replay, historical load and caches

No changes to AddDataSeries, OnMarketData, Calculate, replay, history or caches. No data operations performed.

## Rendering and performance

Unchanged. Only property-grid visibility changed; stored box values still govern existing rendering.

## Verification and deployment

Existing source-preservation harness permits only the two specific visibility attributes and confirms the exact hidden property set. Offline authored and deployed/generated semantic checks passed with zero errors; 55 saved XML files remain unchanged. Exact-target source deployment completed; authored parity passed. Scoped diff check passed. Prior source is recoverable from commit db268b4; original full backup remains available.

## NinjaTrader compile and manual validation

No native F5, UI or template-load tests performed. Julian must F5 and reopen settings to confirm the controls are absent. Offline checks do not establish runtime validation.

## Risks, follow-up and Full_Suite eligibility

Existing template-enabled boxes still render although their controls are hidden. No property deletion or saved-value reset. Full_Suite untouched and not eligible until native compilation and Julian's manual validation.
