using System;
using System.Drawing;
using System.IO;
using GameAssistant.Core.Models;
using GameAssistant.Services.Configuration;
using Newtonsoft.Json;

namespace TestConfigSerialization
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("测试配置文件读取和 Rectangle 类型的序列化/反序列化...");

            try
            {
                // 创建 ConfigurationService 实例
                var configService = new ConfigurationService();

                Console.WriteLine("\n1. 读取识别区域配置:");
                var regions = configService.GetRecognitionRegions();

                // 输出读取到的配置
                Console.WriteLine($"\t英雄阵容区域: {regions.HeroRosterRegion}");
                Console.WriteLine($"\t小地图区域: {regions.MinimapRegion}");
                Console.WriteLine($"\t装备面板区域: {regions.EquipmentPanelRegion}");
                Console.WriteLine($"\t状态栏区域: {regions.StatusBarRegion}");

                // 测试直接序列化/反序列化 RecognitionRegions
                Console.WriteLine("\n2. 测试 RecognitionRegions 的直接序列化/反序列化:");

                var testRegions = new RecognitionRegions
                {
                    HeroRosterRegion = new Rectangle(10, 10, 200, 80),
                    MinimapRegion = new Rectangle(700, 400, 300, 150),
                    EquipmentPanelRegion = new Rectangle(400, 200, 200, 350),
                    StatusBarRegion = new Rectangle(100, 450, 500, 100)
                };

                var json = JsonConvert.SerializeObject(testRegions, Formatting.Indented);
                Console.WriteLine("\t序列化结果:\n" + json);

                var deserialized = JsonConvert.DeserializeObject<RecognitionRegions>(json);
                Console.WriteLine($"\n\t反序列化后:\n" +
                    $"\t英雄阵容区域: {deserialized.HeroRosterRegion}\n" +
                    $"\t小地图区域: {deserialized.MinimapRegion}\n" +
                    $"\t装备面板区域: {deserialized.EquipmentPanelRegion}\n" +
                    $"\t状态栏区域: {deserialized.StatusBarRegion}");

                // 检查配置文件是否存在和内容
                string configPath = Path.Combine("Config", "recognition_regions.json");
                Console.WriteLine($"\n3. 配置文件存在: {File.Exists(configPath)}");

                if (File.Exists(configPath))
                {
                    string fileContent = File.ReadAllText(configPath);
                    Console.WriteLine("\t文件内容:\n" + fileContent);

                    // 尝试直接从配置文件读取
                    var regionsFromFile = JsonConvert.DeserializeObject<RecognitionRegions>(fileContent);
                    if (regionsFromFile != null)
                    {
                        Console.WriteLine($"\n\t从文件读取的区域:");
                        Console.WriteLine($"\t英雄阵容区域: {regionsFromFile.HeroRosterRegion}");
                        Console.WriteLine($"\t小地图区域: {regionsFromFile.MinimapRegion}");
                        Console.WriteLine($"\t装备面板区域: {regionsFromFile.EquipmentPanelRegion}");
                        Console.WriteLine($"\t状态栏区域: {regionsFromFile.StatusBarRegion}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n错误: {ex.Message}\n{ex.StackTrace}");
            }

            Console.WriteLine("\n测试完成。");
        }
    }
}