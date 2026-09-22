using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using QRAuth.Services;

namespace QRAuth.Controllers
{
    /// <summary>
    /// HTTP surface for the DenyPage QR login flow. Deliberately a plain MVC
    /// controller (no Shared/BaseController dependency) so this repo keeps building
    /// standalone via QRAuth.csproj (see README "Локальная сборка").
    ///
    /// These routes must be reachable by a visitor who is NOT yet authorized — add
    /// them to accsdb.whitepattern in init.conf (e.g. "^/(adminpanel|tgbot/qr)"),
    /// same as any other public webhook/static path per this module's README.
    /// </summary>
    [ApiController]
    [Route("tgbot/qr")]
    public class QrAuthController : ControllerBase
    {
        [HttpGet("start")]
        public ActionResult Start()
        {
            if (!ModInit.conf.enable)
                return NotFound();

            return Ok(new { session = QrAuthSessions.Create() });
        }

        [HttpGet("status")]
        public ActionResult Status([FromQuery] string session)
        {
            if (string.IsNullOrWhiteSpace(session))
                return BadRequest(new { error = "session is required" });

            var (status, token) = QrAuthSessions.ConsumeIfConfirmed(session);
            return Ok(new { status, token });
        }

        /// <summary>Fire-and-forget ping from the deny-page password form (see doLogin() in
        /// DenyPageGenerator.cs) — the only way this module learns about a plain-password
        /// login, since that request goes straight to Lampac's own /testaccsdb and never
        /// touches this module otherwise. Always 200s so a missing/unknown token can't be
        /// used to probe which tokens exist.</summary>
        [HttpPost("login-ping")]
        public async Task<ActionResult> LoginPing([FromQuery] string token)
        {
            if (!ModInit.conf.enable || string.IsNullOrWhiteSpace(token))
                return Ok();

            var bot  = TelegramBotHostedService.Bot;
            var repo = TelegramBotHostedService.Repo;
            if (bot == null || repo == null)
                return Ok();

            var user = repo.GetByToken(token);
            if (user == null)
                return Ok();

            var text = string.Join("\n", new[]
            {
                "🔑  <b>Вход по паролю</b>",
                $"👤  <b>{System.Net.WebUtility.HtmlEncode(user.Comment)}</b>",
                $"🆔  <code>{user.TgId}</code>"
            });

            foreach (var adminId in ModInit.conf.admin_ids)
            {
                try
                {
                    await bot.SendMessage(adminId, text, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
                }
                catch (Exception ex)
                {
                    FileLog.Write($"[TelegramBot] notify admin {adminId} failed", ex);
                }
            }

            return Ok();
        }
    }
}
