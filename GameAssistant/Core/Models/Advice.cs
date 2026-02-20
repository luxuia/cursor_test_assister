namespace GameAssistant.Core.Models
{
    /// <summary>
    /// 游戏建议
    /// </summary>
    public class Advice
    {
        /// <summary>
        /// 建议ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 建议内容
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 优先级（数字越大优先级越高）
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// 建议类型
        /// </summary>
        public AdviceType Type { get; set; }

        /// <summary>
        /// 适用场景
        /// </summary>
        public string Scenario { get; set; } = string.Empty;

        /// <summary>
        /// 匹配的条件
        /// </summary>
        public AdviceCondition? MatchedCondition { get; set; }
    }

    /// <summary>
    /// 建议类型
    /// </summary>
    public enum AdviceType
    {
        Combat,      // 战斗建议
        Strategy,    // 策略建议
        Equipment,   // 装备建议
        Positioning, // 站位建议
        Warning      // 警告提示
    }
}
