using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameAssistant.Core.Interfaces;
using GameAssistant.Core.Models;

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

            // 提取英雄组合
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
        /// 判断游戏阶段（基于游戏时间或其他因素）
        /// </summary>
        public static GamePhase DetermineGamePhase(GameState gameState)
        {
            // TODO: 实现游戏阶段判断逻辑
            // 可以根据游戏时间、英雄等级、装备数量等因素判断
            return GamePhase.Unknown;
        }
    }
}
