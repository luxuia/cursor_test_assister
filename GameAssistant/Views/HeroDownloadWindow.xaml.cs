using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using GameAssistant.Tools;
using HeroInfo = GameAssistant.Tools.HeroIconDownloader.HeroInfo;
using ItemInfo = GameAssistant.Tools.HeroIconDownloader.ItemInfo;

namespace GameAssistant.Views
{
    public partial class HeroDownloadWindow : Window
    {
        private List<HeroInfo> _allHeroes = new List<HeroInfo>();
        private List<ItemInfo> _allItems = new List<ItemInfo>();
        private HeroIconDownloader? _downloader;
        private bool _isHeroMode = true;

        public HeroDownloadWindow()
        {
            InitializeComponent();
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string game = ((System.Windows.Controls.ComboBoxItem)GameComboBox.SelectedItem).Content.ToString() ?? "";
                string type = ((System.Windows.Controls.ComboBoxItem)TypeComboBox.SelectedItem).Content.ToString() ?? "";
                _isHeroMode = type == "英雄";

                if (_isHeroMode)
                {
                    LoadHeroes(game);
                }
                else
                {
                    LoadItems(game);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载列表失败: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadHeroes(string game)
        {
            string jsonFile = game switch
            {
                "DOTA 2" => "GameAssistant/Data/Dota2Heroes_Complete.json",
                "League of Legends" => "GameAssistant/Data/LolHeroes.json",
                _ => "GameAssistant/Data/Dota2Heroes_Complete.json"
            };

            _allHeroes = HeroIconDownloader.LoadHeroesFromJson(jsonFile);
            
            ItemsListBox.ItemsSource = _allHeroes.Select(h => new
            {
                h.Id,
                h.Name,
                h.NameCn,
                Attribute = h.Attribute,
                Cost = 0,
                IsSelected = true
            }).ToList();

            ListTitleText.Text = "英雄列表";
            CountText.Text = $" ({_allHeroes.Count})";
            DownloadButton.IsEnabled = _allHeroes.Count > 0;
            StatusText.Text = $"已加载 {_allHeroes.Count} 个英雄";
        }

        private void LoadItems(string game)
        {
            string jsonFile = game switch
            {
                "DOTA 2" => "GameAssistant/Data/Dota2Items.json",
                "League of Legends" => "GameAssistant/Data/LolItems.json",
                _ => "GameAssistant/Data/Dota2Items.json"
            };

            _allItems = HeroIconDownloader.LoadItemsFromJson(jsonFile);
            
            ItemsListBox.ItemsSource = _allItems.Select(i => new
            {
                i.Id,
                i.Name,
                i.NameCn,
                Attribute = "",
                i.Cost,
                IsSelected = true
            }).ToList();

            ListTitleText.Text = "物品列表";
            CountText.Text = $" ({_allItems.Count})";
            DownloadButton.IsEnabled = _allItems.Count > 0;
            StatusText.Text = $"已加载 {_allItems.Count} 个物品";
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isHeroMode && _allHeroes.Count == 0)
            {
                MessageBox.Show("请先加载英雄列表", "提示", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!_isHeroMode && _allItems.Count == 0)
            {
                MessageBox.Show("请先加载物品列表", "提示", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                DownloadButton.IsEnabled = false;
                DownloadProgressBar.Value = 0;
                LogText.Text = "";
                
                string downloadDir = _isHeroMode ? "Templates/Heroes" : "Templates/Equipment";
                _downloader = new HeroIconDownloader(downloadDir);
                
                var progress = new Progress<string>(message =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        LogText.Text += message + "\n";
                        // 滚动到底部
                        var scrollViewer = FindVisualChild<System.Windows.Controls.ScrollViewer>(LogText.Parent as FrameworkElement);
                        scrollViewer?.ScrollToEnd();
                        
                        // 更新进度
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

                if (_isHeroMode)
                {
                    await _downloader.DownloadDota2HeroIconsAsync(_allHeroes, progress);
                }
                else
                {
                    await _downloader.DownloadDota2ItemIconsAsync(_allItems, progress);
                }
                
                MessageBox.Show("下载完成！", "成功", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                
                StatusText.Text = "下载完成";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"下载失败: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = $"错误: {ex.Message}";
            }
            finally
            {
                DownloadButton.IsEnabled = true;
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
