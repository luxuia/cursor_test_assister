using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using GameAssistant.Core.Models;

namespace GameAssistant.Views
{
    public partial class OverlayWindow : Window
    {
        private bool _isMinimized = false;
        private double _originalHeight;

        public OverlayWindow()
        {
            InitializeComponent();
            _originalHeight = Height;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleMinimize();
            }
            else
            {
                DragMove();
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleMinimize();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void ToggleMinimize()
        {
            if (_isMinimized)
            {
                Height = _originalHeight;
                AdviceItemsControl.Visibility = Visibility.Visible;
                _isMinimized = false;
            }
            else
            {
                _originalHeight = Height;
                Height = 50;
                AdviceItemsControl.Visibility = Visibility.Collapsed;
                _isMinimized = true;
            }
        }

        public void UpdateAdviceList(System.Collections.Generic.List<Advice> adviceList)
        {
            Dispatcher.Invoke(() =>
            {
                AdviceItemsControl.ItemsSource = adviceList;
            });
        }

        public void UpdateStatus(string status)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = status;
            });
        }

        public void UpdateFPS(int fps)
        {
            Dispatcher.Invoke(() =>
            {
                FPSText.Text = $"FPS: {fps}";
            });
        }
    }
}
