using System;
using System.Threading.Tasks;
using System.Windows;
using GameAssistant.Tools;

namespace GameAssistant
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 检查是否需要从 Liquipedia 生成数据
            if (e.Args.Length > 0 && e.Args[0] == "generate-hero-data")
            {
                try
                {
                    string outputDir = e.Args.Length > 1 ? e.Args[1] : null;
                    await Program.RunLiquipediaDataGenerator(outputDir);
                    Shutdown();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"生成失败: {ex.Message}");
                    Shutdown(1);
                }
            }
            // 检查是否需要更新图标
            else if (e.Args.Length > 0 && e.Args[0] == "update-icons")
            {
                try
                {
                    await Program.UpdateIconsFromLiquipedia();
                    Shutdown();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"更新失败: {ex.Message}");
                    Shutdown(1);
                }
            }
            // 检查是否需要运行图像识别测试
            else if (e.Args.Length > 0 && e.Args[0] == "test-recognition")
            {
                try
                {
                    Program.RunImageRecognitionTest();
                    Shutdown();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"测试失败: {ex.Message}");
                    Shutdown(1);
                }
            }
        }
    }
}
