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
    }
}
