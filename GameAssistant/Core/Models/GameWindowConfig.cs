using System;

namespace GameAssistant.Core.Models
{
    /// <summary>
    /// 游戏窗口配置
    /// </summary>
    public class GameWindowConfig
    {
        /// <summary>
        /// 游戏窗口标题（用于查找窗口）
        /// </summary>
        public string WindowTitle { get; set; } = string.Empty;

        /// <summary>
        /// 游戏进程名称
        /// </summary>
        public string ProcessName { get; set; } = string.Empty;

        /// <summary>
        /// 是否全屏模式
        /// </summary>
        public bool IsFullScreen { get; set; }

        /// <summary>
        /// 窗口句柄（运行时设置）
        /// </summary>
        public IntPtr WindowHandle { get; set; } = IntPtr.Zero;
    }
}
