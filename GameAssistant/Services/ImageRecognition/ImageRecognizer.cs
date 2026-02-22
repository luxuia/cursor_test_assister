using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GameAssistant.Core.Interfaces;
using GameAssistant.Core.Models;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace GameAssistant.Services.ImageRecognition
{
    /// <summary>
    /// 图像识别器实现
    /// </summary>
    public class ImageRecognizer : IImageRecognizer
    {
        private readonly IConfigurationService _configurationService;
        private RecognitionRegions _regions;
        private RecognitionParameters _parameters;
        private readonly TemplateMatcher _templateMatcher;

        public ImageRecognizer(IConfigurationService configurationService)
        {
            _configurationService = configurationService;
            _regions = configurationService.GetRecognitionRegions();
            _parameters = configurationService.GetRecognitionParameters();
            _templateMatcher = new TemplateMatcher("Templates");
            _templateMatcher.PreloadAllTemplates();
        }

        /// <summary>
        /// 确保 Mat 为 CV_8U 深度，满足 CvtColor/MatchTemplate 等要求，避免 "depth == CV_8U" 断言失败。
        /// 始终 ConvertTo 到 8U，不依赖 Type()/Depth() 判断。
        /// </summary>
        private static Mat EnsureMat8U(Mat mat)
        {
            if (mat.Empty()) return mat;
            int c = mat.Channels();
            var targetType = c == 1 ? MatType.CV_8UC1 : (c == 3 ? MatType.CV_8UC3 : MatType.CV_8UC4);
            Mat dst = new Mat();
            var depth = mat.Depth();
            double scale = depth == MatType.CV_32F ? 255.0 : (depth == MatType.CV_16U ? (1.0 / 256.0) : 1.0);
            mat.ConvertTo(dst, targetType, scale, 0);
            return dst;
        }

        public async Task<HeroRosterResult> RecognizeHeroRosterAsync(Bitmap frame)
        {
            return await Task.Run(() =>
            {
                // #region agent log
                try { var logPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "debug-222291.log")); File.AppendAllText(logPath, JsonSerializer.Serialize(new { sessionId = "222291", hypothesisId = "H3", location = "ImageRecognizer.RecognizeHeroRosterAsync", message = "start use frame", data = new { frameHash = frame.GetHashCode(), w = frame.Width, h = frame.Height }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n"); } catch { }
                // #endregion
                var result = new HeroRosterResult();
                
                // 裁剪英雄阵容区域
                var region = _regions.HeroRosterRegion;
                if (region.Width > 0 && region.Height > 0 && 
                    region.X + region.Width <= frame.Width && 
                    region.Y + region.Height <= frame.Height)
                {
                    using var cropped = frame.Clone(region, frame.PixelFormat);
                    using var matRaw = BitmapConverter.ToMat(cropped);
                    using var mat = EnsureMat8U(matRaw);
                    
                    // 1. 模板匹配识别英雄头像
                    result = RecognizeHeroesByTemplate(mat, region);
                    
                    // 2. 颜色分析识别英雄状态（存活/死亡）
                    foreach (var hero in result.Heroes)
                    {
                        hero.IsAlive = DetectHeroAliveStatus(mat, hero);
                        hero.HealthPercentage = DetectHeroHealthPercentage(mat, hero);
                    }
                }
                
                return result;
            });
        }

        private HeroRosterResult RecognizeHeroesByTemplate(Mat regionMat, Rectangle region)
        {
            var result = new HeroRosterResult();
            
            // 获取英雄模板目录
            string heroesTemplateDir = Path.Combine("Templates", "Heroes");
            if (!Directory.Exists(heroesTemplateDir))
                return result;

            // 获取所有英雄模板
            var templateFiles = Directory.GetFiles(heroesTemplateDir, "*.png");
            var matches = new List<(MatchResult match, string heroId)>();

            foreach (var templateFile in templateFiles)
            {
                string heroId = Path.GetFileNameWithoutExtension(templateFile);
                _templateMatcher.LoadTemplate($"Heroes/{heroId}", templateFile);
                
                var matchResults = _templateMatcher.Match(regionMat, $"Heroes/{heroId}", _parameters.HeroRecognition.HeroMatchThreshold);
                foreach (var match in matchResults)
                {
                    matches.Add((match, heroId));
                }
            }

            // 按置信度排序，取最佳匹配
            var sortedMatches = matches.OrderByDescending(m => m.match.Confidence).ToList();
            
            // 去重：如果多个模板匹配到同一位置，只保留置信度最高的
            var usedPositions = new HashSet<(int x, int y)>();
            int positionIndex = 0;

            foreach (var (match, heroId) in sortedMatches)
            {
                int centerX = match.Location.X + match.TemplateSize.Width / 2;
                int centerY = match.Location.Y + match.TemplateSize.Height / 2;
                
                // 检查是否与已有英雄位置重叠
                bool isDuplicate = false;
                foreach (var (x, y) in usedPositions)
                {
                    if (Math.Abs(centerX - x) < _parameters.HeroRecognition.HeroPositionTolerance &&
                        Math.Abs(centerY - y) < _parameters.HeroRecognition.HeroPositionTolerance)
                    {
                        isDuplicate = true;
                        break;
                    }
                }

                if (!isDuplicate && match.Confidence >= _parameters.HeroRecognition.HeroMatchThreshold &&
                    result.Heroes.Count < _parameters.HeroRecognition.MaxHeroCount)
                {
                    usedPositions.Add((centerX, centerY));
                    result.Heroes.Add(new HeroInfo
                    {
                        HeroId = heroId,
                        HeroName = heroId, // 可以从配置文件映射
                        Position = positionIndex++,
                        IsAlive = true,
                        HealthPercentage = 100.0
                    });
                }
            }

            result.Confidence = result.Heroes.Count > 0 ? 0.8 : 0.0;
            return result;
        }

        private bool DetectHeroAliveStatus(Mat regionMat, HeroInfo hero)
        {
            // 简单的存活检测：检查英雄头像区域是否有足够的非黑色像素
            // 实际应该根据游戏UI特点调整
            try
            {
                // 这里需要根据实际英雄位置来计算
                // 暂时返回true，后续可以根据血条或状态图标判断
                return true;
            }
            catch
            {
                return true;
            }
        }

        private double DetectHeroHealthPercentage(Mat regionMat, HeroInfo hero)
        {
            // 检测英雄血条百分比
            // 实际实现需要根据英雄位置找到对应的血条区域
            // 暂时返回100，后续完善
            return 100.0;
        }

        public async Task<MinimapResult> RecognizeMinimapAsync(Bitmap frame)
        {
            return await Task.Run(() =>
            {
                var result = new MinimapResult();
                
                // 裁剪小地图区域
                var region = _regions.MinimapRegion;
                if (region.Width > 0 && region.Height > 0 &&
                    region.X + region.Width <= frame.Width &&
                    region.Y + region.Height <= frame.Height)
                {
                    using var cropped = frame.Clone(region, frame.PixelFormat);
                    using var matRaw = BitmapConverter.ToMat(cropped);
                    using var mat = EnsureMat8U(matRaw);
                    
                    // 1. 颜色阈值识别英雄标记点
                    result = RecognizeMinimapMarkers(mat, region);
                }
                
                return result;
            });
        }

        private MinimapResult RecognizeMinimapMarkers(Mat minimapMat, Rectangle region)
        {
            var result = new MinimapResult();
            
            try
            {
                // 转换为HSV颜色空间
                Mat hsv = new Mat();
                Cv2.CvtColor(minimapMat, hsv, ColorConversionCodes.BGR2HSV);

                // 定义英雄标记点的颜色范围（从配置读取）
                // 己方英雄标记颜色
                Scalar lowerAlly = new Scalar(
                    _parameters.MinimapRecognition.AllyMarkerColor.H - _parameters.MinimapRecognition.AllyMarkerColor.Tolerance,
                    _parameters.MinimapRecognition.AllyMarkerColor.S,
                    _parameters.MinimapRecognition.AllyMarkerColor.V);
                Scalar upperAlly = new Scalar(
                    _parameters.MinimapRecognition.AllyMarkerColor.H + _parameters.MinimapRecognition.AllyMarkerColor.Tolerance,
                    255,
                    255);

                // 敌方英雄标记颜色
                Scalar lowerEnemy = new Scalar(
                    _parameters.MinimapRecognition.EnemyMarkerColor.H - _parameters.MinimapRecognition.EnemyMarkerColor.Tolerance,
                    _parameters.MinimapRecognition.EnemyMarkerColor.S,
                    _parameters.MinimapRecognition.EnemyMarkerColor.V);
                Scalar upperEnemy = new Scalar(
                    _parameters.MinimapRecognition.EnemyMarkerColor.H + _parameters.MinimapRecognition.EnemyMarkerColor.Tolerance,
                    255,
                    255);

                // 检测己方英雄标记
                Mat allyMask = new Mat();
                Cv2.InRange(hsv, lowerAlly, upperAlly, allyMask);

                // 检测敌方英雄标记
                Mat enemyMask = new Mat();
                Cv2.InRange(hsv, lowerEnemy, upperEnemy, enemyMask);

                // 合并掩码
                Mat combinedMask = new Mat();
                Cv2.BitwiseOr(allyMask, enemyMask, combinedMask);

                // 查找轮廓
                OpenCvSharp.Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(combinedMask, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                int markerIndex = 0;
                foreach (var contour in contours)
                {
                    if (contour.Length < 5) continue; // 至少需要5个点才能拟合椭圆

                    // 计算轮廓面积
                    double area = Cv2.ContourArea(contour);
                    if (area < _parameters.MinimapRecognition.MinMarkerArea || area > _parameters.MinimapRecognition.MaxMarkerArea) continue; // 过滤太小或太大的区域

                    // 拟合椭圆获取中心点
                    RotatedRect ellipse = Cv2.FitEllipse(contour);
                    var center = ellipse.Center;

                    // 转换为相对于原图的坐标
                    var minimapCoord = new System.Drawing.Point(
                        (int)(center.X + region.X),
                        (int)(center.Y + region.Y)
                    );

                    result.HeroPositions.Add(new HeroPosition
                    {
                        HeroId = $"minimap_marker_{markerIndex++}",
                        MinimapCoordinate = minimapCoord
                    });
                }

                // 合并距离过近的标记并限制数量，减少噪点与空白行
                const int mergeDistancePx = 12;
                var merged = new List<HeroPosition>();
                foreach (var pos in result.HeroPositions)
                {
                    if (merged.Count >= 10) break;
                    var p = pos.MinimapCoordinate;
                    bool tooClose = merged.Any(m =>
                        Math.Abs(m.MinimapCoordinate.X - p.X) <= mergeDistancePx &&
                        Math.Abs(m.MinimapCoordinate.Y - p.Y) <= mergeDistancePx);
                    if (!tooClose)
                        merged.Add(pos);
                }
                result.HeroPositions = merged;

                allyMask.Dispose();
                enemyMask.Dispose();
                combinedMask.Dispose();
                hsv.Dispose();

                result.Confidence = result.HeroPositions.Count > 0 ? 0.7 : 0.0;
            }
            catch
            {
                result.Confidence = 0.0;
            }

            return result;
        }

        public async Task<EquipmentResult> RecognizeEquipmentAsync(Bitmap frame)
        {
            return await Task.Run(() =>
            {
                var result = new EquipmentResult();
                
                // 裁剪装备面板区域
                var region = _regions.EquipmentPanelRegion;
                if (region.Width > 0 && region.Height > 0 &&
                    region.X + region.Width <= frame.Width &&
                    region.Y + region.Height <= frame.Height)
                {
                    using var cropped = frame.Clone(region, frame.PixelFormat);
                    using var matRaw = BitmapConverter.ToMat(cropped);
                    using var mat = EnsureMat8U(matRaw);
                    
                    // 1. 模板匹配识别装备图标
                    result = RecognizeEquipmentByTemplate(mat, region);
                }
                
                return result;
            });
        }

        private EquipmentResult RecognizeEquipmentByTemplate(Mat regionMat, Rectangle region)
        {
            var result = new EquipmentResult();
            string equipmentTemplateDir = Path.Combine("Templates", "Equipment");
            if (!Directory.Exists(equipmentTemplateDir))
                return result;

            using Mat grayRegion = ToGrayOnce(regionMat);
            var templateFiles = Directory.GetFiles(equipmentTemplateDir, "*.png");
            var allCandidates = new ConcurrentBag<EquipmentInfo>();
            double threshold = _parameters.EquipmentRecognition.EquipmentMatchThreshold;

            Parallel.ForEach(templateFiles, templateFile =>
            {
                string equipmentId = Path.GetFileNameWithoutExtension(templateFile);
                _templateMatcher.LoadTemplate($"Equipment/{equipmentId}", templateFile);
                var matchResults = _templateMatcher.MatchGray(grayRegion, $"Equipment/{equipmentId}", threshold, multiScale: true);
                foreach (var match in matchResults)
                {
                    allCandidates.Add(new EquipmentInfo
                    {
                        EquipmentId = equipmentId,
                        EquipmentName = equipmentId,
                        Slot = 0,
                        Bounds = new System.Drawing.Rectangle(region.X + match.Location.X, region.Y + match.Location.Y, match.TemplateSize.Width, match.TemplateSize.Height),
                        MatchScore = match.Confidence,
                        Properties = new Dictionary<string, object>()
                    });
                }
            });

            result.EquipmentList = DedupeOverlappingEquipment(allCandidates.ToList(), maxCount: 10);
            for (int i = 0; i < result.EquipmentList.Count; i++)
                result.EquipmentList[i].Slot = i;

            result.Confidence = result.EquipmentList.Count > 0 ? 0.75 : 0.0;
            return result;
        }

        /// <summary>将区域 Mat 转为灰度（CV_8UC1），仅分配一次供多模板复用。</summary>
        private static Mat ToGrayOnce(Mat source)
        {
            if (source.Channels() == 1 && source.Depth() == MatType.CV_8U)
                return source.Clone();
            var gray = new Mat();
            if (source.Channels() == 3)
                Cv2.CvtColor(source, gray, ColorConversionCodes.BGR2GRAY);
            else if (source.Channels() == 4)
                Cv2.CvtColor(source, gray, ColorConversionCodes.BGRA2GRAY);
            else
                gray = source.Clone();
            return gray;
        }

        /// <summary>
        /// 重叠去重：按匹配度从高到低取，与已保留框重叠的丢弃，最多 maxCount 条
        /// </summary>
        private static List<EquipmentInfo> DedupeOverlappingEquipment(List<EquipmentInfo> candidates, int maxCount)
        {
            var sorted = candidates.OrderByDescending(e => e.MatchScore).ToList();
            var kept = new List<EquipmentInfo>();
            const double overlapCenterThreshold = 25; // 两框中心距离小于此视为重叠

            foreach (var c in sorted)
            {
                if (kept.Count >= maxCount) break;
                var r = c.Bounds;
                if (r == null) continue;
                double cx = r.Value.X + r.Value.Width / 2.0;
                double cy = r.Value.Y + r.Value.Height / 2.0;
                bool overlaps = false;
                foreach (var k in kept)
                {
                    if (k.Bounds == null) continue;
                    var kr = k.Bounds.Value;
                    double kcx = kr.X + kr.Width / 2.0;
                    double kcy = kr.Y + kr.Height / 2.0;
                    double dist = Math.Sqrt((cx - kcx) * (cx - kcx) + (cy - kcy) * (cy - kcy));
                    if (dist < overlapCenterThreshold) { overlaps = true; break; }
                }
                if (!overlaps) kept.Add(c);
            }
            return kept;
        }

        public async Task<StatusResult> RecognizeStatusAsync(Bitmap frame)
        {
            return await Task.Run(() =>
            {
                var result = new StatusResult();
                
                // 裁剪状态栏区域
                var region = _regions.StatusBarRegion;
                if (region.Width > 0 && region.Height > 0 &&
                    region.X + region.Width <= frame.Width &&
                    region.Y + region.Height <= frame.Height)
                {
                    using var cropped = frame.Clone(region, frame.PixelFormat);
                    using var matRaw = BitmapConverter.ToMat(cropped);
                    using var mat = EnsureMat8U(matRaw);
                    
                    // 识别血量百分比
                    result.HealthPercentage = RecognizeHealthPercentage(mat);
                    
                    // 识别技能状态（传入区域以便计算 Bounds）
                    result.Skills = RecognizeSkills(mat, region);
                    
                    result.Confidence = 0.7;
                }
                
                return result;
            });
        }

        /// <summary>
        /// 识别血量百分比（基于血条颜色和长度）
        /// </summary>
        private double RecognizeHealthPercentage(Mat statusMat)
        {
            try
            {
                // 转换为HSV颜色空间，便于识别红色血条
                Mat hsv = new Mat();
                Cv2.CvtColor(statusMat, hsv, ColorConversionCodes.BGR2HSV);
                
                // 定义血量颜色范围
                // 低血量颜色（红色/橙色）
                Scalar lowerLowHealth = new Scalar(
                    _parameters.HealthRecognition.LowHealthColor.H - _parameters.HealthRecognition.LowHealthColor.Tolerance,
                    _parameters.HealthRecognition.LowHealthColor.S,
                    _parameters.HealthRecognition.LowHealthColor.V);
                Scalar upperLowHealth = new Scalar(
                    _parameters.HealthRecognition.LowHealthColor.H + _parameters.HealthRecognition.LowHealthColor.Tolerance,
                    255,
                    255);

                // 高血量颜色（绿色）
                Scalar lowerHighHealth = new Scalar(
                    _parameters.HealthRecognition.HighHealthColor.H - _parameters.HealthRecognition.HighHealthColor.Tolerance,
                    _parameters.HealthRecognition.HighHealthColor.S,
                    _parameters.HealthRecognition.HighHealthColor.V);
                Scalar upperHighHealth = new Scalar(
                    _parameters.HealthRecognition.HighHealthColor.H + _parameters.HealthRecognition.HighHealthColor.Tolerance,
                    255,
                    255);
                
                Mat redMask1 = new Mat();
                Mat redMask2 = new Mat();
                Mat greenMask = new Mat();
                
                // 检测血量颜色
                Mat lowHealthMask = new Mat();
                Mat highHealthMask = new Mat();

                Cv2.InRange(hsv, lowerLowHealth, upperLowHealth, lowHealthMask);
                Cv2.InRange(hsv, lowerHighHealth, upperHighHealth, highHealthMask);

                // 合并所有血条颜色掩码
                Mat healthMask = new Mat();
                Cv2.BitwiseOr(lowHealthMask, highHealthMask, healthMask);
                
                double healthPercentage = 0;

                switch (_parameters.HealthRecognition.RecognitionMethod)
                {
                    case HealthRecognitionMethod.PixelCount:
                        // 方法1：计算血条像素占比
                        int totalPixels = statusMat.Rows * statusMat.Cols;
                        int healthPixels = Cv2.CountNonZero(healthMask);
                        healthPercentage = (double)healthPixels / totalPixels * 100.0;
                        break;

                    case HealthRecognitionMethod.LengthMeasurement:
                        // 方法2：查找血条轮廓，计算长度（更准确）
                        healthPercentage = CalculateHealthByLength(healthMask, statusMat);
                        break;

                    case HealthRecognitionMethod.Combined:
                        // 方法3：综合使用两种方法
                        int totalPixelsCombined = statusMat.Rows * statusMat.Cols;
                        int healthPixelsCombined = Cv2.CountNonZero(healthMask);
                        double pixelPercentage = (double)healthPixelsCombined / totalPixelsCombined * 100.0;

                        double lengthPercentage = CalculateHealthByLength(healthMask, statusMat);
                        healthPercentage = (pixelPercentage + lengthPercentage) / 2;
                        break;
                }
                
                redMask1.Dispose();
                redMask2.Dispose();
                greenMask.Dispose();
                healthMask.Dispose();
                hsv.Dispose();
                
                return Math.Clamp(healthPercentage, 0, 100);
            }
            catch
            {
                return 100.0; // 默认值
            }
        }

        /// <summary>
        /// 通过血条长度计算血量百分比
        /// </summary>
        private double CalculateHealthByLength(Mat healthMask, Mat originalMat)
        {
            try
            {
                // 查找血条轮廓
                OpenCvSharp.Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(healthMask, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);
                
                if (contours.Length == 0)
                    return 0;
                
                // 找到最大的轮廓（通常是血条）
                var largestContour = contours.OrderByDescending(c => Cv2.ContourArea(c)).First();
                
                // 获取血条的边界矩形
                var boundingRect = Cv2.BoundingRect(largestContour);
                
                // 假设血条是水平的，计算长度占比
                // 需要知道血条的最大长度（可以从配置或模板获取）
                double maxHealthBarWidth = originalMat.Width * _parameters.HealthRecognition.MaxHealthBarWidthRatio;
                double currentHealthBarWidth = boundingRect.Width;
                
                double healthPercentage = (currentHealthBarWidth / maxHealthBarWidth) * 100.0;
                
                return Math.Clamp(healthPercentage, 0, 100);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 识别技能状态：直接匹配 + 重叠去重，不做槽位预计算。statusRegion 为状态栏在原图中的区域。
        /// </summary>
        private List<SkillStatus> RecognizeSkills(Mat statusMat, System.Drawing.Rectangle statusRegion)
        {
            var allMatches = new ConcurrentBag<(MatchResult match, string skillId)>();
            try
            {
                string skillsTemplateDir = Path.Combine("Templates", "Skills");
                if (!Directory.Exists(skillsTemplateDir))
                    return new List<SkillStatus>();

                var templateFiles = Directory.GetFiles(skillsTemplateDir, "*.png");
                double threshold = _parameters.SkillRecognition.SkillMatchThreshold;
                using Mat grayStatus = ToGrayOnce(statusMat);

                Parallel.ForEach(templateFiles, templateFile =>
                {
                    string skillId = Path.GetFileNameWithoutExtension(templateFile);
                    _templateMatcher.LoadTemplate($"Skills/{skillId}", templateFile);
                    var matchResults = _templateMatcher.MatchGray(grayStatus, $"Skills/{skillId}", threshold, multiScale: true);
                    foreach (var match in matchResults)
                        allMatches.Add((match, skillId));
                });
            }
            catch
            {
                return new List<SkillStatus>();
            }

            int maxSlots = Math.Min(10, Math.Max(1, _parameters.SkillRecognition.MaxSkillCount));
            var kept = DedupeOverlappingSkillMatches(allMatches.ToList(), maxSlots, centerThresholdPx: 22);

            var skills = new List<SkillStatus>();
            for (int i = 0; i < kept.Count; i++)
            {
                var (match, skillId) = kept[i];
                bool isAvailable = DetectSkillAvailability(statusMat, match);
                double cooldown = DetectSkillCooldown(statusMat, match);
                skills.Add(new SkillStatus
                {
                    SkillId = skillId,
                    SkillName = skillId,
                    IsAvailable = isAvailable,
                    CooldownSeconds = cooldown,
                    Slot = i,
                    Bounds = new System.Drawing.Rectangle(statusRegion.X + match.Location.X, statusRegion.Y + match.Location.Y, match.TemplateSize.Width, match.TemplateSize.Height),
                    MatchScore = match.Confidence
                });
            }
            return skills;
        }

        /// <summary>
        /// 技能匹配重叠去重：按置信度排序，中心距离过近的只保留最高分，最多 maxCount 条
        /// </summary>
        private static List<(MatchResult match, string skillId)> DedupeOverlappingSkillMatches(
            List<(MatchResult match, string skillId)> candidates, int maxCount, double centerThresholdPx)
        {
            var sorted = candidates.OrderByDescending(x => x.match.Confidence).ToList();
            var kept = new List<(MatchResult match, string skillId)>();
            foreach (var c in sorted)
            {
                if (kept.Count >= maxCount) break;
                double cx = c.match.Location.X + c.match.TemplateSize.Width / 2.0;
                double cy = c.match.Location.Y + c.match.TemplateSize.Height / 2.0;
                bool overlaps = kept.Any(k =>
                {
                    double kcx = k.match.Location.X + k.match.TemplateSize.Width / 2.0;
                    double kcy = k.match.Location.Y + k.match.TemplateSize.Height / 2.0;
                    return Math.Sqrt((cx - kcx) * (cx - kcx) + (cy - kcy) * (cy - kcy)) < centerThresholdPx;
                });
                if (!overlaps) kept.Add(c);
            }
            return kept;
        }

        /// <summary>
        /// 检测技能是否可用
        /// </summary>
        private bool DetectSkillAvailability(Mat statusMat, MatchResult match)
        {
            try
            {
                // 提取技能图标区域
                var roi = new OpenCvSharp.Rect(
                    match.Location.X,
                    match.Location.Y,
                    match.TemplateSize.Width,
                    match.TemplateSize.Height
                );
                
                if (roi.X + roi.Width > statusMat.Width || roi.Y + roi.Height > statusMat.Height)
                    return false;

                Mat skillRoi = new Mat(statusMat, roi);
                Mat gray = new Mat();
                Cv2.CvtColor(skillRoi, gray, ColorConversionCodes.BGR2GRAY);
                
                // 计算平均亮度
                Scalar mean = Cv2.Mean(gray);
                double avgBrightness = mean.Val0;
                
                // 如果平均亮度很低，说明技能在冷却中（变灰）
                bool isAvailable = avgBrightness > _parameters.SkillRecognition.SkillAvailabilityThreshold;
                
                skillRoi.Dispose();
                gray.Dispose();
                
                return isAvailable;
            }
            catch
            {
                return true; // 默认可用
            }
        }

        /// <summary>
        /// 检测技能冷却时间
        /// </summary>
        private double DetectSkillCooldown(Mat statusMat, MatchResult match)
        {
            // 简单的冷却时间检测：通过技能图标的灰度值估算
            // 实际实现可能需要OCR识别冷却数字，或通过动画帧判断
            try
            {
                var roi = new OpenCvSharp.Rect(
                    match.Location.X,
                    match.Location.Y,
                    match.TemplateSize.Width,
                    match.TemplateSize.Height
                );
                
                if (roi.X + roi.Width > statusMat.Width || roi.Y + roi.Height > statusMat.Height)
                    return 0;

                Mat skillRoi = new Mat(statusMat, roi);
                Mat gray = new Mat();
                Cv2.CvtColor(skillRoi, gray, ColorConversionCodes.BGR2GRAY);
                
                Scalar mean = Cv2.Mean(gray);
                double avgBrightness = mean.Val0;
                
                // 如果技能可用，冷却时间为0
                if (avgBrightness > 100)
                    return 0;
                
                // 否则估算冷却时间（这里只是示例，实际需要更复杂的算法）
                // 可以根据灰度值线性估算，或使用OCR识别数字
                double cooldownEstimate = (100 - avgBrightness) / 10.0; // 简单估算
                
                skillRoi.Dispose();
                gray.Dispose();
                
                return Math.Clamp(cooldownEstimate, 0, 60); // 限制在0-60秒
            }
            catch
            {
                return 0;
            }
        }
    }
}
