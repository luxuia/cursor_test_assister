using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
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

        public async Task<HeroRosterResult> RecognizeHeroRosterAsync(Bitmap frame)
        {
            return await Task.Run(() =>
            {
                var result = new HeroRosterResult();
                
                // 裁剪英雄阵容区域
                var region = _regions.HeroRosterRegion;
                if (region.Width > 0 && region.Height > 0 && 
                    region.X + region.Width <= frame.Width && 
                    region.Y + region.Height <= frame.Height)
                {
                    using var cropped = frame.Clone(region, frame.PixelFormat);
                    using var mat = BitmapConverter.ToMat(cropped);
                    
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
                
                var matchResults = _templateMatcher.Match(regionMat, $"Heroes/{heroId}", _parameters.TemplateMatchThreshold);
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
                
                // 检查是否与已有英雄位置重叠（容差20像素）
                bool isDuplicate = false;
                foreach (var (x, y) in usedPositions)
                {
                    if (Math.Abs(centerX - x) < 20 && Math.Abs(centerY - y) < 20)
                    {
                        isDuplicate = true;
                        break;
                    }
                }

                if (!isDuplicate && match.Confidence >= _parameters.TemplateMatchThreshold)
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
                    using var mat = BitmapConverter.ToMat(cropped);
                    
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

                // 定义英雄标记点的颜色范围（通常是蓝色、绿色、红色等）
                // 己方英雄通常是蓝色或绿色
                Scalar lowerBlue = new Scalar(100, 50, 50);
                Scalar upperBlue = new Scalar(130, 255, 255);
                
                Scalar lowerGreen = new Scalar(50, 50, 50);
                Scalar upperGreen = new Scalar(80, 255, 255);

                // 检测蓝色标记（己方英雄）
                Mat blueMask = new Mat();
                Cv2.InRange(hsv, lowerBlue, upperBlue, blueMask);
                
                // 检测绿色标记（己方英雄）
                Mat greenMask = new Mat();
                Cv2.InRange(hsv, lowerGreen, upperGreen, greenMask);
                
                // 合并掩码
                Mat combinedMask = new Mat();
                Cv2.BitwiseOr(blueMask, greenMask, combinedMask);

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
                    if (area < 10 || area > 500) continue; // 过滤太小或太大的区域

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

                blueMask.Dispose();
                greenMask.Dispose();
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
                    using var mat = BitmapConverter.ToMat(cropped);
                    
                    // 1. 模板匹配识别装备图标
                    result = RecognizeEquipmentByTemplate(mat, region);
                }
                
                return result;
            });
        }

        private EquipmentResult RecognizeEquipmentByTemplate(Mat regionMat, Rectangle region)
        {
            var result = new EquipmentResult();
            
            // 获取装备模板目录
            string equipmentTemplateDir = Path.Combine("Templates", "Equipment");
            if (!Directory.Exists(equipmentTemplateDir))
                return result;

            // 定义装备槽位位置（需要根据实际游戏UI调整）
            var slotPositions = new[]
            {
                new { Slot = 0, X = 0, Y = 0 }, // 示例位置，需要实际配置
                new { Slot = 1, X = 50, Y = 0 },
                new { Slot = 2, X = 100, Y = 0 },
                new { Slot = 3, X = 0, Y = 50 },
                new { Slot = 4, X = 50, Y = 50 },
                new { Slot = 5, X = 100, Y = 50 }
            };

            // 获取所有装备模板
            var templateFiles = Directory.GetFiles(equipmentTemplateDir, "*.png");
            
            foreach (var templateFile in templateFiles)
            {
                string equipmentId = Path.GetFileNameWithoutExtension(templateFile);
                _templateMatcher.LoadTemplate($"Equipment/{equipmentId}", templateFile);
                
                var matchResults = _templateMatcher.Match(regionMat, $"Equipment/{equipmentId}", _parameters.TemplateMatchThreshold);
                
                foreach (var match in matchResults)
                {
                    // 确定装备槽位（根据位置判断）
                    int slot = DetermineEquipmentSlot(match.Location, slotPositions);
                    
                    result.EquipmentList.Add(new EquipmentInfo
                    {
                        EquipmentId = equipmentId,
                        EquipmentName = equipmentId,
                        Slot = slot,
                        Properties = new Dictionary<string, object>()
                    });
                }
            }

            result.Confidence = result.EquipmentList.Count > 0 ? 0.75 : 0.0;
            return result;
        }

        private int DetermineEquipmentSlot(OpenCvSharp.Point location, dynamic[] slotPositions)
        {
            // 根据位置找到最近的槽位
            int minDistance = int.MaxValue;
            int closestSlot = 0;
            
            foreach (var slotPos in slotPositions)
            {
                int distance = (int)Math.Sqrt(
                    Math.Pow(location.X - slotPos.X, 2) + 
                    Math.Pow(location.Y - slotPos.Y, 2)
                );
                
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestSlot = slotPos.Slot;
                }
            }
            
            return closestSlot;
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
                    using var mat = BitmapConverter.ToMat(cropped);
                    
                    // 识别血量百分比
                    result.HealthPercentage = RecognizeHealthPercentage(mat);
                    
                    // 识别技能状态
                    result.Skills = RecognizeSkills(mat);
                    
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
                
                // 定义红色范围（血条通常是红色/绿色）
                // 红色血条（低血量）
                Scalar lowerRed1 = new Scalar(0, 50, 50);
                Scalar upperRed1 = new Scalar(10, 255, 255);
                
                // 红色血条（高血量，偏橙）
                Scalar lowerRed2 = new Scalar(170, 50, 50);
                Scalar upperRed2 = new Scalar(180, 255, 255);
                
                // 绿色血条（满血或高血量）
                Scalar lowerGreen = new Scalar(50, 50, 50);
                Scalar upperGreen = new Scalar(80, 255, 255);
                
                Mat redMask1 = new Mat();
                Mat redMask2 = new Mat();
                Mat greenMask = new Mat();
                
                Cv2.InRange(hsv, lowerRed1, upperRed1, redMask1);
                Cv2.InRange(hsv, lowerRed2, upperRed2, redMask2);
                Cv2.InRange(hsv, lowerGreen, upperGreen, greenMask);
                
                // 合并所有血条颜色掩码
                Mat healthMask = new Mat();
                Cv2.BitwiseOr(redMask1, redMask2, healthMask);
                Mat tempMask = new Mat();
                Cv2.BitwiseOr(healthMask, greenMask, tempMask);
                healthMask = tempMask;
                
                // 方法1：计算血条像素占比（简单但可能不准确）
                int totalPixels = statusMat.Rows * statusMat.Cols;
                int healthPixels = Cv2.CountNonZero(healthMask);
                double healthPercentageByArea = (double)healthPixels / totalPixels * 100.0;
                
                // 方法2：查找血条轮廓，计算长度（更准确）
                double healthPercentageByLength = CalculateHealthByLength(healthMask, statusMat);
                
                // 使用两种方法的平均值，或选择更准确的方法
                double healthPercentage = healthPercentageByLength > 0 ? healthPercentageByLength : healthPercentageByArea;
                
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
                double maxHealthBarWidth = originalMat.Width * 0.8; // 假设血条最大宽度为图像宽度的80%
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
        /// 识别技能状态
        /// </summary>
        private List<SkillStatus> RecognizeSkills(Mat statusMat)
        {
            var skills = new List<SkillStatus>();
            
            try
            {
                // 获取技能模板目录
                string skillsTemplateDir = Path.Combine("Templates", "Skills");
                if (!Directory.Exists(skillsTemplateDir))
                    return skills;

                // 获取所有技能模板
                var templateFiles = Directory.GetFiles(skillsTemplateDir, "*.png");
                int slotIndex = 0;

                foreach (var templateFile in templateFiles)
                {
                    string skillId = Path.GetFileNameWithoutExtension(templateFile);
                    _templateMatcher.LoadTemplate($"Skills/{skillId}", templateFile);
                    
                    var matchResults = _templateMatcher.Match(statusMat, $"Skills/{skillId}", _parameters.TemplateMatchThreshold);
                    
                    foreach (var match in matchResults)
                    {
                        // 检测技能是否可用（通过灰度值或透明度判断）
                        bool isAvailable = DetectSkillAvailability(statusMat, match);
                        
                        // 检测冷却时间（通过技能图标灰度/透明度）
                        double cooldown = DetectSkillCooldown(statusMat, match);
                        
                        skills.Add(new SkillStatus
                        {
                            SkillId = skillId,
                            SkillName = skillId,
                            IsAvailable = isAvailable,
                            CooldownSeconds = cooldown,
                            Slot = slotIndex++
                        });
                    }
                }
            }
            catch
            {
                // 忽略错误，返回空列表
            }

            return skills;
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
                Cv2.CvtColor(skillRoi, gray, ColorConversionCodes.BGR2Grayscale);
                
                // 计算平均亮度
                Scalar mean = Cv2.Mean(gray);
                double avgBrightness = mean.Val0;
                
                // 如果平均亮度很低，说明技能在冷却中（变灰）
                // 阈值可以根据实际游戏调整
                bool isAvailable = avgBrightness > 100; // 阈值可调
                
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
                Cv2.CvtColor(skillRoi, gray, ColorConversionCodes.BGR2Grayscale);
                
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
