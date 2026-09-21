#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.Extensions.Logging;
using QRAuth.Models;

namespace QRAuth.Services
{
    /// <summary>Access to the shared users.json. Reads are used for the QR-confirm lookup;
    /// AddUser is used by the admin approval flow to grant access. Anything else about a
    /// record (editing, revoking) is still done by hand.</summary>
    public class UsersRepository
    {
        static readonly object _writeLock = new();

        // default JSON encoder escapes Cyrillic (and most non-ASCII) as \uXXXX — unreadable
        // when someone opens users.json by hand to check a comment
        static readonly JsonSerializerOptions _writeOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic)
        };

        readonly string _path;
        readonly ILogger<UsersRepository> _logger;

        public UsersRepository(string path, ILogger<UsersRepository> logger)
        {
            _path   = path;
            _logger = logger;
        }

        public List<LampacUser> ReadAll()
        {
            try
            {
                if (!File.Exists(_path)) return new();
                var json = File.ReadAllText(_path);
                return JsonSerializer.Deserialize<List<LampacUser>>(json) ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TelegramBot] ReadUsers error");
                return new();
            }
        }

        public LampacUser? GetByTgId(long tgId) =>
            ReadAll().FirstOrDefault(u => u.TgId == tgId);

        /// <summary>Grants a fresh token to a Telegram id, replacing any existing record for
        /// that id. Returns the generated token (the password the user logs in with).</summary>
        public string AddUser(long tgId, TimeSpan validFor, string comment)
        {
            lock (_writeLock)
            {
                var users = ReadAll();
                users.RemoveAll(u => u.TgId == tgId);

                var token = GenerateToken();
                users.Add(new LampacUser
                {
                    Id      = token,
                    TgId    = tgId,
                    Group   = 1,
                    Expires = (DateTime.UtcNow + validFor).ToString("O"),
                    Comment = comment
                });

                File.WriteAllText(_path, JsonSerializer.Serialize(users, _writeOptions));
                return token;
            }
        }

        static string GenerateToken()
        {
            Span<byte> bytes = stackalloc byte[9];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }
    }
}
