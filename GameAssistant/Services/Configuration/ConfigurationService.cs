using System;
using System.Drawing;
using System.IO;
using GameAssistant.Core.Interfaces;
using GameAssistant.Core.Models;
using Newtonsoft.Json;

namespace GameAssistant.Services.Configuration
{
    /// <summary>
    /// 配置服务实现
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private const string ConfigDirectory = "Config";
        private const string RegionsFile = "recognition_regions.json";
        private const string ParametersFile = "recognition_parameters.json";
        private const string WindowConfigFile = "game_window.json";

        public ConfigurationService()
        {
            // 确保配置目录存在
            if (!Directory.Exists(ConfigDirectory))
            {
                Directory.CreateDirectory(ConfigDirectory);
            }
        }

        public RecognitionRegions GetRecognitionRegions()
        {
            string filePath = Path.Combine(ConfigDirectory, RegionsFile);
            
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    return JsonConvert.DeserializeObject<RecognitionRegions>(json) ?? GetDefaultRegions();
                }
                catch
                {
                    return GetDefaultRegions();
                }
            }
            
            return GetDefaultRegions();
        }

        public void SaveRecognitionRegions(RecognitionRegions regions)
        {
            string filePath = Path.Combine(ConfigDirectory, RegionsFile);
            string json = JsonConvert.SerializeObject(regions, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        public RecognitionParameters GetRecognitionParameters()
        {
            string filePath = Path.Combine(ConfigDirectory, ParametersFile);
            
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    return JsonConvert.DeserializeObject<RecognitionParameters>(json) ?? GetDefaultParameters();
                }
                catch
                {
                    return GetDefaultParameters();
                }
            }
            
            return GetDefaultParameters();
        }

        public void SaveRecognitionParameters(RecognitionParameters parameters)
        {
            string filePath = Path.Combine(ConfigDirectory, ParametersFile);
            string json = JsonConvert.SerializeObject(parameters, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        public GameWindowConfig GetGameWindowConfig()
        {
            string filePath = Path.Combine(ConfigDirectory, WindowConfigFile);
            
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    return JsonConvert.DeserializeObject<GameWindowConfig>(json) ?? new GameWindowConfig();
                }
                catch
                {
                    return new GameWindowConfig();
                }
            }
            
            return new GameWindowConfig();
        }

        public void SaveGameWindowConfig(GameWindowConfig config)
        {
            string filePath = Path.Combine(ConfigDirectory, WindowConfigFile);
            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        private RecognitionRegions GetDefaultRegions()
        {
            // 默认区域配置（需要根据实际游戏调整）
            return new RecognitionRegions
            {
                HeroRosterRegion = new Rectangle(10, 10, 400, 100),
                MinimapRegion = new Rectangle(1600, 800, 300, 300),
                EquipmentPanelRegion = new Rectangle(800, 200, 400, 600),
                StatusBarRegion = new Rectangle(100, 900, 500, 150)
            };
        }

        private RecognitionParameters GetDefaultParameters()
        {
            return new RecognitionParameters
            {
                TemplateMatchThreshold = 0.8,
                OCRConfidenceThreshold = 0.7,
                ColorTolerance = 10,
                RecognitionInterval = 2,
                EnableIncrementalRecognition = true
            };
        }
    }
}
