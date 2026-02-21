using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenCvSharp;

namespace GameAssistant.Services.ImageRecognition
{
    /// <summary>
    /// 模板匹配器
    /// </summary>
    public class TemplateMatcher
    {
        private readonly Dictionary<string, Mat> _templateCache = new Dictionary<string, Mat>();
        private readonly string _templateDirectory;

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
                var template = Cv2.ImRead(path, ImreadModes.Grayscale);
                _templateCache[templateName] = template;
            }
        }

        /// <summary>
        /// 模板匹配（支持多尺度）
        /// </summary>
        public List<MatchResult> Match(Mat source, string templateName, double threshold = 0.8, bool multiScale = false)
        {
            if (!_templateCache.TryGetValue(templateName, out var template))
            {
                LoadTemplate(templateName);
                if (!_templateCache.TryGetValue(templateName, out template))
                {
                    return new List<MatchResult>();
                }
            }

            var results = new List<MatchResult>();
            
            // 转换为灰度图
            Mat graySource = new Mat();
            if (source.Channels() == 3)
            {
                Cv2.CvtColor(source, graySource, ColorConversionCodes.BGR2GRAY);
            }
            else
            {
                graySource = source.Clone();
            }

            if (multiScale)
            {
                // 多尺度匹配
                double[] scales = { 0.8, 0.9, 1.0, 1.1, 1.2 };
                foreach (double scale in scales)
                {
                    int newWidth = (int)(template.Width * scale);
                    int newHeight = (int)(template.Height * scale);
                    
                    if (newWidth > graySource.Width || newHeight > graySource.Height)
                        continue;
                    
                    Mat scaledTemplate = new Mat();
                    Cv2.Resize(template, scaledTemplate, new OpenCvSharp.Size(newWidth, newHeight));
                    
                    Mat result = new Mat();
                    Cv2.MatchTemplate(graySource, scaledTemplate, result, TemplateMatchModes.CCoeffNormed);
                    
                    FindMatches(result, scaledTemplate, threshold, results, templateName);
                    
                    scaledTemplate.Dispose();
                    result.Dispose();
                }
            }
            else
            {
                // 单尺度匹配
                Mat result = new Mat();
                Cv2.MatchTemplate(graySource, template, result, TemplateMatchModes.CCoeffNormed);
                FindMatches(result, template, threshold, results, templateName);
                result.Dispose();
            }

            graySource.Dispose();

            // 去重：如果多个匹配结果位置相近，只保留置信度最高的
            return RemoveDuplicateMatches(results);
        }

        private void FindMatches(Mat result, Mat template, double threshold, List<MatchResult> results, string templateName)
        {
            Mat binary = new Mat();
            Cv2.Threshold(result, binary, threshold, 1.0, ThresholdTypes.Binary);
            
            while (true)
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
                Cv2.Rectangle(result, 
                    new OpenCvSharp.Rect(
                        Math.Max(0, maxLoc.X - template.Width / 2), 
                        Math.Max(0, maxLoc.Y - template.Height / 2),
                        template.Width, 
                        template.Height), 
                    Scalar.All(0), -1);
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
        /// 预加载所有模板
        /// </summary>
        public void PreloadAllTemplates()
        {
            if (!Directory.Exists(_templateDirectory))
                return;

            var templateFiles = Directory.GetFiles(_templateDirectory, "*.png");
            foreach (var file in templateFiles)
            {
                string templateName = Path.GetFileNameWithoutExtension(file);
                LoadTemplate(templateName, file);
            }
        }

        /// <summary>
        /// 清理缓存
        /// </summary>
        public void ClearCache()
        {
            foreach (var template in _templateCache.Values)
            {
                template.Dispose();
            }
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
