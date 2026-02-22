using System.Collections.Generic;

namespace GameAssistant.Core.Models
{
    /// <summary>
    /// 建议匹配条件
    /// </summary>
    public class AdviceCondition
    {
        /// <summary>
        /// 英雄组合（英雄ID列表）
        /// </summary>
        public List<string>? HeroCombination { get; set; }

        /// <summary>
        /// 装备组合（装备ID列表）
        /// </summary>
        public List<string>? EquipmentCombination { get; set; }

        /// <summary>
        /// 血量阈值（低于此值时触发）
        /// </summary>
        public double? HealthThreshold { get; set; }

        /// <summary>
        /// 地图位置区域
        /// </summary>
        public string? MapRegion { get; set; }

        /// <summary>
        /// 游戏阶段
        /// </summary>
        public GamePhase? Phase { get; set; }

        /// <summary>
        /// 技能状态条件
        /// </summary>
        public Dictionary<string, bool>? SkillConditions { get; set; }

        /// <summary>
        /// 对位/敌方英雄 ID 列表（规则触发需敌方存在其中至少一人）
        /// </summary>
        public List<string>? VersusHeroCombination { get; set; }

        /// <summary>
        /// 自定义条件（键值对）
        /// </summary>
        public Dictionary<string, object>? CustomConditions { get; set; }
    }
}
