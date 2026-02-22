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
using GameAssistant.Tools;
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

                // 更新小地图标记：仅有效项、带序号、最多 10 条，显式构建列表避免空白行
                if (minimap != null && minimap.HeroPositions != null)
                {
                    const int maxMarkers = 10;
                    var markers = new List<MinimapMarkerItem>();
                    int index = 1;
                    foreach (var p in minimap.HeroPositions)
                    {
                        if (p == null || index > maxMarkers) break;
                        string text = $"{index}. 位置: ({p.MinimapCoordinate.X}, {p.MinimapCoordinate.Y})";
                        if (string.IsNullOrWhiteSpace(text)) continue;
                        markers.Add(new MinimapMarkerItem { Text = text });
                        index++;
                    }
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

                // 在画面上只绘制识别出来的内容（装备、技能、小地图标记）的框和序号，并隐藏四大识别区域框避免重叠
                if (_currentFrame != null)
                    UpdateAnnotatedRegionImage(_currentFrame, equipment, status, minimap);
                HeroRegionRect.Visibility = Visibility.Collapsed;
                MinimapRegionRect.Visibility = Visibility.Collapsed;
                EquipmentRegionRect.Visibility = Visibility.Collapsed;
                StatusRegionRect.Visibility = Visibility.Collapsed;
            });
        }

        /// <summary>
        /// 小地图标记列表项，用于绑定避免空白行
        /// </summary>
        private sealed class MinimapMarkerItem
        {
            public string Text { get; set; } = string.Empty;
        }

        /// <summary>
        /// 在画面上绘制识别出的装备、技能、小地图标记的框和序号（不画大区域框）。
        /// </summary>
        private void UpdateAnnotatedRegionImage(Bitmap frame, EquipmentResult? equipment, StatusResult? status, MinimapResult? minimap)
        {
            try
            {
                using var annotated = frame.Clone() as Bitmap;
                if (annotated == null) return;

                using (var g = Graphics.FromImage(annotated))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    int fontSize = Math.Max(10, annotated.Width / 80);
                    using var font = new System.Drawing.Font("Segoe UI", fontSize, System.Drawing.FontStyle.Bold);

                    // 装备：每个识别到的装备画框 + 序号 + 匹配度
                    if (equipment?.EquipmentList != null)
                    {
                        using var equipPen = new Pen(System.Drawing.Color.Lime, 2);
                        using var equipBrush = new SolidBrush(System.Drawing.Color.Lime);
                        int idx = 1;
                        foreach (var item in equipment.EquipmentList)
                        {
                            if (item.Bounds == null) continue;
                            var r = item.Bounds.Value;
                            g.DrawRectangle(equipPen, r.X, r.Y, r.Width, r.Height);
                            var numStr = idx.ToString();
                            var size = g.MeasureString(numStr, font);
                            g.DrawString(numStr, font, equipBrush, r.X, r.Y - size.Height - 1);
                            var scoreStr = $"{(int)(item.MatchScore * 100)}%";
                            g.DrawString(scoreStr, font, equipBrush, r.X, r.Y + r.Height + 1);
                            idx++;
                        }
                    }

                    // 技能：每个识别到的技能画框 + 序号 + 匹配度
                    if (status?.Skills != null)
                    {
                        using var skillPen = new Pen(System.Drawing.Color.Yellow, 2);
                        using var skillBrush = new SolidBrush(System.Drawing.Color.Yellow);
                        int idx = 1;
                        foreach (var s in status.Skills)
                        {
                            if (s.Bounds == null) continue;
                            var r = s.Bounds.Value;
                            g.DrawRectangle(skillPen, r.X, r.Y, r.Width, r.Height);
                            var numStr = idx.ToString();
                            var size = g.MeasureString(numStr, font);
                            g.DrawString(numStr, font, skillBrush, r.X, r.Y - size.Height - 1);
                            var scoreStr = $"{(int)(s.MatchScore * 100)}%";
                            g.DrawString(scoreStr, font, skillBrush, r.X, r.Y + r.Height + 1);
                            idx++;
                        }
                    }

                    // 小地图标记点：小框 + 序号
                    if (minimap?.HeroPositions != null && minimap.HeroPositions.Count > 0)
                    {
                        using var markerFont = new System.Drawing.Font("Segoe UI", Math.Max(8, annotated.Width / 120), System.Drawing.FontStyle.Bold);
                        using var markerPen = new Pen(System.Drawing.Color.Cyan, 2);
                        using var markerBrush = new SolidBrush(System.Drawing.Color.Cyan);
                        int idx = 1;
                        foreach (var pos in minimap.HeroPositions.Take(10))
                        {
                            if (pos == null) continue;
                            int x = pos.MinimapCoordinate.X, y = pos.MinimapCoordinate.Y;
                            int box = 6;
                            g.DrawRectangle(markerPen, x - box, y - box, box * 2, box * 2);
                            var numStr = idx.ToString();
                            var ms = g.MeasureString(numStr, markerFont);
                            g.DrawString(numStr, markerFont, markerBrush, x - ms.Width / 2f, y - ms.Height / 2f);
                            idx++;
                        }
                    }
                }

                using (var clone = annotated.Clone() as Bitmap)
                {
                    if (clone != null)
                        RegionImage.Source = ConvertBitmap(clone);
                }
            }
            catch
            {
                // 失败时保持原图
            }
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
                foreach (var dir in DataPathHelper.GetCandidateDataDirectories())
                {
                    // 旧格式：categories 嵌套
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
                                    if (!string.IsNullOrEmpty(id))
                                    {
                                        _itemCnById[id] = it.nameCn;
                                        if (id.StartsWith("item_", StringComparison.OrdinalIgnoreCase))
                                            _itemCnById[id.Substring(5)] = it.nameCn;
                                    }
                                }
                    }
                    // FromWeb 格式：根节点 items 数组（含 Steam API 保存的 item_xxx id，同时注册无前缀键便于匹配模板 id）
                    var itemsFromWebPath = System.IO.Path.Combine(dir, "Dota2Items_FromWeb.json");
                    if (File.Exists(itemsFromWebPath))
                    {
                        var fromWeb = JsonConvert.DeserializeObject<ItemsFromWebRoot>(File.ReadAllText(itemsFromWebPath));
                        if (fromWeb?.items != null)
                            foreach (var it in fromWeb.items)
                            {
                                if (string.IsNullOrEmpty(it.nameCn)) continue;
                                var id = (it.id ?? "").Trim();
                                if (!string.IsNullOrEmpty(id))
                                {
                                    _itemCnById[id] = it.nameCn;
                                    if (id.StartsWith("item_", StringComparison.OrdinalIgnoreCase))
                                        _itemCnById[id.Substring(5)] = it.nameCn;
                                }
                            }
                    }
                }
                foreach (var dir in DataPathHelper.GetCandidateDataDirectories())
                {
                    // 旧格式：Abilities/abilities 数组
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
                    // FromWeb 格式：根节点 abilities 数组
                    var abilitiesFromWebPath = System.IO.Path.Combine(dir, "Dota2Abilities_FromWeb.json");
                    if (File.Exists(abilitiesFromWebPath))
                    {
                        var fromWeb = JsonConvert.DeserializeObject<AbilitiesFromWebRoot>(File.ReadAllText(abilitiesFromWebPath));
                        if (fromWeb?.abilities != null)
                            foreach (var a in fromWeb.abilities)
                            {
                                if (string.IsNullOrEmpty(a.nameCn)) continue;
                                var id = (a.id ?? "").Trim();
                                if (!string.IsNullOrEmpty(id)) _abilityCnById[id] = a.nameCn;
                            }
                    }
                    // 补充：仅 id -> nameCn 的映射文件（用于补全 FromWeb 里缺中文的技能）
                    var nameCnPath = System.IO.Path.Combine(dir, "Dota2Abilities_NameCn.json");
                    if (File.Exists(nameCnPath))
                    {
                        var nameCnData = JsonConvert.DeserializeObject<AbilityNameCnRoot>(File.ReadAllText(nameCnPath));
                        if (nameCnData?.abilities != null)
                            foreach (var x in nameCnData.abilities)
                            {
                                if (string.IsNullOrEmpty(x.nameCn)) continue;
                                var id = (x.id ?? x.Id ?? "").Trim();
                                if (!string.IsNullOrEmpty(id)) _abilityCnById[id] = x.nameCn ?? x.NameCn ?? "";
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
        private class ItemsFromWebRoot { public List<ItemInfoForMerge>? items { get; set; } }
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
        private class AbilitiesFromWebRoot { public List<AbilityInfoForMerge>? abilities { get; set; } }
        private class AbilityNameCnRoot { public List<AbilityInfoForMerge>? abilities { get; set; } }

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
