using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace GameAssistant.Tools
{
    /// <summary>
    /// 遍历所有英雄，生成或补全 HeroGuides.json（每英雄一条：出装/技能/玩法/克制/克制列表/被克制列表）。
    /// 已有条目的英雄保留，缺失英雄用空字符串补全，保证与 Dota2Heroes_FromWeb 数量一致。
    /// </summary>
    public static class HeroGuideGenerator
    {
        public static void MergeAllHeroes(string heroesPath = "Data/Dota2Heroes_FromWeb.json", string guidesPath = "Data/HeroGuides.json")
        {
            if (!File.Exists(heroesPath))
            {
                Console.WriteLine($"未找到: {heroesPath}");
                return;
            }

            var heroesJson = File.ReadAllText(heroesPath);
            var heroesData = JsonConvert.DeserializeObject<HeroesRoot>(heroesJson);
            var heroes = heroesData?.Heroes ?? new List<HeroRef>();

            Dictionary<string, HeroGuideEntry> guideMap = new Dictionary<string, HeroGuideEntry>(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(guidesPath))
            {
                var guidesJson = File.ReadAllText(guidesPath);
                var guidesData = JsonConvert.DeserializeObject<HeroGuidesRoot>(guidesJson);
                foreach (var g in guidesData?.Guides ?? Array.Empty<HeroGuideEntry>())
                    guideMap[g.HeroId] = g;
            }

            var merged = new List<HeroGuideEntry>();
            foreach (var h in heroes)
            {
                if (guideMap.TryGetValue(h.Id, out var existing))
                {
                    merged.Add(existing);
                    continue;
                }
                merged.Add(new HeroGuideEntry
                {
                    HeroId = h.Id,
                    HeroNameCn = h.NameCn ?? h.Name ?? h.Id,
                    ItemAdvice = "",
                    ItemStarting = "",
                    ItemEarly = "",
                    ItemMid = "",
                    ItemLate = "",
                    SkillAdvice = "",
                    SkillReleaseOrder = "",
                    SkillTips = "",
                    PlayAdvice = "",
                    CounterAdvice = "",
                    CountersList = new List<string>(),
                    CounteredByList = new List<string>()
                });
            }

            var dir = Path.GetDirectoryName(guidesPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var output = new HeroGuidesRoot { Description = "每英雄一条，heroId 与 Dota2Heroes_FromWeb 一致。", Guides = merged };
            File.WriteAllText(guidesPath, JsonConvert.SerializeObject(output, Formatting.Indented));
            Console.WriteLine($"已合并 {merged.Count} 条英雄攻略: {guidesPath}");
        }

        private class HeroesRoot
        {
            [JsonProperty("heroes")]
            public List<HeroRef>? Heroes { get; set; }
        }

        private class HeroRef
        {
            [JsonProperty("id")]
            public string Id { get; set; } = "";

            [JsonProperty("name")]
            public string Name { get; set; } = "";

            [JsonProperty("nameCn")]
            public string? NameCn { get; set; }
        }

        private class HeroGuidesRoot
        {
            [JsonProperty("description")]
            public string? Description { get; set; }

            [JsonProperty("guides")]
            public List<HeroGuideEntry> Guides { get; set; } = new List<HeroGuideEntry>();
        }

        private class HeroGuideEntry
        {
            [JsonProperty("heroId")]
            public string HeroId { get; set; } = "";

            [JsonProperty("heroNameCn")]
            public string HeroNameCn { get; set; } = "";

            [JsonProperty("itemAdvice")]
            public string ItemAdvice { get; set; } = "";

            [JsonProperty("itemStarting")]
            public string? ItemStarting { get; set; }

            [JsonProperty("itemEarly")]
            public string? ItemEarly { get; set; }

            [JsonProperty("itemMid")]
            public string? ItemMid { get; set; }

            [JsonProperty("itemLate")]
            public string? ItemLate { get; set; }

            [JsonProperty("skillAdvice")]
            public string SkillAdvice { get; set; } = "";

            [JsonProperty("skillReleaseOrder")]
            public string? SkillReleaseOrder { get; set; }

            [JsonProperty("skillTips")]
            public string? SkillTips { get; set; }

            [JsonProperty("playAdvice")]
            public string PlayAdvice { get; set; } = "";

            [JsonProperty("counterAdvice")]
            public string CounterAdvice { get; set; } = "";

            [JsonProperty("countersList")]
            public List<string>? CountersList { get; set; }

            [JsonProperty("counteredByList")]
            public List<string>? CounteredByList { get; set; }
        }
    }
}
