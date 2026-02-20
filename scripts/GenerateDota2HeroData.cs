// 运行此脚本生成完整的DOTA 2英雄数据
// 使用方法：在项目根目录运行
// dotnet script scripts/GenerateDota2HeroData.cs

using GameAssistant.Tools;

// 生成完整的DOTA 2英雄数据
Dota2HeroDataGenerator.GenerateCompleteHeroData("GameAssistant/Data/Dota2Heroes.json");
Console.WriteLine("DOTA 2英雄数据已生成！");
