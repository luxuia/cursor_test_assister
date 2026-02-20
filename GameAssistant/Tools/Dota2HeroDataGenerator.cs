using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace GameAssistant.Tools
{
    /// <summary>
    /// DOTA 2英雄数据生成器
    /// 用于生成完整的英雄数据JSON文件
    /// </summary>
    public static class Dota2HeroDataGenerator
    {
        /// <summary>
        /// 生成完整的DOTA 2英雄数据
        /// </summary>
        public static void GenerateCompleteHeroData(string outputPath = "Data/Dota2Heroes.json")
        {
            var heroes = new List<HeroInfo>();

            // Strength Heroes (36)
            AddHeroes(heroes, "Strength", new[]
            {
                ("alchemist", "Alchemist", "炼金术士"),
                ("axe", "Axe", "斧王"),
                ("bristleback", "Bristleback", "刚背兽"),
                ("centaur_warrunner", "Centaur Warrunner", "半人马战行者"),
                ("chaos_knight", "Chaos Knight", "混沌骑士"),
                ("clockwerk", "Clockwerk", "发条技师"),
                ("dawnbreaker", "Dawnbreaker", "破晓辰星"),
                ("doom", "Doom", "末日使者"),
                ("dragon_knight", "Dragon Knight", "龙骑士"),
                ("earth_spirit", "Earth Spirit", "大地之灵"),
                ("earthshaker", "Earthshaker", "撼地者"),
                ("elder_titan", "Elder Titan", "上古巨神"),
                ("huskar", "Huskar", "哈斯卡"),
                ("kunkka", "Kunkka", "昆卡"),
                ("largo", "Largo", "拉戈"),
                ("legion_commander", "Legion Commander", "军团指挥官"),
                ("lifestealer", "Lifestealer", "噬魂鬼"),
                ("lycan", "Lycan", "狼人"),
                ("mars", "Mars", "玛尔斯"),
                ("night_stalker", "Night Stalker", "暗夜魔王"),
                ("ogre_magi", "Ogre Magi", "食人魔魔法师"),
                ("omniknight", "Omniknight", "全能骑士"),
                ("phoenix", "Phoenix", "凤凰"),
                ("primal_beast", "Primal Beast", "原始野兽"),
                ("pudge", "Pudge", "帕吉"),
                ("slardar", "Slardar", "斯拉达"),
                ("spirit_breaker", "Spirit Breaker", "裂魂人"),
                ("sven", "Sven", "斯温"),
                ("tidehunter", "Tidehunter", "潮汐猎人"),
                ("timbersaw", "Timbersaw", "伐木机"),
                ("tiny", "Tiny", "小小"),
                ("treant_protector", "Treant Protector", "树精卫士"),
                ("tusk", "Tusk", "巨牙海民"),
                ("underlord", "Underlord", "孽主"),
                ("undying", "Undying", "不朽尸王"),
                ("wraith_king", "Wraith King", "冥魂大帝")
            });

            // Agility Heroes (35)
            AddHeroes(heroes, "Agility", new[]
            {
                ("anti_mage", "Anti-Mage", "敌法师"),
                ("bloodseeker", "Bloodseeker", "血魔"),
                ("bounty_hunter", "Bounty Hunter", "赏金猎人"),
                ("broodmother", "Broodmother", "育母蜘蛛"),
                ("clinkz", "Clinkz", "克林克兹"),
                ("drow_ranger", "Drow Ranger", "卓尔游侠"),
                ("ember_spirit", "Ember Spirit", "灰烬之灵"),
                ("faceless_void", "Faceless Void", "虚空假面"),
                ("gyrocopter", "Gyrocopter", "矮人直升机"),
                ("hoodwink", "Hoodwink", "森海飞霞"),
                ("juggernaut", "Juggernaut", "主宰"),
                ("kez", "Kez", "克兹"),
                ("lone_druid", "Lone Druid", "德鲁伊"),
                ("luna", "Luna", "露娜"),
                ("medusa", "Medusa", "美杜莎"),
                ("meepo", "Meepo", "米波"),
                ("mirana", "Mirana", "米拉娜"),
                ("monkey_king", "Monkey King", "齐天大圣"),
                ("morphling", "Morphling", "变体精灵"),
                ("naga_siren", "Naga Siren", "娜迦海妖"),
                ("phantom_assassin", "Phantom Assassin", "幻影刺客"),
                ("phantom_lancer", "Phantom Lancer", "幻影长矛手"),
                ("razor", "Razor", "剃刀"),
                ("riki", "Riki", "力丸"),
                ("shadow_fiend", "Shadow Fiend", "影魔"),
                ("slark", "Slark", "斯拉克"),
                ("sniper", "Sniper", "狙击手"),
                ("spectre", "Spectre", "幽鬼"),
                ("templar_assassin", "Templar Assassin", "圣堂刺客"),
                ("terrorblade", "Terrorblade", "恐怖利刃"),
                ("troll_warlord", "Troll Warlord", "巨魔战将"),
                ("ursa", "Ursa", "熊战士"),
                ("vengeful_spirit", "Vengeful Spirit", "复仇之魂"),
                ("viper", "Viper", "冥界亚龙"),
                ("weaver", "Weaver", "编织者")
            });

            // Intelligence Heroes (34)
            AddHeroes(heroes, "Intelligence", new[]
            {
                ("ancient_apparition", "Ancient Apparition", "远古冰魄"),
                ("chen", "Chen", "陈"),
                ("crystal_maiden", "Crystal Maiden", "水晶室女"),
                ("dark_seer", "Dark Seer", "黑暗贤者"),
                ("dark_willow", "Dark Willow", "邪影芳灵"),
                ("disruptor", "Disruptor", "干扰者"),
                ("enchantress", "Enchantress", "魅惑魔女"),
                ("grimstroke", "Grimstroke", "天涯墨客"),
                ("invoker", "Invoker", "祈求者"),
                ("jakiro", "Jakiro", "杰奇洛"),
                ("keeper_of_the_light", "Keeper of the Light", "光之守卫"),
                ("leshrac", "Leshrac", "拉席克"),
                ("lich", "Lich", "巫妖"),
                ("lina", "Lina", "莉娜"),
                ("lion", "Lion", "莱恩"),
                ("muerta", "Muerta", "穆尔塔"),
                ("necrophos", "Necrophos", "瘟疫法师"),
                ("oracle", "Oracle", "神谕者"),
                ("outworld_destroyer", "Outworld Destroyer", "殁境神蚀者"),
                ("puck", "Puck", "帕克"),
                ("pugna", "Pugna", "帕格纳"),
                ("queen_of_pain", "Queen of Pain", "痛苦女王"),
                ("ringmaster", "Ringmaster", "马戏团大师"),
                ("rubick", "Rubick", "拉比克"),
                ("shadow_demon", "Shadow Demon", "暗影恶魔"),
                ("shadow_shaman", "Shadow Shaman", "暗影萨满"),
                ("silencer", "Silencer", "沉默术士"),
                ("skywrath_mage", "Skywrath Mage", "天怒法师"),
                ("storm_spirit", "Storm Spirit", "风暴之灵"),
                ("tinker", "Tinker", "修补匠"),
                ("warlock", "Warlock", "术士"),
                ("winter_wyvern", "Winter Wyvern", "寒冬飞龙"),
                ("witch_doctor", "Witch Doctor", "巫医"),
                ("zeus", "Zeus", "宙斯")
            });

            // Universal Heroes (23)
            AddHeroes(heroes, "Universal", new[]
            {
                ("abaddon", "Abaddon", "亚巴顿"),
                ("arc_warden", "Arc Warden", "天穹守望者"),
                ("bane", "Bane", "祸乱之源"),
                ("batrider", "Batrider", "蝙蝠骑士"),
                ("beastmaster", "Beastmaster", "兽王"),
                ("brewmaster", "Brewmaster", "酒仙"),
                ("dazzle", "Dazzle", "戴泽"),
                ("death_prophet", "Death Prophet", "死亡先知"),
                ("enigma", "Enigma", "谜团"),
                ("io", "Io", "艾欧"),
                ("magnus", "Magnus", "马格纳斯"),
                ("marci", "Marci", "玛西"),
                ("natures_prophet", "Nature's Prophet", "先知"),
                ("nyx_assassin", "Nyx Assassin", "司夜刺客"),
                ("pangolier", "Pangolier", "石鳞剑士"),
                ("sand_king", "Sand King", "沙王"),
                ("snapfire", "Snapfire", "电炎绝手"),
                ("spirit_bear", "Spirit Bear", "熊灵"),
                ("techies", "Techies", "工程师"),
                ("venomancer", "Venomancer", "剧毒术士"),
                ("visage", "Visage", "维萨吉"),
                ("void_spirit", "Void Spirit", "虚无之灵"),
                ("windranger", "Windranger", "风行者")
            });

            var data = new HeroData
            {
                Game = "Dota2",
                Version = "7.40c",
                TotalHeroes = heroes.Count,
                Heroes = heroes
            };

            // 确保目录存在
            string directory = Path.GetDirectoryName(outputPath) ?? "Data";
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 保存JSON文件
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(outputPath, json, Encoding.UTF8);
        }

        private static void AddHeroes(List<HeroInfo> heroes, string attribute, (string id, string name, string nameCn)[] heroList)
        {
            foreach (var (id, name, nameCn) in heroList)
            {
                heroes.Add(new HeroInfo
                {
                    Id = id,
                    Name = name,
                    NameCn = nameCn,
                    Attribute = attribute,
                    IconUrl = BuildLiquipediaIconUrl(id, name)
                });
            }
        }

        private static string BuildLiquipediaIconUrl(string id, string name)
        {
            // Liquipedia图标URL格式
            // 根据实际Liquipedia结构，图标URL格式为：
            // https://liquipedia.net/commons/images/thumb/{hash}/{HeroName}_icon.png/64px-{HeroName}_icon.png
            // 由于hash值不同，这里提供一个基础URL，实际下载时会尝试多个可能的URL
            string formattedName = name.Replace(" ", "_").Replace("'", "%27").Replace("-", "_");
            return $"https://liquipedia.net/commons/images/thumb/0/0b/{formattedName}_icon.png/64px-{formattedName}_icon.png";
        }
    }
}
