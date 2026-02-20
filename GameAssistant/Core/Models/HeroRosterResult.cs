using System.Collections.Generic;

namespace GameAssistant.Core.Models
{
    /// <summary>
    /// 英雄阵容识别结果
    /// </summary>
    public class HeroRosterResult
    {
        /// <summary>
        /// 英雄列表
        /// </summary>
        public List<HeroInfo> Heroes { get; set; } = new List<HeroInfo>();

        /// <summary>
        /// 识别置信度
        /// </summary>
        public double Confidence { get; set; }
    }

    /// <summary>
    /// 英雄信息
    /// </summary>
    public class HeroInfo
    {
        /// <summary>
        /// 英雄ID
        /// </summary>
        public string HeroId { get; set; } = string.Empty;

        /// <summary>
        /// 英雄名称
        /// </summary>
        public string HeroName { get; set; } = string.Empty;

        /// <summary>
        /// 位置索引
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// 是否存活
        /// </summary>
        public bool IsAlive { get; set; } = true;

        /// <summary>
        /// 血量百分比
        /// </summary>
        public double HealthPercentage { get; set; } = 100.0;
    }
}
