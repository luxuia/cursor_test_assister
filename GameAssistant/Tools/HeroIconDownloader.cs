using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GameAssistant.Tools
{
    /// <summary>
    /// 英雄和物品图标下载工具
    /// </summary>
    public class HeroIconDownloader
    {
        private readonly HttpClient _httpClient;
        private readonly string _downloadDirectory;

        public HeroIconDownloader(string downloadDirectory = "Templates/Heroes")
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "GameAssistant/1.0");
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
        /// 通用图标下载方法
        /// </summary>
        private async Task DownloadIconsAsync(List<IconInfo> icons, IProgress<string>? progress = null)
        {
            int total = icons.Count;
            int completed = 0;

            foreach (var icon in icons)
            {
                try
                {
                    progress?.Report($"正在下载: {icon.Name} ({completed + 1}/{total})");
                    
                    // 尝试多个可能的URL
                    string[] iconUrls = icon.IconUrl != null 
                        ? new[] { icon.IconUrl } 
                        : BuildLiquipediaIconUrls(icon.Id, icon.Name);
                    
                    bool downloaded = false;
                    foreach (var iconUrl in iconUrls)
                    {
                        try
                        {
                            await DownloadIconAsync(icon.Id, iconUrl);
                            downloaded = true;
                            break;
                        }
                        catch
                        {
                            // 尝试下一个URL
                            continue;
                        }
                    }
                    
                    if (downloaded)
                    {
                        completed++;
                        progress?.Report($"完成: {icon.Name} ({completed}/{total})");
                    }
                    else
                    {
                        progress?.Report($"下载失败 {icon.Name}: 所有URL都失败");
                    }
                }
                catch (Exception ex)
                {
                    progress?.Report($"下载失败 {icon.Name}: {ex.Message}");
                }
            }

            progress?.Report($"下载完成！成功: {completed}/{total}");
        }

        /// <summary>
        /// 下载单个图标
        /// </summary>
        private async Task DownloadIconAsync(string itemId, string iconUrl)
        {
            try
            {
                var response = await _httpClient.GetAsync(iconUrl);
                response.EnsureSuccessStatusCode();

                var imageData = await response.Content.ReadAsByteArrayAsync();
                string filePath = Path.Combine(_downloadDirectory, $"{itemId}.png");
                
                await File.WriteAllBytesAsync(filePath, imageData);
            }
            catch (Exception ex)
            {
                throw new Exception($"下载图标失败 {itemId}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 构建Liquipedia图标URL（尝试多个可能的URL）
        /// </summary>
        private string[] BuildLiquipediaIconUrls(string itemId, string itemName)
        {
            // Liquipedia图标可能有不同的hash值，尝试多个可能的URL
            var urls = new List<string>();
            
            // 方法1：使用itemId
            string formattedId = FormatHeroIdForUrl(itemId);
            urls.Add($"https://liquipedia.net/commons/images/thumb/0/0b/{formattedId}_icon.png/64px-{formattedId}_icon.png");
            
            // 方法2：使用itemName（处理特殊字符）
            string formattedName = itemName
                .Replace(" ", "_")
                .Replace("'", "%27")
                .Replace("-", "_")
                .Replace("'s", "s");
            urls.Add($"https://liquipedia.net/commons/images/thumb/0/0b/{formattedName}_icon.png/64px-{formattedName}_icon.png");
            
            // 方法3：尝试不同的hash值
            string[] hashPrefixes = { "0/0b", "8/8a", "8/8c", "2/2a", "2/2c", "4/4a", "4/4c", "1/1c", "3/3c" };
            foreach (var hash in hashPrefixes)
            {
                urls.Add($"https://liquipedia.net/commons/images/thumb/{hash}/{formattedName}_icon.png/64px-{formattedName}_icon.png");
                urls.Add($"https://liquipedia.net/commons/images/thumb/{hash}/{formattedId}_icon.png/64px-{formattedId}_icon.png");
            }
            
            return urls.ToArray();
        }

        /// <summary>
        /// 格式化英雄ID为URL格式
        /// </summary>
        private string FormatHeroIdForUrl(string heroId)
        {
            // 将下划线替换，首字母大写
            var parts = heroId.Split('_');
            var formatted = "";
            foreach (var part in parts)
            {
                if (part.Length > 0)
                {
                    formatted += char.ToUpper(part[0]) + part.Substring(1);
                }
            }
            return formatted;
        }

        /// <summary>
        /// 从JSON文件加载英雄列表
        /// </summary>
        public static List<HeroInfo> LoadHeroesFromJson(string jsonFilePath)
        {
            string actualPath = FindFile(jsonFilePath);
            string json = File.ReadAllText(actualPath);
            var data = JsonConvert.DeserializeObject<HeroData>(json);
            
            return data?.Heroes ?? new List<HeroInfo>();
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
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string NameCn { get; set; } = string.Empty;
        public string Attribute { get; set; } = string.Empty;
        public string? IconUrl { get; set; }
    }

    /// <summary>
    /// 英雄数据
    /// </summary>
    public class HeroData
    {
        public string Game { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public int TotalHeroes { get; set; }
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
