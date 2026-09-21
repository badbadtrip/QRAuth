# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

A dynamic plugin module for the **Lampac NextGen** server. It generates `plugins/override/deny.js` — a custom authentication/access-denial page — based on configuration read from the host's `init.conf` file under the `[DenyPage]` section.

## Architecture

The module has three components:

- **`ModInit.cs`** — Entry point implementing `IModuleLoaded`. On load, it creates the output directory, runs `SyncAndGenerate()` once, subscribes to `EventListener.UpdateInitFile`, and starts a 3-second polling timer. Hash-based diffing prevents unnecessary file writes.
- **`DenyPageGenerator.cs`** — Pure static builder. `Build(DenyPageConf)` produces the full `deny.js` content as a string using `StringBuilder`. All JS string values are sanitized via `JsonSerializer.Serialize` (the `Js()` helper). The QR code block (right column) is only emitted when `tg_target` is set and `show_qr` is true.
- **`Models/DenyPageConf.cs`** — Flat config model populated by `ModuleInvoke.Init("DenyPage", new DenyPageConf())` from the host's `init.conf`. All fields have defaults; empty strings fall back to Russian-language UI defaults inside `Build()`.

## Config Integration

Config is loaded from the host system via `ModuleInvoke.Init("DenyPage", new DenyPageConf())` — this is a Lampac framework call, not something defined in this repo. The section name in `init.conf` is `DenyPage`.

## Output

The generated file is always written to:
```
<AppDomain.BaseDirectory>/plugins/override/deny.js
```

The JS file is auto-generated — do not edit it manually; it is overwritten on every config reload.

## Plugin Manifest

`manifest.json` defines the plugin metadata loaded by the Lampac host:
- `"dynamic": true` — plugin is hot-loadable without server restart
- `"enable": true` — active by default

## Generated JS Auth Flow

The output `deny.js` injects two functions:
1. `checkAutch()` — runs immediately on load; calls `{localhost}/testaccsdb` to check if the current device needs authorization. If `res.accsdb` is true, hides the app UI and calls `addDevice()`.
2. `addDevice(message)` — renders the full-screen password form into `document.body`.

On successful login, if `result.uid` is set, a new account was created (displays the new password, then redirects). Otherwise sets `lampac_unic_id` in storage and redirects to `/`.

## Key Design Constraints

- When adding new config fields, add them to `DenyPageConf` first, then wire them into `Build()`.
- `tg_target` accepts a bare username (`@botname`), full `https://t.me/` URL, or `tg://` deep link — normalization happens in `NormalizeTgUrl()`.
- The QR is rendered client-side by `qr-code-styling` (rounded dots, plain — no center logo), lazy-loaded from a CDN (`cdn.jsdelivr.net`) — not bundled. If the CDN fails, `renderQr()` falls back to a flat `api.qrserver.com` image.
- The right column (QR block) is only emitted when `tg_target` is set **and** `show_qr` is `true`.

## Field vs. Render Status

Not all `DenyPageConf` fields are rendered. Current state:

| Field | Status |
|---|---|
| `page_title`, `page_subtitle` | Rendered |
| `step1_text`, `step2_text` | Rendered as a plain 2-line list (`#dpc-steps`) below the login button — no numbered badge |
| `qr_caption`, `qr_subcaption` | Rendered (QR block only) |
| `tg_button_text` | Rendered as the QR-button's `aria-label` (icon-only button, no visible label) |
| `tg_target`, `show_qr` | Control QR block visibility |
| `page_badge` | In model; not read by `Build()` |

Card layout is full-bleed everywhere — browser included: `#dpc{padding:0}`, `#dpc-w{width:100%;height:100%}`, no border-radius/box-shadow, no `max-width` cap — it fills the viewport edge-to-edge like Lampa's own app UI does, not a centered rounded modal. Text nodes carry their own `ch`-unit max-widths (subtitle/step text) so lines don't stretch unreadably wide on huge monitors.

**Sizing (fonts, padding, QR box, gaps) is in `em`, not `px`/`vw`/breakpoints, and there is deliberately no TV/desktop detection of any kind.** `#dpc` is appended straight onto `<body>`, and Lampa's own bundled client (`lampac/Modules/LampaWeb/widgets/{samsung,lg}/app.js`, function `size()` in the `Layer` module) already sets `body`'s font-size to `max(window.innerWidth / 84.17 * interface_size_multiplier, 10.6px)` — recalculated on resize and on the user's Settings → Interface size change, and applied once during Lampa's own boot sequence (`Layer.update()`, called right after `Render.app()`) before our `deny.js` ever runs. Every size in the generator is expressed in `em` off that inherited value, so this page always matches whatever scale Lampa itself is already using on that exact device — TV, desktop browser, phone, whichever `interface_size` the user picked — without us needing to know or guess what kind of screen we're on.

This is the second (and much simpler) fix for a saga worth knowing about if it recurs: a `min-width`-breakpoint "TV mode" was tried first and broke on wide desktop browser windows (viewport width can't distinguish a TV from a monitor). A `Lampa.Platform.any()`-gated `.dpc-tv` class was tried next (a real, working signal — tizen/webos/android vs a plain browser tab) but was still redundant: it needlessly reintroduced a device/TV split for a problem Lampa had already solved at the `body` font-size level. **Do not reintroduce viewport-width breakpoints or `Lampa.Platform` checks for sizing** — if something looks wrong at a given size, the fix is almost always to adjust the `em` ratio, not to add a new detection branch. See docs/auth-ux-guidelines.md §9 for the underlying research (still correct background even though the final fix here is simpler than what it initially concluded was necessary).

The one real breakpoint that remains (`@media(max-width:700px)`) is a structural layout reflow (two columns → stacked) for genuinely narrow viewports — that's an ordinary "content needs a different layout below N px" case, not a device guess, and is orthogonal to the `em` sizing.

`tools/PreviewRunner/preview.html` has an "innerWidth (имитация Lampa.size())" field that applies the exact same formula to `body.style.fontSize`, so you can preview any simulated screen width locally without a real device.
