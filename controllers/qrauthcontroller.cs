using Microsoft.AspNetCore.Mvc;
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
    }
}
