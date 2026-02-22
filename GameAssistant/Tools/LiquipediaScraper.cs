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

        /// <summary>
        /// 将 Liquipedia/Wikimedia 缩略图 URL 转为原图 URL（页面常用 21px/60px 缩略图，分辨率很低）。
        /// 例：.../thumb/3/3d/File.png/21px-File.png → .../3/3d/File.png
        /// </summary>
        private static string ToFullSizeImageUrl(string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url)) return url ?? "";
                if (url.IndexOf("/thumb/", StringComparison.OrdinalIgnoreCase) < 0) return url;
                url = url.Replace("/thumb/", "/");
                int lastSlash = url.LastIndexOf('/');
                if (lastSlash > 0 && lastSlash < url.Length)
                {
                    string tail = url.Substring(lastSlash);
                    if (Regex.IsMatch(tail, @"\/\d+px-", RegexOptions.IgnoreCase))
                        return url.Substring(0, lastSlash);
                }
                return url;
            }
            catch { return url ?? ""; }
        }

        /// <summary> 生成可用于文件名的安全字符串，避免非法字符导致崩溃 </summary>
        private static string SafeFileName(string id)
        {
            if (string.IsNullOrEmpty(id)) return "unknown";
            char[] invalid = Path.GetInvalidFileNameChars();
            var sb = new System.Text.StringBuilder(id.Length);
            foreach (char c in id)
                sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
            return sb.Length > 0 ? sb.ToString() : "unknown";
        }

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
        /// 技能列表仅从各英雄页（/dota2/英雄名）抓取：解析英雄链接后逐页取 img[src*='abilityicon_dota2_gameasset']，同 id 不重复，缩略图 URL 转原图。
        /// </summary>
        public async Task<List<AbilityInfo>> FetchAbilitiesAsync(IProgress<string>? progress = null)
        {
            progress?.Report("正在解析英雄列表...");
            var unique = new Dictionary<string, AbilityInfo>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var heroPaths = await GetHeroWikiPathsAsync(progress);
                if (heroPaths.Count == 0)
                {
                    progress?.Report("未获取到英雄页，无法拉取技能");
                    return new List<AbilityInfo>();
                }
                progress?.Report($"从英雄页获取技能 (0/{heroPaths.Count})…");
                var semaphore = new SemaphoreSlim(2);
                var dictLock = new object();
                int doneCount = 0;
                int totalHeroes = heroPaths.Count;
                await Task.Delay(800);
                var tasks = heroPaths.Select(async path =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        await Task.Delay(450);
                        string? heroHtml = await FetchStringWithRetryAsync(LiquipediaBaseUrl + path, progress);
                        if (string.IsNullOrEmpty(heroHtml)) return;
                        var heroDoc = await _htmlParser.ParseDocumentAsync(heroHtml);
                        var imgs = heroDoc.QuerySelectorAll("img[src*='abilityicon_dota2_gameasset']");
                        foreach (var img in imgs)
                            TryAddAbilityFromImg(img, unique, dictLock);
                    }
                    catch { /* 单英雄页失败忽略 */ }
                    finally
                    {
                        semaphore.Release();
                        int n = Interlocked.Increment(ref doneCount);
                        if (n % 8 == 0 || n == totalHeroes)
                            progress?.Report($"从英雄页获取技能 ({n}/{totalHeroes})…");
                    }
                });
                await Task.WhenAll(tasks);
                progress?.Report($"成功获取 {unique.Count} 个技能");
                return new List<AbilityInfo>(unique.Values);
            }
            catch (Exception ex)
            {
                progress?.Report($"获取技能列表失败: {ex.Message}");
                return new List<AbilityInfo>(unique.Values);
            }
        }

        /// <summary> 从单个 img 元素解析技能并加入字典（若 id 已存在则不覆盖）。lockObj 为多线程时传入用于加锁。返回是否新增。 </summary>
        private static bool TryAddAbilityFromImg(AngleSharp.Dom.IElement image, Dictionary<string, AbilityInfo> unique, object? lockObj)
        {
            var iconUrl = image.GetAttribute("src");
            if (string.IsNullOrEmpty(iconUrl)) return false;
            var match = Regex.Match(iconUrl, @"([^/]+)_abilityicon_dota2_gameasset\.png");
            if (!match.Success) return false;
            var name = match.Groups[1].Value.Replace("_", " ").Trim();
            if (iconUrl.StartsWith("//")) iconUrl = "https:" + iconUrl;
            else if (iconUrl.StartsWith("/")) iconUrl = LiquipediaBaseUrl + iconUrl;
            iconUrl = ToFullSizeImageUrl(iconUrl);
            var id = name.ToLower().Replace(" ", "_").Replace("'", "").Replace("-", "_");
            var info = new AbilityInfo { Id = id, Name = name, NameCn = "", IconUrl = iconUrl };
            bool added = false;
            void Add()
            {
                if (!unique.ContainsKey(id)) { unique[id] = info; added = true; }
            }
            if (lockObj != null) lock (lockObj) Add(); else Add();
            return added;
        }

        /// <summary> 请求 URL 获取 HTML，遇 429/5xx 时等待后重试，避免被限流导致 0 条技能 </summary>
        private async Task<string?> FetchStringWithRetryAsync(string url, IProgress<string>? progress, int maxRetries = 2)
        {
            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    using var response = await _httpClient.GetAsync(url);
                    if ((int)response.StatusCode == 429 || (int)response.StatusCode >= 500)
                    {
                        if (attempt < maxRetries)
                        {
                            int waitMs = 5000 + attempt * 3000;
                            progress?.Report($"请求被限流或服务繁忙，{waitMs / 1000} 秒后重试…");
                            await Task.Delay(waitMs);
                            continue;
                        }
                        return null;
                    }
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync();
                }
                catch (HttpRequestException)
                {
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(4000 + attempt * 2000);
                        continue;
                    }
                    return null;
                }
            }
            return null;
        }

        /// <summary> 从 Portal:Heroes 解析出各英雄的 wiki 路径，如 /dota2/Axe </summary>
        private async Task<List<string>> GetHeroWikiPathsAsync(IProgress<string>? progress)
        {
            var paths = new List<string>();
            try
            {
                string html = await _httpClient.GetStringAsync($"{LiquipediaBaseUrl}/dota2/index.php?title=Portal:Heroes");
                var document = await _htmlParser.ParseDocumentAsync(html);
                var links = document.QuerySelectorAll("div.heroes-panel__hero-card__title a[href*='/dota2/']");
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var a in links)
                {
                    var href = a.GetAttribute("href");
                    if (string.IsNullOrEmpty(href) || href.Contains("Portal:", StringComparison.OrdinalIgnoreCase) ||
                        href.Contains("Category:", StringComparison.OrdinalIgnoreCase) || href.Contains("File:", StringComparison.OrdinalIgnoreCase))
                        continue;
                    href = href.TrimStart('/');
                    if (!href.StartsWith("dota2/", StringComparison.OrdinalIgnoreCase)) continue;
                    string path = "/" + href;
                    if (path.Contains("?")) path = path.Substring(0, path.IndexOf('?'));
                    if (seen.Add(path)) paths.Add(path);
                }
                progress?.Report($"已解析 {paths.Count} 个英雄页用于补充技能");
            }
            catch (Exception ex)
            {
                progress?.Report($"解析英雄列表失败(仅用 Portal 技能): {ex.Message}");
            }
            return paths;
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
                    await DownloadImageAsync(ToFullSizeImageUrl(hero.IconUrl ?? ""), Path.Combine(outputDirectory, $"{hero.Id}.png"));
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
                    await DownloadImageAsync(ToFullSizeImageUrl(item.IconUrl ?? ""), Path.Combine(outputDirectory, $"{item.Id}.png"));
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
            if (abilities == null || abilities.Count == 0)
            {
                progress?.Report("没有可下载的技能");
                return 0;
            }
            try
            {
                Directory.CreateDirectory(outputDirectory);
            }
            catch (Exception ex)
            {
                progress?.Report($"无法创建目录: {ex.Message}");
                return 0;
            }
            int total = abilities.Count;
            int successCount = 0;
            var semaphore = new SemaphoreSlim(MaxConcurrentDownloads);

            async Task DownloadOne(AbilityInfo ability)
            {
                if (ability == null) return;
                await semaphore.WaitAsync();
                try
                {
                    string url = ToFullSizeImageUrl(ability.IconUrl ?? "");
                    if (string.IsNullOrWhiteSpace(url))
                    {
                        progress?.Report($"跳过(无链接): {ability.Name}");
                        return;
                    }
                    string safeName = SafeFileName(ability.Id);
                    string outputPath = Path.Combine(outputDirectory, $"{safeName}.png");
                    await DownloadImageAsync(url, outputPath);
                    int n = Interlocked.Increment(ref successCount);
                    progress?.Report($"下载技能: {ability.Name} ({n}/{total})");
                }
                catch (Exception ex)
                {
                    progress?.Report($"下载失败 {ability.Name}: {ex.Message}");
                }
                finally { semaphore.Release(); }
            }

            await Task.WhenAll(abilities.Where(a => a != null).Select(DownloadOne));
            progress?.Report($"技能图标下载完成: {successCount}/{total}");
            return successCount;
        }

        private async Task DownloadImageAsync(string url, string outputPath)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("图片地址为空", nameof(url));
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            byte[]? imageData = await response.Content.ReadAsByteArrayAsync();
            if (imageData == null || imageData.Length == 0)
                throw new InvalidOperationException("响应内容为空");
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
