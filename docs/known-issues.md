# Known Issues

## NT8 Ghost Files

Description: NinjaTrader may report compiler errors for names or classes that no longer exist in the current source.

Cause: Old `.cs` files remain in `Documents/NinjaTrader 8/bin/Custom`.

Workaround: Inspect and delete stale files directly from the live Custom folder. Then press `F5` in the NinjaScript Editor.

Confidence: Confirmed.

## Rolling Profile Delta Text Flicker

Description: Delta text can jitter during smooth zoom/pan when dynamic aggregation changes buckets too eagerly.

Cause: Visual height and chart scale changes can make the aggregation multiplier oscillate.

Workaround: Use hysteresis before changing text/aggregation thresholds.

Confidence: Confirmed by handoff; verify in current `Full_Suite` behavior.

## SharpDX Resource Leaks

Description: Chart resizing, instrument changes, or render target changes can leak resources or invalidate brushes.

Cause: Direct2D/DirectWrite resources are tied to render targets and must be disposed.

Workaround: Dispose in `OnRenderTargetChanged` and termination paths; avoid uncontrolled allocations in `OnRender`.

Confidence: Confirmed platform risk.

## Stale Deployment Scripts

Description: Several scripts point to old Anti-Gravity or `C:\Users\Owner` paths.

Cause: Historical workspace moves.

Workaround: Use repo-relative scripts and deploy from `Working_Suite`.

Confidence: Confirmed in this checkout.

## OrcaJournal Missing From Repo

Description: Handoff references OrcaJournal architecture and files, but they are not present in this checkout.

Cause: Unknown. It may be unpushed, separate, or planned.

Workaround: Do not edit journal work until the actual source is provided.

Confidence: Confirmed absence in current checkout.
