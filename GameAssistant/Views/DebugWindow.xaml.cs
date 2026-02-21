using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using GameAssistant.Core.Models;
using GameAssistant.ViewModels;

namespace GameAssistant.Views
{
    public partial class DebugWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private DispatcherTimer? _autoRefreshTimer;
        private Bitmap? _currentFrame;

        public DebugWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            
            Loaded += DebugWindow_Loaded;
            Closing += DebugWindow_Closing;
        }

        private void DebugWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 设置自动刷新
            AutoRefreshCheckBox.Checked += (s, args) => StartAutoRefresh();
            AutoRefreshCheckBox.Unchecked += (s, args) => StopAutoRefresh();
            
            // 显示识别区域
            UpdateRegionOverlays();
        }

        private void DebugWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            StopAutoRefresh();
        }

        private void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            CaptureAndRecognize();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            CaptureAndRecognize();
        }

        private async void CaptureAndRecognize()
        {
            try
            {
                StatusText.Text = "正在捕获和识别...";
                
                // 这里需要从ViewModel获取当前的画面采集器
                // 暂时使用占位实现
                var startTime = DateTime.Now;
                
                // 模拟识别结果（实际应该调用识别服务）
                await Task.Delay(100);
                
                var recognitionTime = (DateTime.Now - startTime).TotalMilliseconds;
                RecognitionTimeText.Text = $"识别耗时: {recognitionTime:F2}ms";
                
                StatusText.Text = "识别完成";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"错误: {ex.Message}";
            }
        }

        public void UpdateRecognitionResults(
            HeroRosterResult? heroRoster,
            MinimapResult? minimap,
            EquipmentResult? equipment,
            StatusResult? status)
        {
            Dispatcher.Invoke(() =>
            {
                // 更新英雄列表
                if (heroRoster != null)
                {
                    HeroesList.ItemsSource = heroRoster.Heroes;
                }
                else
                {
                    HeroesList.ItemsSource = null;
                }

                // 更新小地图标记
                if (minimap != null)
                {
                    var markers = minimap.HeroPositions.Select(p => 
                        $"位置: ({p.MinimapCoordinate.X}, {p.MinimapCoordinate.Y})"
                    ).ToList();
                    MinimapMarkersList.ItemsSource = markers;
                }
                else
                {
                    MinimapMarkersList.ItemsSource = null;
                }

                // 更新装备列表
                if (equipment != null)
                {
                    EquipmentList.ItemsSource = equipment.EquipmentList;
                }
                else
                {
                    EquipmentList.ItemsSource = null;
                }

                // 更新状态信息
                if (status != null)
                {
                    HealthText.Text = $"血量: {status.HealthPercentage:F1}%";
                    ManaText.Text = $"魔法: {status.ManaPercentage:F1}%";
                    SkillsList.ItemsSource = status.Skills;
                }
                else
                {
                    HealthText.Text = "血量: --";
                    ManaText.Text = "魔法: --";
                    SkillsList.ItemsSource = null;
                }
            });
        }

        public void UpdateFrame(Bitmap frame)
        {
            Dispatcher.Invoke(() =>
            {
                _currentFrame = frame;
                
                // 转换为WPF ImageSource
                var bitmapSource = ConvertBitmap(frame);
                OriginalImage.Source = bitmapSource;
                RegionImage.Source = bitmapSource;
            });
        }

        private BitmapSource ConvertBitmap(Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                bitmap.PixelFormat);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width,
                bitmapData.Height,
                bitmap.HorizontalResolution,
                bitmap.VerticalResolution,
                System.Windows.Media.PixelFormats.Bgr24,
                null,
                bitmapData.Scan0,
                bitmapData.Stride * bitmapData.Height,
                bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);
            return bitmapSource;
        }

        private void UpdateRegionOverlays()
        {
            var config = _viewModel.ConfigurationService;
            var regions = config.GetRecognitionRegions();

            // 更新区域矩形显示
            UpdateRegionRect(HeroRegionRect, regions.HeroRosterRegion, "英雄阵容");
            UpdateRegionRect(MinimapRegionRect, regions.MinimapRegion, "小地图");
            UpdateRegionRect(EquipmentRegionRect, regions.EquipmentPanelRegion, "装备面板");
            UpdateRegionRect(StatusRegionRect, regions.StatusBarRegion, "状态栏");
        }

        private void UpdateRegionRect(System.Windows.Shapes.Rectangle rect, System.Drawing.Rectangle region, string label)
        {
            if (region.Width > 0 && region.Height > 0)
            {
                Canvas.SetLeft(rect, region.X);
                Canvas.SetTop(rect, region.Y);
                rect.Width = region.Width;
                rect.Height = region.Height;
                rect.Visibility = Visibility.Visible;
            }
            else
            {
                rect.Visibility = Visibility.Collapsed;
            }
        }

        private void StartAutoRefresh()
        {
            StopAutoRefresh();
            
            if (int.TryParse(RefreshIntervalTextBox.Text, out int interval) && interval > 0)
            {
                _autoRefreshTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(interval)
                };
                _autoRefreshTimer.Tick += (s, e) => CaptureAndRecognize();
                _autoRefreshTimer.Start();
            }
        }

        private void StopAutoRefresh()
        {
            _autoRefreshTimer?.Stop();
            _autoRefreshTimer = null;
        }
    }
}
