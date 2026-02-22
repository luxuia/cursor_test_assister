using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace GameAssistant.Tools
{
    /// <summary>
    /// Steam Web API 客户端，用于获取 Dota 2 英雄、物品的官方中英文名称。
    /// API Key 从 Data/steam_api_key.txt 读取，请勿将 Key 提交到版本库。
    /// </summary>
    public static class SteamApiClient
    {
        private const string BaseUrl = "https://api.steampowered.com";
        private const string AppId = "570";

        private static HttpClient? _httpClient;
        private static readonly object HttpClientLock = new object();

        private static HttpClient HttpClient
        {
            get
            {
                if (_httpClient != null) return _httpClient;
                lock (HttpClientLock)
                {
                    if (_httpClient != null) return _httpClient;
                    var handler = new HttpClientHandler();
                    try
                    {
                        handler.ServerCertificateCustomValidationCallback = (_, __, ___, ____) => true;
                    }
                    catch { /* 部分平台无此 API */ }
                    _httpClient = new HttpClient(handler)
                    {
                        Timeout = TimeSpan.FromSeconds(30)
                    };
                    _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "GameAssistant/1.0 (Dota2; Steam Web API)");
                }
                return _httpClient;
            }
        }

        /// <summary>
        /// 从 Data/steam_api_key.txt 读取 API Key（Trim）。若文件不存在或为空则返回 null。
        /// </summary>
        public static string? GetApiKey()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var path = Path.Combine(baseDir, "Data", "steam_api_key.txt");
                if (!File.Exists(path))
                    path = Path.Combine(Directory.GetCurrentDirectory(), "Data", "steam_api_key.txt");
                if (!File.Exists(path)) return null;
                var key = File.ReadAllText(path).Trim();
                return string.IsNullOrEmpty(key) ? null : key;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 仅从 Steam API 拉取英雄中文名（zh_CN 单次请求）。返回 id -> nameCn，失败返回 null。
        /// </summary>
        public static async Task<Dictionary<string, string>?> GetHeroNameCnMapAsync(IProgress<string>? progress = null)
        {
            var key = GetApiKey();
            if (string.IsNullOrEmpty(key)) return null;
            progress?.Report("正在从 Steam API 拉取英雄中文名...");
            var (list, error) = await GetHeroesRawWithErrorAsync(key, "zh_CN").ConfigureAwait(false);
            if (list == null || list.Count == 0)
            {
                progress?.Report(error ?? "Steam 英雄中文未获取到");
                return null;
            }
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var h in list)
            {
                var id = SteamHeroNameToId(h.Name);
                var nameCn = (h.LocalizedName ?? "").Trim();
                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(nameCn))
                    map[id] = nameCn;
            }
            progress?.Report($"Steam 已拉取 {map.Count} 个英雄中文名");
            return map;
        }

        /// <summary>
        /// 仅从 Steam API 拉取物品中文名（zh_CN 单次请求）。返回 id -> nameCn（含 item_xxx 与无前缀两种键），失败返回 null。
        /// </summary>
        public static async Task<Dictionary<string, string>?> GetItemNameCnMapAsync(IProgress<string>? progress = null)
        {
            var key = GetApiKey();
            if (string.IsNullOrEmpty(key)) return null;
            progress?.Report("正在从 Steam API 拉取物品中文名...");
            var (list, error) = await GetGameItemsRawWithErrorAsync(key, "zh_CN").ConfigureAwait(false);
            if (list == null || list.Count == 0)
            {
                progress?.Report(error ?? "Steam 物品中文未获取到");
                return null;
            }
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var i in list)
            {
                var id = (i.Name ?? "").Trim();
                var nameCn = (i.LocalizedName ?? "").Trim();
                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(nameCn)) continue;
                map[id] = nameCn;
                if (id.StartsWith("item_", StringComparison.OrdinalIgnoreCase))
                    map[id.Substring(5)] = nameCn;
            }
            progress?.Report($"Steam 已拉取 {list.Count} 个物品中文名");
            return map;
        }

        private static string SteamHeroNameToId(string? name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            const string prefix = "npc_dota_hero_";
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return name.Substring(prefix.Length).Trim();
            return name.Trim();
        }

        private static async Task<(List<SteamHeroRaw>? list, string? error)> GetHeroesRawWithErrorAsync(string key, string language)
        {
            try
            {
                var url = $"{BaseUrl}/IEconDOTA2_{AppId}/GetHeroes/v0001/?key={Uri.EscapeDataString(key)}&language={Uri.EscapeDataString(language)}";
                using var response = await HttpClient.GetAsync(url).ConfigureAwait(false);
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return (null, $"Steam API 请求失败: HTTP {(int)response.StatusCode} - {json?.Substring(0, Math.Min(200, json?.Length ?? 0))}");
                var root = JObject.Parse(json);
                var heroes = root["result"]?["heroes"] as JArray;
                if (heroes == null) return (null, "Steam API 返回格式异常（无 result.heroes）");
                var list = heroes.Select(t => new SteamHeroRaw
                {
                    Name = t["name"]?.ToString(),
                    Id = t["id"]?.Value<int?>(),
                    LocalizedName = t["localized_name"]?.ToString()
                }).ToList();
                return (list, null);
            }
            catch (HttpRequestException ex)
            {
                var msg = $"网络请求失败: {ex.Message}";
                if (ex.InnerException != null) msg += " | " + ex.InnerException.Message;
                return (null, msg);
            }
            catch (TaskCanceledException)
            {
                return (null, "请求超时，请检查网络或稍后重试");
            }
            catch (Exception ex)
            {
                return (null, $"Steam API 错误: {ex.Message}");
            }
        }

        private static async Task<(List<SteamItemRaw>? list, string? error)> GetGameItemsRawWithErrorAsync(string key, string language)
        {
            try
            {
                var url = $"{BaseUrl}/IEconDOTA2_{AppId}/GetGameItems/v1/?key={Uri.EscapeDataString(key)}&language={Uri.EscapeDataString(language)}";
                using var response = await HttpClient.GetAsync(url).ConfigureAwait(false);
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return (null, $"Steam API 请求失败: HTTP {(int)response.StatusCode} - {json?.Substring(0, Math.Min(200, json?.Length ?? 0))}");
                var root = JObject.Parse(json);
                var items = root["result"]?["items"] as JArray;
                if (items == null) return (null, "Steam API 返回格式异常（无 result.items）");
                var list = items.Select(t => new SteamItemRaw
                {
                    Name = t["name"]?.ToString(),
                    LocalizedName = t["localized_name"]?.ToString(),
                    Cost = t["cost"]?.Value<int>() ?? 0
                }).ToList();
                return (list, null);
            }
            catch (HttpRequestException ex)
            {
                var msg = $"网络请求失败: {ex.Message}";
                if (ex.InnerException != null) msg += " | " + ex.InnerException.Message;
                return (null, msg);
            }
            catch (TaskCanceledException)
            {
                return (null, "请求超时，请检查网络或稍后重试");
            }
            catch (Exception ex)
            {
                return (null, $"Steam API 错误: {ex.Message}");
            }
        }

        private class SteamHeroRaw
        {
            public string? Name { get; set; }
            public int? Id { get; set; }
            public string? LocalizedName { get; set; }
        }

        private class SteamItemRaw
        {
            public string? Name { get; set; }
            public string? LocalizedName { get; set; }
            public int Cost { get; set; }
        }
    }
}
