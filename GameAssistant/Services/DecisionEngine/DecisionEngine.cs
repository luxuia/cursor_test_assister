using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GameAssistant.Core.Interfaces;
using GameAssistant.Core.Models;
using Newtonsoft.Json;

namespace GameAssistant.Services.DecisionEngine
{
    /// <summary>
    /// 决策分析引擎实现
    /// </summary>
    public class DecisionEngine : IDecisionEngine
    {
        private readonly IAdviceDatabase _adviceDatabase;
        private GameState? _currentGameState;
        private readonly Queue<GameState> _stateHistory = new Queue<GameState>();
        private const int MaxHistorySize = 10;

        public DecisionEngine(IAdviceDatabase adviceDatabase)
        {
            _adviceDatabase = adviceDatabase;
        }

        public void UpdateGameState(GameState gameState)
        {
            _currentGameState = gameState;
            
            // 维护状态历史
            _stateHistory.Enqueue(gameState);
            if (_stateHistory.Count > MaxHistorySize)
            {
                _stateHistory.Dequeue();
            }
        }

        public async Task<List<Advice>> AnalyzeAsync(GameState gameState)
        {
            if (gameState == null)
                return new List<Advice>();

            // 更新当前状态
            UpdateGameState(gameState);

            // 构建匹配条件
            var condition = BuildConditionFromGameState(gameState);

            // 查询匹配的规则
            var rules = await _adviceDatabase.QueryRulesAsync(condition);

            // 生成建议列表
            var adviceList = rules.Select(rule => new Advice
            {
                Id = rule.Id,
                Content = rule.AdviceContent,
                Priority = rule.Priority,
                Type = rule.Type,
                Scenario = GetScenarioDescription(gameState),
                MatchedCondition = condition
            }).ToList();

            // 英雄攻略：己方英雄出装/技能/玩法；敌方英雄克制要点与克制列表
            var heroGuideAdvice = GetHeroGuideAdvice(gameState);
            adviceList.AddRange(heroGuideAdvice);

            // 去重和合并相似建议
            adviceList = DeduplicateAdvice(adviceList);

            // 按优先级排序
            adviceList = adviceList.OrderByDescending(a => a.Priority).ToList();

            return adviceList;
        }

        private AdviceCondition BuildConditionFromGameState(GameState gameState)
        {
            var condition = new AdviceCondition();

            // 从状态信息提取条件
            if (gameState.Status != null)
            {
                condition.HealthThreshold = gameState.Status.HealthPercentage;
            }

            condition.Phase = gameState.Phase;

            // 对位：敌方英雄列表（用于对位攻略与规则中的 VersusHeroCombination 匹配）
            if (gameState.EnemyHeroes != null && gameState.EnemyHeroes.Any())
                condition.VersusHeroCombination = new List<string>(gameState.EnemyHeroes);

            // 提取己方英雄组合
            if (gameState.HeroRoster != null && gameState.HeroRoster.Heroes.Any())
            {
                condition.HeroCombination = gameState.HeroRoster.Heroes
                    .Select(h => h.HeroId)
                    .ToList();
            }

            // 提取装备组合
            if (gameState.Equipment != null && gameState.Equipment.EquipmentList.Any())
            {
                condition.EquipmentCombination = gameState.Equipment.EquipmentList
                    .Select(e => e.EquipmentId)
                    .ToList();
            }

            // 提取技能状态
            if (gameState.Status != null && gameState.Status.Skills.Any())
            {
                condition.SkillConditions = gameState.Status.Skills
                    .ToDictionary(s => s.SkillId, s => s.IsAvailable);
            }

            return condition;
        }

        private string GetScenarioDescription(GameState gameState)
        {
            var scenarios = new List<string>();

            if (gameState.Phase != GamePhase.Unknown)
            {
                scenarios.Add($"游戏{gameState.Phase}期");
            }

            if (gameState.Status != null)
            {
                if (gameState.Status.HealthPercentage < 30)
                {
                    scenarios.Add("低血量状态");
                }
                else if (gameState.Status.HealthPercentage < 50)
                {
                    scenarios.Add("中等血量状态");
                }
            }

            if (gameState.Minimap != null && gameState.Minimap.HeroPositions.Any())
            {
                scenarios.Add($"小地图上有{gameState.Minimap.HeroPositions.Count}个英雄");
            }

            return string.Join("，", scenarios);
        }

        private List<Advice> DeduplicateAdvice(List<Advice> adviceList)
        {
            // 简单的去重逻辑：如果内容相似，保留优先级更高的
            var result = new List<Advice>();
            var seenContents = new HashSet<string>();

            foreach (var advice in adviceList.OrderByDescending(a => a.Priority))
            {
                // 简单的相似度检查（实际应该使用更复杂的文本相似度算法）
                string normalizedContent = advice.Content.ToLower().Trim();
                
                if (!seenContents.Contains(normalizedContent))
                {
                    seenContents.Add(normalizedContent);
                    result.Add(advice);
                }
            }

            return result;
        }

