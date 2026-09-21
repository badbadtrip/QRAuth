#nullable enable
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace QRAuth.Services
{
    /// <summary>
    /// Bot has two jobs: confirm the deny-page QR login handshake for a user who
    /// already has active access, and let a user request access with an admin
    /// (configured via admin_ids) granting/denying it with a button tap. Everything
    /// beyond that (self-service beyond one request, revocation, broadcasts, audit) is
    /// still out of scope — grow it back deliberately if that stops being enough.
    /// </summary>
    public class BotSession
    {
        static readonly TimeSpan RequestCooldown = TimeSpan.FromMinutes(10);
        static readonly ConcurrentDictionary<long, DateTime> _lastRequest = new();
        static readonly ConcurrentDictionary<long, string> _pendingRequesterNames = new();
        static readonly ConcurrentDictionary<long, string> _pendingQrSessions = new();

        readonly UsersRepository _repo;

        public BotSession(UsersRepository repo)
        {
            _repo = repo;
        }

        static DateTime ParseExpiry(string expires) =>
            DateTimeOffset.TryParse(expires, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var dto)
                ? dto.UtcDateTime
                : DateTime.MinValue;

        static bool IsAdmin(long tgId) => Array.IndexOf(ModInit.conf.admin_ids, tgId) >= 0;

        public async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
        {
            try
            {
                if (update.CallbackQuery is { } cb) { await HandleCallbackAsync(bot, cb, ct); return; }
                if (update.Message is { } msg) await HandleMessageAsync(bot, msg, ct);
            }
            catch (Exception ex)
            {
                FileLog.Write("[TelegramBot] HandleUpdate error", ex);

                // otherwise the button just spins forever with no feedback for whoever tapped it
                if (update.CallbackQuery is { } cb)
                {
                    try { await bot.AnswerCallbackQuery(cb.Id, "Ошибка на сервере, смотри tgbot.log.", showAlert: true, cancellationToken: ct); }
                    catch { /* best effort */ }
                }
            }
        }

        async Task HandleMessageAsync(ITelegramBotClient bot, Message msg, CancellationToken ct)
        {
            var text = msg.Text?.Trim() ?? "";
            if (!text.StartsWith("/start")) return;

            var payload = text.Length > 6 ? text.Substring(6).Trim() : "";
            if (payload.StartsWith("qr_", StringComparison.Ordinal))
            {
                await HandleQrStartAsync(bot, msg, payload.Substring(3), ct);
                return;
            }

            await SendWelcomeAsync(bot, msg, ct);
        }

        async Task SendWelcomeAsync(ITelegramBotClient bot, Message msg, CancellationToken ct)
        {
            long userId = msg.From?.Id ?? 0;
            var existing = _repo.GetByTgId(userId);
            if (existing != null && ParseExpiry(existing.Expires) >= DateTime.UtcNow)
            {
                await bot.SendMessage(msg.Chat.Id,
                    "👋  Привет!\n\nУ вас уже есть активный доступ к Lampa.",
                    cancellationToken: ct);
                return;
            }

            var kb = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("🔑  Запросить доступ", "reqaccess") }
            });
            await bot.SendMessage(msg.Chat.Id,
                "👋  Привет!\n\nЭтот бот подтверждает вход в Lampa по QR-коду с экрана авторизации и может запросить для вас доступ у администратора.",
                replyMarkup: kb, cancellationToken: ct);
        }

        /// <summary>Deep link from the deny-page QR: https://t.me/&lt;bot&gt;?start=qr_&lt;sessionId&gt;.</summary>
        async Task HandleQrStartAsync(ITelegramBotClient bot, Message msg, string sessionId, CancellationToken ct)
        {
            long userId = msg.From?.Id ?? 0;

            if (string.IsNullOrWhiteSpace(sessionId) || sessionId.Length > 32)
            {
                await bot.SendMessage(msg.Chat.Id, "Некорректная ссылка входа.", cancellationToken: ct);
                return;
            }

            var existing = _repo.GetByTgId(userId);
            if (existing == null || ParseExpiry(existing.Expires) < DateTime.UtcNow)
            {
                // remembered so a fast admin approval can auto-confirm this same session
                // (see HandleGrantAsync) instead of making the user rescan the QR
                _pendingQrSessions[userId] = sessionId;

                // no dead end — same welcome + "request access" button as plain /start
                await SendWelcomeAsync(bot, msg, ct);
                return;
            }

            var kb = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("✅  Подтвердить вход", "qrauth:" + sessionId) }
            });
            await bot.SendMessage(msg.Chat.Id,
                "🖥  Кто-то пытается войти в Lampa с помощью этого QR-кода.\n\nЕсли это вы — нажмите кнопку ниже.",
                replyMarkup: kb, cancellationToken: ct);
        }

        async Task HandleCallbackAsync(ITelegramBotClient bot, CallbackQuery cb, CancellationToken ct)
        {
            var data = cb.Data ?? "";

            if (data == "reqaccess") { await HandleRequestAccessAsync(bot, cb, ct); return; }
            if (data.StartsWith("grant:", StringComparison.Ordinal)) { await HandleGrantAsync(bot, cb, data.Substring(6), ct); return; }
            if (data.StartsWith("deny:", StringComparison.Ordinal)) { await HandleDenyAsync(bot, cb, data.Substring(5), ct); return; }
            if (data.StartsWith("qrauth:", StringComparison.Ordinal)) { await HandleQrAuthAsync(bot, cb, data.Substring(7), ct); return; }

            await bot.AnswerCallbackQuery(cb.Id, cancellationToken: ct);
        }

        async Task HandleQrAuthAsync(ITelegramBotClient bot, CallbackQuery cb, string sessionId, CancellationToken ct)
        {
            var user = _repo.GetByTgId(cb.From.Id);
            if (user == null || ParseExpiry(user.Expires) < DateTime.UtcNow)
            {
                await bot.AnswerCallbackQuery(cb.Id, "Доступ не активен.", showAlert: true, cancellationToken: ct);
                return;
            }

            if (!QrAuthSessions.TryConfirm(sessionId, user.Id))
            {
                await bot.AnswerCallbackQuery(cb.Id, "Ссылка устарела, отсканируйте QR заново.", showAlert: true, cancellationToken: ct);
                return;
            }

            await bot.AnswerCallbackQuery(cb.Id, "✅  Готово", cancellationToken: ct);
            long chatId = cb.Message?.Chat.Id ?? 0;
            int msgId = cb.Message?.MessageId ?? 0;
            await bot.EditMessageText(chatId, msgId, "✅  Вход подтверждён. Вернитесь на экран входа Lampa.", cancellationToken: ct);
        }

        async Task HandleRequestAccessAsync(ITelegramBotClient bot, CallbackQuery cb, CancellationToken ct)
        {
            long userId = cb.From.Id;

            var existing = _repo.GetByTgId(userId);
            if (existing != null && ParseExpiry(existing.Expires) >= DateTime.UtcNow)
            {
                await bot.AnswerCallbackQuery(cb.Id, "У вас уже есть активный доступ.", showAlert: true, cancellationToken: ct);
                return;
            }

            if (ModInit.conf.admin_ids.Length == 0)
            {
                await bot.AnswerCallbackQuery(cb.Id, "Администратор не настроен.", showAlert: true, cancellationToken: ct);
                return;
            }

            if (_lastRequest.TryGetValue(userId, out var last) && DateTime.UtcNow - last < RequestCooldown)
            {
                await bot.AnswerCallbackQuery(cb.Id, "Заявка уже отправлена, дождитесь ответа администратора.", showAlert: true, cancellationToken: ct);
                return;
            }
            _lastRequest[userId] = DateTime.UtcNow;

            var name = cb.From.Username is { Length: > 0 } uname
                ? $"@{uname} / {cb.From.FirstName}"
                : cb.From.FirstName;
            _pendingRequesterNames[userId] = name;
            var kb = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("✅  Выдать", "grant:" + userId),
                    InlineKeyboardButton.WithCallbackData("❌  Отклонить", "deny:" + userId)
                }
            });

            foreach (var adminId in ModInit.conf.admin_ids)
            {
                try
                {
                    await bot.SendMessage(adminId,
                        $"📩  Запрос доступа от {name} (id={userId}).",
                        replyMarkup: kb, cancellationToken: ct);
                }
                catch (Exception ex)
                {
                    // admin hasn't started the bot / chat unreachable — don't block other admins
                    FileLog.Write($"[TelegramBot] notify admin {adminId} failed", ex);
                }
            }

            await bot.AnswerCallbackQuery(cb.Id, "Заявка отправлена администратору.", cancellationToken: ct);
        }

        async Task HandleGrantAsync(ITelegramBotClient bot, CallbackQuery cb, string tgIdStr, CancellationToken ct)
        {
            if (!IsAdmin(cb.From.Id))
            {
                await bot.AnswerCallbackQuery(cb.Id, "Недоступно.", showAlert: true, cancellationToken: ct);
                return;
            }
            if (!long.TryParse(tgIdStr, out var tgId))
            {
                await bot.AnswerCallbackQuery(cb.Id, cancellationToken: ct);
                return;
            }

            var days = ModInit.conf.default_access_days;
            var requester = _pendingRequesterNames.TryGetValue(tgId, out var reqName) ? reqName : tgId.ToString();

            string token;
            try
            {
                // comment is just the account name — kept parseable so an admin listing
                // (or anything else keyed on it) can rely on its shape
                token = _repo.AddUser(tgId, TimeSpan.FromDays(days), requester);
            }
            catch (Exception ex)
            {
                FileLog.Write($"[TelegramBot] AddUser failed (tgId={tgId})", ex);
                await bot.AnswerCallbackQuery(cb.Id, "Ошибка записи в users.json, смотри tgbot.log на сервере.", showAlert: true, cancellationToken: ct);
                return;
            }

            _pendingRequesterNames.TryRemove(tgId, out _);
            _lastRequest.TryRemove(tgId, out _);
            FileLog.Write($"[TelegramBot] Доступ выдан tgId={tgId} на {days} дн., admin={cb.From.Id}");
            await bot.AnswerCallbackQuery(cb.Id, "✅  Доступ выдан.", cancellationToken: ct);
            await MarkHandledAsync(bot, cb, "✅  Выдано.", ct);

            // if the user requested access from a still-live QR scan, confirming it here
            // logs the deny page in immediately, without the user touching anything —
            // works only while that page is still polling that exact session (~3 min)
            var autoConfirmed = _pendingQrSessions.TryRemove(tgId, out var sessionId)
                && QrAuthSessions.TryConfirm(sessionId, token);
            if (autoConfirmed)
                FileLog.Write($"[TelegramBot] QR-сессия {sessionId} авто-подтверждена при выдаче tgId={tgId}");

            var text = autoConfirmed
                ? $"✅  Администратор выдал вам доступ к Lampa. Экран входа должен открыться сам.\n\nЕсли нет — пароль: <code>{token}</code>\nДействует {days} дн."
                : $"✅  Администратор выдал вам доступ к Lampa.\n\nОтсканируйте QR на экране входа ещё раз — он войдёт сам. Либо введите пароль вручную: <code>{token}</code>\nДействует {days} дн.";

            try
            {
                await bot.SendMessage(tgId, text, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, cancellationToken: ct);
            }
            catch (Exception ex)
            {
                FileLog.Write($"[TelegramBot] notify user {tgId} failed", ex);
            }
        }

        async Task HandleDenyAsync(ITelegramBotClient bot, CallbackQuery cb, string tgIdStr, CancellationToken ct)
        {
            if (!IsAdmin(cb.From.Id))
            {
                await bot.AnswerCallbackQuery(cb.Id, "Недоступно.", showAlert: true, cancellationToken: ct);
                return;
            }
            if (!long.TryParse(tgIdStr, out var tgId))
            {
                await bot.AnswerCallbackQuery(cb.Id, cancellationToken: ct);
                return;
            }

            _pendingRequesterNames.TryRemove(tgId, out _);
            _pendingQrSessions.TryRemove(tgId, out _);
            _lastRequest.TryRemove(tgId, out _);
            await bot.AnswerCallbackQuery(cb.Id, "Отклонено.", cancellationToken: ct);
            await MarkHandledAsync(bot, cb, "❌  Отклонено.", ct);

            try
            {
                await bot.SendMessage(tgId, "❌  Администратор отклонил заявку на доступ.", cancellationToken: ct);
            }
            catch (Exception ex)
            {
                FileLog.Write($"[TelegramBot] notify user {tgId} failed (deny)", ex);
            }
        }

        static async Task MarkHandledAsync(ITelegramBotClient bot, CallbackQuery cb, string suffix, CancellationToken ct)
        {
            long chatId = cb.Message?.Chat.Id ?? 0;
            int msgId = cb.Message?.MessageId ?? 0;
            var original = cb.Message?.Text ?? "";
            await bot.EditMessageText(chatId, msgId, original + "\n\n" + suffix, cancellationToken: ct);
        }
    }
}
