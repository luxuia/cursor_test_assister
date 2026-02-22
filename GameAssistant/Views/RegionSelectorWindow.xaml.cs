using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GameAssistant.Views
{
    public partial class RegionSelectorWindow : Window
    {
        private bool _isSelecting = false;
        private System.Windows.Point _startPoint;
        private readonly IntPtr _targetWindowHandle;

        public System.Drawing.Rectangle SelectedRegion { get; private set; } = new System.Drawing.Rectangle(0, 0, 0, 0);

        /// <param name="regionName">区域名称</param>
        /// <param name="targetWindowHandle">游戏窗口句柄；非零时选中的区域将转换为相对该窗口的坐标</param>
        public RegionSelectorWindow(string regionName, IntPtr targetWindowHandle = default)
        {
            InitializeComponent();
            Title = $"选择{regionName}";
            _targetWindowHandle = targetWindowHandle;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isSelecting = true;
            _startPoint = e.GetPosition(SelectionCanvas);
            SelectionRectangle.Visibility = Visibility.Visible;
            Canvas.SetLeft(SelectionRectangle, _startPoint.X);
            Canvas.SetTop(SelectionRectangle, _startPoint.Y);
            SelectionRectangle.Width = 0;
            SelectionRectangle.Height = 0;
            CaptureMouse();
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isSelecting)
            {
                var currentPoint = e.GetPosition(SelectionCanvas);
                var left = Math.Min(_startPoint.X, currentPoint.X);
                var top = Math.Min(_startPoint.Y, currentPoint.Y);
                var width = Math.Abs(currentPoint.X - _startPoint.X);
                var height = Math.Abs(currentPoint.Y - _startPoint.Y);

                Canvas.SetLeft(SelectionRectangle, left);
                Canvas.SetTop(SelectionRectangle, top);
                SelectionRectangle.Width = width;
                SelectionRectangle.Height = height;
            }
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isSelecting)
            {
                _isSelecting = false;
                ReleaseMouseCapture();

                var screenStart = PointToScreen(_startPoint);
                var screenEnd = PointToScreen(e.GetPosition(SelectionCanvas));
                int x = (int)Math.Min(screenStart.X, screenEnd.X);
                int y = (int)Math.Min(screenStart.Y, screenEnd.Y);
                int w = (int)Math.Abs(screenEnd.X - screenStart.X);
                int h = (int)Math.Abs(screenEnd.Y - screenStart.Y);

                // 若指定了游戏窗口句柄，将屏幕坐标转换为相对该窗口的坐标
                if (_targetWindowHandle != IntPtr.Zero && GetWindowRect(_targetWindowHandle, out RECT winRect))
                {
                    x -= winRect.Left;
                    y -= winRect.Top;
                }

                SelectedRegion = new System.Drawing.Rectangle(x, y, w, h);
            }
        }

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

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
            else if (e.Key == Key.Enter && !_isSelecting && SelectedRegion.Width > 0 && SelectedRegion.Height > 0)
            {
                DialogResult = true;
                Close();
            }
        }
    }
}
