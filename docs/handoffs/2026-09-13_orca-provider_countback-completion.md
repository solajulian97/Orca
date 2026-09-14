# Count-back input versus completion configuration — 2026-09-13

## Objective / observed evidence

Continue the authorized history-observer workflow after explicit-end initialization did not prevent endpoint resolution. Julian's instance `10ee6b76e916447aa972f66ffb2a0674` requested ToLocal `2026-09-13T20:36:33.8909942` (639249285938909942 ticks). Before inspection the only changed field was ToLocal.Ticks, now `2026-09-13T20:36:33.9005086` (639249285939005086 ticks). Difference: 95,144 ticks, or 9.5144 ms. The strict input comparison rejected and cleanup reported requestReleased=True at 1.63s. No bars/count were inspected.

Together with the earlier sentinel-resolution run, this establishes that the observed count-back path replaces even an explicitly assigned endpoint. The previous source/deployment/fixture checks did not establish that the platform would honor that endpoint. Correct the observer's lifecycle model rather than keep trying to prevent the observed resolution or add an arbitrary millisecond tolerance.

## Files changed

- `Orca Trades/Working_Suite/Indicators/OrcaProviderHistoryConfigurationCapture.cs`
- `Orca Trades/Working_Suite/Indicators/OrcaProviderHistoryProbe.cs`
- `tests/OrcaProviderStream.Tests/ProviderHistoryConfigurationTests.cs`
- `tests/OrcaProviderHistory.Tests/Program.cs`
- `docs/indicators/ORCA_PROVIDER_EXPERIMENTS.md`
- This handoff.

## Behavior / contract boundaries

The generic Capture, canonical encoding, identity keys and RequireUnchanged APIs remain strict and unchanged. A new observer-specific CaptureCountBackCompletion operation returns a distinct immutable completion snapshot:

1. Require ordered, nonempty UTC issuance/callback-receipt timestamps and a validated current configuration snapshot.
2. Require count-back mode in both snapshots. Compare every encoded field exactly except ToLocal.Ticks. BarsBack, FromLocal ticks/kind, ToLocal.Kind, instrument, merge, lookup, adjustment/reset settings, session, clocks and rollover fields remain strict. Date-range requests are not eligible.
3. If ToLocal did not change, preserve the existing stable endpoint. If it changed, require Unspecified local kind, reject invalid/ambiguous local time, and convert using the captured request-local timezone.
4. Require the resolved endpoint within the inclusive UTC interval from actual Request issuance to the first completion callback's receipt. No tolerance/grace window is added. Report the changed endpoint, interval qualification and unchanged other fields.
5. Return the completion snapshot, leaving the original input snapshot intact. Inspect at most 1,000 rows, then use generic strict RequireUnchanged against the completion snapshot. A later one-tick endpoint change still rejects.

The observer captures issuedUtc immediately before Request and completedUtc at callback entry, not when queued inspection runs. It retains the callback receipt time across nested dispatcher deferral. The existing one-callback gate makes this a one-time transition for an observer instance. Output adds request-window times and `baseline=completion` to the final configuration comparison; request-end labels its policy `STRICT_INTENT_THEN_COMPLETION`.

This is diagnostic compatibility, not authenticated provenance or permission to publish history. A coincidental external change to an endpoint within the same measured interval cannot be distinguished from platform resolution by these checks. Clock jumps, mismatched clock interpretation, historical Playback endpoints or unusual platform normalization can still reject. Do not broaden acceptance on conjecture. No consumer uses this completion helper and no provider identity or UTC coverage is certified from it.

## User settings / data series / history / cache / rendering / performance

- Settings added/changed/deprecated/removed: none; diagnostic output wording/lines changed.
- Secondary Tick/Second/Bid/Ask/Last/Volumetric/custom series: none added/changed. No AddDataSeries.
- Existing repository-only 1,000 futures Last Tick-1 BarsRequest remains one-shot with DoNotMerge, unadjusted history and unchanged chart session settings. Explicit input endpoint retained as diagnostic evidence, not assumed to survive Request unchanged.
- Tick Replay: unchanged, not required for the observer. No OnMarketData/OnBarUpdate/Update subscriptions, Calculate-mode changes, automatic reconnect, retry or rerun.
- Historical load: same count-back request and 1,000-row inspection cap. A qualified completion may now reach inspection instead of rejecting normal endpoint resolution. No download/repair, broad reload, live stitching or migration enabled.
- Cache/persistence: no shared/production cache access, deletion, mutation or new persistence.
- Rendering: none; no chart invalidation or render-path work.
- Performance: one issuance/receipt UTC read and one bounded configuration comparison/endpoint conversion per request. Existing output, snapshot and row caps retained; no measured speedup claim.

## Verification / deployment / validation

- 513 core checks passed, including separate input/completion snapshots, unchanged generic rejection, immutable-field mutations, changed BarsBack/mode/range, exact interval endpoints, out-of-window values, reversed/non-UTC intervals, DST ambiguous/nonexistent local times and strict post-completion endpoint checks.
- 194 linked observer checks passed, including modeled endpoint resolution, nested completion receipt-time retention, post-inspection endpoint mutation rejection and all earlier lifecycle cases.
- Regressions: 243 canonical-probe plus 14 subscription-observer checks passed. Total: 964.
- Offline installed-platform C# 7.3 semantic check: zero errors across 14 sources; existing structural guards passed.
- Targeted deployment: helper and probe only; prior authored live versions matched commit 6550b8a. Backup: `.codex-backups/provider-countback-resolution-20260913-204303/`.
- Authored source/live SHA-256 parity: helper `9AB575D2B2D127FAC769AF73A13A3888BBE9C9C6ECE9944D3537C74874DED885`; probe `DCAAEBAC9BA74754C679082CA4E7F0E6313E43877AF0EE0582B3BDCF44F6490A`.
- F5/load and this revised completion path in NinjaTrader: pending fresh user output. The prior runtime confirms only old-path rejection/release, not this change or historical availability.
- Full_Suite untouched and not eligible. Canonical probe and production indicators unchanged. Unrelated dirty/staged Opening Ranges work preserved.

## Next runtime gate

Remove the completed history observer, F5, add a fresh one on the same unchanged chart, and provide all output including request-window, countback-end resolution, sample-summary or failure, final configuration comparison and cleanup. Successful sample inspection would establish only the observed request/read/disposal path; UTC coverage, quote provenance, historical/live reconciliation and consumer migration remain separate gates.
