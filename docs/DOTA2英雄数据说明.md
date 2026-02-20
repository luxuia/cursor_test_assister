# DOTA 2 英雄数据说明

## 📊 数据来源

英雄数据来自 [Liquipedia DOTA 2 Wiki](https://liquipedia.net/dota2/Portal:Heroes)

当前版本：**7.40c**
英雄总数：**128个**

## 🎮 英雄分类

### Strength（力量）- 36个
- Alchemist（炼金术士）
- Axe（斧王）
- Bristleback（刚背兽）
- Centaur Warrunner（半人马战行者）
- Chaos Knight（混沌骑士）
- Clockwerk（发条技师）
- Dawnbreaker（破晓辰星）
- Doom（末日使者）
- Dragon Knight（龙骑士）
- Earth Spirit（大地之灵）
- Earthshaker（撼地者）
- Elder Titan（上古巨神）
- Huskar（哈斯卡）
- Kunkka（昆卡）
- Legion Commander（军团指挥官）
- Lifestealer（噬魂鬼）
- Lycan（狼人）
- Mars（玛尔斯）
- Night Stalker（暗夜魔王）
- Ogre Magi（食人魔魔法师）
- Omniknight（全能骑士）
- Phoenix（凤凰）
- Primal Beast（原始野兽）
- Pudge（帕吉）
- Slardar（斯拉达）
- Spirit Breaker（裂魂人）
- Sven（斯温）
- Tidehunter（潮汐猎人）
- Timbersaw（伐木机）
- Tiny（小小）
- Treant Protector（树精卫士）
- Tusk（巨牙海民）
- Underlord（孽主）
- Undying（不朽尸王）
- Wraith King（冥魂大帝）

### Agility（敏捷）- 35个
- Anti-Mage（敌法师）
- Bloodseeker（血魔）
- Bounty Hunter（赏金猎人）
- Broodmother（育母蜘蛛）
- Clinkz（克林克兹）
- Drow Ranger（卓尔游侠）
- Ember Spirit（灰烬之灵）
- Faceless Void（虚空假面）
- Gyrocopter（矮人直升机）
- Hoodwink（森海飞霞）
- Juggernaut（主宰）
- Lone Druid（德鲁伊）
- Luna（露娜）
- Medusa（美杜莎）
- Meepo（米波）
- Mirana（米拉娜）
- Monkey King（齐天大圣）
- Morphling（变体精灵）
- Naga Siren（娜迦海妖）
- Phantom Assassin（幻影刺客）
- Phantom Lancer（幻影长矛手）
- Razor（剃刀）
- Riki（力丸）
- Shadow Fiend（影魔）
- Slark（斯拉克）
- Sniper（狙击手）
- Spectre（幽鬼）
- Templar Assassin（圣堂刺客）
- Terrorblade（恐怖利刃）
- Troll Warlord（巨魔战将）
- Ursa（熊战士）
- Vengeful Spirit（复仇之魂）
- Viper（冥界亚龙）
- Weaver（编织者）

### Intelligence（智力）- 34个
- Ancient Apparition（远古冰魄）
- Chen（陈）
- Crystal Maiden（水晶室女）
- Dark Seer（黑暗贤者）
- Dark Willow（邪影芳灵）
- Disruptor（干扰者）
- Enchantress（魅惑魔女）
- Grimstroke（天涯墨客）
- Invoker（祈求者）
- Jakiro（杰奇洛）
- Keeper of the Light（光之守卫）
- Leshrac（拉席克）
- Lich（巫妖）
- Lina（莉娜）
- Lion（莱恩）
- Muerta（穆尔塔）
- Necrophos（瘟疫法师）
- Oracle（神谕者）
- Outworld Destroyer（殁境神蚀者）
- Puck（帕克）
- Pugna（帕格纳）
- Queen of Pain（痛苦女王）
- Ringmaster（马戏团大师）
- Rubick（拉比克）
- Shadow Demon（暗影恶魔）
- Shadow Shaman（暗影萨满）
- Silencer（沉默术士）
- Skywrath Mage（天怒法师）
- Storm Spirit（风暴之灵）
- Tinker（修补匠）
- Warlock（术士）
- Winter Wyvern（寒冬飞龙）
- Witch Doctor（巫医）
- Zeus（宙斯）

### Universal（全才）- 23个
- Abaddon（亚巴顿）
- Arc Warden（天穹守望者）
- Bane（祸乱之源）
- Batrider（蝙蝠骑士）
- Beastmaster（兽王）
- Brewmaster（酒仙）
- Dazzle（戴泽）
- Death Prophet（死亡先知）
- Enigma（谜团）
- Io（艾欧）
- Magnus（马格纳斯）
- Marci（玛西）
- Nature's Prophet（先知）
- Nyx Assassin（司夜刺客）
- Pangolier（石鳞剑士）
- Sand King（沙王）
- Snapfire（电炎绝手）
- Spirit Bear（熊灵）
- Techies（工程师）
- Venomancer（剧毒术士）
- Visage（维萨吉）
- Void Spirit（虚无之灵）
- Windranger（风行者）

## 📥 使用下载工具

### 步骤

1. **打开下载工具**
   - 点击主窗口的"下载英雄图标"按钮

2. **选择游戏**
   - 在下拉菜单中选择"DOTA 2"

3. **加载英雄列表**
   - 点击"加载英雄列表"按钮
   - 系统会从 `Data/Dota2Heroes.json` 加载英雄数据

4. **开始下载**
   - 点击"开始下载"按钮
   - 工具会自动从Liquipedia下载所有英雄图标
   - 图标会保存到 `Templates/Heroes/` 目录

### 下载的图标格式

- **文件名**：`{hero_id}.png`
- **尺寸**：64x64像素
- **格式**：PNG
- **来源**：Liquipedia Commons

## 📝 数据文件格式

英雄数据存储在 `Data/Dota2Heroes.json`：

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
      "iconUrl": "https://liquipedia.net/..."
    }
  ]
}
```

## 🔄 更新数据

### 手动更新

1. 访问 [Liquipedia DOTA 2 Heroes](https://liquipedia.net/dota2/Portal:Heroes)
2. 获取最新的英雄列表
3. 更新 `Data/Dota2Heroes.json` 文件
4. 重新运行下载工具

### 自动更新（待实现）

未来版本可能会添加自动检测和更新功能。

## ⚠️ 注意事项

1. **版权**
   - 图标来自Liquipedia，遵循其使用条款
   - 仅用于个人学习和研究

2. **网络**
   - 下载需要网络连接
   - 如果下载失败，可以手动从Liquipedia下载

3. **版本**
   - 游戏更新后，英雄列表可能会变化
   - 需要定期更新数据文件

## 🛠️ 相关文件

- `Data/Dota2Heroes.json` - 英雄数据文件
- `Tools/HeroIconDownloader.cs` - 下载工具
- `Views/HeroDownloadWindow.xaml` - 下载界面

## 📚 参考链接

- [Liquipedia DOTA 2 Heroes](https://liquipedia.net/dota2/Portal:Heroes)
- [DOTA 2 官方网站](https://www.dota2.com/)
