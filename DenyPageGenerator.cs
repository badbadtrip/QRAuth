using System.Text;
using System.Text.Json;
using QRAuth.Models;

namespace QRAuth
{
    public static class DenyPageGenerator
    {
        // telegram-icon-transparent.png, downscaled to 128x128 and base64-encoded —
        // see the comment where it's used in Build() for why it's inlined instead of
        // shipped as a separate file.
        private const string TgIconBase64 =
            "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";

        public static string Build(DenyPageConf conf)
        {
            string tgUrl = NormalizeTgUrl(conf.tg_target);
            bool   hasTg = !string.IsNullOrWhiteSpace(tgUrl);
            string qrSize = "480";

            string jsTgUrl  = Js(tgUrl);
            string jsTitle  = Js(string.IsNullOrWhiteSpace(conf.page_title)     ? "Вход в Lampa" : conf.page_title);
            string jsSub    = Js(string.IsNullOrWhiteSpace(conf.page_subtitle)  ? "Доступ ограничен. Пароль можно получить у администратора." : conf.page_subtitle);
            string jsStep1  = Js(string.IsNullOrWhiteSpace(conf.step1_text)     ? "Нажмите «Войти по паролю»" : conf.step1_text);
            string jsStep2  = Js(string.IsNullOrWhiteSpace(conf.step2_text)     ? "Введите пароль, который выдал администратор, и подтвердите" : conf.step2_text);
            string jsQrCap  = Js(string.IsNullOrWhiteSpace(conf.qr_caption)     ? "Нет пароля?" : conf.qr_caption);
            // Short, benefit-driven CTA copy instead of a generic "scan me" instruction —
            // matches standard QR-CTA guidance (a specific benefit reads better and fits
            // on one line instead of wrapping into a narrow multi-line ladder).
            string jsQrSub  = Js(string.IsNullOrWhiteSpace(conf.qr_subcaption)  ? "Получить пароль у бота" : conf.qr_subcaption);
            string jsTgBtn  = Js(string.IsNullOrWhiteSpace(conf.tg_button_text) ? "Открыть Telegram" : conf.tg_button_text);

            var sb = new StringBuilder();
            sb.AppendLine("// QRAuth deny-page v5.0-preview - auto-generated from init.conf[DenyPage]");
            sb.AppendLine("// DO NOT EDIT - overwritten on config reload.");
            sb.AppendLine();
            sb.AppendLine("var network = new Lampa.Reguest();");
            sb.AppendLine();

            // ── CSS ──────────────────────────────────────────────────────────
            sb.AppendLine("(function(){");
            sb.AppendLine("  var s = document.createElement('style');");
            sb.AppendLine("  s.textContent = [");

            sb.AppendLine("    ':root{--dpc-ease-out:cubic-bezier(0.23,1,0.32,1)}',");
            sb.AppendLine("    '#dpc{color-scheme:dark;position:fixed;inset:0;z-index:99999;display:flex;align-items:center;justify-content:center;font-family:\"Manrope\",\"Segoe UI\",system-ui,sans-serif;padding:0;box-sizing:border-box;overflow:auto;background:#050308}',");
            sb.AppendLine("    '@keyframes dpcIn{from{opacity:0;transform:translateY(14px) scale(.97)}to{opacity:1;transform:translateY(0) scale(1)}}',");
            sb.AppendLine("    '@keyframes dpcStagger{from{opacity:0;transform:translateY(8px)}to{opacity:1;transform:translateY(0)}}',");

            // Card
            sb.AppendLine("    '#dpc-w{position:relative;overflow:hidden;width:100%;height:100%;background:#0d0710;animation:dpcIn .5s var(--dpc-ease-out)}',");

            // Blob layer — percentage-sized radial-gradients directly on the background,
            // not fixed-px blurred circles pinned to the right edge. Fixed-px blobs made sense
            // on the old ~1040px-capped card (they overlapped into one wash); on a full-bleed,
            // width:100% card they end up scattered in separate corners with dead space between
            // (see docs/auth-ux-guidelines.md — logged as a concrete case of the same "don't tie
            // decorative sizing to an assumed container width" mistake as the TV/desktop bug).
            // Percentage radii scale with the box, so this always reads as one cohesive wash.
            // blur(60px) over a near-fullscreen area is a heavy filter op on weak TV
            // GPUs; 40px still reads as a soft wash (the radial-gradients already fade
            // to transparent well before their own edges, so most of the softness comes
            // from the gradient stops, not the blur radius) but is cheaper to composite.
            sb.AppendLine("    '#dpc-bg{position:absolute;inset:-10%;overflow:hidden;z-index:0;pointer-events:none;filter:blur(40px);background:radial-gradient(65% 60% at 100% 6%,#ff8fd0 0%,#c13bea 42%,transparent 78%),radial-gradient(62% 68% at 100% 94%,#ff5ea8 0%,#7b2ff7 44%,transparent 80%),radial-gradient(46% 52% at 106% 50%,#c13bea 0%,#4b1fb0 46%,transparent 82%)}',");
            sb.AppendLine("    '#dpc-bg::after{content:\\'\\';position:absolute;inset:0;background:linear-gradient(112deg,#0d0710 0%,#0d0710 38%,rgba(13,7,16,.55) 56%,rgba(13,7,16,0) 78%)}',");

            // Content grid
            // max-width + auto margins keeps the two columns from drifting apart on huge
            // monitors (em-based, same "don't stretch on huge monitors" reasoning as the
            // ch-unit text caps below — see CLAUDE.md). Since 1em tracks innerWidth via
            // Lampa's own body-fontSize formula, this is a fixed viewport fraction on every
            // screen, not a px/vw breakpoint.
            sb.AppendLine("    '#dpc-content{position:relative;z-index:1;display:flex;gap:0;height:100%;min-height:460px;max-width:72em;margin:0 auto}',");

            // Left column. Sizes are in `em`, not `px`/`vw`/media-query breakpoints — `#dpc` is
            // appended straight onto <body>, and Lampa itself already sets body's font-size to
            // `max(innerWidth/84.17 * interface_size_multiplier, 10.6px)` (Modules/LampaWeb/
            // widgets/{samsung,lg}/app.js, function size()), recalculated on resize and on the
            // user's Settings → Interface size change. That's Lampa's own answer to "how big
            // should UI be on this screen" — TV, desktop browser, phone, whatever the user picked
            // in Settings — so inheriting it via `em` means this page always matches the scale of
            // the rest of the app on that exact device, with zero platform-detection of our own.
            // Do not set an explicit font-size anywhere above #dpc-title, or the em chain breaks.
            sb.AppendLine("    '#dpc-l{flex:1;padding:3.15em 2.93em;display:flex;flex-direction:column;justify-content:center;gap:1.35em;overflow-y:auto;min-width:0}',");
            sb.AppendLine("    '#dpc-logo{display:flex;align-items:center;gap:0.68em;opacity:0;animation:dpcStagger .4s var(--dpc-ease-out) .05s forwards}',");
            sb.AppendLine("    '#dpc-logo-mark{width:1.73em;height:1.73em;flex-shrink:0}',");
            sb.AppendLine("    '#dpc-logo-mark svg{display:block;width:100%;height:100%;filter:drop-shadow(0 1px 3px rgba(0,0,0,.45))}',");
            sb.AppendLine("    '#dpc-logo-text{font-weight:700;font-size:0.99em;letter-spacing:1.5px;color:#f2f0ff;text-transform:uppercase}',");
            sb.AppendLine("    '#dpc-logo-next{font-weight:400;color:#8a83a8;letter-spacing:1.5px}',");
            sb.AppendLine("    '#dpc-title{font-size:2.25em;font-weight:800;color:#fbfaff;line-height:1.25;margin:0;letter-spacing:-.4px;opacity:0;animation:dpcStagger .45s var(--dpc-ease-out) .1s forwards}',");
            sb.AppendLine("    '#dpc-subtitle{font-size:0.99em;color:#c9c4dd;line-height:1.6;margin:0;max-width:40ch;opacity:0;animation:dpcStagger .45s var(--dpc-ease-out) .16s forwards}',");
            sb.AppendLine("    '#dpc-actions{display:flex;flex-direction:column;gap:0.83em;margin-top:0.38em;opacity:0;animation:dpcStagger .45s var(--dpc-ease-out) .22s forwards}',");

            // Primary pill button (white, barely-there gradient — docs/auth-ux-guidelines.md
            // §10.4: flat/near-flat is the premium baseline). Rest-state shadow is layered
            // (tight/medium/wide, each with its own offset+blur+opacity) instead of one
            // blurred shadow — §10.4's "designer shadow" technique. Hover/active are plain
            // `transition`s (lift + deeper layered shadow / `scale(.97)`), not a
            // `@keyframes` loop — §10.1/§10.2: a hover effect should play once per state
            // change, not run as an infinite decorative animation. (Two decorative
            // rewrites — a pulse ring, then a moving-gradient/blob redesign — were tried
            // and reverted here; this plain version is the one that stuck.)
            // Only `transform` is in the transition list, not `box-shadow` — box-shadow
            // isn't compositor-only like transform/opacity, animating it forces a repaint
            // every frame of the transition. This button is focused by default on TV load
            // (§10.3), so the rest→focus shadow change fires immediately during the page's
            // busiest render window; letting the shadow snap instantly instead of
            // transitioning removes that repaint cost, the lift still animates smoothly.
            sb.AppendLine("    '#dpc-btn{-webkit-appearance:none;appearance:none;display:inline-flex;align-items:center;justify-content:center;gap:0.75em;width:auto;align-self:flex-start;padding:1.2em 2.25em;background:linear-gradient(180deg,#ffffff,#f0edfb);color:#131316;border:none;border-radius:999px;font-family:inherit;font-size:1.13em;font-weight:700;white-space:nowrap;cursor:pointer;box-shadow:0 1px 2px rgba(0,0,0,.18),0 4px 8px rgba(0,0,0,.14),0 14px 28px rgba(0,0,0,.16),inset 0 1px 0 rgba(255,255,255,.9);will-change:transform;transition:transform 160ms var(--dpc-ease-out)}',");
            sb.AppendLine("    '#dpc-btn svg{width:1.35em;height:1.35em;flex-shrink:0}',");
            sb.AppendLine("    '#dpc-btn:disabled{opacity:.45;cursor:default}',");
            sb.AppendLine("    '@media(hover:hover) and (pointer:fine){#dpc-btn:not(:disabled):hover{transform:translateY(-2px);box-shadow:0 2px 4px rgba(0,0,0,.2),0 8px 16px rgba(0,0,0,.18),0 24px 48px rgba(0,0,0,.22),inset 0 1px 0 rgba(255,255,255,1)}}',");
            sb.AppendLine("    '#dpc-btn:not(:disabled):active{transform:scale(.97) translateY(0)}',");
            sb.AppendLine("    '#dpc-err{font-size:0.6em;min-height:1.15em;line-height:1.5;padding-left:4px;transition:color 160ms ease}',");

            // Step list — plain text lines, no numbered badge (the number added nothing;
            // two short lines already read in order without it).
            sb.AppendLine("    '#dpc-steps{display:flex;flex-direction:column;gap:0.7em;margin-top:0.68em;padding-top:1.35em;border-top:1px solid rgba(255,255,255,.08);opacity:0;animation:dpcStagger .45s var(--dpc-ease-out) .28s forwards}',");
            sb.AppendLine("    '#dpc-steps .dpc-step-t{font-size:0.99em;color:#c9c4dd;line-height:1.6;max-width:42ch}',");

            // Right column (QR)
            sb.AppendLine("    '#dpc-r{width:29em;flex-shrink:0;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:1em;padding:3em 2em;text-align:center}',");
            sb.AppendLine("    '#dpc-qrcap{font-size:0.86em;font-weight:700;color:#c9c4dd;letter-spacing:.2px;opacity:0;animation:dpcStagger .4s var(--dpc-ease-out) .2s forwards}',");
            // No backdrop-filter here — it's the single most common cause of jank on
            // weak TV WebKit (Tizen/webOS): every frame, the browser has to re-sample
            // and blur whatever sits behind this element (the animated/blurred #dpc-bg
            // wash right underneath it) in real time, often falling back to slow
            // software compositing where GPU support is spotty. Plain translucent
            // rgba() gives the same "not flashy white" result at essentially zero cost
            // — no blur-of-what's-behind, just a flat semi-transparent fill.
            sb.AppendLine("    '#dpc-qr-wrap{position:relative;width:22.5em;height:22.5em;flex-shrink:0;background:rgba(255,255,255,.62);border:1px solid rgba(255,255,255,.35);border-radius:1.9em;padding:0.9em;display:flex;align-items:center;justify-content:center;box-shadow:0 18px 44px rgba(0,0,0,.3);opacity:0;animation:dpcStagger .5s var(--dpc-ease-out) .26s forwards}',");
            sb.AppendLine("    '#dpc-qr-box{position:relative;width:100%;height:100%}',");
            // qr-code-styling рисует SVG с фиксированным пиксельным width/height, снятым один раз
            // при построении (container.clientWidth), и НЕ проставляет viewBox вообще (проверено
            // в живом DOM: svg.getAttribute('viewBox') === null). Без viewBox форсирование
            // width/height:100% через CSS не масштабирует уже нарисованные пути — оно просто меняет
            // видимый viewport, а координаты точек остаются в исходных пиксельных юнитах со сборки,
            // из-за чего QR "съезжает"/не совпадает с белой подложкой после изменения окна (сама
            // разметка не перестраивается, а слушателя на resize нет и не должно быть). Раньше тут
            // был комментарий про "сохранение viewBox" — но сохранять было нечего, его никогда не
            // было. Реальный фикс: renderQr() сам проставляет viewBox сразу после rendering (см.
            // ниже) — тогда браузер honestly масштабирует контент под текущий размер контейнера.
            sb.AppendLine("    '#dpc-qr-box svg{display:block!important;width:100%!important;height:100%!important}',");
            sb.AppendLine("    '#dpc-qr-box img{display:block;width:100%;height:auto;border-radius:4px;mix-blend-mode:multiply}',");
            // Same body-text treatment as #dpc-subtitle on the left — regular
            // weight, same muted color, same line-height — so descriptive text reads as
            // one consistent style across both columns instead of the right side being
            // bolder/brighter/differently-aligned. The shadow is only for legibility over
            // the bright gradient backdrop here, not a weight/emphasis choice.
            // Row: round Telegram button on the left, caption text next to it (left-
            // aligned, reading naturally right after the button — button then label,
            // not text pushed away from it).
            sb.AppendLine("    '#dpc-qr-cta{display:flex;align-items:center;justify-content:center;gap:0.85em;width:100%;max-width:22.5em}',");
            // max-width forces this onto 2 lines instead of 1 long + 1 short; text-wrap:
            // balance (supported in current Chromium/Firefox, harmless no-op elsewhere)
            // splits those 2 lines evenly instead of the default greedy "cram the first
            // line full, dump the leftover word on the second" wrap.
            sb.AppendLine("    '#dpc-qrsub{flex:1;min-width:0;max-width:20ch;font-size:0.92em;font-weight:400;color:#c9c4dd;line-height:1.5;text-align:left;text-wrap:balance;text-shadow:0 1px 3px rgba(0,0,0,.4)}',");
            sb.AppendLine("    '#dpc-qrpill{display:inline-flex;align-items:center;justify-content:center;width:2.75em;height:2.75em;flex-shrink:0;border-radius:50%;cursor:pointer;text-decoration:none;filter:drop-shadow(0 6px 14px rgba(0,0,0,.35));transition:transform 160ms var(--dpc-ease-out);opacity:0;animation:dpcStagger .45s var(--dpc-ease-out) .32s forwards}',");
            sb.AppendLine("    '#dpc-qrpill img{width:100%;height:100%;display:block;border-radius:50%}',");
            sb.AppendLine("    '@media(hover:hover) and (pointer:fine){#dpc-qrpill:not(:active):hover{transform:scale(1.08)}}',");
            sb.AppendLine("    '#dpc-qrpill:active{transform:scale(.92)}',");

            // Responsive — layout reflow only (two columns → stacked), never sizing: sizing is
            // already fluid via em/Lampa's body font-size above, so there's nothing left to guess
            // per breakpoint. This is the ordinary "content needs a different layout below N px"
            // case (web.dev's own recommended reason to add a breakpoint at all), not a device
            // detection — see docs/auth-ux-guidelines.md §9.
            sb.AppendLine("    '@media(max-width:700px){#dpc{background:transparent;align-items:flex-start}#dpc-content{flex-direction:column}#dpc-l{flex:0 0 auto;overflow:visible}#dpc-r{flex:0 0 auto;width:100%}#dpc-btn{width:100%}}',");

            // Reduced motion
            sb.AppendLine("    '@media(prefers-reduced-motion:reduce){#dpc-w,#dpc-logo,#dpc-title,#dpc-subtitle,#dpc-actions,#dpc-steps,#dpc-qrcap,#dpc-qr-wrap,#dpc-qrpill,#dpc-blocked{animation:none!important;opacity:1!important;transform:none!important}}',");

            // TV focus ring — neutral dark (not a muted brand hue) per §10.3: a solid
            // ≥2px ring passes WCAG 2.4.13's area+contrast test on paper regardless of
            // hue, but a soft-blurred brand-tinted ring on a near-white button still
            // reads as smeared rather than a crisp "selected" click, even at a passing
            // contrast ratio — so this uses the button's own text color, not #6D5DFB.
            //
            // On TV this button is focused THE MOMENT the page appears —
            // Lampa.Controller.collectionFocus(false, $('#dpc-w')) below picks the
            // nearest/first .selector, which is this button, with no user input yet.
            // So :focus/.focus is this button's default resting look on TV, not a rare
            // transient a mouse-hover would be — it has to look intentional sitting
            // still, not just a thin ring dropped on an otherwise-flat button. Apple
            // tvOS HIG (§10.3) recommends combining several signals (scale/elevation +
            // outline) rather than a ring alone — so focus gets the same lift + layered
            // shadow as :hover, plus the ring, instead of just the ring by itself.
            sb.AppendLine("    '#dpc-btn:focus,#dpc-btn.focus{transform:translateY(-2px);box-shadow:0 0 0 3px #131316,0 2px 4px rgba(0,0,0,.2),0 8px 16px rgba(0,0,0,.18),0 24px 48px rgba(0,0,0,.22)!important;outline:none}',");
            sb.AppendLine("    '#dpc-qrpill:focus,#dpc-qrpill.focus{box-shadow:0 0 0 2px rgba(13,7,16,.9),0 0 0 4px #2CA5E0!important;outline:none}',");

            sb.AppendLine("    '.settings-input{z-index:100000!important}',");
            sb.AppendLine("    '.selectbox{z-index:100001!important}',");

            // Banned/expired state (checkAutch's denymsg branch) — deliberately NOT the
            // addDevice() login form: retrying a password can't undo a ban or expiry, so
            // showing that form would just invite a pointless retry loop. Reuses #dpc/#dpc-w/
            // #dpc-bg (the same full-bleed card chrome) so it doesn't look like a different,
            // broken page — just a simple centered message instead of the two-column layout.
            // min-height matches #dpc-content's own fallback above — without it this block's
            // height:100% has nothing definite to resolve against (short content, no padding-
            // driven intrinsic height like the two-column layout has) and it collapses to
            // near-zero, landing near the top of #dpc-w instead of true vertical center.
            sb.AppendLine("    '#dpc-blocked{position:relative;z-index:1;display:flex;flex-direction:column;align-items:center;justify-content:center;height:100%;min-height:460px;text-align:center;gap:1.1em;padding:2em;max-width:34em;margin:0 auto;opacity:0;animation:dpcIn .5s var(--dpc-ease-out) forwards}',");
            sb.AppendLine("    '#dpc-blocked-title{font-size:1.85em;font-weight:800;color:#fbfaff;margin:0;letter-spacing:-.3px}',");
            sb.AppendLine("    '#dpc-blocked-msg{font-size:1em;color:#c9c4dd;line-height:1.6;margin:0;max-width:32ch}'");

            sb.AppendLine("  ].join('');");
            sb.AppendLine("  document.head.appendChild(s);");
            sb.AppendLine("})();");
            sb.AppendLine();

            // ── addDevice ────────────────────────────────────────────────────
            sb.AppendLine("function addDevice(message) {");
            sb.AppendLine("  if (document.getElementById('dpc')) return;");
            sb.AppendLine();

            sb.AppendLine("  var svgLock = '<svg width=\"17\" height=\"17\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"#131316\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><rect x=\"5\" y=\"11\" width=\"14\" height=\"9\" rx=\"2\"/><path d=\"M8 11V7a4 4 0 0 1 8 0v4\"/></svg>';");
            // User-supplied Telegram PNG (telegram-icon-transparent.png), downscaled to
            // 128x128 and inlined as base64 — keeps deny.js a single generated file with
            // no extra asset to deploy/copy alongside it. Only emitted when the QR button
            // actually needs it (~28KB base64) — otherwise it'd bloat every deny.js even
            // when tg_target/show_qr are unset.
            if (hasTg && conf.show_qr)
            {
                sb.AppendLine("  var tgIconSrc = 'data:image/png;base64," + TgIconBase64 + "';");
            }
            // Настоящий значок Lampa (концентрические кольца) — взят из иконки Tizen/webOS
            // виджета (lampac/Modules/LampaWeb/widgets/samsung|lg/app/img/logo-icon.svg),
            // это официальный app-icon клиента — используется только в лого шапки, где
            // бренд должен быть узнаваем.
            sb.AppendLine("  var svgLampaIcon = '<path d=\"M81.6744 103.11C98.5682 93.7234 110 75.6967 110 55C110 24.6243 85.3757 0 55 0C24.6243 0 0 24.6243 0 55C0 75.6967 11.4318 93.7234 28.3255 103.11C14.8869 94.3724 6 79.224 6 62C6 34.938 27.938 13 55 13C82.062 13 104 34.938 104 62C104 79.224 95.1131 94.3725 81.6744 103.11Z\" fill=\"#fff\"/><path d=\"M92.9546 80.0076C95.5485 74.5501 97 68.4446 97 62C97 38.804 78.196 20 55 20C31.804 20 13 38.804 13 62C13 68.4446 14.4515 74.5501 17.0454 80.0076C16.3618 77.1161 16 74.1003 16 71C16 49.4609 33.4609 32 55 32C76.5391 32 94 49.4609 94 71C94 74.1003 93.6382 77.1161 92.9546 80.0076Z\" fill=\"#fff\"/><path d=\"M55 89C69.3594 89 81 77.3594 81 63C81 57.9297 79.5486 53.1983 77.0387 49.1987C82.579 54.7989 86 62.5 86 71C86 88.1208 72.1208 102 55 102C37.8792 102 24 88.1208 24 71C24 62.5 27.421 54.7989 32.9613 49.1987C30.4514 53.1983 29 57.9297 29 63C29 77.3594 40.6406 89 55 89Z\" fill=\"#fff\"/><path d=\"M73 63C73 72.9411 64.9411 81 55 81C45.0589 81 37 72.9411 37 63C37 53.0589 45.0589 45 55 45C64.9411 45 73 53.0589 73 63Z\" fill=\"#fff\"/>';");
            sb.AppendLine("  var svgLogoMark = '<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 110 104\">' + svgLampaIcon + '</svg>';");
            sb.AppendLine();

            // Скруглённые точки/логотип по центру QR даёт только клиентская отрисовка —
            // api.qrserver.com отдаёт только плоскую растровую картинку без стилизации.
            // Библиотека грузится лениво (лежит на CDN, не бандлится в плагин).
            // renderQr() is called twice for the same container: once immediately with the
            // static tgUrl, then again from startQrAuth() once the session dynUrl is back.
            // Both paths are async (CDN script load / fetch round-trip), so without a guard
            // whichever settles second wins the write but doesn't cancel the other's in-flight
            // load — two <script> tags, two onloads, two qr.append(container) calls with no
            // clear between them, producing two overlapping/offset QR codes. A per-container
            // generation counter makes a stale call's build()/fallback() a no-op, and a single
            // shared CDN-load promise (instead of one <script> tag per call) means only the
            // winning generation's build() ever runs.
            sb.AppendLine("  function renderQr(container, url) {");
            sb.AppendLine("    var gen = (container._dpcQrGen = (container._dpcQrGen || 0) + 1);");
            sb.AppendLine("    function fallback() {");
            sb.AppendLine("      if (container._dpcQrGen !== gen) return;");
            sb.AppendLine("      container.innerHTML = '';");
            sb.AppendLine("      container.insertAdjacentHTML('beforeend',");
            sb.AppendLine("        '<img src=\"https://api.qrserver.com/v1/create-qr-code/?size=" + qrSize + "x" + qrSize + "&ecc=M&margin=4&data=' + encodeURIComponent(url) + '\" loading=\"lazy\" />');");
            sb.AppendLine("    }");
            sb.AppendLine("    function build() {");
            sb.AppendLine("      if (container._dpcQrGen !== gen) return;");
            sb.AppendLine("      try {");
            sb.AppendLine("        container.innerHTML = '';");
            sb.AppendLine("        var size = (container.clientWidth || 162) - 0;");
            sb.AppendLine("        var qr = new QRCodeStyling({");
            sb.AppendLine("          width: size,");
            sb.AppendLine("          height: size,");
            sb.AppendLine("          type: 'svg',");
            sb.AppendLine("          data: url,");
            sb.AppendLine("          margin: 2,");
            // Минимализм в QR — это ОДИН плоский фирменный цвет, не двухцветный градиент:
            // на мелких модулях смена оттенка через паттерн читается пёстро/шумно, а не
            // премиально. Один глубокий фиолетовый тон — тот же принцип, что "тёмные модули
            // на светлом" (максимальный контраст/сканируемость), но в фирменном оттенке
            // вместо нейтрального угольного.
            // EC level H (~30% redundancy) was needed only to survive the center-logo
            // overlay this QR used to have — that logo is gone now (see history), so the
            // extra redundancy has no logo to protect against. M (~15%, the library's own
            // default) is still fine for a clean, unobstructed code, and needs fewer
            // modules for the same data — cheaper for the JS engine to build the SVG path
            // set on weak TV hardware, and less visually dense for the same physical box.
            sb.AppendLine("          qrOptions: { errorCorrectionLevel: 'M' },");
            sb.AppendLine("          dotsOptions: { type: 'rounded', color: '#1e1f21' },");
            sb.AppendLine("          cornersSquareOptions: { type: 'extra-rounded', color: '#1e1f21' },");
            sb.AppendLine("          cornersDotOptions: { type: 'dot', color: '#1e1f21' },");
            // Transparent SVG background lets #dpc-qr-box's frosted-glass CSS be the actual
            // paper the dots sit on, instead of a flat opaque white square baked into the code.
            sb.AppendLine("          backgroundOptions: { color: 'transparent' }");
            sb.AppendLine("        });");
            sb.AppendLine("        qr.append(container);");
            // qr-code-styling's <svg> has no viewBox (see #dpc-qr-box svg CSS comment above) —
            // add one ourselves so the width/height:100% CSS actually rescales the drawn QR
            // instead of just resizing an unscaled viewport around static-coordinate paths.
            sb.AppendLine("        var builtSvg = container.querySelector('svg');");
            sb.AppendLine("        if (builtSvg && !builtSvg.getAttribute('viewBox')) builtSvg.setAttribute('viewBox', '0 0 ' + size + ' ' + size);");
            sb.AppendLine("      } catch (e) { fallback(); }");
            sb.AppendLine("    }");
            sb.AppendLine("    if (window.QRCodeStyling) { build(); return; }");
            sb.AppendLine("    if (!window._dpcQrLoad) {");
            sb.AppendLine("      window._dpcQrLoad = new Promise(function(resolve, reject) {");
            sb.AppendLine("        var sc = document.createElement('script');");
            sb.AppendLine("        sc.src = 'https://cdn.jsdelivr.net/npm/qr-code-styling@1.6.0-rc.1/lib/qr-code-styling.js';");
            sb.AppendLine("        sc.onload = resolve;");
            sb.AppendLine("        sc.onerror = reject;");
            sb.AppendLine("        document.head.appendChild(sc);");
            sb.AppendLine("      });");
            sb.AppendLine("    }");
            sb.AppendLine("    window._dpcQrLoad.then(build, fallback);");
            sb.AppendLine("  }");
            sb.AppendLine();

            // Left column HTML
            // Текстовые узлы оставлены пустыми и заполняются через textContent ниже —
            // insertAdjacentHTML не должен получать значения из init.conf напрямую (XSS).
            sb.AppendLine("  var leftHtml = ''");
            sb.AppendLine("    + '<div id=\"dpc-l\">'");
            sb.AppendLine("    + '<div id=\"dpc-logo\"><span id=\"dpc-logo-mark\">' + svgLogoMark + '</span><span id=\"dpc-logo-text\">Lampac<span id=\"dpc-logo-next\"></span></span></div>'");
            sb.AppendLine("    + '<h1 id=\"dpc-title\"></h1>'");
            sb.AppendLine("    + '<p id=\"dpc-subtitle\"></p>'");
            sb.AppendLine("    + '<div id=\"dpc-actions\">'");
            sb.AppendLine("    + '<button id=\"dpc-btn\" type=\"button\" class=\"selector\">' + svgLock + '<span id=\"dpc-btn-text\">Войти по паролю</span></button>'");
            sb.AppendLine("    + '<div id=\"dpc-err\"></div>'");
            sb.AppendLine("    + '</div>'");
            sb.AppendLine("    + '<div id=\"dpc-steps\">'");
            sb.AppendLine("    + '<div class=\"dpc-step-t\" id=\"dpc-step1\"></div>'");
            sb.AppendLine("    + '<div class=\"dpc-step-t\" id=\"dpc-step2\"></div>'");
            sb.AppendLine("    + '</div>'");
            sb.AppendLine("    + '</div>';");
            sb.AppendLine();

            // Right column HTML (only if tg + show_qr) — QR image, then a row with the
            // round Telegram button and caption text next to it (see #dpc-qr-cta above).
            if (hasTg && conf.show_qr)
            {
                sb.AppendLine("  var tgUrl  = " + jsTgUrl + ";");
                sb.AppendLine("  var rightHtml = ''");
                sb.AppendLine("    + '<div id=\"dpc-r\">'");
                sb.AppendLine("    + '<div id=\"dpc-qrcap\"></div>'");
                sb.AppendLine("    + '<div id=\"dpc-qr-wrap\"><div id=\"dpc-qr-box\"></div></div>'");
                sb.AppendLine("    + '<div id=\"dpc-qr-cta\">'");
                sb.AppendLine("    + '<a id=\"dpc-qrpill\" class=\"selector\" target=\"_blank\" rel=\"noopener\"><img src=\"' + tgIconSrc + '\" alt=\"Telegram\" /></a>'");
                sb.AppendLine("    + '<div id=\"dpc-qrsub\"></div>'");
                sb.AppendLine("    + '</div>'");
                sb.AppendLine("    + '</div>';");
            }
            else
            {
                sb.AppendLine("  var rightHtml = '';");
            }

            sb.AppendLine();
            sb.AppendLine("  var html = '<div id=\"dpc\"><div id=\"dpc-w\"><div id=\"dpc-bg\"></div><div id=\"dpc-content\">' + leftHtml + rightHtml + '</div></div></div>';");
            sb.AppendLine("  document.body.insertAdjacentHTML('beforeend', html);");
            sb.AppendLine();

            // Текст из init.conf проставляется через textContent/href, а не в разметку —
            // так браузер сам экранирует HTML-спецсимволы вместо ручного экранирования.
            sb.AppendLine("  document.getElementById('dpc-logo-next').textContent = ' NextGen';");
            sb.AppendLine("  document.getElementById('dpc-title').textContent = " + jsTitle + ";");
            sb.AppendLine("  document.getElementById('dpc-subtitle').textContent = " + jsSub + ";");
            sb.AppendLine("  document.getElementById('dpc-step1').textContent = " + jsStep1 + ";");
            sb.AppendLine("  document.getElementById('dpc-step2').textContent = " + jsStep2 + ";");
            sb.AppendLine();
            if (hasTg && conf.show_qr)
            {
                sb.AppendLine("  document.getElementById('dpc-qrcap').textContent = " + jsQrCap + ";");
                sb.AppendLine("  document.getElementById('dpc-qrsub').textContent = " + jsQrSub + ";");
                sb.AppendLine("  document.getElementById('dpc-qrpill').setAttribute('aria-label', " + jsTgBtn + ");");
                sb.AppendLine("  document.getElementById('dpc-qrpill').href = tgUrl;");
                sb.AppendLine("  renderQr(document.getElementById('dpc-qr-box'), tgUrl);");
                sb.AppendLine();

                // ── QR login handshake ────────────────────────────────────────
                // Static tgUrl above is only the pre-session fallback (shown instantly,
                // and kept as the link if the bot module is disabled/unreachable — same
                // behavior as before this block existed). startQrAuth() asks the bot
                // module for a fresh pairing session (services/qrauthsessions.cs), swaps
                // the QR/pill href for the session-bound deep link (?start=qr_<id>), then
                // polls for confirmation. Once BotSession's "✅ Подтвердить вход" button
                // confirms it, the poll gets back the user's existing Lampac token and
                // logs in with it through the exact same doLogin() path as a typed
                // password — the bot token IS the password (see usersrepository.cs).
                sb.AppendLine("  var qrSessionId = null;");
                sb.AppendLine("  var qrPollTimer = null;");
                sb.AppendLine("  var qrStartPending = false;");
                sb.AppendLine();
                sb.AppendLine("  function stopQrPoll() {");
                sb.AppendLine("    if (qrPollTimer) { clearInterval(qrPollTimer); qrPollTimer = null; }");
                sb.AppendLine("  }");
                sb.AppendLine();
                sb.AppendLine("  function pollQrSession() {");
                sb.AppendLine("    stopQrPoll();");
                sb.AppendLine("    qrPollTimer = setInterval(function() {");
                sb.AppendLine("      if (!qrSessionId) return;");
                sb.AppendLine("      var qrNet = new Lampa.Reguest();");
                sb.AppendLine("      qrNet.silent('{localhost}/tgbot/qr/status?session=' + encodeURIComponent(qrSessionId), function(res) {");
                sb.AppendLine("        if (res && res.status === 'confirmed' && res.token) {");
                sb.AppendLine("          stopQrPoll();");
                sb.AppendLine("          if (_btn && !_btn.disabled) doLogin(res.token);");
                sb.AppendLine("        } else if (res && res.status === 'expired') {");
                sb.AppendLine("          stopQrPoll();");
                sb.AppendLine("          startQrAuth();");
                sb.AppendLine("        }");
                sb.AppendLine("      }, function() {});");
                sb.AppendLine("    }, 2000);");
                sb.AppendLine("  }");
                sb.AppendLine();
                sb.AppendLine("  function startQrAuth() {");
                sb.AppendLine("    if (qrStartPending) return;");
                sb.AppendLine("    qrStartPending = true;");
                sb.AppendLine("    var qrNet = new Lampa.Reguest();");
                sb.AppendLine("    qrNet.silent('{localhost}/tgbot/qr/start', function(res) {");
                sb.AppendLine("      qrStartPending = false;");
                sb.AppendLine("      if (!res || !res.session) return;");
                sb.AppendLine("      qrSessionId = res.session;");
                sb.AppendLine("      var dynUrl = tgUrl.split('?')[0] + '?start=qr_' + qrSessionId;");
                sb.AppendLine("      if (_qrpill) _qrpill.href = dynUrl;");
                sb.AppendLine("      renderQr(document.getElementById('dpc-qr-box'), dynUrl);");
                sb.AppendLine("      pollQrSession();");
                sb.AppendLine("    }, function() { qrStartPending = false; });");
                sb.AppendLine("  }");
                sb.AppendLine();
                sb.AppendLine("  startQrAuth();");
                sb.AppendLine();
            }

            sb.AppendLine("  var _btn  = document.getElementById('dpc-btn');");
            sb.AppendLine("  var _err  = document.getElementById('dpc-err');");
            sb.AppendLine("  var _qrpill = document.getElementById('dpc-qrpill');");
            sb.AppendLine("  var _focusGuard = true;");
            sb.AppendLine("  var _pendingContinue = null;");
            sb.AppendLine();

            // ── waitAuthorized ──────────────────────────────────────────────────
            // После успешного логина сервер не всегда успевает применить сессию/cookie
            // к моменту, когда мы делаем location.href='/' — следующий testaccsdb на
            // главной иногда всё ещё видит accsdb:true, и страница входа мелькает снова.
            // Вместо гадания с фиксированной задержкой — реально дожидаемся accsdb:false,
            // опрашивая тот же {localhost}/testaccsdb, прежде чем редиректить.
            sb.AppendLine("  function waitAuthorized(cb) {");
            sb.AppendLine("    var tries = 0;");
            sb.AppendLine("    function check() {");
            sb.AppendLine("      tries++;");
            sb.AppendLine("      var u = '{localhost}/testaccsdb';");
            sb.AppendLine("      var uid = Lampa.Storage.get('lampac_unic_id', '');");
            sb.AppendLine("      if (uid) {");
            sb.AppendLine("        u = Lampa.Utils.addUrlComponent(u, 'uid=' + encodeURIComponent(uid));");
            sb.AppendLine("      } else {");
            sb.AppendLine("        var email = Lampa.Storage.get('account_email');");
            sb.AppendLine("        if (email) u = Lampa.Utils.addUrlComponent(u, 'account_email=' + encodeURIComponent(email));");
            sb.AppendLine("      }");
            sb.AppendLine("      var probe = new Lampa.Reguest();");
            sb.AppendLine("      probe.silent(u, function(res) {");
            sb.AppendLine("        if (!res.accsdb || tries >= 10) cb();");
            sb.AppendLine("        else setTimeout(check, 250);");
            sb.AppendLine("      }, function() { cb(); });");
            sb.AppendLine("    }");
            sb.AppendLine("    check();");
            sb.AppendLine("  }");
            sb.AppendLine();

            // ── doLogin ──────────────────────────────────────────────────────
            sb.AppendLine("  function doLogin(val) {");
            sb.AppendLine("    if (!val) return;");
            sb.AppendLine();
            sb.AppendLine("    _btn.disabled = true;");
            sb.AppendLine("    document.getElementById('dpc-btn-text').textContent = '...';");
            sb.AppendLine("    _err.textContent = '';");
            sb.AppendLine();
            sb.AppendLine("    network.clear();");
            sb.AppendLine("    var u = '{localhost}/testaccsdb';");
            sb.AppendLine("    u = Lampa.Utils.addUrlComponent(u, 'account_email=' + encodeURIComponent(val));");
            sb.AppendLine("    var uid = Lampa.Storage.get('lampac_unic_id', '');");
            sb.AppendLine("    if (uid) u = Lampa.Utils.addUrlComponent(u, 'uid=' + encodeURIComponent(uid));");
            sb.AppendLine("    network.silent(u, function(result) {");
            sb.AppendLine("      if (result.success) {");
            sb.AppendLine("        if (result.uid) {");
            sb.AppendLine("          _err.style.color = '#4ec87a';");
            sb.AppendLine("          _err.textContent = 'Аккаунт создан. Пароль: ' + result.uid + ' — запомните его, он больше не будет показан.';");
            sb.AppendLine("          Lampa.Storage.set('lampac_unic_id', result.uid);");
            // Пароль показан один раз и больше не восстановим — не уводим пользователя
            // мгновенным редиректом, а ждём явного подтверждения кнопкой (см. doc/auth-ux-guidelines.md, п.4/8).
            sb.AppendLine("          _btn.disabled = false;");
            sb.AppendLine("          document.getElementById('dpc-btn-text').textContent = 'Понятно, продолжить';");
            sb.AppendLine("          _pendingContinue = function() {");
            sb.AppendLine("            _pendingContinue = null;");
            sb.AppendLine("            _btn.disabled = true;");
            sb.AppendLine("            document.getElementById('dpc-btn-text').textContent = '...';");
            sb.AppendLine("            waitAuthorized(function() {");
            sb.AppendLine("              localStorage.removeItem('activity');");
            sb.AppendLine("              window.location.href = '/';");
            sb.AppendLine("            });");
            sb.AppendLine("          };");
            sb.AppendLine("        } else {");
            sb.AppendLine("          Lampa.Storage.set('lampac_unic_id', val);");
            sb.AppendLine("          waitAuthorized(function() {");
            sb.AppendLine("            localStorage.removeItem('activity');");
            sb.AppendLine("            window.location.href = '/';");
            sb.AppendLine("          });");
            sb.AppendLine("        }");
            sb.AppendLine("      } else {");
            sb.AppendLine("        _err.style.color = '#e0788a';");
            sb.AppendLine("        _err.textContent = 'Неправильный пароль';");
            sb.AppendLine("        _btn.disabled = false;");
            sb.AppendLine("        document.getElementById('dpc-btn-text').textContent = 'Войти по паролю';");
            sb.AppendLine("      }");
            sb.AppendLine("    }, function() {");
            sb.AppendLine("      _err.style.color = '#e0788a';");
            sb.AppendLine("      _err.textContent = 'Ошибка соединения';");
            sb.AppendLine("      _btn.disabled = false;");
            sb.AppendLine("      document.getElementById('dpc-btn-text').textContent = 'Войти по паролю';");
            sb.AppendLine("    }, { code: val });");
            sb.AppendLine("  }");
            sb.AppendLine();

            // ── openInput ────────────────────────────────────────────────────
            // Тот же Lampa.Input.edit, что использует стоковый deny.js — это встроенная
            // в Lampa текстовая клавиатура (не нативный HTML input), она уже умеет
            // работать на Apple TV/tvOS и других TV-платформах без наших ручных хаков.
            sb.AppendLine("  function openInput() {");
            sb.AppendLine("    var returned = false;");
            sb.AppendLine("    _focusGuard = false;");
            sb.AppendLine("    function ensureReturn() {");
            sb.AppendLine("      if (returned) return;");
            sb.AppendLine("      returned = true;");
            sb.AppendLine("      _focusGuard = true;");
            sb.AppendLine("      Lampa.Controller.toggle('dpc_component');");
            sb.AppendLine("    }");
            sb.AppendLine("    Lampa.Input.edit({");
            sb.AppendLine("      free: true,");
            sb.AppendLine("      title: 'Введите пароль',");
            sb.AppendLine("      nosave: true,");
            sb.AppendLine("      value: '',");
            sb.AppendLine("      nomic: true");
            sb.AppendLine("    }, function(new_value) {");
            sb.AppendLine("      // Lampa.Input.edit при закрытии жёстко переключает Controller на 'settings_component'");
            sb.AppendLine("      // (см. её исходник back()), а не на предыдущий активный — возвращаем сами,");
            sb.AppendLine("      // иначе после неверного пароля пульт перестаёт попадать на кнопку.");
            sb.AppendLine("      ensureReturn();");
            sb.AppendLine("      doLogin(new_value);");
            sb.AppendLine("    });");
            sb.AppendLine("    // Подстраховка: на некоторых платформах системный 'Отменить' закрывает");
            sb.AppendLine("    // клавиатуру, не вызывая наш колбэк вообще (значение никогда не долетает) —");
            sb.AppendLine("    // тогда фокус пульта зависает без активного компонента. Следим за исчезновением");
            sb.AppendLine("    // .settings-input из DOM и сами возвращаем фокус, если колбэк так и не пришёл.");
            sb.AppendLine("    var tries = 0;");
            sb.AppendLine("    var watch = setInterval(function() {");
            sb.AppendLine("      tries++;");
            sb.AppendLine("      if (returned) { clearInterval(watch); return; }");
            sb.AppendLine("      if (!document.querySelector('.settings-input')) {");
            sb.AppendLine("        clearInterval(watch);");
            sb.AppendLine("        ensureReturn();");
            sb.AppendLine("      } else if (tries > 1200) {");
            sb.AppendLine("        clearInterval(watch);");
            sb.AppendLine("      }");
            sb.AppendLine("    }, 100);");
            sb.AppendLine("  }");
            sb.AppendLine();

            // Кнопки — обычные .selector-элементы. Lampa.Controller сам вешает MutationObserver
            // на любой .selector в DOM и транслирует нативный click в 'hover:enter' с задержкой
            // ~20мс — этим событием подтверждается выбор что с мыши/тача, что с пульта.
            sb.AppendLine("  $(_btn).on('hover:enter', function(e) {");
            sb.AppendLine("    e.preventDefault();");
            sb.AppendLine("    if (_btn.disabled) return;");
            sb.AppendLine("    if (_pendingContinue) { _pendingContinue(); return; }");
            sb.AppendLine("    openInput();");
            sb.AppendLine("  });");
            sb.AppendLine();

            if (hasTg && conf.show_qr)
            {
                sb.AppendLine("  if (_qrpill) {");
                sb.AppendLine("    $(_qrpill).on('hover:enter', function(e) {");
                sb.AppendLine("      e.preventDefault();");
                sb.AppendLine("      window.open(_qrpill.href, '_blank', 'noopener');");
                sb.AppendLine("    });");
                sb.AppendLine("  }");
                sb.AppendLine();
            }

            // ── TV-навигация между кнопками ──────────────────────────────────
            // Настоящий Lampa.Controller вместо самодельного document-keydown:
            // collectionSet/collectionFocus сами вычисляют геометрию между .selector-
            // элементами внутри #dpc-w, а OK/Enter на активном долетает как 'hover:enter'.
            sb.AppendLine("  Lampa.Controller.add('dpc_component', {");
            sb.AppendLine("    toggle: function() {");
            sb.AppendLine("      Lampa.Controller.collectionSet($('#dpc-w'));");
            sb.AppendLine("      Lampa.Controller.collectionFocus(false, $('#dpc-w'));");
            sb.AppendLine("    },");
            sb.AppendLine("    back: function() {}");
            sb.AppendLine("  });");
            sb.AppendLine();
            // На слабых/маленьких устройствах загрузка самой Lampa завершается ПОЗЖЕ, чем мы
            // показываем оверлей, и Lampa сама дергает Controller.toggle на свой компонент —
            // фокус пульта угоняется под капот. Держим фокус силой: любой чужой toggle,
            // пока #dpc жив, тут же перебивается обратно на dpc_component. НО пока открыта
            // клавиатура Lampa.Input.edit (свой компонент 'keybord'), это же правило само
            // угоняло фокус у неё — пультом невозможно было набрать пароль. _focusGuard
            // выключается на время работы с клавиатурой (см. openInput).
            sb.AppendLine("  Lampa.Controller.listener.follow('toggle', function(e) {");
            sb.AppendLine("    if (_focusGuard && e.name !== 'dpc_component' && document.getElementById('dpc')) Lampa.Controller.toggle('dpc_component');");
            sb.AppendLine("  });");
            sb.AppendLine("  Lampa.Controller.toggle('dpc_component');");
            sb.AppendLine("}");
            sb.AppendLine();

            // ── showBlocked ─────────────────────────────────────────────────
            // Rendered instead of addDevice() when the server already gave a firm reason
            // (ban or expiry) — no password field, nothing to retry.
            sb.AppendLine("function showBlocked(msg) {");
            sb.AppendLine("  if (document.getElementById('dpc')) return;");
            sb.AppendLine("  var html = '<div id=\"dpc\"><div id=\"dpc-w\"><div id=\"dpc-bg\"></div>'");
            sb.AppendLine("    + '<div id=\"dpc-blocked\">'");
            sb.AppendLine("    + '<h1 id=\"dpc-blocked-title\"></h1>'");
            sb.AppendLine("    + '<p id=\"dpc-blocked-msg\"></p>'");
            sb.AppendLine("    + '</div></div></div>';");
            sb.AppendLine("  document.body.insertAdjacentHTML('beforeend', html);");
            sb.AppendLine("  document.getElementById('dpc-blocked-title').textContent = 'Доступ заблокирован';");
            sb.AppendLine("  document.getElementById('dpc-blocked-msg').textContent = msg || '';");
            sb.AppendLine("}");
            sb.AppendLine();

            // ── checkAutch ───────────────────────────────────────────────────
            sb.AppendLine("function checkAutch() {");
            sb.AppendLine("  var url = '{localhost}/testaccsdb';");
            sb.AppendLine("  var uid = Lampa.Storage.get('lampac_unic_id', '');");
            sb.AppendLine("  if (uid) {");
            sb.AppendLine("    url = Lampa.Utils.addUrlComponent(url, 'uid=' + encodeURIComponent(uid));");
            sb.AppendLine("  } else {");
            sb.AppendLine("    var email = Lampa.Storage.get('account_email');");
            sb.AppendLine("    if (email) url = Lampa.Utils.addUrlComponent(url, 'account_email=' + encodeURIComponent(email));");
            sb.AppendLine("  }");
            sb.AppendLine("  var token = '{token}';");
            sb.AppendLine("  if (token) url = Lampa.Utils.addUrlComponent(url, 'token={token}');");
            sb.AppendLine("  network.silent(url, function(res) {");
            sb.AppendLine("    if (res.accsdb) {");
            sb.AppendLine("      window.start_deep_link = { component: 'denypages', page: 1, url: '' };");
            sb.AppendLine("      if (res.newuid) { Lampa.Storage.set('lampac_unic_id', Lampa.Utils.uid(8).toLowerCase()); }");
            sb.AppendLine("      window.sync_disable = true;");
            sb.AppendLine("      document.getElementById('app').style.display = 'none';");
            sb.AppendLine("      var _pw = document.getElementById('loading-element');");
            sb.AppendLine("      if (_pw) _pw.style.display = 'none';");
            sb.AppendLine("      if (res.denymsg) { showBlocked(res.denymsg); }");
            sb.AppendLine("      else { setTimeout(function() { addDevice(res.msg); }, 500); }");
            sb.AppendLine("    } else {");
            sb.AppendLine("      network.clear(); network = null;");
            sb.AppendLine("    }");
            sb.AppendLine("  }, function() {});");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("checkAutch();");

            return sb.ToString();
        }

        private static string NormalizeTgUrl(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            raw = raw.Trim();
            if (raw.StartsWith("https://") || raw.StartsWith("http://") || raw.StartsWith("tg://"))
                return raw;
            return $"https://t.me/{raw.TrimStart('@')}";
        }

        private static string Js(string? value)
            => JsonSerializer.Serialize(value ?? "");
    }
}
