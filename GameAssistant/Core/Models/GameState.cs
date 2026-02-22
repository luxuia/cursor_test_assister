using System;
using System.Collections.Generic;

namespace GameAssistant.Core.Models
{
    /// <summary>
    /// 游戏状态
    /// </summary>
    public class GameState
    {
        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// 英雄阵容信息
        /// </summary>
        public HeroRosterResult? HeroRoster { get; set; }

        /// <summary>
        /// 小地图信息
        /// </summary>
        public MinimapResult? Minimap { get; set; }

        /// <summary>
        /// 装备信息
        /// </summary>
        public EquipmentResult? Equipment { get; set; }

        /// <summary>
        /// 状态信息（血量、技能等）
        /// </summary>
        public StatusResult? Status { get; set; }

        /// <summary>
        /// 游戏阶段（前期/中期/后期）
        /// </summary>
        public GamePhase Phase { get; set; } = GamePhase.Unknown;

        /// <summary>
        /// 敌方/对位英雄 ID 列表（与 Dota2Heroes id 一致，用于对位攻略匹配；若未识别则为空）
        /// </summary>
        public List<string>? EnemyHeroes { get; set; }
    }

    /// <summary>
    /// 游戏阶段
    /// </summary>
    public enum GamePhase
    {
        Unknown,
        Early,   // 前期
        Mid,     // 中期
        Late     // 后期
    }
}
