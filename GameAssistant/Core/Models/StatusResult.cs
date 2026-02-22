using System.Collections.Generic;

namespace GameAssistant.Core.Models
{
    /// <summary>
    /// 状态识别结果（血量、技能等）
    /// </summary>
    public class StatusResult
    {
        /// <summary>
        /// 血量百分比
        /// </summary>
        public double HealthPercentage { get; set; } = 100.0;

        /// <summary>
        /// 魔法值百分比
        /// </summary>
        public double ManaPercentage { get; set; } = 100.0;

        /// <summary>
        /// 技能状态列表
        /// </summary>
        public List<SkillStatus> Skills { get; set; } = new List<SkillStatus>();

        /// <summary>
        /// 识别置信度
        /// </summary>
        public double Confidence { get; set; }
    }

    /// <summary>
    /// 技能状态
    /// </summary>
    public class SkillStatus
    {
        /// <summary>
        /// 技能ID
        /// </summary>
        public string SkillId { get; set; } = string.Empty;

        /// <summary>
        /// 技能名称（英文/ID）
        /// </summary>
        public string SkillName { get; set; } = string.Empty;

        /// <summary>
        /// 技能中文名，用于界面显示
        /// </summary>
        public string SkillNameCn { get; set; } = string.Empty;

        /// <summary>
        /// 显示名：有中文则显示中文，否则显示英文
        /// </summary>
        public string DisplayName => !string.IsNullOrEmpty(SkillNameCn) ? SkillNameCn : SkillName;

        /// <summary>
        /// 是否可用
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// 冷却时间（秒）
        /// </summary>
        public double CooldownSeconds { get; set; }

        /// <summary>
        /// 技能槽位
        /// </summary>
        public int Slot { get; set; }
    }
}
