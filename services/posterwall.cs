#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Shared;
using Shared.Models.Base;
using Shared.Services;

namespace QRAuth.Services
{
    /// <summary>
    /// Poster wall behind the deny page (Netflix-style). The page is shown to visitors
    /// who are NOT authorized yet, and Lampac's accsdb middleware blocks /tmdb/* for them —
    /// so instead of opening the whole TMDB proxy via whitepattern, this module fetches
    /// posters itself through the same /tmdb proxy as a local request (lcrqpasswd header
    /// passes accsdb), caches them on disk and serves only those files from
    /// /tgbot/qr/poster/{n} (let through by ModInit.AllowQrRoutes).
    /// TVs only ever talk to this server, never to TMDB directly (blocked in some regions).
    /// </summary>
    public static class PosterWall
    {
        public const int MaxPosters = 30;
        static readonly TimeSpan MaxAge = TimeSpan.FromHours(24);

        static readonly string Dir =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache", "qrauth", "posters");
        static string MetaPath => Path.Combine(Dir, "meta.txt");

        static readonly SemaphoreSlim _gate = new(1, 1);

        // Fallback chain when the host's own /tmdb proxy can't reach api.themoviedb.org /
        // image.tmdb.org (blocked in RU/BY without a proxy). Same public mirrors the Lampa
        // client itself uses with "Проксировать TMDB" on — src/core/tmdb/proxy.js
        // (path_api / path_api_backup / ImageMirror) and src/core/manifest.js (cub domains).
        static readonly string[] ApiMirrors =
        {
            "https://apitmdb.cub.red/3/",
            "https://apitmdb.kurwa-bober.ninja/3/",
            "https://apitmdb.nackhui.com/3/",
            "http://lampa.byskaz.ru/tmdb/api/3/"
        };
        static readonly string[] ImgMirrors =
        {
            "https://imagetmdb.com/",
            "https://nl.imagetmdb.com/",
            "https://de.imagetmdb.com/",
            "https://pl.imagetmdb.com/"
        };

        /// <summary>Number of cached posters (0 = none yet / disabled / fetch failed).</summary>
        public static int Count { get; private set; }

        /// <summary>Cache-busting stamp of the current set, appended as ?v= by the page.</summary>
        public static long Version { get; private set; }

        /// <summary>Short machine-readable outcome of the last refresh, returned by
        /// /tgbot/qr/posters so a failure is diagnosable without shell access to the
        /// server (details still go to tgbot.log): pending | ok | tmdb_unreachable |
        /// tmdb_empty | images_failed | error.</summary>
        public static string State { get; private set; } = "pending";

        public static string? FilePath(int n)
        {
            if (n < 0 || n >= Count) return null;
            string path = Path.Combine(Dir, n + ".jpg");
            return File.Exists(path) ? path : null;
        }

        /// <summary>Restores Count/Version from disk after a restart, so a fresh set isn't
        /// re-downloaded just because the process came back up.</summary>
        static void LoadMeta(string source)
        {
            try
            {
                if (!File.Exists(MetaPath)) return;
                var parts = File.ReadAllText(MetaPath).Split(';');
                if (parts.Length < 3 || parts[2] != source) return;
                Count   = int.Parse(parts[0]);
                Version = long.Parse(parts[1]);
            }
            catch { Count = 0; }
        }

