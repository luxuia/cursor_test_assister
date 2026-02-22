using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GameAssistant.Tools
{
    /// <summary>
    /// 英雄和物品图标下载工具（并发下载，与技能一致）
    /// </summary>
    public class HeroIconDownloader
    {
        private const int MaxConcurrentDownloads = 12;

        private readonly HttpClient _httpClient;
        private readonly string _downloadDirectory;

        public HeroIconDownloader(string downloadDirectory = "Templates/Heroes")
        {
            var handler = new HttpClientHandler();
            try
            {
                handler.ServerCertificateCustomValidationCallback = (_, __, ___, ____) => true;
            }
            catch { }
            _httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "GameAssistant/1.0 (https://github.com/luxuia/cursor_test_assister; Dota2 icon download)");
            _downloadDirectory = downloadDirectory;
            
            if (!Directory.Exists(_downloadDirectory))
            {
                Directory.CreateDirectory(_downloadDirectory);
            }
        }

        /// <summary>
        /// 从Liquipedia下载DOTA 2英雄图标
        /// </summary>
        public async Task DownloadDota2HeroIconsAsync(List<HeroInfo> heroes, IProgress<string>? progress = null)
        {
            await DownloadIconsAsync(heroes.Select(h => new IconInfo 
            { 
                Id = h.Id, 
                Name = h.Name,
                IconUrl = h.IconUrl 
            }).ToList(), progress);
        }

        /// <summary>
        /// 从Liquipedia下载DOTA 2物品图标
        /// </summary>
        public async Task DownloadDota2ItemIconsAsync(List<ItemInfo> items, IProgress<string>? progress = null)
        {
            await DownloadIconsAsync(items.Select(i => new IconInfo 
            { 
                Id = i.Id, 
                Name = i.Name,
                IconUrl = i.IconUrl 
            }).ToList(), progress);
        }

        /// <summary>
        /// 通用图标下载方法（并发，最多 MaxConcurrentDownloads 路同时下载）
        /// </summary>
        private async Task DownloadIconsAsync(List<IconInfo> icons, IProgress<string>? progress = null)
        {
            int total = icons.Count;
            int successCount = 0;
            var semaphore = new SemaphoreSlim(MaxConcurrentDownloads);

            async Task DownloadOne(IconInfo icon)
            {
                await semaphore.WaitAsync().ConfigureAwait(false);
                try
                {
                    string[] iconUrls = icon.IconUrl != null
                        ? new[] { icon.IconUrl }
                        : BuildLiquipediaIconUrls(icon.Id, icon.Name);
                    foreach (var iconUrl in iconUrls)
                    {
                        try
                        {
                            await DownloadIconAsync(icon.Id, iconUrl).ConfigureAwait(false);
                            int n = Interlocked.Increment(ref successCount);
                            progress?.Report($"完成: {icon.Name} ({n}/{total})");
                            return;
                        }
                        catch
                        {
                            await Task.Delay(200).ConfigureAwait(false);
                        }
                    }
                    progress?.Report($"下载失败 {icon.Name}: 所有URL都失败");
                }
                catch (Exception ex)
                {
                    progress?.Report($"下载失败 {icon.Name}: {ex.Message}");
                }
                finally
                {
                    semaphore.Release();
                }
            }

            await Task.WhenAll(icons.Select(DownloadOne)).ConfigureAwait(false);
            progress?.Report($"下载完成！成功: {successCount}/{total}");
        }

        /// <summary>
        /// 下载单个图标
        /// </summary>
        private async Task DownloadIconAsync(string itemId, string iconUrl)
        {
            if (string.IsNullOrWhiteSpace(iconUrl))
                throw new Exception($"图标 URL 为空（{itemId}），可能数据来自 Steam API 无图标链接，请先从 Liquipedia 加载再下载图标");
            var response = await _httpClient.GetAsync(iconUrl).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new Exception($"HTTP {(int)response.StatusCode}: {iconUrl}");
            var imageData = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            if (imageData == null || imageData.Length == 0)
                throw new Exception("响应内容为空");
            string filePath = Path.Combine(_downloadDirectory, $"{itemId}.png");
            await File.WriteAllBytesAsync(filePath, imageData).ConfigureAwait(false);
        }

        /// <summary>
        /// 构建Liquipedia图标URL（尝试多个可能的 hash 路径）
        /// </summary>
        private string[] BuildLiquipediaIconUrls(string itemId, string itemName)
        {
            string formattedId = FormatHeroIdForUrl(itemId);
            string formattedName = itemName
                .Replace(" ", "_")
                .Replace("'", "%27")
                .Replace("-", "_");

            var urls = new List<string>();
            string[] hashPrefixes = { "0/0b", "8/8a", "9/9c", "8/8c", "2/2a", "2/2c", "4/4a", "4/4c", "1/1c", "3/3c", "7/7c", "5/5a" };
            foreach (var hash in hashPrefixes)
            {
                urls.Add($"https://liquipedia.net/commons/images/thumb/{hash}/{formattedId}_icon.png/64px-{formattedId}_icon.png");
                if (string.Equals(formattedId, formattedName, StringComparison.OrdinalIgnoreCase) == false)
                    urls.Add($"https://liquipedia.net/commons/images/thumb/{hash}/{formattedName}_icon.png/64px-{formattedName}_icon.png");
            }
            return urls.ToArray();
        }

        /// <summary>
        /// 格式化英雄ID为URL格式（保留下划线，如 Centaur_Warrunner）
        /// </summary>
        private static string FormatHeroIdForUrl(string heroId)
        {
            var parts = heroId.Split('_');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                    parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1);
            }
            return string.Join("_", parts);
        }

        /// <summary>
        /// 从JSON文件加载英雄列表（支持小写 key：game/heroes/iconUrl）
        /// </summary>
        public static List<HeroInfo> LoadHeroesFromJson(string jsonFilePath)
        {
            string actualPath = FindFile(jsonFilePath);
            string json = File.ReadAllText(actualPath);
            var data = JsonConvert.DeserializeObject<HeroData>(json);
            return data?.Heroes ?? new List<HeroInfo>();
        }

        /// <summary>
        /// 加载 DOTA2 完整英雄列表（128），并合并带 iconUrl 的条目以提升下载成功率
        /// </summary>
        public static List<HeroInfo> LoadDota2HeroesFull()
        {
            var full = LoadHeroesFromJson("Data/Dota2Heroes_Complete.json");
            try
            {
                var withUrls = LoadHeroesFromJson("Data/Dota2Heroes.json");
                var urlById = withUrls.Where(h => !string.IsNullOrEmpty(h.IconUrl)).ToDictionary(h => h.Id, h => h.IconUrl!);
                foreach (var h in full)
                {
                    if (urlById.TryGetValue(h.Id, out var url))
                        h.IconUrl = url;
                }
            }
            catch (FileNotFoundException) { /* 无带 URL 的补充文件则仅用完整列表 */ }
            return full;
        }

        /// <summary>
        /// 从JSON文件加载物品列表
        /// </summary>
        public static List<ItemInfo> LoadItemsFromJson(string jsonFilePath)
        {
            string actualPath = FindFile(jsonFilePath);
            string json = File.ReadAllText(actualPath);
            var data = JsonConvert.DeserializeObject<ItemData>(json);
            
            var items = new List<ItemInfo>();
            if (data?.Categories != null)
            {
                // 提取所有类别的物品
                foreach (var category in data.Categories.Values)
                {
                    foreach (var subCategory in category.Values)
                    {
                        items.AddRange(subCategory);
                    }
                }
            }
            
            return items;
        }

        private static string FindFile(string filePath)
        {
            string[] possiblePaths = {
                filePath,
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath),
                Path.Combine(Directory.GetCurrentDirectory(), filePath)
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            throw new FileNotFoundException($"文件不存在，尝试的路径: {string.Join(", ", possiblePaths)}");
        }
    }

    /// <summary>
    /// 英雄信息
    /// </summary>
    public class HeroInfo
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        [JsonProperty("nameCn")]
        public string NameCn { get; set; } = string.Empty;
        [JsonProperty("attribute")]
        public string Attribute { get; set; } = string.Empty;
        [JsonProperty("iconUrl")]
        public string? IconUrl { get; set; }
    }

    /// <summary>
    /// 英雄数据
    /// </summary>
    public class HeroData
    {
        [JsonProperty("game")]
        public string Game { get; set; } = string.Empty;
        [JsonProperty("version")]
        public string Version { get; set; } = string.Empty;
        [JsonProperty("totalHeroes")]
        public int TotalHeroes { get; set; }
        [JsonProperty("heroes")]
        public List<HeroInfo> Heroes { get; set; } = new List<HeroInfo>();
    }

    /// <summary>
    /// 物品信息
    /// </summary>
    public class ItemInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string NameCn { get; set; } = string.Empty;
        public int Cost { get; set; }
        public string? IconUrl { get; set; }
    }

    /// <summary>
    /// 物品数据
    /// </summary>
    public class ItemData
    {
        public string Game { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public int TotalItems { get; set; }
        public Dictionary<string, Dictionary<string, List<ItemInfo>>>? Categories { get; set; }
    }

    /// <summary>
    /// 图标信息（通用）
    /// </summary>
    internal class IconInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? IconUrl { get; set; }
    }
}
