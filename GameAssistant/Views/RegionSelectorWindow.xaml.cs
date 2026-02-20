using System;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GameAssistant.Views
{
    public partial class RegionSelectorWindow : Window
    {
        private bool _isSelecting = false;
        private System.Windows.Point _startPoint;
        public Rectangle SelectedRegion { get; private set; } = new Rectangle(0, 0, 0, 0);

        public RegionSelectorWindow(string regionName)
        {
            InitializeComponent();
            Title = $"选择{regionName}";
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
                
                // 转换为屏幕坐标
                var screenStart = PointToScreen(_startPoint);
                var screenEnd = PointToScreen(e.GetPosition(SelectionCanvas));
                
                SelectedRegion = new Rectangle(
                    (int)Math.Min(screenStart.X, screenEnd.X),
                    (int)Math.Min(screenStart.Y, screenEnd.Y),
                    (int)Math.Abs(screenEnd.X - screenStart.X),
                    (int)Math.Abs(screenEnd.Y - screenStart.Y)
                );
            }
        }

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
