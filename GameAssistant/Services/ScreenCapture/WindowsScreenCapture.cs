using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using GameAssistant.Core.Interfaces;

namespace GameAssistant.Services.ScreenCapture
{
    /// <summary>
    /// Windows画面采集实现
    /// </summary>
    public class WindowsScreenCapture : IScreenCapture
    {
        private bool _isCapturing;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly object _lockObject = new object();
        private IntPtr _targetWindowHandle = IntPtr.Zero;

        public bool IsCapturing
        {
            get
            {
                lock (_lockObject)
                {
                    return _isCapturing;
                }
            }
        }

        public int TargetFPS { get; set; } = 60;

        public event EventHandler<Bitmap>? FrameCaptured;

        public WindowsScreenCapture(IntPtr windowHandle)
        {
            _targetWindowHandle = windowHandle;
        }

        public void StartCapture()
        {
            lock (_lockObject)
            {
                if (_isCapturing)
                    return;

                _isCapturing = true;
                _cancellationTokenSource = new CancellationTokenSource();
                Task.Run(() => CaptureLoop(_cancellationTokenSource.Token));
            }
        }

        public void StopCapture()
        {
            lock (_lockObject)
            {
                if (!_isCapturing)
                    return;

                _isCapturing = false;
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        public async Task<Bitmap?> CaptureFrameAsync()
        {
            return await Task.Run(() =>
            {
                if (_targetWindowHandle == IntPtr.Zero)
                    return null;

                return CaptureWindow(_targetWindowHandle);
            });
        }

        private async Task CaptureLoop(CancellationToken cancellationToken)
        {
            int delayMs = 1000 / TargetFPS;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var bitmap = CaptureWindow(_targetWindowHandle);
                    if (bitmap != null)
                    {
                        FrameCaptured?.Invoke(this, bitmap);
                        bitmap.Dispose(); // 释放资源，避免内存泄漏
                    }
                }
                catch (Exception ex)
                {
                    // 记录错误日志
                    System.Diagnostics.Debug.WriteLine($"Capture error: {ex.Message}");
                }

                await Task.Delay(delayMs, cancellationToken);
            }
        }

        private Bitmap? CaptureWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
                return null;

            // 获取窗口矩形
            if (!GetWindowRect(hWnd, out RECT rect))
                return null;

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            if (width <= 0 || height <= 0)
                return null;

            // 创建位图
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            Graphics graphics = Graphics.FromImage(bitmap);

            // 复制窗口内容
            IntPtr hdcSrc = GetWindowDC(hWnd);
            IntPtr hdcDest = graphics.GetHdc();
            BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, SRCCOPY);
            graphics.ReleaseHdc(hdcDest);
            ReleaseDC(hWnd, hdcSrc);

            graphics.Dispose();
            return bitmap;
        }

        public void Dispose()
        {
            StopCapture();
        }

        #region Win32 API

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest, int nWidth, int nHeight,
            IntPtr hObjectSource, int nXSrc, int nYSrc, int dwRop);

        private const int SRCCOPY = 0x00CC0020;

        #endregion
    }
}
