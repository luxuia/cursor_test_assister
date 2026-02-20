using System;
using System.Collections.Generic;
using System.IO;
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
        /// 模板匹配
        /// </summary>
        public List<MatchResult> Match(Mat source, string templateName, double threshold = 0.8)
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
            Mat result = new Mat();
            
            // 转换为灰度图
            Mat graySource = new Mat();
            if (source.Channels() == 3)
            {
                Cv2.CvtColor(source, graySource, ColorConversionCodes.BGR2Grayscale);
            }
            else
            {
                graySource = source.Clone();
            }

            // 模板匹配
            Cv2.MatchTemplate(graySource, template, result, TemplateMatchModes.CCoeffNormed);

            // 查找所有匹配点
            Cv2.Threshold(result, result, threshold, 1.0, ThresholdTypes.Binary);
            
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
                    new OpenCvSharp.Rect(maxLoc.X - template.Width / 2, maxLoc.Y - template.Height / 2, 
                        template.Width, template.Height), 
                    Scalar.All(0), -1);
            }

            graySource.Dispose();
            result.Dispose();

            return results;
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
