using System;
using System.Windows;
using GameAssistant.Tools;

namespace GameAssistant
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // 检查是否需要生成英雄数据
            if (e.Args.Length > 0 && e.Args[0] == "generate-hero-data")
            {
                try
                {
                    Dota2HeroDataGenerator.GenerateCompleteHeroData();
                    Console.WriteLine("DOTA 2英雄数据已生成！");
                    Shutdown();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"生成失败: {ex.Message}");
                    Shutdown(1);
                }
            }
        }
    }
}
