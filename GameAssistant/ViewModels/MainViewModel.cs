using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using GameAssistant.Core.Interfaces;
using GameAssistant.Core.Models;
using GameAssistant.Services.Configuration;
using GameAssistant.Services.Database;
using GameAssistant.Services.DecisionEngine;
using GameAssistant.Services.ImageRecognition;
using GameAssistant.Services.ScreenCapture;

namespace GameAssistant.ViewModels
{
    public class MainViewModel
    {
        private readonly IScreenCapture? _screenCapture;
        private readonly IImageRecognizer _imageRecognizer;
        private readonly IDecisionEngine _decisionEngine;
        private readonly DispatcherTimer _recognitionTimer;
        private int _frameCount = 0;
        private DateTime _lastFPSTime = DateTime.Now;
        private int _currentFPS = 0;
        private long _lastRecognitionTimeMs = 0;

        public IConfigurationService ConfigurationService { get; }
        public List<Advice> CurrentAdviceList { get; private set; } = new List<Advice>();
        public string StatusText { get; private set; } = "未启动";
        public int CurrentFPS => _currentFPS;
        public long RecognitionTimeMs => _lastRecognitionTimeMs;

        public MainViewModel()
        {
            // 初始化服务
            ConfigurationService = new ConfigurationService();
            var adviceDatabase = new AdviceDatabase();
            _decisionEngine = new DecisionEngine(adviceDatabase);
            _imageRecognizer = new ImageRecognizer(ConfigurationService);

            // 初始化数据库
            Task.Run(async () => await adviceDatabase.InitializeAsync());

            // 创建识别定时器
            _recognitionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100) // 10次/秒
            };
            _recognitionTimer.Tick += RecognitionTimer_Tick;
        }

        public async Task InitializeAsync()
        {
            // 初始化完成
            StatusText = "就绪";
        }

        public void StartRecognition()
        {
            var config = ConfigurationService.GetGameWindowConfig();
            IntPtr windowHandle = config.WindowHandle;

            if (windowHandle == IntPtr.Zero)
            {
                StatusText = "错误：未找到游戏窗口";
                return;
            }

            _screenCapture = new WindowsScreenCapture(windowHandle)
            {
                TargetFPS = 60
            };
            _screenCapture.FrameCaptured += ScreenCapture_FrameCaptured;
            _screenCapture.StartCapture();

            _recognitionTimer.Start();
            StatusText = "识别中...";
        }

        public void StopRecognition()
        {
            _recognitionTimer.Stop();
            _screenCapture?.StopCapture();
            _screenCapture?.Dispose();
            StatusText = "已停止";
        }

        public void SetGameWindowHandle(IntPtr handle)
        {
            var config = ConfigurationService.GetGameWindowConfig();
            config.WindowHandle = handle;
            ConfigurationService.SaveGameWindowConfig(config);
        }

        private async void RecognitionTimer_Tick(object? sender, EventArgs e)
        {
            if (_screenCapture == null || !_screenCapture.IsCapturing)
                return;

            var parameters = ConfigurationService.GetRecognitionParameters();
            _frameCount++;

            // 根据配置的识别间隔决定是否识别
            if (_frameCount % parameters.RecognitionInterval != 0)
                return;

            try
            {
                var startTime = DateTime.Now;
                var frame = await _screenCapture.CaptureFrameAsync();
                
                if (frame == null)
                    return;

                // 并行识别各个区域
                var heroTask = _imageRecognizer.RecognizeHeroRosterAsync(frame);
                var minimapTask = _imageRecognizer.RecognizeMinimapAsync(frame);
                var equipmentTask = _imageRecognizer.RecognizeEquipmentAsync(frame);
                var statusTask = _imageRecognizer.RecognizeStatusAsync(frame);

                await Task.WhenAll(heroTask, minimapTask, equipmentTask, statusTask);

                // 构建游戏状态
                var gameState = new GameState
                {
                    HeroRoster = await heroTask,
                    Minimap = await minimapTask,
                    Equipment = await equipmentTask,
                    Status = await statusTask,
                    Phase = DetermineGamePhase(await statusTask)
                };

                // 生成建议
                var adviceList = await _decisionEngine.AnalyzeAsync(gameState);
                CurrentAdviceList = adviceList;

                // 计算耗时
                _lastRecognitionTimeMs = (long)(DateTime.Now - startTime).TotalMilliseconds;

                frame.Dispose();
            }
            catch (Exception ex)
            {
                StatusText = $"错误: {ex.Message}";
            }

            // 计算FPS
            var elapsed = (DateTime.Now - _lastFPSTime).TotalSeconds;
            if (elapsed >= 1.0)
            {
                _currentFPS = (int)(_frameCount / elapsed);
                _frameCount = 0;
                _lastFPSTime = DateTime.Now;
            }
        }

        private void ScreenCapture_FrameCaptured(object? sender, Bitmap e)
        {
            // 帧已捕获，识别逻辑在定时器中处理
        }

        private GamePhase DetermineGamePhase(StatusResult? status)
        {
            // 简单的阶段判断逻辑
            // TODO: 实现更复杂的判断
            return GamePhase.Unknown;
        }

        public string GetGameStateText()
        {
            if (CurrentAdviceList == null || !CurrentAdviceList.Any())
                return "等待识别数据...";

            var lines = new List<string>();
            lines.Add($"当前建议数量: {CurrentAdviceList.Count}");
            lines.Add("");
            lines.Add("建议列表:");
            foreach (var advice in CurrentAdviceList.Take(5))
            {
                lines.Add($"- [{advice.Type}] {advice.Content}");
            }

            return string.Join("\n", lines);
        }
    }
}
