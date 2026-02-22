using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using GameAssistant.Core.Models;
using GameAssistant.ViewModels;
using Microsoft.Win32;
using Newtonsoft.Json;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace GameAssistant.Views
{
    public partial class DebugWindow : System.Windows.Window
    {
        // #region agent log
        private static void DebugLog(string hypothesisId, string location, string message, object? data = null)
        {
            try
            {
                var logPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "debug-222291.log"));
                var payload = new Dictionary<string, object?>
                {
                    ["sessionId"] = "222291",
                    ["hypothesisId"] = hypothesisId,
                    ["location"] = location,
                    ["message"] = message,
                    ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };
                if (data != null) payload["data"] = data;
                File.AppendAllText(logPath, System.Text.Json.JsonSerializer.Serialize(payload) + "\n");
            }
            catch { }
        }
        // #endregion

        private readonly MainViewModel _viewModel;
        private DispatcherTimer? _autoRefreshTimer;
        private Bitmap? _currentFrame;
        private VideoCapture? _videoCapture;
        private int _videoFrameIndex;
        private int _videoFrameCount;

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
            ReleaseVideo();
            _currentFrame?.Dispose();
            _currentFrame = null;
        }

        private void ReleaseVideo()
        {
            _videoCapture?.Dispose();
            _videoCapture = null;
            _videoFrameCount = 0;
            _videoFrameIndex = 0;
            Dispatcher.Invoke(() =>
            {
                PrevFrameButton.Visibility = Visibility.Collapsed;
                NextFrameButton.Visibility = Visibility.Collapsed;
                VideoFrameLabel.Visibility = Visibility.Collapsed;
            });
        }

        private void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            CaptureAndRecognize();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            CaptureAndRecognize();
        }

        private async void UploadImageButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "图片|*.png;*.jpg;*.jpeg;*.bmp|所有文件|*.*",
                Title = "选择图片"
            };
            if (dlg.ShowDialog() != true) return;
            ReleaseVideo();
            try
            {
                StatusText.Text = "正在加载图片...";
                using var loaded = new Bitmap(dlg.FileName);
                _currentFrame?.Dispose();
                _currentFrame = loaded.Clone() as Bitmap;
                if (_currentFrame == null) { StatusText.Text = "无法加载图片"; return; }
                // #region agent log
                DebugLog("H4", "DebugWindow.UploadImage", "before UpdateFrame and Recognize", new { frameHash = _currentFrame.GetHashCode() });
                // #endregion
                UpdateFrame(_currentFrame);
                StatusText.Text = "正在识别...";
                var (hero, minimap, equipment, status) = await _viewModel.RecognizeFrameAsync(_currentFrame);
                UpdateRecognitionResults(hero, minimap, equipment, status);
                RecognitionTimeText.Text = "识别耗时: (上传图片)";
                StatusText.Text = "图片识别完成";
            }
            catch (Exception ex)
            {
                // #region agent log
                DebugLog("H4", "DebugWindow.UploadImage", "catch", new { exMsg = ex.Message });
                // #endregion
                StatusText.Text = $"错误: {ex.Message}";
            }
        }

        private async void UploadVideoButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "视频|*.mp4;*.avi;*.mkv;*.wmv;*.mov|所有文件|*.*",
                Title = "选择视频"
            };
            if (dlg.ShowDialog() != true) return;
            ReleaseVideo();
            try
            {
                StatusText.Text = "正在打开视频...";
                _videoCapture = new VideoCapture(dlg.FileName);
                _videoFrameCount = (int)_videoCapture.Get(VideoCaptureProperties.FrameCount);
                if (_videoFrameCount <= 0) { ReleaseVideo(); StatusText.Text = "无法读取视频帧数"; return; }
                _videoFrameIndex = 0;
                PrevFrameButton.Visibility = Visibility.Visible;
                NextFrameButton.Visibility = Visibility.Visible;
                VideoFrameLabel.Visibility = Visibility.Visible;
                await ShowVideoFrameAndRecognizeAsync();
            }
            catch (Exception ex)
            {
                ReleaseVideo();
                StatusText.Text = $"错误: {ex.Message}";
            }
        }

        private void PrevFrameButton_Click(object sender, RoutedEventArgs e)
        {
            if (_videoCapture == null) return;
            _videoFrameIndex = Math.Max(0, _videoFrameIndex - 1);
            _ = ShowVideoFrameAndRecognizeAsync();
        }

        private void NextFrameButton_Click(object sender, RoutedEventArgs e)
        {
            if (_videoCapture == null) return;
            _videoFrameIndex = Math.Min(_videoFrameCount - 1, _videoFrameIndex + 1);
            _ = ShowVideoFrameAndRecognizeAsync();
        }

        private async Task ShowVideoFrameAndRecognizeAsync()
        {
            if (_videoCapture == null) return;
            try
            {
                _videoCapture.Set(VideoCaptureProperties.PosFrames, _videoFrameIndex);
                using var mat = new Mat();
                if (!_videoCapture.Read(mat) || mat.Empty())
                {
                    StatusText.Text = "读取帧失败";
                    return;
                }
                using var frame = BitmapConverter.ToBitmap(mat);
                // #region agent log
                DebugLog("H4", "DebugWindow.ShowVideoFrame", "before Dispose and set _currentFrame", new { disposingHash = _currentFrame?.GetHashCode() ?? 0 });
                // #endregion
                _currentFrame?.Dispose();
                _currentFrame = frame.Clone() as Bitmap;
                if (_currentFrame == null) return;
                UpdateFrame(_currentFrame);
                VideoFrameLabel.Text = $"{_videoFrameIndex + 1} / {_videoFrameCount}";
                StatusText.Text = "正在识别...";
                var (hero, minimap, equipment, status) = await _viewModel.RecognizeFrameAsync(_currentFrame);
                UpdateRecognitionResults(hero, minimap, equipment, status);
                StatusText.Text = $"第 {_videoFrameIndex + 1} 帧识别完成";
            }
            catch (Exception ex)
            {
                // #region agent log
                DebugLog("H4", "DebugWindow.ShowVideoFrame", "catch", new { exMsg = ex.Message });
                // #endregion
                StatusText.Text = $"错误: {ex.Message}";
            }
        }

        private async void CaptureAndRecognize()
        {
            try
            {
                StatusText.Text = "正在捕获和识别...";
                var startTime = DateTime.Now;

                Bitmap? frame = await _viewModel.CaptureOneFrameAsync();
                if (frame == null)
                {
                    StatusText.Text = "未找到游戏窗口或捕获失败，请先在配置中查找窗口";
                    return;
                }

                // #region agent log
                DebugLog("H1", "DebugWindow.CaptureAndRecognize", "before Dispose _currentFrame", new { disposingHash = _currentFrame != null ? _currentFrame.GetHashCode() : 0, newFrameHash = frame.GetHashCode(), newFrameSize = $"{frame.Width}x{frame.Height}" });
                // #endregion
                _currentFrame?.Dispose();
                _currentFrame = frame;
                UpdateFrame(frame);
                // #region agent log
                DebugLog("H1", "DebugWindow.CaptureAndRecognize", "before RecognizeFrameAsync", new { frameHash = frame.GetHashCode() });
                // #endregion
                var (hero, minimap, equipment, status) = await _viewModel.RecognizeFrameAsync(frame);
                // #region agent log
                DebugLog("H1", "DebugWindow.CaptureAndRecognize", "after RecognizeFrameAsync");
                // #endregion
                UpdateRecognitionResults(hero, minimap, equipment, status);

                var recognitionTime = (DateTime.Now - startTime).TotalMilliseconds;
                RecognitionTimeText.Text = $"识别耗时: {recognitionTime:F2}ms";
                StatusText.Text = "识别完成";
            }
            catch (Exception ex)
            {
                // #region agent log
                DebugLog("H1", "DebugWindow.CaptureAndRecognize", "catch", new { exMsg = ex.Message, exType = ex.GetType().Name });
                // #endregion
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

                // 更新装备列表（合并中文名后绑定）
                if (equipment != null)
                {
                    EnsureItemAndAbilityCnMaps();
                    MergeEquipmentNameCn(equipment.EquipmentList);
                    EquipmentList.ItemsSource = equipment.EquipmentList;
                }
                else
                {
                    EquipmentList.ItemsSource = null;
                }

                // 更新状态信息（合并中文名后绑定）
                if (status != null)
                {
                    EnsureItemAndAbilityCnMaps();
                    MergeSkillNameCn(status.Skills);
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

        private static Dictionary<string, string>? _itemCnById;
        private static Dictionary<string, string>? _abilityCnById;

        private static void EnsureItemAndAbilityCnMaps()
        {
            if (_itemCnById != null && _abilityCnById != null) return;
            _itemCnById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _abilityCnById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var dataDir = System.IO.Path.Combine(baseDir, "Data");
                var curData = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Data");
                foreach (var dir in new[] { dataDir, curData })
                {
                    foreach (var fileName in new[] { "Dota2Items_Complete.json", "Dota2Items.json" })
                    {
                        var path = System.IO.Path.Combine(dir, fileName);
                        if (!File.Exists(path)) continue;
                        var data = JsonConvert.DeserializeObject<ItemDataForMerge>(File.ReadAllText(path));
                        if (data?.categories == null) continue;
                        foreach (var cat in data.categories.Values)
                            foreach (var sub in cat.Values)
                                foreach (var it in sub)
                                {
                                    if (string.IsNullOrEmpty(it.nameCn)) continue;
                                    var id = (it.id ?? "").Trim();
                                    if (!string.IsNullOrEmpty(id)) _itemCnById[id] = it.nameCn;
                                }
                    }
                }
                foreach (var dir in new[] { dataDir, curData })
                {
                    foreach (var fileName in new[] { "Dota2Abilities_Complete.json", "Dota2Abilities.json" })
                    {
                        var path = System.IO.Path.Combine(dir, fileName);
                        if (!File.Exists(path)) continue;
                        var data = JsonConvert.DeserializeObject<AbilityDataForMerge>(File.ReadAllText(path));
                        var list = data?.abilities ?? data?.Abilities;
                        if (list == null) continue;
                        foreach (var a in list)
                        {
                            var id = (a.id ?? a.Id ?? "").Trim();
                            var nameCn = a.nameCn ?? a.NameCn;
                            if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(nameCn))
                                _abilityCnById[id] = nameCn;
                        }
                    }
                }
            }
            catch { /* 忽略 */ }
        }

        private static void MergeEquipmentNameCn(IList<EquipmentInfo> list)
        {
            if (_itemCnById == null || list == null) return;
            foreach (var item in list)
            {
                if (_itemCnById.TryGetValue(item.EquipmentId, out var nameCn))
                    item.EquipmentNameCn = nameCn;
            }
        }

        private static void MergeSkillNameCn(IList<SkillStatus> list)
        {
            if (_abilityCnById == null || list == null) return;
            foreach (var item in list)
            {
                if (_abilityCnById.TryGetValue(item.SkillId, out var nameCn))
                    item.SkillNameCn = nameCn;
            }
        }

        private class ItemDataForMerge { public Dictionary<string, Dictionary<string, List<ItemInfoForMerge>>>? categories { get; set; } }
        private class ItemInfoForMerge { public string id { get; set; } = ""; public string? nameCn { get; set; } }
        private class AbilityDataForMerge
        {
            [JsonProperty("abilities")] public List<AbilityInfoForMerge>? abilities { get; set; }
            [JsonProperty("Abilities")] public List<AbilityInfoForMerge>? Abilities { get; set; }
        }
        private class AbilityInfoForMerge
        {
            [JsonProperty("id")] public string? id { get; set; }
            [JsonProperty("Id")] public string? Id { get; set; }
            [JsonProperty("nameCn")] public string? nameCn { get; set; }
            [JsonProperty("NameCn")] public string? NameCn { get; set; }
        }

        public void UpdateFrame(Bitmap frame)
        {
            Dispatcher.Invoke(() =>
            {
                // #region agent log
                DebugLog("H2", "DebugWindow.UpdateFrame", "entry", new { frameHash = frame.GetHashCode(), w = frame.Width, h = frame.Height });
                // #endregion
                _currentFrame = frame;

                // 仅用克隆生成显示用 BitmapSource，不锁原图，避免 “object is currently in use elsewhere”
                BitmapSource? bitmapSource = null;
                using (var clone = frame.Clone() as Bitmap)
                {
                    if (clone != null)
                        bitmapSource = ConvertBitmap(clone);
                }
                // #region agent log
                DebugLog("H2", "DebugWindow.UpdateFrame", "after ConvertBitmap", new { hasSource = bitmapSource != null });
                // #endregion
                if (bitmapSource != null)
                {
                    OriginalImage.Source = bitmapSource;
                    RegionImage.Source = bitmapSource;
                }

                RegionCanvas.Width = frame.Width;
                RegionCanvas.Height = frame.Height;
                RegionImage.Width = frame.Width;
                RegionImage.Height = frame.Height;
            });
        }

        private BitmapSource ConvertBitmap(Bitmap bitmap)
        {
            // #region agent log
            DebugLog("H2", "DebugWindow.ConvertBitmap", "entry", new { bitmapHash = bitmap.GetHashCode(), w = bitmap.Width, h = bitmap.Height });
            // #endregion
            var rect = new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, bitmap.PixelFormat);
            // #region agent log
            DebugLog("H2", "DebugWindow.ConvertBitmap", "after LockBits");
            // #endregion

            System.Windows.Media.PixelFormat pixelFormat;
            if (bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb
                || bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppPArgb)
            {
                pixelFormat = System.Windows.Media.PixelFormats.Bgra32;
            }
            else if (bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format24bppRgb)
            {
                pixelFormat = System.Windows.Media.PixelFormats.Bgr24;
            }
            else
            {
                bitmap.UnlockBits(bitmapData);
                using var converted = new System.Drawing.Bitmap(bitmap.Width, bitmap.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (var g = System.Drawing.Graphics.FromImage(converted))
                    g.DrawImage(bitmap, 0, 0);
                return ConvertBitmap(converted);
            }

            int bufferSize = bitmapData.Stride * bitmapData.Height;
            var buffer = new byte[bufferSize];
            System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, buffer, 0, bufferSize);
            bitmap.UnlockBits(bitmapData);
            // #region agent log
            DebugLog("H2", "DebugWindow.ConvertBitmap", "after UnlockBits");
            // #endregion

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width,
                bitmapData.Height,
                bitmap.HorizontalResolution,
                bitmap.VerticalResolution,
                pixelFormat,
                null,
                buffer,
                bitmapData.Stride);
            bitmapSource.Freeze();
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
