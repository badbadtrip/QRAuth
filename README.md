# QRAuth

[![License: MIT](https://img.shields.io/badge/license-MIT-blue?style=flat-square)](LICENSE)
[![Lampac](https://img.shields.io/badge/Lampac-NextGen-blueviolet?style=flat-square)](https://github.com/lampac-nextgen/lampac)
[![Platform](https://img.shields.io/badge/platform-Roslyn%20%7C%20.NET-informational?style=flat-square)](https://github.com/lampac-nextgen/lampac)
[![Telegram.Bot](https://img.shields.io/badge/Telegram.Bot-22.4.4-26A5E4?style=flat-square&logo=telegram)](https://github.com/TelegramBots/Telegram.Bot)

Плагин для **Lampac NextGen**: экран входа с QR-кодом + Telegram-бот, который выдаёт доступ и подтверждает вход по QR.

<p align="center">
  <img src="screenshots/lampac.png" width="900" alt="Экран входа Lampac NextGen">
</p>

Как это работает: пользователь сканирует QR → жмёт "Запросить доступ" → админ одобряет кнопкой → бот дописывает `users.json` и сразу логинит именно ту сессию - без ввода пароля. Один аккаунт держит сразу несколько устройств (TV, телефон, браузер).

<details>
<summary>Подробнее про QR-вход и мульти-устройства</summary>

<br>

- **Один аккаунт - много устройств.** Токен не привязан к устройству: один пароль/токен можно ввести на TV, телефоне и в браузере одновременно. Ограничений по количеству нет (только `accsdb.maxip_hour`/`maxrequest_hour`, если заданы, - это лимит частоты запросов, не число устройств).
- **QR удобнее пароля на TV** - не нужна экранная клавиатура, только сканирование и одна кнопка "Подтвердить" в Telegram.
- **QR одноразовый и живёт 3 минуты.** Своя сессия на каждую загрузку страницы - два устройства/пользователя друг другу не мешают. По истечении 3 минут страница сама запрашивает новый QR, без перезагрузки.
- Пароль тоже выдаётся при одобрении (для входа на другом устройстве вручную), но сам запрос через QR его не требует.

</details>

Админ управляет доступом через `/users` в боте - список, бан/разбан, статистика.

---

## Быстрый старт

**1. Скопируйте папку `QRAuth/` в `mods/` рядом с исполняемым файлом Lampac.**

Roslyn компилирует `.cs` на лету - собирать ничего не нужно, `Telegram.Bot.dll` уже лежит в `references/`.

**2. Включите `accsdb` в основном `init.conf` сервера Lampac** (это функция самого Lampac, не этого модуля):

```json
"accsdb": {
  "enable": true,
  "whitepattern": "^/(adminpanel|tgbot/qr)",
  "shared_passwd": "ваш_пароль",
  "accounts": {},
  "users": []
}
```

`whitepattern` обязательно должен пропускать `tgbot/qr` - иначе QR-вход будет заблокирован раньше, чем дойдёт до модуля.

**3. Добавьте в `init.conf` секции модуля:**

```json
"TelegramBot": {
  "enable": true,
  "bot_token": "123456:ABC-DEF...",
  "admin_ids": [123456789]
},
"DenyPage": {
  "tg_target": "@YourBot",
  "show_qr": true,
  "page_title": "Вход в систему",
  "page_subtitle": "Для доступа к серверу введите пароль, выданный администратором."
}
```

Токен бота - у [@BotFather](https://t.me/BotFather) (`/newbot`). Свой Telegram ID для `admin_ids` - у [@userinfobot](https://t.me/userinfobot).

**4. Перезапустите сервер.** Дальше правки `DenyPage` подхватываются на лету, но при первой установке (или после добавления/удаления `.cs`-файлов) нужен полный рестарт процесса.

---

## Что умеет бот

- **`/start`** → кнопка "🔑 Запросить доступ" для нового пользователя.
- Заявка уходит всем `admin_ids` с кнопками "✅ Выдать" / "❌ Отклонить". Одобрение сразу дописывает `users.json` и присылает пользователю пароль.
- **`/users`** (только для админов) - список пользователей, карточка каждого, бан/разбан, статистика (всего / активных / заблокированных).
- **QR-вход**: тот, у кого уже есть доступ, сканирует QR на экране входа и подтверждает вход одной кнопкой в боте - не нужно вводить пароль руками.
- Доступ не имеет срока действия - только бан/разбан. Продления, рассылок и аудит-лога нет: сделано минимально, разрастим по необходимости.

Полное описание протокола заявок, QR-хендшейка и формата `users.json` - в [CLAUDE.md](CLAUDE.md) вместе с архитектурой модуля.

---

## Конфигурация - все поля

<details>
<summary><b>TelegramBot</b></summary>

<br>

| Поле | Тип | По умолчанию | Описание |
|---|---|---|---|
| `enable` | `bool` | `true` | Выключить бота без удаления файлов (экран входа продолжит работать) |
| `bot_token` | `string` | `""` | Токен от BotFather. Пусто + `enable=true` → предупреждение в консоли, бот не поднимается, сервер не падает |
| `users_file_path` | `string` | `"users.json"` | Файл пользователей - тот же, что читает `accsdb` |
| `log_path` | `string` | `"tgbot.log"` | Собственный лог модуля |
| `admin_ids` | `long[]` | `[]` | Кто получает заявки и видит `/users`. Пусто → кнопка запроса доступа отвечает "Администратор не настроен" |

> [!WARNING]
> **Docker:** `users_file_path` и `log_path` - пути **внутри контейнера** (обычно `/lampac/...`), не на хосте.

</details>

<details>
<summary><b>DenyPage</b></summary>

<br>

| Поле | Тип | Описание |
|---|---|---|
| `tg_target` | `string` | `@username`, `https://t.me/…` или `tg://` - управляет QR-блоком, должен совпадать с ботом из `TelegramBot` |
| `show_qr` | `bool` | QR виден только если `tg_target` задан **и** `show_qr = true` |
| `page_title`, `page_subtitle` | `string` | Заголовок и подзаголовок |
| `step1_text`, `step2_text` | `string` | Строки инструкции под кнопкой входа |
| `qr_caption`, `qr_subcaption` | `string` | Заголовок и подпись QR-блока |
| `tg_button_text` | `string` | `aria-label` иконки-кнопки Telegram |

</details>

---

## Отключение

| Способ | Эффект |
|---|---|
| `TelegramBot.enable=false` | бот не запускается, экран входа работает без QR |
| `DenyPage.show_qr=false` | экран входа без QR-блока, форма пароля остаётся |
| `"enable": false` в `manifest.json` | модуль вообще не грузится (нужен рестарт сервера) |

---

## Разработка

Локальная сборка для IDE/линтинга (сам Lampac её не использует):

```
dotnet build QRAuth.csproj
```

Требует .NET 10 SDK. Архитектура и порядок добавления новых полей - в [CLAUDE.md](CLAUDE.md).

---

## Лицензия

[MIT](LICENSE)
