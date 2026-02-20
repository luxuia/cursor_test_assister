# DOTA 2 英雄数据使用指南

## 📥 快速开始

### 1. 生成英雄数据文件

首先需要生成完整的英雄数据JSON文件：

**方法一：使用程序生成**
```bash
dotnet run --project GameAssistant/GameAssistant.csproj -- generate-hero-data
```

**方法二：手动运行生成器**
在代码中调用：
```csharp
GameAssistant.Tools.Dota2HeroDataGenerator.GenerateCompleteHeroData();
```

### 2. 下载英雄图标

1. **打开下载工具**
   - 启动程序
   - 点击主窗口的"下载英雄图标"按钮

2. **选择游戏**
   - 在下拉菜单中选择"DOTA 2"

3. **加载英雄列表**
   - 点击"加载英雄列表"
   - 系统会从 `Data/Dota2Heroes.json` 加载所有128个英雄

4. **开始下载**
   - 点击"开始下载"
   - 工具会自动从Liquipedia下载所有英雄图标
   - 下载进度会实时显示

## 📊 英雄数据

### 数据来源
- **网站**：[Liquipedia DOTA 2 Wiki](https://liquipedia.net/dota2/Portal:Heroes)
- **版本**：7.40c
- **总数**：128个英雄

### 英雄分类

- **Strength（力量）**：36个
- **Agility（敏捷）**：35个  
- **Intelligence（智力）**：34个
- **Universal（全才）**：23个

## 📁 文件结构

```
GameAssistant/
├── Data/
│   └── Dota2Heroes.json          # 英雄数据文件
├── Tools/
│   ├── HeroIconDownloader.cs     # 下载工具
│   └── Dota2HeroDataGenerator.cs # 数据生成器
├── Views/
│   └── HeroDownloadWindow.xaml   # 下载界面
└── Templates/
    └── Heroes/                    # 下载的图标保存位置
        ├── alchemist.png
        ├── axe.png
        └── ...
```

## 🔧 使用说明

### 下载图标

图标会自动下载到 `Templates/Heroes/` 目录，文件名格式为：`{hero_id}.png`

例如：
- `alchemist.png` - 炼金术士
- `axe.png` - 斧王
- `pudge.png` - 帕吉

### 数据格式

英雄数据JSON格式：
```json
{
  "game": "Dota2",
  "version": "7.40c",
  "totalHeroes": 128,
  "heroes": [
    {
      "id": "alchemist",
      "name": "Alchemist",
      "nameCn": "炼金术士",
      "attribute": "Strength",
      "iconUrl": "https://..."
    }
  ]
}
```

## ⚠️ 注意事项

1. **网络连接**
   - 下载需要稳定的网络连接
   - 如果某个图标下载失败，可以手动重试

2. **版权**
   - 图标来自Liquipedia，遵循其使用条款
   - 仅用于个人学习和研究

3. **版本更新**
   - 游戏更新后，英雄列表可能变化
   - 需要重新生成数据文件

## 🐛 常见问题

### Q: 下载失败怎么办？

A: 
1. 检查网络连接
2. 某些英雄的图标URL可能需要调整
3. 可以手动从Liquipedia下载图标

### Q: 如何更新英雄数据？

A:
1. 访问Liquipedia获取最新列表
2. 运行数据生成器更新JSON文件
3. 重新下载图标

### Q: 图标下载不完整？

A:
- 检查下载日志查看失败的英雄
- 可以单独重新下载失败的图标
- 或手动从Liquipedia下载

## 📚 相关文档

- [DOTA 2英雄数据说明](docs/DOTA2英雄数据说明.md)
- [模板采集指南](docs/模板采集指南.md)

## 🔗 参考链接

- [Liquipedia DOTA 2 Heroes](https://liquipedia.net/dota2/Portal:Heroes)
- [DOTA 2 官方网站](https://www.dota2.com/)