        /// <summary>
        /// 从英雄攻略中获取建议：己方英雄给出装/技能/玩法；敌方英雄给怎么克制与克制列表。
        /// </summary>
        private List<Advice> GetHeroGuideAdvice(GameState gameState)
        {
            var result = new List<Advice>();
            var guides = LoadHeroGuides();
            if (guides == null) return result;
            var guideMap = guides.ToDictionary(g => g.HeroId, g => g, StringComparer.OrdinalIgnoreCase);

            // 己方英雄：出装（出门/前/中/后期或合并）、技能（释放顺序+各技能技巧或合并）、玩法
            var ourHeroes = gameState.HeroRoster?.Heroes;
            if (ourHeroes != null)
            {
                foreach (var h in ourHeroes)
                {
                    if (!guideMap.TryGetValue(h.HeroId, out var g)) continue;
                    // 出装：优先分阶段
                    if (!string.IsNullOrWhiteSpace(g.ItemStarting))
                        result.Add(MakeGuideAdvice($"[{g.HeroNameCn}] 出门: {g.ItemStarting}", g.HeroNameCn, 6));
                    if (!string.IsNullOrWhiteSpace(g.ItemEarly))
                        result.Add(MakeGuideAdvice($"[{g.HeroNameCn}] 前期: {g.ItemEarly}", g.HeroNameCn, 6));
                    if (!string.IsNullOrWhiteSpace(g.ItemMid))
                        result.Add(MakeGuideAdvice($"[{g.HeroNameCn}] 中期: {g.ItemMid}", g.HeroNameCn, 6));
                    if (!string.IsNullOrWhiteSpace(g.ItemLate))
                        result.Add(MakeGuideAdvice($"[{g.HeroNameCn}] 后期: {g.ItemLate}", g.HeroNameCn, 6));
                    if (string.IsNullOrWhiteSpace(g.ItemStarting) && string.IsNullOrWhiteSpace(g.ItemEarly) && string.IsNullOrWhiteSpace(g.ItemMid) && string.IsNullOrWhiteSpace(g.ItemLate) && !string.IsNullOrWhiteSpace(g.ItemAdvice))
                        result.Add(MakeGuideAdvice($"[{g.HeroNameCn}] 出装: {g.ItemAdvice}", g.HeroNameCn, 6));
                    // 技能：优先释放顺序+各技能技巧
                    if (!string.IsNullOrWhiteSpace(g.SkillReleaseOrder))
                        result.Add(MakeGuideAdvice($"[{g.HeroNameCn}] 释放顺序: {g.SkillReleaseOrder}", g.HeroNameCn, 5));
                    if (!string.IsNullOrWhiteSpace(g.SkillTips))
                        result.Add(MakeGuideAdvice($"[{g.HeroNameCn}] 技能技巧: {g.SkillTips}", g.HeroNameCn, 5));
                    if (string.IsNullOrWhiteSpace(g.SkillReleaseOrder) && string.IsNullOrWhiteSpace(g.SkillTips) && !string.IsNullOrWhiteSpace(g.SkillAdvice))
                        result.Add(MakeGuideAdvice($"[{g.HeroNameCn}] 技能: {g.SkillAdvice}", g.HeroNameCn, 5));
                    if (!string.IsNullOrWhiteSpace(g.PlayAdvice))
                        result.Add(MakeGuideAdvice($"[{g.HeroNameCn}] 玩法: {g.PlayAdvice}", g.HeroNameCn, 5));
                }
            }

            // 敌方英雄：怎么克制、克制/被克制列表
            var enemyIds = gameState.EnemyHeroes;
            if (enemyIds != null)
            {
                foreach (var enemyId in enemyIds)
                {
                    if (!guideMap.TryGetValue(enemyId, out var g)) continue;
                    if (!string.IsNullOrWhiteSpace(g.CounterAdvice))
                        result.Add(MakeGuideAdvice($"[克制{g.HeroNameCn}] {g.CounterAdvice}", $"敌方{g.HeroNameCn}", 7));
                    var counters = g.CounteredByList?.Where(x => !string.IsNullOrEmpty(x)).Take(5).ToList();
                    if (counters != null && counters.Count > 0)
                        result.Add(MakeGuideAdvice($"[克制{g.HeroNameCn}] 可选: {string.Join("、", counters)}", $"敌方{g.HeroNameCn}", 6));
                }
            }

            return result;
        }

        private static Advice MakeGuideAdvice(string content, string scenario, int priority)
        {
            return new Advice
            {
                Id = -1,
                Content = content,
                Priority = priority,
                Type = AdviceType.Strategy,
                Scenario = scenario,
                MatchedCondition = null
            };
        }

        private static List<HeroGuideEntry>? LoadHeroGuides()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var path = Path.Combine(baseDir, "Data", "HeroGuides.json");
                if (!File.Exists(path))
                    path = Path.Combine(baseDir, "..", "..", "..", "Data", "HeroGuides.json");
                if (!File.Exists(path))
                    return null;
                var json = File.ReadAllText(path);
                var data = JsonConvert.DeserializeObject<HeroGuidesData>(json);
                return data?.Guides;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 判断游戏阶段：用装备数量做简单启发式（无装备信息时返回 Unknown）
        /// </summary>
        public static GamePhase DetermineGamePhase(GameState? gameState)
        {
            if (gameState?.Equipment?.EquipmentList == null)
                return GamePhase.Unknown;
            int count = gameState.Equipment.EquipmentList.Count;
            if (count <= 2) return GamePhase.Early;
            if (count <= 5) return GamePhase.Mid;
            return GamePhase.Late;
        }
    }

    internal class HeroGuidesData
    {
        [JsonProperty("guides")]
        public List<HeroGuideEntry>? Guides { get; set; }
    }

    internal class HeroGuideEntry
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
