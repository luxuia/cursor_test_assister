using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using GameAssistant.Tools;
using Newtonsoft.Json;

namespace GameAssistant.Views
{
    /// <summary>列表项包装，IsSelected 可写以支持 TwoWay 绑定</summary>
    public class HeroListItemViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        private bool _isSelected = true;
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string NameCn { get; set; } = "";
        public string Attribute { get; set; } = "";
        public int Cost { get; set; }
        public HeroInfo Hero { get; set; } = null!;
        public bool IsSelected { get => _isSelected; set { _isSelected = value; PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsSelected))); } }
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }

    /// <summary>列表项包装，IsSelected 可写以支持 TwoWay 绑定</summary>
    public class ItemListItemViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        private bool _isSelected = true;
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string NameCn { get; set; } = "";
        public string Attribute { get; set; } = "";
        public int Cost { get; set; }
        public ItemInfo Item { get; set; } = null!;
        public bool IsSelected { get => _isSelected; set { _isSelected = value; PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsSelected))); } }
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }

    /// <summary>技能列表项包装</summary>
    public class AbilityListItemViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        private bool _isSelected = true;
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string NameCn { get; set; } = "";
        public string Attribute { get; set; } = "";
        public int Cost { get; set; }
        public AbilityInfo Ability { get; set; } = null!;
        public bool IsSelected { get => _isSelected; set { _isSelected = value; PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsSelected))); } }
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }

    public partial class HeroDownloadWindow : Window
    {
        private List<HeroListItemViewModel> _heroListItems = new List<HeroListItemViewModel>();
        private List<ItemListItemViewModel> _itemListItems = new List<ItemListItemViewModel>();
        private List<AbilityListItemViewModel> _abilityListItems = new List<AbilityListItemViewModel>();
        private HeroIconDownloader? _downloader;
        private readonly LiquipediaScraper _scraper = new LiquipediaScraper();
        private string _currentType = "英雄"; // 英雄 | 物品 | 技能

        public HeroDownloadWindow()
        {
            InitializeComponent();
        }

        private async void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            string? game = null;
            string? type = null;
            if (GameComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem gameItem)
                game = gameItem.Content?.ToString();
            if (TypeComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem typeItem)
                type = typeItem.Content?.ToString();

            game ??= "DOTA 2";
            type ??= "英雄";
            _currentType = type;

            if (game != "DOTA 2")
            {
                try
                {
                    if (type == "英雄") LoadHeroesFromJson(game);
                    else if (type == "物品") LoadItemsFromJson(game);
                    else { MessageBox.Show("仅 DOTA 2 支持从网页加载技能", "提示", MessageBoxButton.OK, MessageBoxImage.Information); return; }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"加载列表失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return;
            }

            LoadButton.IsEnabled = false;
            StatusText.Text = "正在加载...";
            try
            {
                if (type == "英雄")
                {
                    var progress = new Progress<string>(m => StatusText.Text = m);
                    StatusText.Text = "正在从 Liquipedia 获取英雄列表...";
                    var heroes = await _scraper.FetchHeroesAsync(progress);
                    MergeChineseNamesForHeroes(heroes);
                    var steamCn = await SteamApiClient.GetHeroNameCnMapAsync(progress).ConfigureAwait(true);
                    if (steamCn != null)
                    {
                        foreach (var h in heroes)
                        {
                            if (steamCn.TryGetValue(h.Id, out var nameCn) && !string.IsNullOrEmpty(nameCn))
                                h.NameCn = nameCn;
                        }
                    }
                    if (heroes.Count > 0)
                    {
                        _heroListItems = heroes.Select(h => new HeroListItemViewModel { Id = h.Id, Name = h.Name, NameCn = h.NameCn ?? "", Attribute = h.Attribute, Cost = 0, Hero = h, IsSelected = true }).ToList();
                        ItemsListBox.ItemsSource = _heroListItems;
                        ListTitleText.Text = "英雄列表";
                        CountText.Text = $" ({_heroListItems.Count})";
                        StatusText.Text = $"已加载 {_heroListItems.Count} 个英雄（中英）";
                        SaveFetchedHeroesToJson(heroes, fromSteam: false);
                    }
                }
                else if (type == "物品")
                {
                    var progress = new Progress<string>(m => StatusText.Text = m);
                    StatusText.Text = "正在从 Liquipedia 获取物品列表...";
                    var items = await _scraper.FetchItemsAsync(progress);
                    MergeChineseNamesForItems(items);
                    var steamCn = await SteamApiClient.GetItemNameCnMapAsync(progress).ConfigureAwait(true);
                    if (steamCn != null)
                    {
                        foreach (var i in items)
                        {
                            if (steamCn.TryGetValue(i.Id, out var nameCn) && !string.IsNullOrEmpty(nameCn))
                                i.NameCn = nameCn;
                            else if (steamCn.TryGetValue("item_" + i.Id, out nameCn) && !string.IsNullOrEmpty(nameCn))
                                i.NameCn = nameCn;
                        }
                    }
                    if (items.Count > 0)
                    {
                        _itemListItems = items.Select(i => new ItemListItemViewModel { Id = i.Id, Name = i.Name, NameCn = i.NameCn ?? "", Attribute = "", Cost = i.Cost, Item = i, IsSelected = true }).ToList();
                        ItemsListBox.ItemsSource = _itemListItems;
                        ListTitleText.Text = "物品列表";
                        CountText.Text = $" ({_itemListItems.Count})";
                        StatusText.Text = $"已加载 {_itemListItems.Count} 个物品（中英）";
                        SaveFetchedItemsToJson(items, fromSteam: false);
                    }
                }
                else // 技能（Steam 无技能 API，仍用 Liquipedia）
                {
                    StatusText.Text = "正在从 Liquipedia 获取技能列表...";
                    var progress = new Progress<string>(m => StatusText.Text = m);
                    var abilities = await _scraper.FetchAbilitiesAsync(progress);
                    MergeChineseNamesForAbilities(abilities);
                    _abilityListItems = abilities.Select(a => new AbilityListItemViewModel { Id = a.Id, Name = a.Name, NameCn = a.NameCn ?? "", Attribute = "", Cost = 0, Ability = a, IsSelected = true }).ToList();
                    ItemsListBox.ItemsSource = _abilityListItems;
                    ListTitleText.Text = "技能列表";
                    CountText.Text = $" ({_abilityListItems.Count})";
                    StatusText.Text = $"已从网页加载 {_abilityListItems.Count} 个技能（中英）";
                    SaveFetchedAbilitiesToJson(abilities);
                }
                DownloadButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                StatusText.Text = "加载失败";
                MessageBox.Show($"加载失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadButton.IsEnabled = true;
            }
        }

        private void LoadHeroesFromJson(string game)
        {
            string jsonFile = game == "League of Legends" ? "Data/LolHeroes.json" : "Data/Dota2Heroes.json";
            var heroes = HeroIconDownloader.LoadHeroesFromJson(jsonFile);
            _heroListItems = heroes.Select(h => new HeroListItemViewModel { Id = h.Id, Name = h.Name, NameCn = h.NameCn ?? "", Attribute = h.Attribute, Cost = 0, Hero = h, IsSelected = true }).ToList();
            ItemsListBox.ItemsSource = _heroListItems;
            ListTitleText.Text = "英雄列表";
            CountText.Text = $" ({_heroListItems.Count})";
            DownloadButton.IsEnabled = _heroListItems.Count > 0;
            StatusText.Text = $"已加载 {_heroListItems.Count} 个英雄";
        }

        private void LoadItemsFromJson(string game)
        {
            string jsonFile = game == "League of Legends" ? "Data/LolItems.json" : "Data/Dota2Items.json";
            var items = HeroIconDownloader.LoadItemsFromJson(jsonFile);
            _itemListItems = items.Select(i => new ItemListItemViewModel { Id = i.Id, Name = i.Name, NameCn = i.NameCn ?? "", Attribute = "", Cost = i.Cost, Item = i, IsSelected = true }).ToList();
            ItemsListBox.ItemsSource = _itemListItems;
            ListTitleText.Text = "物品列表";
            CountText.Text = $" ({_itemListItems.Count})";
            DownloadButton.IsEnabled = _itemListItems.Count > 0;
            StatusText.Text = $"已加载 {_itemListItems.Count} 个物品";
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentType == "英雄" && _heroListItems.Count == 0)
            {
                MessageBox.Show("请先从网页加载英雄列表", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (_currentType == "物品" && _itemListItems.Count == 0)
            {
                MessageBox.Show("请先从网页加载物品列表", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (_currentType == "技能" && _abilityListItems.Count == 0)
            {
                MessageBox.Show("请先从网页加载技能列表", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var progress = new Progress<string>(message =>
            {
                Dispatcher.Invoke(() =>
                {
                    LogText.Text += message + "\n";
                    var scrollViewer = FindVisualChild<System.Windows.Controls.ScrollViewer>(LogText.Parent as FrameworkElement);
                    scrollViewer?.ScrollToEnd();
                    if (message.Contains("/"))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(message, @"\((\d+)/(\d+)\)");
                        if (match.Success)
                        {
                            int current = int.Parse(match.Groups[1].Value);
                            int total = int.Parse(match.Groups[2].Value);
                            DownloadProgressBar.Maximum = total;
                            DownloadProgressBar.Value = current;
                            ProgressText.Text = $"{current} / {total}";
                        }
                    }
                });
            });

            try
            {
                DownloadButton.IsEnabled = false;
                DownloadProgressBar.Value = 0;
                LogText.Text = "";

                if (_currentType == "英雄")
                {
                    var toDownload = _heroListItems.Where(x => x.IsSelected).Select(x => x.Hero).ToList();
                    _downloader = new HeroIconDownloader("Templates/Heroes");
                    await _downloader.DownloadDota2HeroIconsAsync(toDownload, progress);
                }
                else if (_currentType == "物品")
                {
                    var toDownload = _itemListItems.Where(x => x.IsSelected).Select(x => x.Item).ToList();
                    _downloader = new HeroIconDownloader("Templates/Equipment");
                    await _downloader.DownloadDota2ItemIconsAsync(toDownload, progress);
                }
                else
                {
                    var toDownload = _abilityListItems.Where(x => x.IsSelected).Select(x => x.Ability).ToList();
                    await _scraper.DownloadAbilityIconsAsync(toDownload, "Templates/Skills", progress);
                }

                MessageBox.Show("下载完成！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                StatusText.Text = "下载完成";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"下载失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = $"错误: {ex.Message}";
            }
            finally
            {
                DownloadButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// 从本地 Data 合并中文名到拉取的英雄列表（中英双版）。
        /// 优先使用 Dota2Heroes_Complete.json（128 英雄全量），否则用 Dota2Heroes.json。
        /// </summary>
        private static void MergeChineseNamesForHeroes(List<HeroInfo> heroes)
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var dataDir = Path.Combine(baseDir, "Data");
                string? path = Path.Combine(dataDir, "Dota2Heroes_Complete.json");
                if (!File.Exists(path)) path = Path.Combine(dataDir, "Dota2Heroes.json");
                if (!File.Exists(path)) path = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Dota2Heroes_Complete.json");
                if (!File.Exists(path)) path = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Dota2Heroes.json");
                if (!File.Exists(path)) return;
                var data = JsonConvert.DeserializeObject<HeroDataForMerge>(File.ReadAllText(path));
                if (data?.heroes == null) return;
                var cnMap = data.heroes.Where(h => !string.IsNullOrEmpty(h.nameCn)).ToDictionary(h => h.id.Trim(), h => h.nameCn!, StringComparer.OrdinalIgnoreCase);
                foreach (var h in heroes)
                {
                    if (cnMap.TryGetValue(h.Id, out var nameCn)) h.NameCn = nameCn;
                }
            }
            catch { /* 忽略 */ }
        }

        /// <summary>
        /// 物品中文：从 Dota2Items_Complete.json / Dota2Items.json / Dota2Items_FromWeb.json 合并；先按 id 匹配，再按英文 name 回退。
        /// </summary>
        private static void MergeChineseNamesForItems(List<ItemInfo> items)
        {
            try
            {
                var cnById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var cnByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var dir in DataPathHelper.GetCandidateDataDirectories())
                {
                    foreach (var fileName in new[] { "Dota2Items_Complete.json", "Dota2Items.json" })
                    {
                        var path = Path.Combine(dir, fileName);
                        if (!File.Exists(path)) continue;
                        var data = JsonConvert.DeserializeObject<ItemDataForMerge>(File.ReadAllText(path));
                        if (data?.categories == null) continue;
                        foreach (var cat in data.categories.Values)
                            foreach (var sub in cat.Values)
                                foreach (var it in sub)
                                {
                                    if (string.IsNullOrEmpty(it.nameCn)) continue;
                                    var id = (it.id ?? "").Trim();
                                    var name = (it.name ?? "").Trim();
                                    if (!string.IsNullOrEmpty(id)) cnById[id] = it.nameCn;
                                    if (!string.IsNullOrEmpty(name)) cnByName[name] = it.nameCn;
                                }
                    }
                    // FromWeb 扁平列表（Liquipedia/Steam 保存的格式）
                    var fromWebPath = Path.Combine(dir, "Dota2Items_FromWeb.json");
                    if (File.Exists(fromWebPath))
                    {
                        var fromWeb = JsonConvert.DeserializeObject<ItemsFromWebRoot>(File.ReadAllText(fromWebPath));
                        if (fromWeb?.items != null)
                            foreach (var it in fromWeb.items)
                            {
                                if (string.IsNullOrEmpty(it.nameCn)) continue;
                                var id = (it.id ?? "").Trim();
                                if (!string.IsNullOrEmpty(id))
                                {
                                    cnById[id] = it.nameCn;
                                    if (id.StartsWith("item_", StringComparison.OrdinalIgnoreCase))
                                        cnById[id.Substring(5)] = it.nameCn;
                                }
                                var name = (it.name ?? "").Trim();
                                if (!string.IsNullOrEmpty(name)) cnByName[name] = it.nameCn;
                            }
                    }
                }
                foreach (var i in items)
                {
                    if (cnById.TryGetValue(i.Id, out var nameCn)) { i.NameCn = nameCn; continue; }
                    if (cnById.TryGetValue("item_" + i.Id, out nameCn)) { i.NameCn = nameCn; continue; }
                    if (!string.IsNullOrEmpty(i.Name) && cnByName.TryGetValue(i.Name, out nameCn)) i.NameCn = nameCn;
                }
            }
            catch { /* 忽略 */ }
        }

        /// <summary>
        /// 技能中文：从 Dota2Abilities_Complete.json / Dota2Abilities.json / Dota2Abilities_FromWeb.json / Dota2Abilities_NameCn.json 合并。
        /// </summary>
        private static void MergeChineseNamesForAbilities(List<AbilityInfo> abilities)
        {
            try
            {
                var cnMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var dir in DataPathHelper.GetCandidateDataDirectories())
                {
                    foreach (var fileName in new[] { "Dota2Abilities_Complete.json", "Dota2Abilities.json" })
                    {
                        var path = Path.Combine(dir, fileName);
                        if (!File.Exists(path)) continue;
                        var data = JsonConvert.DeserializeObject<AbilityDataForMerge>(File.ReadAllText(path));
                        var list = data?.abilities ?? data?.Abilities;
                        if (list == null) continue;
                        foreach (var a in list)
                        {
                            var id = (a.id ?? a.Id ?? "").Trim();
                            var nameCn = a.nameCn ?? a.NameCn;
                            if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(nameCn))
                                cnMap[id] = nameCn;
                        }
                    }
                    // FromWeb 与 NameCn 补充（网页拉取/本地补充表）
                    foreach (var fileName in new[] { "Dota2Abilities_FromWeb.json", "Dota2Abilities_NameCn.json" })
                    {
                        var path = Path.Combine(dir, fileName);
                        if (!File.Exists(path)) continue;
                        var data = JsonConvert.DeserializeObject<AbilityDataForMerge>(File.ReadAllText(path));
                        var list = data?.abilities ?? data?.Abilities;
                        if (list == null) continue;
                        foreach (var a in list)
                        {
                            var id = (a.id ?? a.Id ?? "").Trim();
                            var nameCn = a.nameCn ?? a.NameCn;
                            if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(nameCn))
                                cnMap[id] = nameCn;
                        }
                    }
                }
                foreach (var a in abilities)
                {
                    if (cnMap.TryGetValue(a.Id, out var nameCn)) a.NameCn = nameCn;
                }
            }
            catch { /* 忽略 */ }
        }

        private class HeroDataForMerge { public List<HeroInfoForMerge>? heroes { get; set; } }
        private class HeroInfoForMerge { public string id { get; set; } = ""; public string? nameCn { get; set; } }
        private class ItemDataForMerge { public Dictionary<string, Dictionary<string, List<ItemInfoForMerge>>>? categories { get; set; } }
        private class ItemInfoForMerge { public string id { get; set; } = ""; public string? nameCn { get; set; } public string? name { get; set; } }
        private class ItemsFromWebRoot { public List<ItemInfoForMerge>? items { get; set; } }
        private class AbilityDataForMerge
        {
            [JsonProperty("abilities")]
            public List<AbilityInfoForMerge>? abilities { get; set; }
            [JsonProperty("Abilities")]
            public List<AbilityInfoForMerge>? Abilities { get; set; }
        }
        private class AbilityInfoForMerge
        {
            [JsonProperty("id")]
            public string? id { get; set; }
            [JsonProperty("Id")]
            public string? Id { get; set; }
            [JsonProperty("nameCn")]
            public string? nameCn { get; set; }
            [JsonProperty("NameCn")]
            public string? NameCn { get; set; }
        }

        /// <summary>
        /// 将拉取的英雄数据保存为带元数据的 JSON，便于后续使用和对照。
        /// 路径：Data/Dota2Heroes_FromWeb.json
        /// </summary>
        private static void SaveFetchedHeroesToJson(List<HeroInfo> heroes, bool fromSteam = false)
        {
            try
            {
                string dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                Directory.CreateDirectory(dataDir);
                string path = Path.Combine(dataDir, "Dota2Heroes_FromWeb.json");
                var doc = new
                {
                    sourceUrl = fromSteam ? "Steam Web API (IEconDOTA2_570/GetHeroes)" : "https://liquipedia.net/dota2/Portal:Heroes",
                    fetchedAt = DateTime.UtcNow.ToString("o"),
                    game = "Dota2",
                    count = heroes.Count,
                    description = fromSteam ? "英雄 id/name/nameCn/attribute 中英双版，来自 Steam 官方 API" : "英雄 id/name/nameCn/attribute/iconUrl 中英双版对应关系，从 Portal:Heroes 拉取",
                    heroes = heroes.Select(h => new { id = h.Id, name = h.Name, nameCn = h.NameCn ?? "", attribute = h.Attribute, iconUrl = h.IconUrl ?? "" }).ToList()
                };
                File.WriteAllText(path, JsonConvert.SerializeObject(doc, Formatting.Indented));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存英雄 JSON 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 将拉取的物品数据保存为带元数据的 JSON。路径：Data/Dota2Items_FromWeb.json
        /// </summary>
        private static void SaveFetchedItemsToJson(List<ItemInfo> items, bool fromSteam = false)
        {
            try
            {
                string dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                Directory.CreateDirectory(dataDir);
                string path = Path.Combine(dataDir, "Dota2Items_FromWeb.json");
                var doc = new
                {
                    sourceUrl = fromSteam ? "Steam Web API (IEconDOTA2_570/GetGameItems)" : "https://liquipedia.net/dota2/Portal:Items",
                    fetchedAt = DateTime.UtcNow.ToString("o"),
                    game = "Dota2",
                    count = items.Count,
                    description = fromSteam ? "物品 id/name/nameCn/cost 中英双版，来自 Steam 官方 API" : "物品 id/name/nameCn/cost/iconUrl 中英双版对应关系，从网页拉取",
                    items = items.Select(i => new { id = i.Id, name = i.Name, nameCn = i.NameCn ?? "", cost = i.Cost, iconUrl = i.IconUrl ?? "" }).ToList()
                };
                File.WriteAllText(path, JsonConvert.SerializeObject(doc, Formatting.Indented));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存物品 JSON 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 将拉取的技能数据保存为带元数据的 JSON。路径：Data/Dota2Abilities_FromWeb.json
        /// </summary>
        private static void SaveFetchedAbilitiesToJson(List<AbilityInfo> abilities)
        {
            try
            {
                string dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                Directory.CreateDirectory(dataDir);
                string path = Path.Combine(dataDir, "Dota2Abilities_FromWeb.json");
                var doc = new
                {
                    sourceUrl = "https://liquipedia.net/dota2/Portal:Abilities",
                    fetchedAt = DateTime.UtcNow.ToString("o"),
                    game = "Dota2",
                    count = abilities.Count,
                    description = "技能 id/name/nameCn/iconUrl 中英双版对应关系，从网页拉取",
                    abilities = abilities.Select(a => new { id = a.Id, name = a.Name, nameCn = a.NameCn ?? "", iconUrl = a.IconUrl ?? "" }).ToList()
                };
                File.WriteAllText(path, JsonConvert.SerializeObject(doc, Formatting.Indented));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存技能 JSON 失败: {ex.Message}");
            }
        }

        private static T? FindVisualChild<T>(DependencyObject? parent) where T : DependencyObject
        {
            if (parent == null) return null;
            
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;
                
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }
    }
}
