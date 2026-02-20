using System;
using System.Drawing;
using System.Threading.Tasks;

namespace GameAssistant.Core.Interfaces
{
    /// <summary>
    /// 画面采集接口
    /// </summary>
    public interface IScreenCapture : IDisposable
    {
        /// <summary>
        /// 开始采集
        /// </summary>
        void StartCapture();

        /// <summary>
        /// 停止采集
        /// </summary>
        void StopCapture();

        /// <summary>
        /// 获取当前帧
        /// </summary>
        Task<Bitmap?> CaptureFrameAsync();

        /// <summary>
        /// 帧采集事件
        /// </summary>
        event EventHandler<Bitmap>? FrameCaptured;

        /// <summary>
        /// 是否正在采集
        /// </summary>
        bool IsCapturing { get; }

        /// <summary>
        /// 目标帧率
        /// </summary>
        int TargetFPS { get; set; }
    }
}
