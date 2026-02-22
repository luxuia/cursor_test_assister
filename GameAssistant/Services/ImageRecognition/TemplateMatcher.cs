using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using OpenCvSharp;

namespace GameAssistant.Services.ImageRecognition
{
    /// <summary>
    /// 模板匹配器（支持多尺度预计算与灰度图重载以加速匹配）
    /// </summary>
    public class TemplateMatcher
    {
        private readonly Dictionary<string, Mat> _templateCache = new Dictionary<string, Mat>();
        private readonly string _templateDirectory;

        /// <summary>多尺度因子（与优化前一致，覆盖约 0.75x～1.25x UI 缩放，预计算后不牺牲识别数量）</summary>
        private static readonly double[] MultiScaleFactors = { 0.75, 0.85, 0.95, 1.0, 1.08, 1.16, 1.25 };

        private static string ScaleCacheKey(string templateName, double scale) =>
            templateName + "_s" + (scale == 1.0 ? "1" : scale.ToString("G", CultureInfo.InvariantCulture));

        public TemplateMatcher(string templateDirectory = "Templates")
        {
            _templateDirectory = templateDirectory;
            
            // 确保模板目录存在
            if (!Directory.Exists(_templateDirectory))
            {
                Directory.CreateDirectory(_templateDirectory);
            }
        }

        /// <summary>
        /// 加载模板到缓存
        /// </summary>
        public void LoadTemplate(string templateName, string? filePath = null)
        {
            if (_templateCache.ContainsKey(templateName))
                return;

            string path = filePath ?? Path.Combine(_templateDirectory, $"{templateName}.png");
            
            if (File.Exists(path))
            {
                var loaded = Cv2.ImRead(path, ImreadModes.Grayscale);
                if (!loaded.Empty())
                {
                    var template = EnsureTemplate8U(loaded);
                    if (template != loaded)
                        loaded.Dispose();
                    _templateCache[templateName] = template;
                    // 装备/技能多尺度预计算，避免每帧 Resize
                    if (templateName.StartsWith("Equipment/", StringComparison.Ordinal) || templateName.StartsWith("Skills/", StringComparison.Ordinal))
                    {
                        foreach (double s in MultiScaleFactors)
                        {
                            string key = ScaleCacheKey(templateName, s);
                            if (_templateCache.ContainsKey(key)) continue;
                            if (s == 1.0)
                                _templateCache[key] = template;
                            else
                            {
                                var scaled = new Mat();
                                Cv2.Resize(template, scaled, new OpenCvSharp.Size((int)(template.Width * s), (int)(template.Height * s)));
                                _templateCache[key] = scaled;
                            }
                        }
                    }
                }
            }
        }

        private static Mat EnsureTemplate8U(Mat mat)
        {
            if (mat.Empty()) return mat;
            if (mat.Depth() == MatType.CV_8U) return mat;
            Mat dst = new Mat();
            double scale = (mat.Depth() == MatType.CV_32F) ? 255.0 : (mat.Depth() == MatType.CV_16U ? (1.0 / 256.0) : 1.0);
            mat.ConvertTo(dst, MatType.CV_8UC1, scale, 0);
            return dst;
        }

        /// <summary>
        /// 模板匹配（支持多尺度）。内部转灰度后调用 MatchGray。
        /// </summary>
        public List<MatchResult> Match(Mat source, string templateName, double threshold = 0.8, bool multiScale = false)
        {
            if (!_templateCache.TryGetValue(templateName, out _))
            {
                LoadTemplate(templateName);
                if (!_templateCache.TryGetValue(templateName, out _))
                    return new List<MatchResult>();
            }

            Mat source8u = source;
            bool needDisposeSource = false;
            if (source.Depth() != MatType.CV_8U)
            {
                source8u = new Mat();
                double scale = (source.Depth() == MatType.CV_32F) ? 255.0 : (source.Depth() == MatType.CV_16U ? (1.0 / 256.0) : 1.0);
                int c = source.Channels();
                var t = c == 1 ? MatType.CV_8UC1 : (c == 3 ? MatType.CV_8UC3 : MatType.CV_8UC4);
                source.ConvertTo(source8u, t, scale, 0);
                needDisposeSource = true;
            }

            Mat graySource = new Mat();
            if (source8u.Channels() == 3)
                Cv2.CvtColor(source8u, graySource, ColorConversionCodes.BGR2GRAY);
            else if (source8u.Channels() == 4)
                Cv2.CvtColor(source8u, graySource, ColorConversionCodes.BGRA2GRAY);
            else
                graySource = source8u.Clone();

            try
            {
                return MatchGray(graySource, templateName, threshold, multiScale);
            }
            finally
            {
                graySource.Dispose();
                if (needDisposeSource)
                    source8u.Dispose();
            }
        }

