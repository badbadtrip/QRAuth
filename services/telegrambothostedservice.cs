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

            try
            {
                await bot.SetMyCommands(new[]
                {
                    new Telegram.Bot.Types.BotCommand { Command = "start", Description = "Подтвердить вход по QR" }
                }, cancellationToken: ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[TelegramBot] SetMyCommands warning");
            }

            // /users only shows in the menu for admins (chat-scoped command list) — everyone
            // else keeps the plain default set above, HandleMessageAsync's IsAdmin check is
            // the actual enforcement, this is just menu discoverability.
            foreach (var adminId in ModInit.conf.admin_ids)
            {
                try
                {
                    await bot.SetMyCommands(new[]
                    {
                        new Telegram.Bot.Types.BotCommand { Command = "start", Description = "Подтвердить вход по QR" },
                        new Telegram.Bot.Types.BotCommand { Command = "users", Description = "Список пользователей" }
                    }, scope: new Telegram.Bot.Types.BotCommandScopeChat { ChatId = adminId }, cancellationToken: ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // admin hasn't started the bot yet / chat unreachable — non-fatal, same as notify-admin failures elsewhere
                    _logger.LogWarning(ex, "[TelegramBot] SetMyCommands (admin scope) warning, adminId={AdminId}", adminId);
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
