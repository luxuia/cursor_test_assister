using System.Collections.Generic;
using System.Drawing;

namespace GameAssistant.Core.Models
{
    /// <summary>
    /// 装备识别结果
    /// </summary>
    public class EquipmentResult
    {
        /// <summary>
        /// 装备列表
        /// </summary>
        public List<EquipmentInfo> EquipmentList { get; set; } = new List<EquipmentInfo>();

        /// <summary>
        /// 目标英雄ID（如果识别的是其他玩家的装备）
        /// </summary>
        public string? TargetHeroId { get; set; }

        /// <summary>
        /// 识别置信度
        /// </summary>
        public double Confidence { get; set; }
    }

    /// <summary>
    /// 装备信息
    /// </summary>
    public class EquipmentInfo
    {
        /// <summary>
        /// 装备ID
        /// </summary>
        public string EquipmentId { get; set; } = string.Empty;

        /// <summary>
        /// 装备名称（英文/ID）
        /// </summary>
        public string EquipmentName { get; set; } = string.Empty;

        /// <summary>
        /// 装备中文名，用于界面显示
        /// </summary>
        public string EquipmentNameCn { get; set; } = string.Empty;

        /// <summary>
        /// 显示名：有中文则显示中文，否则显示英文
        /// </summary>
        public string DisplayName => !string.IsNullOrEmpty(EquipmentNameCn) ? EquipmentNameCn : EquipmentName;

        /// <summary>
        /// 装备槽位
        /// </summary>
        public int Slot { get; set; }

        /// <summary>
        /// 在原图中的检测框（用于调试绘制）
        /// </summary>
        public Rectangle? Bounds { get; set; }

        /// <summary>
        /// 模板匹配相似度 0～1，用于显示匹配程度
        /// </summary>
        public double MatchScore { get; set; }

        /// <summary>
        /// 装备属性（键值对）
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
}
