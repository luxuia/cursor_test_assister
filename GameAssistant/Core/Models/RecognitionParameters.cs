using System;

namespace GameAssistant.Core.Models
{
    /// <summary>
    /// 识别参数配置
    /// </summary>
    public class RecognitionParameters
    {
        /// <summary>
        /// 模板匹配阈值（0-1）
        /// </summary>
        public double TemplateMatchThreshold { get; set; } = 0.8;

        /// <summary>
        /// OCR置信度阈值
        /// </summary>
        public double OCRConfidenceThreshold { get; set; } = 0.7;

        /// <summary>
        /// 颜色匹配容差
        /// </summary>
        public int ColorTolerance { get; set; } = 10;

        /// <summary>
        /// 识别频率（每N帧识别一次）
        /// </summary>
        public int RecognitionInterval { get; set; } = 2;

        /// <summary>
        /// 是否启用增量识别
        /// </summary>
        public bool EnableIncrementalRecognition { get; set; } = true;

        /// <summary>
        /// 英雄识别参数
        /// </summary>
        public HeroRecognitionParameters HeroRecognition { get; set; } = new HeroRecognitionParameters();

        /// <summary>
        /// 装备识别参数
        /// </summary>
        public EquipmentRecognitionParameters EquipmentRecognition { get; set; } = new EquipmentRecognitionParameters();

        /// <summary>
        /// 技能识别参数
        /// </summary>
        public SkillRecognitionParameters SkillRecognition { get; set; } = new SkillRecognitionParameters();

        /// <summary>
        /// 血量识别参数
        /// </summary>
        public HealthRecognitionParameters HealthRecognition { get; set; } = new HealthRecognitionParameters();

        /// <summary>
        /// 小地图识别参数
        /// </summary>
        public MinimapRecognitionParameters MinimapRecognition { get; set; } = new MinimapRecognitionParameters();
    }

    /// <summary>
    /// 英雄识别参数
    /// </summary>
    public class HeroRecognitionParameters
    {
        /// <summary>
        /// 英雄头像匹配阈值
        /// </summary>
        public double HeroMatchThreshold { get; set; } = 0.75;

        /// <summary>
        /// 英雄存活状态判断阈值
        /// </summary>
        public double HeroAliveThreshold { get; set; } = 0.6;

        /// <summary>
        /// 英雄位置重叠容差（像素）
        /// </summary>
        public int HeroPositionTolerance { get; set; } = 20;

        /// <summary>
        /// 最大识别英雄数量
        /// </summary>
        public int MaxHeroCount { get; set; } = 10;
    }

    /// <summary>
    /// 装备识别参数
    /// </summary>
    public class EquipmentRecognitionParameters
    {
        /// <summary>
        /// 装备图标匹配阈值
        /// </summary>
        public double EquipmentMatchThreshold { get; set; } = 0.7;

        /// <summary>
        /// 装备槽位检测阈值
        /// </summary>
        public double SlotDetectionThreshold { get; set; } = 0.5;

        /// <summary>
        /// 装备槽位尺寸容差（像素）
        /// </summary>
        public int SlotSizeTolerance { get; set; } = 5;

        /// <summary>
        /// 最大装备槽位数
        /// </summary>
        public int MaxEquipmentSlots { get; set; } = 6;
    }

    /// <summary>
    /// 技能识别参数
    /// </summary>
    public class SkillRecognitionParameters
    {
        /// <summary>
        /// 技能图标匹配阈值
        /// </summary>
        public double SkillMatchThreshold { get; set; } = 0.65;

        /// <summary>
        /// 技能可用判断阈值（亮度）
        /// </summary>
        public int SkillAvailabilityThreshold { get; set; } = 100;

        /// <summary>
        /// 技能冷却时间识别阈值
        /// </summary>
        public double SkillCooldownThreshold { get; set; } = 0.7;

        /// <summary>
        /// 最大技能数量
        /// </summary>
        public int MaxSkillCount { get; set; } = 4;
    }

    /// <summary>
    /// 血量识别参数
    /// </summary>
    public class HealthRecognitionParameters
    {
        /// <summary>
        /// 血量识别方法
        /// </summary>
        public HealthRecognitionMethod RecognitionMethod { get; set; } = HealthRecognitionMethod.PixelCount;

        /// <summary>
        /// 血量颜色范围（低血量）
        /// </summary>
        public ColorRange LowHealthColor { get; set; } = new ColorRange { H = 0, S = 50, V = 50, Tolerance = 10 };

        /// <summary>
        /// 血量颜色范围（高血量）
        /// </summary>
        public ColorRange HighHealthColor { get; set; } = new ColorRange { H = 50, S = 50, V = 50, Tolerance = 10 };

        /// <summary>
        /// 血条最大宽度比例
        /// </summary>
        public double MaxHealthBarWidthRatio { get; set; } = 0.8;

        /// <summary>
        /// 血量识别最小面积
        /// </summary>
        public int MinHealthBarArea { get; set; } = 50;
    }

    /// <summary>
    /// 血量识别方法
    /// </summary>
    public enum HealthRecognitionMethod
    {
        PixelCount,
        LengthMeasurement,
        Combined
    }

    /// <summary>
    /// 颜色范围配置
    /// </summary>
    public class ColorRange
    {
        /// <summary>
        /// H值（色相）
        /// </summary>
        public int H { get; set; } = 0;

        /// <summary>
        /// S值（饱和度）
        /// </summary>
        public int S { get; set; } = 50;

        /// <summary>
        /// V值（明度）
        /// </summary>
        public int V { get; set; } = 50;

        /// <summary>
        /// 颜色容差
        /// </summary>
        public int Tolerance { get; set; } = 10;

        /// <summary>
        /// 获取最低颜色值
        /// </summary>
        public OpenCvSharp.Scalar GetLowerBound()
        {
            return new OpenCvSharp.Scalar(
                Math.Max(0, H - Tolerance),
                Math.Max(0, S - Tolerance),
                Math.Max(0, V - Tolerance)
            );
        }

        /// <summary>
        /// 获取最高颜色值
        /// </summary>
        public OpenCvSharp.Scalar GetUpperBound()
        {
            return new OpenCvSharp.Scalar(
                Math.Min(180, H + Tolerance),
                Math.Min(255, S + Tolerance),
                Math.Min(255, V + Tolerance)
            );
        }
    }

    /// <summary>
    /// 小地图识别参数
    /// </summary>
    public class MinimapRecognitionParameters
    {
        /// <summary>
        /// 英雄标记检测最小面积
        /// </summary>
        public int MinMarkerArea { get; set; } = 10;

        /// <summary>
        /// 英雄标记检测最大面积
        /// </summary>
        public int MaxMarkerArea { get; set; } = 500;

        /// <summary>
        /// 己方英雄标记颜色范围
        /// </summary>
        public ColorRange AllyMarkerColor { get; set; } = new ColorRange { H = 110, S = 50, V = 50, Tolerance = 15 };

        /// <summary>
        /// 敌方英雄标记颜色范围
        /// </summary>
        public ColorRange EnemyMarkerColor { get; set; } = new ColorRange { H = 0, S = 50, V = 50, Tolerance = 15 };

        /// <summary>
        /// 地图边界检测阈值
        /// </summary>
        public double MapBoundaryThreshold { get; set; } = 0.1;
    }
}
