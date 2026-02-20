using System.Collections.Generic;
using System.Drawing;

namespace GameAssistant.Core.Models
{
    /// <summary>
    /// 小地图识别结果
    /// </summary>
    public class MinimapResult
    {
        /// <summary>
        /// 英雄位置列表
        /// </summary>
        public List<HeroPosition> HeroPositions { get; set; } = new List<HeroPosition>();

        /// <summary>
        /// 识别置信度
        /// </summary>
        public double Confidence { get; set; }
    }

    /// <summary>
    /// 英雄位置
    /// </summary>
    public class HeroPosition
    {
        /// <summary>
        /// 英雄ID
        /// </summary>
        public string HeroId { get; set; } = string.Empty;

        /// <summary>
        /// 在小地图上的坐标
        /// </summary>
        public Point MinimapCoordinate { get; set; }

        /// <summary>
        /// 游戏世界坐标（如果可映射）
        /// </summary>
        public PointF? WorldCoordinate { get; set; }
    }
}
