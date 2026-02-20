namespace GameAssistant.Core.Models
{
    /// <summary>
    /// 建议规则
    /// </summary>
    public class AdviceRule
    {
        /// <summary>
        /// 规则ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 匹配条件
        /// </summary>
        public AdviceCondition Condition { get; set; } = new AdviceCondition();

        /// <summary>
        /// 建议内容
        /// </summary>
        public string AdviceContent { get; set; } = string.Empty;

        /// <summary>
        /// 优先级
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// 建议类型
        /// </summary>
        public AdviceType Type { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;
    }
}
