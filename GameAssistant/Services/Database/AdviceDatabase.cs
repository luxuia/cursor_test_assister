using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GameAssistant.Core.Interfaces;
using GameAssistant.Core.Models;
using Newtonsoft.Json;

namespace GameAssistant.Services.Database
{
    /// <summary>
    /// 建议数据库实现（JSON文件）
    /// </summary>
    public class AdviceDatabase : IAdviceDatabase
    {
        private readonly string _jsonFilePath;
        private readonly object _lockObject = new object();
        private List<AdviceRule> _rules = new List<AdviceRule>();
        private int _nextId = 1;

        public AdviceDatabase(string jsonFilePath = "advice_rules.json")
        {
            _jsonFilePath = jsonFilePath;
        }

        public async Task InitializeAsync()
        {
            await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    if (File.Exists(_jsonFilePath))
                    {
                        // 加载现有规则
                        LoadRulesFromFile();
                    }
                    else
                    {
                        // 创建新文件并插入示例数据
                        _rules = new List<AdviceRule>();
                        InsertSampleData();
                        SaveRulesToFile();
                    }
                }
            });
        }

        private void LoadRulesFromFile()
        {
            try
            {
                string json = File.ReadAllText(_jsonFilePath);
                var data = JsonConvert.DeserializeObject<AdviceDatabaseData>(json);
                
                if (data != null)
                {
                    _rules = data.Rules ?? new List<AdviceRule>();
                    _nextId = data.NextId > 0 ? data.NextId : (_rules.Count > 0 ? _rules.Max(r => r.Id) + 1 : 1);
                }
                else
                {
                    _rules = new List<AdviceRule>();
                    _nextId = 1;
                }
            }
            catch
            {
                // 如果文件损坏，重新初始化
                _rules = new List<AdviceRule>();
                _nextId = 1;
                InsertSampleData();
                SaveRulesToFile();
            }
        }

        private void SaveRulesToFile()
        {
            try
            {
                var data = new AdviceDatabaseData
                {
                    Rules = _rules,
                    NextId = _nextId
                };
                
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(_jsonFilePath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"保存规则文件失败: {ex.Message}", ex);
            }
        }

        public async Task<List<AdviceRule>> QueryRulesAsync(AdviceCondition condition)
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    var matchedRules = _rules
                        .Where(r => r.IsEnabled && MatchesCondition(r.Condition, condition))
                        .ToList();

                    return matchedRules;
                }
            });
        }

        public async Task AddRuleAsync(AdviceRule rule)
        {
            await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    // 如果ID为0或已存在，分配新ID
                    if (rule.Id == 0 || _rules.Any(r => r.Id == rule.Id))
                    {
                        rule.Id = _nextId++;
                    }

                    _rules.Add(rule);
                    SaveRulesToFile();
                }
            });
        }

        public async Task DeleteRuleAsync(int ruleId)
        {
            await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    _rules.RemoveAll(r => r.Id == ruleId);
                    SaveRulesToFile();
                }
            });
        }

        public async Task<List<AdviceRule>> GetAllRulesAsync()
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    return new List<AdviceRule>(_rules);
                }
            });
        }

        private bool MatchesCondition(AdviceCondition ruleCondition, AdviceCondition gameCondition)
        {
            // 简单的匹配逻辑，实际应该更复杂
            if (ruleCondition.HealthThreshold.HasValue && gameCondition.HealthThreshold.HasValue)
            {
                if (gameCondition.HealthThreshold.Value > ruleCondition.HealthThreshold.Value)
                    return false;
            }

            if (ruleCondition.Phase.HasValue && gameCondition.Phase.HasValue)
            {
                if (ruleCondition.Phase.Value != gameCondition.Phase.Value)
                    return false;
            }

            // TODO: 实现更复杂的匹配逻辑
            return true;
        }

        private void InsertSampleData()
        {
            var sampleRules = new[]
            {
                new AdviceRule
                {
                    Id = _nextId++,
                    Condition = new AdviceCondition { HealthThreshold = 30.0 },
                    AdviceContent = "血量较低，建议撤退或使用恢复道具",
                    Priority = 8,
                    Type = AdviceType.Warning,
                    IsEnabled = true
                },
                new AdviceRule
                {
                    Id = _nextId++,
                    Condition = new AdviceCondition { Phase = GamePhase.Early },
                    AdviceContent = "游戏前期，建议优先发育和积累资源",
                    Priority = 5,
                    Type = AdviceType.Strategy,
                    IsEnabled = true
                },
                new AdviceRule
                {
                    Id = _nextId++,
                    Condition = new AdviceCondition { Phase = GamePhase.Late },
                    AdviceContent = "游戏后期，建议抱团推进，避免单独行动",
                    Priority = 7,
                    Type = AdviceType.Strategy,
                    IsEnabled = true
                }
            };

            _rules.AddRange(sampleRules);
        }
    }

    /// <summary>
    /// JSON数据库数据结构
    /// </summary>
    internal class AdviceDatabaseData
    {
        public List<AdviceRule> Rules { get; set; } = new List<AdviceRule>();
        public int NextId { get; set; } = 1;
    }
}