        /// <summary>Refetches the set if it's older than a day or the source changed.
        /// Safe to call often — cheap no-op while the cache is fresh.</summary>
        public static async Task RefreshAsync(string source)
        {
            if (!await _gate.WaitAsync(0)) return;
            try
            {
                if (Count == 0) LoadMeta(source);
                var age = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(Version);
                if (Count > 0 && age < MaxAge && File.Exists(MetaPath) && File.ReadAllText(MetaPath).EndsWith(";" + source))
                {
                    State = "ok";
                    return;
                }

                string host   = $"http://{CoreInit.conf.listen.localhost}:{CoreInit.conf.listen.port}";
                var headers   = HeadersModel.Init(("lcrqpasswd", CoreInit.rootPasswd));
                var posters   = new List<string>();
                bool answered = false;

                // Host's own proxy first (honours its proxyapi settings), then public mirrors.
                // Whichever answers page 1 is used for the remaining pages.
                var apiBases = new List<string> { $"{host}/tmdb/api/3/" };
                apiBases.AddRange(ApiMirrors);
                string? apiBase = null;

                for (int page = 1; page <= 3 && posters.Count < MaxPosters; page++)
                {
                    JObject? json = null;
                    foreach (var b in apiBase != null ? new List<string> { apiBase } : apiBases)
                    {
                        json = await Http.Get<JObject>(
                            $"{b}{Endpoint(source)}?api_key={CoreInit.conf.cub.api_key}&language=ru&page={page}",
                            timeoutSeconds: 10, headers: b.StartsWith(host) ? headers : null);
                        if (json?["results"] is JArray) { apiBase = b; break; }
                    }

                    if (json?["results"] is not JArray results) break;
                    if (!answered) FileLog.Write($"[PosterWall] {source}: TMDB API через {apiBase}");
                    answered = true;
                    foreach (var item in results)
                    {
                        if (item.Value<bool?>("adult") == true) continue;
                        string? poster = item.Value<string>("poster_path");
                        if (!string.IsNullOrEmpty(poster) && !posters.Contains(poster))
                            posters.Add(poster);
                        if (posters.Count >= MaxPosters) break;
                    }
                }

                if (posters.Count == 0)
                {
                    State = answered ? "tmdb_empty" : "tmdb_unreachable";
                    FileLog.Write(answered
                        ? $"[PosterWall] {source}: TMDB вернул 0 постеров — оставляю прежний набор ({Count})"
                        : $"[PosterWall] {source}: ни {host}/tmdb/api, ни зеркала CUB не ответили — оставляю прежний набор ({Count})");
                    return;
                }

                // Download into a side directory and swap, so the page never sees a
                // half-written set.
                string tmp = Dir + ".tmp";
                if (Directory.Exists(tmp)) Directory.Delete(tmp, true);
                Directory.CreateDirectory(tmp);

                // Same idea for images: stick with the first base that delivers, fall back
                // down the list per poster only when it fails.
                var imgBases = new List<string> { $"{host}/tmdb/img/" };
                imgBases.AddRange(ImgMirrors);
                int imgIdx = 0;

                int saved = 0;
                foreach (var poster in posters)
                {
                    for (int i = imgIdx; i < imgBases.Count; i++)
                    {
                        string b = imgBases[i];
                        var bytes = await Http.Download($"{b}t/p/w342{poster}", timeoutSeconds: 15, headers: b.StartsWith(host) ? headers : null);
                        if (bytes == null || bytes.Length < 1024) continue;
                        if (i != imgIdx || saved == 0) FileLog.Write($"[PosterWall] картинки через {b}");
                        imgIdx = i;
                        await File.WriteAllBytesAsync(Path.Combine(tmp, saved + ".jpg"), bytes);
                        saved++;
                        break;
                    }
                }

                if (saved == 0)
                {
                    Directory.Delete(tmp, true);
                    State = "images_failed";
                    FileLog.Write($"[PosterWall] {source}: не скачался ни один постер — оставляю прежний набор ({Count})");
                    return;
                }

                long version = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                await File.WriteAllTextAsync(Path.Combine(tmp, "meta.txt"), $"{saved};{version};{source}");

                Count = 0;
                if (Directory.Exists(Dir)) Directory.Delete(Dir, true);
                Directory.CreateDirectory(Path.GetDirectoryName(Dir)!);
                Directory.Move(tmp, Dir);
                Count   = saved;
                Version = version;
                State   = "ok";
                FileLog.Write($"[PosterWall] {source}: сохранено {saved} постеров");
            }
            catch (Exception ex)
            {
                State = "error";
                FileLog.Write("[PosterWall] refresh failed", ex);
            }
            finally
            {
                _gate.Release();
            }
        }

        static string Endpoint(string source) => source switch
        {
            "popular"     => "movie/popular",
            "now_playing" => "movie/now_playing",
            "top_rated"   => "movie/top_rated",
            _             => "trending/all/week"
        };
    }
}
