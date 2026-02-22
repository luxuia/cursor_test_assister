using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GameAssistant.Tools
{
    /// <summary>
    /// Liquipedia 数据生成器
    /// 从 Liquipedia 抓取数据并下载图标
    /// </summary>
    public class LiquipediaDataGenerator
    {
        private const string DefaultDataDirectory = "Data";
        private const string DefaultTemplatesDirectory = "Templates";
        private const string HeroesFile = "Dota2Heroes.json";
        private const string ItemsFile = "Dota2Items.json";
        private const string AbilitiesFile = "Dota2Abilities.json";

        private readonly LiquipediaScraper _scraper;

        public LiquipediaDataGenerator()
        {
            _scraper = new LiquipediaScraper();
        }

        /// <summary>
        /// 生成完整数据
        /// </summary>
        public async Task GenerateAllData(string? outputDir = null, IProgress<string>? progress = null)
        {
            var outputPath = outputDir ?? DefaultDataDirectory;
            Directory.CreateDirectory(outputPath);

            progress?.Report("=== 开始生成 DOTA 2 数据 ===");

            // 生成英雄数据
            await GenerateHeroData(outputPath, progress);

            // 生成物品数据
            await GenerateItemData(outputPath, progress);

            // 生成技能数据
            await GenerateAbilityData(outputPath, progress);

            progress?.Report("=== 所有数据生成完成 ===");
        }

        /// <summary>
        /// 生成英雄数据
        /// </summary>
        public async Task GenerateHeroData(string outputDir, IProgress<string>? progress = null)
        {
            progress?.Report("=== 生成英雄数据 ===");

            // 抓取英雄列表
            var heroes = await _scraper.FetchHeroesAsync(progress);
            if (heroes.Count == 0)
            {
                progress?.Report("没有获取到英雄数据");
                return;
            }

            // 保存英雄数据JSON
            var heroData = new HeroData
            {
                Game = "Dota2",
                Version = DateTime.Now.ToString("yyyy.MM.dd"),
                TotalHeroes = heroes.Count,
                Heroes = heroes
            };

            string heroesPath = Path.Combine(outputDir, HeroesFile);
            string heroJson = JsonConvert.SerializeObject(heroData, Formatting.Indented);
            await File.WriteAllTextAsync(heroesPath, heroJson, Encoding.UTF8);
            progress?.Report($"英雄数据已保存: {heroesPath}");

            // 下载图标
            string templatesDir = Path.Combine(DefaultTemplatesDirectory, "Heroes");
            await _scraper.DownloadHeroIconsAsync(heroes, templatesDir, progress);
            progress?.Report($"英雄图标已保存到: {templatesDir}");
        }

        /// <summary>
        /// 生成物品数据
        /// </summary>
        public async Task GenerateItemData(string outputDir, IProgress<string>? progress = null)
        {
            progress?.Report("=== 生成物品数据 ===");

            // 抓取物品列表
            var items = await _scraper.FetchItemsAsync(progress);
            if (items.Count == 0)
            {
                progress?.Report("没有获取到物品数据");
                return;
            }

            // 保存物品数据JSON
            var itemData = new ItemData
            {
                Game = "Dota2",
                Version = DateTime.Now.ToString("yyyy.MM.dd"),
                TotalItems = items.Count,
                // 按类别组织（简化版本）
                Categories = new Dictionary<string, Dictionary<string, List<ItemInfo>>>
                {
                    ["All"] = new Dictionary<string, List<ItemInfo>>
                    {
                        ["Items"] = items
                    }
                }
            };

            string itemsPath = Path.Combine(outputDir, ItemsFile);
            string itemJson = JsonConvert.SerializeObject(itemData, Formatting.Indented);
            await File.WriteAllTextAsync(itemsPath, itemJson, Encoding.UTF8);
            progress?.Report($"物品数据已保存: {itemsPath}");

            // 下载图标
            string templatesDir = Path.Combine(DefaultTemplatesDirectory, "Items");
            await _scraper.DownloadItemIconsAsync(items, templatesDir, progress);
            progress?.Report($"物品图标已保存到: {templatesDir}");
        }

        /// <summary>
        /// 生成技能数据
        /// </summary>
        public async Task GenerateAbilityData(string outputDir, IProgress<string>? progress = null)
        {
            progress?.Report("=== 生成技能数据 ===");

            // 抓取技能列表
            var abilities = await _scraper.FetchAbilitiesAsync(progress);
            if (abilities.Count == 0)
            {
                progress?.Report("没有获取到技能数据");
                return;
            }

            // 保存技能数据JSON
            var abilityData = new AbilityData
            {
                Game = "Dota2",
                Version = DateTime.Now.ToString("yyyy.MM.dd"),
                TotalAbilities = abilities.Count,
                Abilities = abilities
            };

            string abilitiesPath = Path.Combine(outputDir, AbilitiesFile);
            string abilityJson = JsonConvert.SerializeObject(abilityData, Formatting.Indented);
            await File.WriteAllTextAsync(abilitiesPath, abilityJson, Encoding.UTF8);
            progress?.Report($"技能数据已保存: {abilitiesPath}");

            // 下载图标
            string templatesDir = Path.Combine(DefaultTemplatesDirectory, "Abilities");
            await _scraper.DownloadAbilityIconsAsync(abilities, templatesDir, progress);
            progress?.Report($"技能图标已保存到: {templatesDir}");
        }

        /// <summary>
        /// 只更新图标（不重新抓取数据）
        /// </summary>
        public async Task UpdateIcons(IProgress<string>? progress = null)
        {
            var dataDir = DefaultDataDirectory;

            // 更新英雄图标
            if (File.Exists(Path.Combine(dataDir, HeroesFile)))
            {
                progress?.Report("更新英雄图标...");
                var heroes = HeroIconDownloader.LoadHeroesFromJson(HeroesFile);
                var templatesDir = Path.Combine(DefaultTemplatesDirectory, "Heroes");
                await _scraper.DownloadHeroIconsAsync(heroes, templatesDir, progress);
            }

            // 更新物品图标
            if (File.Exists(Path.Combine(dataDir, ItemsFile)))
            {
                progress?.Report("更新物品图标...");
                var items = HeroIconDownloader.LoadItemsFromJson(ItemsFile);
                var templatesDir = Path.Combine(DefaultTemplatesDirectory, "Items");
                await _scraper.DownloadItemIconsAsync(items, templatesDir, progress);
            }

            // 更新技能图标
            if (File.Exists(Path.Combine(dataDir, AbilitiesFile)))
            {
                progress?.Report("更新技能图标...");
                string json = await File.ReadAllTextAsync(Path.Combine(dataDir, AbilitiesFile));
                var data = JsonConvert.DeserializeObject<AbilityData>(json);
                if (data != null)
                {
                    var templatesDir = Path.Combine(DefaultTemplatesDirectory, "Abilities");
                    await _scraper.DownloadAbilityIconsAsync(data.Abilities, templatesDir, progress);
                }
            }

            progress?.Report("图标更新完成");
        }
    }

    /// <summary>
    /// 技能数据
    /// </summary>
    public class AbilityData
    {
        public string Game { get; set; } = "Dota2";
        public string Version { get; set; } = string.Empty;
        public int TotalAbilities { get; set; }
        public List<AbilityInfo> Abilities { get; set; } = new List<AbilityInfo>();
    }
}
