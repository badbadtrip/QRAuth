using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using QRAuth.Services;

namespace QRAuth.Services
{
    public sealed class TelegramBotHostedService : BackgroundService
    {
        const int GetUpdatesLimit          = 100;
        const int GetUpdatesTimeoutSeconds = 50;
        static readonly TimeSpan ErrorDelay = TimeSpan.FromSeconds(5);

        readonly ILoggerFactory _loggerFactory;
        readonly ILogger<TelegramBotHostedService> _logger;

        public TelegramBotHostedService(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger        = loggerFactory.CreateLogger<TelegramBotHostedService>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await RunBotAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "[TelegramBot] Fatal error");
                throw;
            }
        }

        async Task RunBotAsync(CancellationToken ct)
        {
            var conf = ModInit.conf;

            if (!conf.enable || string.IsNullOrWhiteSpace(conf.bot_token))
            {
                _logger.LogInformation("[TelegramBot] Отключён (enable=false или пустой bot_token).");
                return;
            }

            FileLog.Configure(Path.GetFullPath(conf.log_path));
            FileLog.Write("[TelegramBot] Запуск...");

            var repo    = new UsersRepository(Path.GetFullPath(conf.users_file_path), _loggerFactory.CreateLogger<UsersRepository>());
            var session = new BotSession(repo);
            var bot     = new TelegramBotClient(conf.bot_token.Trim());

            try
            {
                var me = await bot.GetMe(ct).ConfigureAwait(false);
                _logger.LogInformation("[TelegramBot] @{Username} запущен.", me.Username);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TelegramBot] GetMe не удался");
                FileLog.Write("[TelegramBot] GetMe не удался", ex);
                return;
            }

            try
            {
                await bot.DeleteWebhook(dropPendingUpdates: false, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[TelegramBot] DeleteWebhook warning");
            }

            // Just /start in the menu for everyone, admin included — /users stays reachable
            // for admins via the "👥 Пользователи" reply-keyboard button (ShowAdminPanelAsync),
            // no need to clutter the slash-command autocomplete with it.
            try
            {
                await bot.SetMyCommands(new[]
                {
                    new Telegram.Bot.Types.BotCommand { Command = "start", Description = "Запустить бота" }
                }, cancellationToken: ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[TelegramBot] SetMyCommands warning");
            }

            // a chat-scoped command list (set for admins by an older build) outranks the
            // default one above and Telegram keeps serving it forever until explicitly
            // deleted — without this, admins would still see the old /start+/users menu
            foreach (var adminId in ModInit.conf.admin_ids)
            {
                try
                {
                    await bot.DeleteMyCommands(scope: new Telegram.Bot.Types.BotCommandScopeChat { ChatId = adminId }, cancellationToken: ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[TelegramBot] DeleteMyCommands (admin scope) warning, adminId={AdminId}", adminId);
                }
            }

            _logger.LogInformation("[TelegramBot] Long polling запущен (limit={Limit}, timeout={Timeout}s).",
                GetUpdatesLimit, GetUpdatesTimeoutSeconds);

            int? offset = null;

            while (!ct.IsCancellationRequested)
            {
                Update[] updates;
                try
                {
                    updates = await bot.GetUpdates(
                        offset,
                        limit: GetUpdatesLimit,
                        timeout: GetUpdatesTimeoutSeconds,
                        allowedUpdates: null,
                        cancellationToken: ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[TelegramBot] GetUpdates error");
                    FileLog.Write("[TelegramBot] GetUpdates error", ex);
                    try { await Task.Delay(ErrorDelay, ct).ConfigureAwait(false); }
                    catch (OperationCanceledException) { break; }
                    continue;
                }

                foreach (var update in updates)
                {
                    offset = update.Id + 1;
                    try
                    {
                        await session.HandleUpdateAsync(bot, update, ct).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[TelegramBot] HandleUpdate error (UpdateId={UpdateId})", update.Id);
                        FileLog.Write($"[TelegramBot] HandleUpdate error (UpdateId={update.Id})", ex);
                    }
                }
            }
        }
    }
}
