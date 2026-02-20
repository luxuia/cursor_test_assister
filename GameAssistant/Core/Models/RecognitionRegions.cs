using System.Drawing;

namespace GameAssistant.Core.Models
{
    /// <summary>
    /// 识别区域配置
    /// </summary>
    public class RecognitionRegions
    {
        /// <summary>
        /// 英雄阵容区域
        /// </summary>
        public Rectangle HeroRosterRegion { get; set; }

        /// <summary>
        /// 小地图区域
        /// </summary>
        public Rectangle MinimapRegion { get; set; }

        /// <summary>
        /// 装备面板区域
        /// </summary>
        public Rectangle EquipmentPanelRegion { get; set; }

        /// <summary>
        /// 状态栏区域（血量、技能等）
        /// </summary>
        public Rectangle StatusBarRegion { get; set; }
    }
}
