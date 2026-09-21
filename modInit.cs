using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Shared.Models.Events;
using Shared.Models.Module;
using Shared.Models.Module.Interfaces;
using Shared.Services;
using QRAuth.Models;
using QRAuth.Services;

namespace QRAuth
{
    public class ModInit : IModuleLoaded, IModuleConfigure
    {
        public static TelegramBotConf conf = new();

        private static readonly string DenyPagePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins", "override", "deny.js");

        private static string _denyPageHash = "";
        private static Timer? _denyPageTimer;
        private static readonly object _denyPageLock = new();

        public void Configure(ConfigureModel app)
        {
            SyncConf();
            app.services.AddHostedService<TelegramBotHostedService>();
        }

        public void Loaded(InitspaceModel initspace)
        {
            SyncConf();
            EventListener.UpdateInitFile += SyncConf;

            Directory.CreateDirectory(Path.GetDirectoryName(DenyPagePath)!);
            SyncAndGenerateDenyPage();
            EventListener.UpdateInitFile += SyncAndGenerateDenyPage;
            _denyPageTimer = new Timer(_ => SyncAndGenerateDenyPage(), null,
                TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3));
        }

        public void Dispose()
        {
            EventListener.UpdateInitFile -= SyncConf;
            EventListener.UpdateInitFile -= SyncAndGenerateDenyPage;
            _denyPageTimer?.Dispose();
        }

        static void SyncConf()
        {
            conf = ModuleInvoke.Init("TelegramBot", DefaultConf());

            if (conf.enable && string.IsNullOrWhiteSpace(conf.bot_token))
                Console.WriteLine("[TelegramBot] enable=true, но bot_token пустой — проверьте секцию TelegramBot в init.conf.");
        }

        static TelegramBotConf DefaultConf() => new()
        {
            enable              = true,
            bot_token           = "",
            users_file_path     = "users.json",
            log_path            = "tgbot.log",
            admin_ids           = Array.Empty<long>(),
            default_access_days = 30
        };

        static void SyncAndGenerateDenyPage()
        {
            lock (_denyPageLock)
            {
                try
                {
                    var denyConf = ModuleInvoke.Init("DenyPage", new DenyPageConf());
                    string content = DenyPageGenerator.Build(denyConf);
                    string hash = content.GetHashCode().ToString();
                    if (hash == _denyPageHash) return;
                    File.WriteAllText(DenyPagePath, content, System.Text.Encoding.UTF8);
                    _denyPageHash = hash;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DenyPage] {ex.Message}");
                }
            }
        }
    }
}
