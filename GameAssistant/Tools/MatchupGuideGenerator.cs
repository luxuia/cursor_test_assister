using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace GameAssistant.Tools
{
    /// <summary>
    /// 遍历所有英雄生成对位攻略空模板，便于批量填充或爬取。
    /// 输出 HeroMatchupGuides_FullTemplate.json（仅结构，出装/技能/要点为空）。
    /// </summary>
    public static class MatchupGuideGenerator
    {
        public static void GenerateFullTemplate(string heroesJsonPath = "Data/Dota2Heroes_FromWeb.json", string outputPath = "Data/HeroMatchupGuides_FullTemplate.json")
        {
            if (!File.Exists(heroesJsonPath))
            {
                Console.WriteLine($"未找到英雄列表: {heroesJsonPath}");
                return;
            }

            var json = File.ReadAllText(heroesJsonPath);
            var data = JsonConvert.DeserializeObject<HeroListWrapper>(json);
            var heroes = data?.Heroes ?? new List<HeroEntry>();
            if (heroes.Count == 0)
            {
                Console.WriteLine("英雄列表为空");
                return;
            }

            var matchups = new List<HeroMatchupEntry>();
            foreach (var our in heroes)
            {
                foreach (var vs in heroes)
                {
                    if (string.Equals(our.Id, vs.Id, StringComparison.OrdinalIgnoreCase))
                        continue;
                    matchups.Add(new HeroMatchupEntry
                    {
                        OurHeroId = our.Id,
                        OurHeroNameCn = our.NameCn ?? our.Name ?? our.Id,
                        VersusHeroId = vs.Id,
                        VersusHeroNameCn = vs.NameCn ?? vs.Name ?? vs.Id,
                        ItemBuild = "",
                        SkillBuild = "",
                        Tips = ""
                    });
                }
            }

            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var output = new HeroMatchupGuidesData
            {
                Matchups = matchups
            };
            var outJson = JsonConvert.SerializeObject(new { description = "全英雄对位空模板，共 " + matchups.Count + " 对；填充 itemBuild/skillBuild/tips 后可将需要的条目合并到 HeroMatchupGuides.json", matchups = matchups }, Formatting.Indented);
            File.WriteAllText(outputPath, outJson);
            Console.WriteLine($"已生成 {matchups.Count} 条对位空模板: {outputPath}");
        }

        private class HeroListWrapper
        {
            [JsonProperty("heroes")]
            public List<HeroEntry>? Heroes { get; set; }
        }

        private class HeroEntry
        {
            [JsonProperty("id")]
            public string Id { get; set; } = "";

            [JsonProperty("name")]
            public string Name { get; set; } = "";

            [JsonProperty("nameCn")]
            public string? NameCn { get; set; }
        }

        private class HeroMatchupGuidesData
        {
            public List<HeroMatchupEntry>? Matchups { get; set; }
        }

        private class HeroMatchupEntry
        {
            [JsonProperty("ourHeroId")]
            public string OurHeroId { get; set; } = "";

            [JsonProperty("ourHeroNameCn")]
            public string OurHeroNameCn { get; set; } = "";

            [JsonProperty("versusHeroId")]
            public string VersusHeroId { get; set; } = "";

            [JsonProperty("versusHeroNameCn")]
            public string VersusHeroNameCn { get; set; } = "";

            [JsonProperty("itemBuild")]
            public string ItemBuild { get; set; } = "";

            [JsonProperty("skillBuild")]
            public string SkillBuild { get; set; } = "";

            [JsonProperty("tips")]
            public string Tips { get; set; } = "";
        }
    }
}
