using System;
using System.Drawing;
using System.Drawing.Imaging;
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

        public ImageRecognizer(IConfigurationService configurationService)
        {
            _configurationService = configurationService;
            _regions = configurationService.GetRecognitionRegions();
            _parameters = configurationService.GetRecognitionParameters();
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
                    
                    // TODO: 实现具体的英雄识别逻辑
                    // 1. 模板匹配识别英雄头像
                    // 2. OCR识别英雄名称
                    // 3. 颜色分析识别状态
                    
                    result.Confidence = 0.5; // 临时值
                }
                
                return result;
            });
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
                    
                    // TODO: 实现小地图识别逻辑
                    // 1. 颜色阈值识别英雄标记点
                    // 2. 坐标映射
                    
                    result.Confidence = 0.5; // 临时值
                }
                
                return result;
            });
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
                    
                    // TODO: 实现装备识别逻辑
                    // 1. 模板匹配识别装备图标
                    // 2. OCR识别装备名称和属性
                    
                    result.Confidence = 0.5; // 临时值
                }
                
                return result;
            });
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
                    
                    // TODO: 识别技能状态
                    // 1. 技能图标匹配
                    // 2. 冷却时间识别
                    
                    result.Confidence = 0.7; // 临时值
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
                
                // 定义红色范围（血条通常是红色）
                Scalar lowerRed = new Scalar(0, 50, 50);
                Scalar upperRed = new Scalar(10, 255, 255);
                
                Mat mask = new Mat();
                Cv2.InRange(hsv, lowerRed, upperRed, mask);
                
                // 计算血条像素占比
                int totalPixels = statusMat.Rows * statusMat.Cols;
                int healthPixels = Cv2.CountNonZero(mask);
                
                double healthPercentage = (double)healthPixels / totalPixels * 100.0;
                
                mask.Dispose();
                hsv.Dispose();
                
                return Math.Clamp(healthPercentage, 0, 100);
            }
            catch
            {
                return 100.0; // 默认值
            }
        }
    }
}
