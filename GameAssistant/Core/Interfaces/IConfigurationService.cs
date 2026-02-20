using System.Drawing;
using GameAssistant.Core.Models;

namespace GameAssistant.Core.Interfaces
{
    /// <summary>
    /// 配置服务接口
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// 获取识别区域配置
        /// </summary>
        RecognitionRegions GetRecognitionRegions();

        /// <summary>
        /// 保存识别区域配置
        /// </summary>
        void SaveRecognitionRegions(RecognitionRegions regions);

        /// <summary>
        /// 获取识别参数配置
        /// </summary>
        RecognitionParameters GetRecognitionParameters();

        /// <summary>
        /// 保存识别参数配置
        /// </summary>
        void SaveRecognitionParameters(RecognitionParameters parameters);

        /// <summary>
        /// 获取游戏窗口配置
        /// </summary>
        GameWindowConfig GetGameWindowConfig();

        /// <summary>
        /// 保存游戏窗口配置
        /// </summary>
        void SaveGameWindowConfig(GameWindowConfig config);
    }
}
