using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace GameAssistant.Services.ScreenCapture
{
    /// <summary>
    /// 窗口查找工具
    /// </summary>
    public static class WindowFinder
    {
        /// <summary>
        /// 根据窗口标题查找窗口句柄
        /// </summary>
        public static IntPtr FindWindowByTitle(string windowTitle)
        {
            return FindWindow(null, windowTitle);
        }

        /// <summary>
        /// 根据进程名称查找窗口句柄
        /// </summary>
        public static IntPtr FindWindowByProcessName(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length > 0)
            {
                return processes[0].MainWindowHandle;
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// 枚举所有窗口并查找匹配的窗口
        /// </summary>
        public static IntPtr FindWindowByTitleContains(string partialTitle)
        {
            IntPtr foundWindow = IntPtr.Zero;
            EnumWindows((hWnd, lParam) =>
            {
                StringBuilder windowText = new StringBuilder(256);
                GetWindowText(hWnd, windowText, windowText.Capacity);
                if (windowText.ToString().Contains(partialTitle))
                {
                    foundWindow = hWnd;
                    return false; // 停止枚举
                }
                return true; // 继续枚举
            }, IntPtr.Zero);
            return foundWindow;
        }

        /// <summary>
        /// 获取窗口标题
        /// </summary>
        public static string GetWindowTitle(IntPtr hWnd)
        {
            StringBuilder windowText = new StringBuilder(256);
            GetWindowText(hWnd, windowText, windowText.Capacity);
            return windowText.ToString();
        }

        #region Win32 API

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        #endregion
    }
}
