using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using AngleSharp.Html.Parser;
using AngleSharp.Dom;

namespace GameAssistant.Tools
{
    /// <summary>
    /// Liquipedia DOTA 2 数据抓取器
    /// 从 Liquipedia 抓取英雄、物品、技能数据和图标
    /// </summary>
    public class LiquipediaScraper
    {
        private readonly HttpClient _httpClient;
        private readonly HtmlParser _htmlParser;

        private const string LiquipediaBaseUrl = "https://liquipedia.net";
        /// <summary> 并发下载数，避免单线程过慢且不过度请求服务器 </summary>
        private const int MaxConcurrentDownloads = 12;

        static LiquipediaScraper()
        {
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
            {
                return true;
            };
        }

        public LiquipediaScraper()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    return true;
                }
            };

            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "GameAssistant/1.0 (https://github.com/luxuia/cursor_test_assister; Dota2 data)");
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
            _htmlParser = new HtmlParser();
        }

        /// <summary>
        /// 从 https://liquipedia.net/dota2/Portal:Heroes 获取完整英雄列表（128）
        /// 按页面专用结构解析：div.heroes-panel__hero-card 内为头像 img(_icon_dota2_gameasset.png) + 标题 a。
        /// </summary>
        public async Task<List<HeroInfo>> FetchHeroesAsync(IProgress<string>? progress = null)
        {
            progress?.Report("正在从 Portal:Heroes 获取英雄列表...");
            var heroes = new List<HeroInfo>();
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                string url = $"{LiquipediaBaseUrl}/dota2/index.php?title=Portal:Heroes";
                string html = await _httpClient.GetStringAsync(url);
                var document = await _htmlParser.ParseDocumentAsync(html);

                // 页面结构：div.heroes-panel > div.heroes-panel__category (category--strength/agility/intelligence/universal) > ul > li > div.heroes-panel__hero-card
                // 每个 hero-card 内：img[src*="_icon_dota2_gameasset.png"] + div.heroes-panel__hero-card__title > a[href*="/dota2/"]
                var heroCards = document.Body?.QuerySelectorAll("div.heroes-panel__hero-card");
                if (heroCards == null) return heroes;

                foreach (var card in heroCards)
                {
                    // 排除标题子块（class 含 hero-card__title 的是标题 div，不是卡片本身）
                    if (card.ClassName?.Contains("hero-card__title", StringComparison.OrdinalIgnoreCase) == true)
                        continue;

                    var img = card.QuerySelector("img[src*='_icon_dota2_gameasset.png']");
                    var titleLink = card.QuerySelector("div.heroes-panel__hero-card__title a[href*='/dota2/']");
                    if (img == null || titleLink == null) continue;

                    var name = titleLink.TextContent.Trim();
                    if (string.IsNullOrEmpty(name) || name.Length > 50) continue;

                    var href = titleLink.GetAttribute("href") ?? "";
                    if (href.Contains("Portal:", StringComparison.OrdinalIgnoreCase) ||
                        href.Contains("Category:", StringComparison.OrdinalIgnoreCase) ||
                        href.Contains("File:", StringComparison.OrdinalIgnoreCase) ||
                        href.Contains("Primary_attribute", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var id = HeroNameToId(name);
                    if (seenIds.Contains(id)) continue;
                    seenIds.Add(id);

                    string? currentAttribute = null;
                    for (var p = card.ParentElement; p != null; p = p.ParentElement)
                    {
                        var cls = p.ClassName ?? "";
                        if (cls.Contains("category--strength", StringComparison.OrdinalIgnoreCase)) { currentAttribute = "Strength"; break; }
                        if (cls.Contains("category--agility", StringComparison.OrdinalIgnoreCase)) { currentAttribute = "Agility"; break; }
                        if (cls.Contains("category--intelligence", StringComparison.OrdinalIgnoreCase)) { currentAttribute = "Intelligence"; break; }
                        if (cls.Contains("category--universal", StringComparison.OrdinalIgnoreCase)) { currentAttribute = "Universal"; break; }
                    }

                    var iconUrl = img.GetAttribute("src");
                    if (string.IsNullOrEmpty(iconUrl))
                    {
                        var nameForUrl = name.Replace(" ", "_").Replace("'", "%27");
                        iconUrl = $"{LiquipediaBaseUrl}/commons/images/thumb/0/0b/{nameForUrl}_icon.png/64px-{nameForUrl}_icon.png";
                    }
                    else
                    {
                        if (iconUrl.StartsWith("//")) iconUrl = "https:" + iconUrl;
                        else if (iconUrl.StartsWith("/")) iconUrl = LiquipediaBaseUrl + iconUrl;
                    }

                    heroes.Add(new HeroInfo
                    {
                        Id = id,
                        Name = name,
                        NameCn = "",
                        Attribute = currentAttribute ?? "Unknown",
                        IconUrl = iconUrl
                    });
                }

                progress?.Report($"成功获取 {heroes.Count} 个英雄");
                return heroes;
            }
            catch (Exception ex)
            {
                progress?.Report($"获取英雄列表失败: {ex.Message}");
                return heroes;
            }
        }

        private static string HeroNameToId(string name)
        {
            return name.ToLowerInvariant()
                .Replace(" ", "_")
                .Replace("'", "")
                .Replace("-", "_");
        }

        private static bool ContainsDigit(string s)
        {
            foreach (char c in s) if (char.IsDigit(c)) return true;
            return false;
        }

        /// <summary>
        /// 过滤赛事、导航等非英雄链接（如 CCT S2、PGL Wallachia、ESL One、DreamLeague、[edit] 等）
        /// </summary>
        private static bool IsTournamentOrNonHero(string name)
        {
            string n = name.ToLowerInvariant();
            string[] keywords = new[]
            {
                "series", "championship", "slam", " oq", "qualifier", "pro", "season", "world ",
                "cct ", "pgl ", "esl ", "blast ", "epl ", "dreamleague", "lunar snake",
                "wallachia", "birmingham", "elsc", "trending", "tournaments", "upcoming", "ongoing", "completed",
                "send ", "chat ", "contact", "about", "search", "scroll", "portal", "talk", "history"
            };
            foreach (var kw in keywords)
                if (n.Contains(kw)) return true;
            return false;
        }

        /// <summary>
        /// 从 Liquipedia Dota2 物品页获取物品列表（Portal:Items 或 /dota2/Items）
        /// </summary>
        public async Task<List<ItemInfo>> FetchItemsAsync(IProgress<string>? progress = null)
        {
            progress?.Report("正在从网页获取物品列表...");
            var items = new List<ItemInfo>();

            try
            {
                string url = $"{LiquipediaBaseUrl}/dota2/index.php?title=Portal:Items";
                string html;
                try
                {
                    html = await _httpClient.GetStringAsync(url);
                }
                catch
                {
                    html = await _httpClient.GetStringAsync($"{LiquipediaBaseUrl}/dota2/Items");
                }
                var document = await _htmlParser.ParseDocumentAsync(html);

                var itemImages = document.QuerySelectorAll("img[src*='itemicon_dota2_gameasset']");

                foreach (var image in itemImages)
                {
                    var iconUrl = image.GetAttribute("src");
                    if (string.IsNullOrEmpty(iconUrl)) continue;

                    var match = Regex.Match(iconUrl, @"([^/]+)_itemicon_dota2_gameasset\.png");
                    if (!match.Success) continue;

                    var nameWithEncoding = match.Groups[1].Value;
                    var name = HttpUtility.UrlDecode(nameWithEncoding).Replace("_", " ");

                    if (iconUrl.StartsWith("//"))
                    {
                        iconUrl = "https:" + iconUrl;
                    }
                    else if (iconUrl.StartsWith("/"))
                    {
                        iconUrl = LiquipediaBaseUrl + iconUrl;
                    }

                    int cost = 0;
                    var altText = image.GetAttribute("alt");
                    if (!string.IsNullOrEmpty(altText))
                    {
                        var costMatch = Regex.Match(altText, @"\((\d+)\)");
                        if (costMatch.Success)
                        {
                            int.TryParse(costMatch.Groups[1].Value, out cost);
                        }
                    }

                    items.Add(new ItemInfo
                    {
                        Id = name.ToLower().Replace(" ", "_").Replace("'", "").Replace("-", "_").Replace(".", ""),
                        Name = name,
                        NameCn = "",
                        Cost = cost,
                        IconUrl = iconUrl
                    });
                }

                var unique = new Dictionary<string, ItemInfo>();
                foreach (var item in items)
                {
                    if (!unique.ContainsKey(item.Id))
                    {
                        unique[item.Id] = item;
                    }
                }

                progress?.Report($"成功获取 {unique.Count} 个物品");
                return new List<ItemInfo>(unique.Values);
            }
            catch (Exception ex)
            {
                progress?.Report($"获取物品列表失败: {ex.Message}");
                return items;
            }
        }

        /// <summary>
        /// 从 Liquipedia Dota2 技能页获取技能列表（Portal:Abilities 或 /dota2/Abilities）
        /// </summary>
        public async Task<List<AbilityInfo>> FetchAbilitiesAsync(IProgress<string>? progress = null)
        {
            progress?.Report("正在从网页获取技能列表...");
            var abilities = new List<AbilityInfo>();

            try
            {
                string url = $"{LiquipediaBaseUrl}/dota2/index.php?title=Portal:Abilities";
                string html;
                try
                {
                    html = await _httpClient.GetStringAsync(url);
                }
                catch
                {
                    html = await _httpClient.GetStringAsync($"{LiquipediaBaseUrl}/dota2/Abilities");
                }
                var document = await _htmlParser.ParseDocumentAsync(html);

                var abilityImages = document.QuerySelectorAll("img[src*='abilityicon_dota2_gameasset']");

                foreach (var image in abilityImages)
                {
                    var iconUrl = image.GetAttribute("src");
                    if (string.IsNullOrEmpty(iconUrl)) continue;

                    var match = Regex.Match(iconUrl, @"([^/]+)_abilityicon_dota2_gameasset\.png");
                    if (!match.Success) continue;

                    var name = match.Groups[1].Value.Replace("_", " ").Trim();

                    if (iconUrl.StartsWith("//"))
                    {
                        iconUrl = "https:" + iconUrl;
                    }
                    else if (iconUrl.StartsWith("/"))
                    {
                        iconUrl = LiquipediaBaseUrl + iconUrl;
                    }

                    abilities.Add(new AbilityInfo
                    {
                        Id = name.ToLower().Replace(" ", "_").Replace("'", "").Replace("-", "_"),
                        Name = name,
                        NameCn = "",
                        IconUrl = iconUrl
                    });
                }

                var unique = new Dictionary<string, AbilityInfo>();
                foreach (var ability in abilities)
                {
                    if (!unique.ContainsKey(ability.Id))
                    {
                        unique[ability.Id] = ability;
                    }
                }

                progress?.Report($"成功获取 {unique.Count} 个技能");
                return new List<AbilityInfo>(unique.Values);
            }
            catch (Exception ex)
            {
                progress?.Report($"获取技能列表失败: {ex.Message}");
                return abilities;
            }
        }

        public async Task<int> DownloadHeroIconsAsync(List<HeroInfo> heroes, string outputDirectory, IProgress<string>? progress = null)
        {
            Directory.CreateDirectory(outputDirectory);
            int total = heroes.Count;
            int successCount = 0;
            var semaphore = new SemaphoreSlim(MaxConcurrentDownloads);

            async Task DownloadOne(HeroInfo hero)
            {
                await semaphore.WaitAsync();
                try
                {
                    await DownloadImageAsync(hero.IconUrl ?? "", Path.Combine(outputDirectory, $"{hero.Id}.png"));
                    int n = Interlocked.Increment(ref successCount);
                    progress?.Report($"下载英雄: {hero.Name} ({n}/{total})");
                }
                catch (Exception ex)
                {
                    progress?.Report($"下载失败 {hero.Name}: {ex.Message}");
                }
                finally { semaphore.Release(); }
            }

            await Task.WhenAll(heroes.Select(DownloadOne));
            progress?.Report($"英雄图标下载完成: {successCount}/{total}");
            return successCount;
        }

        public async Task<int> DownloadItemIconsAsync(List<ItemInfo> items, string outputDirectory, IProgress<string>? progress = null)
        {
            Directory.CreateDirectory(outputDirectory);
            int total = items.Count;
            int successCount = 0;
            var semaphore = new SemaphoreSlim(MaxConcurrentDownloads);

            async Task DownloadOne(ItemInfo item)
            {
                await semaphore.WaitAsync();
                try
                {
                    await DownloadImageAsync(item.IconUrl ?? "", Path.Combine(outputDirectory, $"{item.Id}.png"));
                    int n = Interlocked.Increment(ref successCount);
                    progress?.Report($"下载物品: {item.Name} ({n}/{total})");
                }
                catch (Exception ex)
                {
                    progress?.Report($"下载失败 {item.Name}: {ex.Message}");
                }
                finally { semaphore.Release(); }
            }

            await Task.WhenAll(items.Select(DownloadOne));
            progress?.Report($"物品图标下载完成: {successCount}/{total}");
            return successCount;
        }

        public async Task<int> DownloadAbilityIconsAsync(List<AbilityInfo> abilities, string outputDirectory, IProgress<string>? progress = null)
        {
            Directory.CreateDirectory(outputDirectory);
            int total = abilities.Count;
            int successCount = 0;
            var semaphore = new SemaphoreSlim(MaxConcurrentDownloads);

            async Task DownloadOne(AbilityInfo ability)
            {
                await semaphore.WaitAsync();
                try
                {
                    await DownloadImageAsync(ability.IconUrl ?? "", Path.Combine(outputDirectory, $"{ability.Id}.png"));
                    int n = Interlocked.Increment(ref successCount);
                    progress?.Report($"下载技能: {ability.Name} ({n}/{total})");
                }
                catch (Exception ex)
                {
                    progress?.Report($"下载失败 {ability.Name}: {ex.Message}");
                }
                finally { semaphore.Release(); }
            }

            await Task.WhenAll(abilities.Select(DownloadOne));
            progress?.Report($"技能图标下载完成: {successCount}/{total}");
            return successCount;
        }

        private async Task DownloadImageAsync(string url, string outputPath)
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var imageData = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(outputPath, imageData);
        }
    }

    public class AbilityInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string NameCn { get; set; } = string.Empty;
        public string? IconUrl { get; set; }
    }
}
