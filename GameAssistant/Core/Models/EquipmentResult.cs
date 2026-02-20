using System.Collections.Generic;

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
        /// 装备名称
        /// </summary>
        public string EquipmentName { get; set; } = string.Empty;

        /// <summary>
        /// 装备槽位
        /// </summary>
        public int Slot { get; set; }

        /// <summary>
        /// 装备属性（键值对）
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
}
