using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using GameAssistant.Core.Models;
using GameAssistant.Services.Configuration;
using GameAssistant.Services.Database;
using GameAssistant.Services.DecisionEngine;
using GameAssistant.Services.ImageRecognition;
using GameAssistant.Services.ScreenCapture;
using GameAssistant.ViewModels;
using GameAssistant.Views;

namespace GameAssistant.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private OverlayWindow? _overlayWindow;
        private DispatcherTimer? _updateTimer;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitializeAsync();
            
            // 创建浮窗
            _overlayWindow = new OverlayWindow();
            _overlayWindow.Show();
            
            // 启动更新定时器
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100) // 10次/秒更新UI
            };
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
        }

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            if (_viewModel.CurrentAdviceList != null && _overlayWindow != null)
            {
                _overlayWindow.UpdateAdviceList(_viewModel.CurrentAdviceList);
                _overlayWindow.UpdateStatus(_viewModel.StatusText);
                _overlayWindow.UpdateFPS(_viewModel.CurrentFPS);
            }
            
            // 更新主窗口状态
            GameStateText.Text = _viewModel.GetGameStateText();
            PerformanceText.Text = $"FPS: {_viewModel.CurrentFPS}\n识别耗时: {_viewModel.RecognitionTimeMs}ms";
            StatusLabel.Text = _viewModel.StatusText;
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.StartRecognition();
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.StopRecognition();
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var ruleWindow = new RuleManagementWindow();
            ruleWindow.ShowDialog();
        }

        private void FindWindowButton_Click(object sender, RoutedEventArgs e)
        {
            string windowTitle = WindowTitleTextBox.Text;
            string processName = ProcessNameTextBox.Text;
            
            IntPtr handle = IntPtr.Zero;
            
            if (!string.IsNullOrEmpty(windowTitle))
            {
                handle = WindowFinder.FindWindowByTitle(windowTitle);
            }
            else if (!string.IsNullOrEmpty(processName))
            {
                handle = WindowFinder.FindWindowByProcessName(processName);
            }
            
            if (handle != IntPtr.Zero)
            {
                MessageBox.Show($"找到窗口: {WindowFinder.GetWindowTitle(handle)}", "成功", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                _viewModel.SetGameWindowHandle(handle);
            }
            else
            {
                MessageBox.Show("未找到匹配的窗口", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectHeroRegionButton_Click(object sender, RoutedEventArgs e)
        {
            ShowRegionSelector("英雄阵容区域", (rect) =>
            {
                var config = _viewModel.ConfigurationService;
                var regions = config.GetRecognitionRegions();
                regions.HeroRosterRegion = rect;
                config.SaveRecognitionRegions(regions);
            });
        }

        private void SelectMinimapRegionButton_Click(object sender, RoutedEventArgs e)
        {
            ShowRegionSelector("小地图区域", (rect) =>
            {
                var config = _viewModel.ConfigurationService;
                var regions = config.GetRecognitionRegions();
                regions.MinimapRegion = rect;
                config.SaveRecognitionRegions(regions);
            });
        }

        private void SelectEquipmentRegionButton_Click(object sender, RoutedEventArgs e)
        {
            ShowRegionSelector("装备面板区域", (rect) =>
            {
                var config = _viewModel.ConfigurationService;
                var regions = config.GetRecognitionRegions();
                regions.EquipmentPanelRegion = rect;
                config.SaveRecognitionRegions(regions);
            });
        }

        private void SelectStatusRegionButton_Click(object sender, RoutedEventArgs e)
        {
            ShowRegionSelector("状态栏区域", (rect) =>
            {
                var config = _viewModel.ConfigurationService;
                var regions = config.GetRecognitionRegions();
                regions.StatusBarRegion = rect;
                config.SaveRecognitionRegions(regions);
            });
        }

        private void ShowRegionSelector(string regionName, Action<Rectangle> onSelected)
        {
            var selector = new RegionSelectorWindow(regionName);
            if (selector.ShowDialog() == true)
            {
                var selectedRect = selector.SelectedRegion;
                onSelected(selectedRect);
                MessageBox.Show($"已保存{regionName}配置", "成功", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DebugButton_Click(object sender, RoutedEventArgs e)
        {
            var debugWindow = new DebugWindow(_viewModel);
            debugWindow.Show();
        }

        private void TemplateCaptureButton_Click(object sender, RoutedEventArgs e)
        {
            var templateWindow = new TemplateCaptureWindow();
            templateWindow.Show();
        }

        private void HeroDownloadButton_Click(object sender, RoutedEventArgs e)
        {
            var downloadWindow = new HeroDownloadWindow();
            downloadWindow.Show();
        }
    }
}
