# Architecture

## Overview

Orca has three practical layers:

- **Core indicators**: NinjaScript indicators that collect tick/bar data and render order-flow information.
- **Add-ons**: NinjaTrader add-ons such as the Orca Risk Manager.
- **Support and reference material**: scripts, stable snapshots, experiments, and decompiled NinjaTrader references.

The editable core suite is `Orca Trades/Working_Suite`. The validated/promoted suite is `Orca Trades/Full_Suite`.

## Indicator Architecture

The indicators generally avoid expensive native `Draw.*` primitives for dense visualizations. Instead, they maintain custom state and render with SharpDX in `OnRender`.

Important patterns:

- Store profile data in dictionaries keyed by price or time bucket.
- Preserve raw or granular values where possible, then aggregate dynamically for display.
- Use chart scale and visible range during rendering to decide what can be drawn legibly.
- Keep rendering resources tied to the active `RenderTarget`.
- Dispose SharpDX resources when the render target changes or the indicator terminates.

## Rendering

SharpDX rendering is central to the suite. This gives Orca more control and performance than native NinjaTrader drawing tools, but it also means lifecycle handling is critical.

Rules:

- Do not allocate expensive `TextFormat`, `TextLayout`, or brush resources inside hot render loops unless wrapped in a short-lived `using` and proven safe.
- Cache reusable render resources where appropriate.
- Rebuild render-target-bound brushes after `OnRenderTargetChanged`.
- Clear text measurement caches when font or scale inputs change.

## NinjaTrader Lifecycle

NinjaTrader state changes are not just startup/shutdown events. Chart changes, instrument changes, timeframe changes, and reloads can re-enter lifecycle paths.

Use `State.SetDefaults`, `State.Configure`, `State.DataLoaded`, and `State.Terminated` deliberately. Treat `State.Terminated` and `OnRenderTargetChanged` as cleanup points.

## Deployment Architecture

Source files are edited outside NinjaTrader and copied into:

`%USERPROFILE%/Documents/NinjaTrader 8/bin/Custom`

NinjaTrader then compiles all scripts in the Custom tree. This means ghost files from previous names or stale mirrors can break the build even when the current source is correct.

## Source-Of-Truth Decision

Confirmed for current collaboration:

- `Orca Trades/Working_Suite/Indicators` is the working copy for core indicators.
- `Orca Trades/Working_Suite/AddOns` is the working copy for core add-ons.
- `Orca Trades/Full_Suite/Indicators` is the validated/promoted copy for core indicators.
- `Orca Trades/Full_Suite/AddOns` is the validated/promoted copy for core add-ons.
- `Orca Trades/NinjaTrader` is not canonical for overlapping files.

Open question:

- MGI indicators and other extra files exist under `Orca Trades/NinjaTrader` but not `Full_Suite`. Julia should confirm whether those are part of the active core suite or separate/legacy modules before they are moved.