        /// <summary>
        /// 使用已为灰度的图像进行匹配（调用方保证 graySource 为 CV_8UC1）。用于区域只转一次灰度并并行匹配时加速。
        /// </summary>
        public List<MatchResult> MatchGray(Mat graySource, string templateName, double threshold = 0.8, bool multiScale = false)
        {
            if (!_templateCache.TryGetValue(templateName, out var template))
            {
                LoadTemplate(templateName);
                if (!_templateCache.TryGetValue(templateName, out template))
                    return new List<MatchResult>();
            }

            var results = new List<MatchResult>();

            if (multiScale)
            {
                foreach (double scale in MultiScaleFactors)
                {
                    string key = ScaleCacheKey(templateName, scale);
                    if (!_templateCache.TryGetValue(key, out var scaledTemplate))
                        scaledTemplate = template;
                    if (scaledTemplate.Width > graySource.Width || scaledTemplate.Height > graySource.Height)
                        continue;
                    Mat result = new Mat();
                    Cv2.MatchTemplate(graySource, scaledTemplate, result, TemplateMatchModes.CCoeffNormed);
                    FindMatches(result, scaledTemplate, threshold, results, templateName);
                    result.Dispose();
                }
            }
            else
            {
                Mat result = new Mat();
                Cv2.MatchTemplate(graySource, template, result, TemplateMatchModes.CCoeffNormed);
                FindMatches(result, template, threshold, results, templateName);
                result.Dispose();
            }

            return RemoveDuplicateMatches(results);
        }

        private void FindMatches(Mat result, Mat template, double threshold, List<MatchResult> results, string templateName)
        {
            Mat binary = new Mat();
            Cv2.Threshold(result, binary, threshold, 1.0, ThresholdTypes.Binary);

            int maxAttempts = 100; // 防止无限循环的安全措施
            int attempts = 0;

            while (attempts < maxAttempts)
            {
                double minVal, maxVal;
                OpenCvSharp.Point minLoc, maxLoc;
                Cv2.MinMaxLoc(result, out minVal, out maxVal, out minLoc, out maxLoc);

                if (maxVal < threshold)
                    break;

                results.Add(new MatchResult
                {
                    Location = maxLoc,
                    Confidence = maxVal,
                    TemplateName = templateName,
                    TemplateSize = new OpenCvSharp.Size(template.Width, template.Height)
                });

                // 将已匹配的区域置零，避免重复匹配
                // 确保矩形区域在图像边界内
                var rect = new OpenCvSharp.Rect(
                    maxLoc.X,
                    maxLoc.Y,
                    template.Width,
                    template.Height);

                // 确保矩形在图像边界内
                if (rect.X < 0) rect.X = 0;
                if (rect.Y < 0) rect.Y = 0;
                if (rect.X + rect.Width > result.Width)
                    rect.Width = result.Width - rect.X;
                if (rect.Y + rect.Height > result.Height)
                    rect.Height = result.Height - rect.Y;

                Cv2.Rectangle(result, rect, Scalar.All(0), -1);

                attempts++;
            }

            if (attempts >= maxAttempts)
            {
                Console.WriteLine("警告: 匹配尝试次数达到最大值，可能存在无限循环");
            }

            binary.Dispose();
        }

        private List<MatchResult> RemoveDuplicateMatches(List<MatchResult> matches)
        {
            if (matches.Count <= 1)
                return matches;

            var uniqueMatches = new List<MatchResult>();
            var used = new bool[matches.Count];

            for (int i = 0; i < matches.Count; i++)
            {
                if (used[i]) continue;

                var currentMatch = matches[i];
                var group = new List<MatchResult> { currentMatch };
                used[i] = true;

                // 查找位置相近的匹配
                for (int j = i + 1; j < matches.Count; j++)
                {
                    if (used[j]) continue;

                    var otherMatch = matches[j];
                    int distance = (int)Math.Sqrt(
                        Math.Pow(currentMatch.Location.X - otherMatch.Location.X, 2) +
                        Math.Pow(currentMatch.Location.Y - otherMatch.Location.Y, 2)
                    );

                    // 如果距离小于阈值（20像素），认为是重复
                    if (distance < 20)
                    {
                        group.Add(otherMatch);
                        used[j] = true;
                    }
                }

                // 保留置信度最高的
                var bestMatch = group.OrderByDescending(m => m.Confidence).First();
                uniqueMatches.Add(bestMatch);
            }

            return uniqueMatches;
        }

        /// <summary>
        /// 预加载所有模板（包含子目录，模板名为相对 Templates 的路径，如 Heroes/axe）
        /// </summary>
        public void PreloadAllTemplates()
        {
            if (!Directory.Exists(_templateDirectory))
                return;

            var templateFiles = Directory.GetFiles(_templateDirectory, "*.png", SearchOption.AllDirectories);
            foreach (var file in templateFiles)
            {
                string rel = Path.GetRelativePath(_templateDirectory, file).Replace('\\', '/');
                string templateName = rel.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                    ? rel.Substring(0, rel.Length - 4)
                    : rel;
                LoadTemplate(templateName, file);
            }
        }

        /// <summary>
        /// 清理缓存（多尺度与基模板可能共享同一 Mat 引用，仅对引用去重后 Dispose）
        /// </summary>
        public void ClearCache()
        {
            foreach (var template in _templateCache.Values.Distinct())
                template.Dispose();
            _templateCache.Clear();
        }
    }

    /// <summary>
    /// 匹配结果
    /// </summary>
    public class MatchResult
    {
        public OpenCvSharp.Point Location { get; set; }
        public double Confidence { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public OpenCvSharp.Size TemplateSize { get; set; }
    }
}
