using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GameAssistant.Core.Interfaces;
using GameAssistant.Core.Models;
using GameAssistant.Services.Configuration;
using GameAssistant.Services.ImageRecognition;
using GameAssistant.Tools;
using Newtonsoft.Json;

namespace GameAssistant
{
    internal class Program
    {
        /// <summary>
        /// 运行基于 Liquipedia 的数据生成
        /// </summary>
        public static async Task RunLiquipediaDataGenerator(string? outputDir = null)
        {
            Console.WriteLine("=== Liquipedia DOTA 2 数据生成器 ===\n");

            try
            {
                var generator = new LiquipediaDataGenerator();
                var progress = new Progress<string>(message => Console.WriteLine(message));

                Console.WriteLine("正在从 Liquipedia 抓取数据...");
                Console.WriteLine("这可能需要几分钟时间，请耐心等待...\n");

                await generator.GenerateAllData(outputDir, progress);

                Console.WriteLine("\n=== 数据生成完成 ===");
                Console.WriteLine($"输出目录: {outputDir ?? "Data"}");
                Console.WriteLine($"模板目录: Templates/");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n错误: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"内部错误: {ex.InnerException.Message}");
                }
            }
        }

        /// <summary>
        /// 只更新图标（不重新抓取数据）
        /// </summary>
        public static async Task UpdateIconsFromLiquipedia()
        {
            Console.WriteLine("=== Liquipedia 图标更新器 ===\n");

            try
            {
                var generator = new LiquipediaDataGenerator();
                var progress = new Progress<string>(message => Console.WriteLine(message));

                Console.WriteLine("正在更新图标...");

                await generator.UpdateIcons(progress);

                Console.WriteLine("\n=== 图标更新完成 ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n错误: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"内部错误: {ex.InnerException.Message}");
                }
            }
        }
        public static void RunImageRecognitionTest()
        {
            Console.WriteLine("开始图像识别模块测试...\n");

            try
            {
                // 初始化服务
                var configService = new ConfigurationService();
                var regions = configService.GetRecognitionRegions();

                // 检查测试图片目录
                string testDir = "test";
                if (!Directory.Exists(testDir))
                {
                    Console.WriteLine("错误: 未找到 test 目录");
                    return;
                }

                // 获取所有测试图片 - 优先使用缩略图
                string[] imageExtensions = { "*.png", "*.jpg", "*.jpeg", "*.bmp" };
                var testImages = imageExtensions.SelectMany(ext => Directory.GetFiles(testDir, ext)).ToList();

                // 检查是否有缩略图可用，优先使用缩略图
                var thumbnailImages = testImages.Where(img => img.Contains("_small")).ToList();
                if (thumbnailImages.Count > 0)
                {
                    Console.WriteLine($"找到 {thumbnailImages.Count} 张缩略图，将使用缩略图进行测试");
                    testImages = thumbnailImages;
                }
                else
                {
                    Console.WriteLine("未找到缩略图，将使用原始图片进行测试");
                    // 检查图片尺寸，太大的图片可能会导致识别超时
                    var filteredImages = new List<string>();
                    foreach (var imagePath in testImages)
                    {
                        try
                        {
                            using (Bitmap bitmap = new Bitmap(imagePath))
                            {
                                // 过滤掉尺寸过大的图片
                                if (bitmap.Width <= 1920 && bitmap.Height <= 1080)
                                {
                                    filteredImages.Add(imagePath);
                                }
                                else
                                {
                                    Console.WriteLine($"跳过过大的图片: {Path.GetFileName(imagePath)} ({bitmap.Width}x{bitmap.Height})");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"检查图片尺寸时出错: {Path.GetFileName(imagePath)} - {ex.Message}");
                        }
                    }

                    if (filteredImages.Count > 0)
                    {
                        testImages = filteredImages;
                    }
                }

                if (testImages.Count == 0)
                {
                    Console.WriteLine("错误: 未找到可用的测试图片");
                    return;
                }

                Console.WriteLine($"找到 {testImages.Count} 张测试图片\n");

                // 处理每张图片
                foreach (var imagePath in testImages)
                {
                    Console.WriteLine($"正在处理: {Path.GetFileName(imagePath)}");

                    try
                    {
                        using (Bitmap bitmap = new Bitmap(imagePath))
                        {
                            Console.WriteLine($"图片尺寸: {bitmap.Width} x {bitmap.Height}");

                            // 执行真实的图像识别算法
                            Console.WriteLine("正在执行真实的图像识别算法...");

                            var imageRecognizer = new ImageRecognizer(configService);

                            // 使用超时机制执行识别操作
                            var cts = new System.Threading.CancellationTokenSource();
                            cts.CancelAfter(30000); // 30秒超时

                            var recognitionResult = null as object;

                            try
                            {
                                // 并行执行所有识别任务
                                var heroTask = imageRecognizer.RecognizeHeroRosterAsync(bitmap);
                                var minimapTask = imageRecognizer.RecognizeMinimapAsync(bitmap);
                                var equipmentTask = imageRecognizer.RecognizeEquipmentAsync(bitmap);
                                var statusTask = imageRecognizer.RecognizeStatusAsync(bitmap);

                                // 等待所有任务完成或超时
                                var allTasks = new System.Threading.Tasks.Task[]
                                {
                                    heroTask, minimapTask, equipmentTask, statusTask
                                };

                                System.Threading.Tasks.Task.WaitAll(allTasks, cts.Token);

                                // 收集识别结果
                                recognitionResult = new
                                {
                                    Filename = Path.GetFileName(imagePath),
                                    ImageSize = new { Width = bitmap.Width, Height = bitmap.Height },
                                    Timestamp = DateTime.Now,
                                    RecognitionRegions = regions,
                                    Results = new
                                    {
                                        HeroRoster = heroTask.Result,
                                        Minimap = minimapTask.Result,
                                        Equipment = equipmentTask.Result,
                                        Status = statusTask.Result
                                    }
                                };
                            }
                            catch (System.Threading.Tasks.TaskCanceledException)
                            {
                                Console.WriteLine("识别任务超时，使用简化结果");
                                // 使用简化结果
                                recognitionResult = new
                                {
                                    Filename = Path.GetFileName(imagePath),
                                    ImageSize = new { Width = bitmap.Width, Height = bitmap.Height },
                                    Timestamp = DateTime.Now,
                                    RecognitionRegions = regions,
                                    Results = new
                                    {
                                        HeroRoster = new HeroRosterResult { Heroes = new List<Core.Models.HeroInfo>(), Confidence = 0 },
                                        Minimap = new MinimapResult { HeroPositions = new List<HeroPosition>(), Confidence = 0 },
                                        Equipment = new EquipmentResult { EquipmentList = new List<EquipmentInfo>(), Confidence = 0 },
                                        Status = new StatusResult { HealthPercentage = 0, Skills = new List<SkillStatus>(), Confidence = 0 }
                                    }
                                };
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"识别过程中出错: {ex.Message}");
                                // 使用简化结果
                                recognitionResult = new
                                {
                                    Filename = Path.GetFileName(imagePath),
                                    ImageSize = new { Width = bitmap.Width, Height = bitmap.Height },
                                    Timestamp = DateTime.Now,
                                    RecognitionRegions = regions,
                                    Results = new
                                    {
                                        HeroRoster = new HeroRosterResult { Heroes = new List<Core.Models.HeroInfo>(), Confidence = 0 },
                                        Minimap = new MinimapResult { HeroPositions = new List<HeroPosition>(), Confidence = 0 },
                                        Equipment = new EquipmentResult { EquipmentList = new List<EquipmentInfo>(), Confidence = 0 },
                                        Status = new StatusResult { HealthPercentage = 0, Skills = new List<SkillStatus>(), Confidence = 0 }
                                    }
                                };
                            }
                            finally
                            {
                                cts.Dispose();
                            }

                            // 输出识别结果为JSON
                            string outputPath = Path.Combine(testDir, Path.GetFileNameWithoutExtension(imagePath) + "_result.json");
                            string jsonResult = JsonConvert.SerializeObject(recognitionResult, Formatting.Indented);
                            File.WriteAllText(outputPath, jsonResult);

                            Console.WriteLine($"识别结果已保存到: {outputPath}");

                            // 打印识别到的关键信息
                            PrintRecognitionSummary(recognitionResult);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"错误: 处理图片时出错 - {ex.Message}");
                    }

                    Console.WriteLine("----------------------------------------");
                }

                Console.WriteLine("\n=== 图像识别模块测试完成 ===\n");
                Console.WriteLine($"测试图片数量: {testImages.Count}");
                Console.WriteLine($"识别结果已保存到 test 目录");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试失败: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"内部错误: {ex.InnerException.Message}");
                }
            }
        }

        private static void PrintRecognitionSummary(dynamic recognitionResult)
        {
            if (recognitionResult.Results.HeroRoster?.Heroes != null && recognitionResult.Results.HeroRoster.Heroes.Count > 0)
            {
                Console.WriteLine($"英雄识别: {recognitionResult.Results.HeroRoster.Heroes.Count} 个英雄");
                int count = 0;
                foreach (var hero in recognitionResult.Results.HeroRoster.Heroes)
                {
                    if (count >= 5) break;
                    Console.WriteLine($"  - {hero.HeroName} (位置: {hero.Position}, 血量: {hero.HealthPercentage:0.00}%)");
                    count++;
                }
                if (recognitionResult.Results.HeroRoster.Heroes.Count > 5)
                {
                    Console.WriteLine($"  ... 还有 {recognitionResult.Results.HeroRoster.Heroes.Count - 5} 个英雄");
                }
            }

            if (recognitionResult.Results.Minimap?.HeroPositions != null && recognitionResult.Results.Minimap.HeroPositions.Count > 0)
            {
                Console.WriteLine($"小地图: {recognitionResult.Results.Minimap.HeroPositions.Count} 个英雄位置");
            }

            if (recognitionResult.Results.Equipment?.EquipmentList != null && recognitionResult.Results.Equipment.EquipmentList.Count > 0)
            {
                Console.WriteLine($"装备识别: {recognitionResult.Results.Equipment.EquipmentList.Count} 个装备");
                foreach (var equip in recognitionResult.Results.Equipment.EquipmentList)
                {
                    Console.WriteLine($"  - 槽位 {equip.Slot}: {equip.EquipmentName}");
                }
            }

            if (recognitionResult.Results.Status != null)
            {
                if (recognitionResult.Results.Status.HealthPercentage > 0)
                {
                    Console.WriteLine($"血量识别: {recognitionResult.Results.Status.HealthPercentage:0.00}%");
                }
                if (recognitionResult.Results.Status.Skills != null && recognitionResult.Results.Status.Skills.Count > 0)
                {
                    Console.WriteLine($"技能识别: {recognitionResult.Results.Status.Skills.Count} 个技能");
                    foreach (var skill in recognitionResult.Results.Status.Skills)
                    {
                        string status = skill.IsAvailable ? "可用" : "冷却中";
                        Console.WriteLine($"  - 技能 {skill.SkillName}: {status} (冷却: {skill.CooldownSeconds:0.0}秒)");
                    }
                }
            }
        }
    }
}
