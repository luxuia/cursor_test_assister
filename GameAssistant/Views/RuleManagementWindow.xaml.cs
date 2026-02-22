using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using GameAssistant.Core.Interfaces;
using GameAssistant.Core.Models;
using GameAssistant.Services.Database;
using Newtonsoft.Json;

namespace GameAssistant.Views
{
    public partial class RuleManagementWindow : Window
    {
        private readonly IAdviceDatabase _database;
        private List<AdviceRule> _rules = new List<AdviceRule>();
        private AdviceRule? _currentRule;

        public RuleManagementWindow()
        {
            InitializeComponent();
            _database = new AdviceDatabase();
            
            Loaded += RuleManagementWindow_Loaded;
        }

        private async void RuleManagementWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadRules();
        }

        private async Task LoadRules()
        {
            try
            {
                _rules = await _database.GetAllRulesAsync();
                RulesListBox.ItemsSource = _rules;
                StatusText.Text = $"已加载 {_rules.Count} 条规则";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载规则失败: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RulesListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            _currentRule = RulesListBox.SelectedItem as AdviceRule;
            EditButton.IsEnabled = _currentRule != null;
            DeleteButton.IsEnabled = _currentRule != null;
            
            if (_currentRule != null)
            {
                LoadRuleToEditor(_currentRule);
            }
            else
            {
                ClearEditor();
            }
        }

        private void LoadRuleToEditor(AdviceRule rule)
        {
            AdviceContentTextBox.Text = rule.AdviceContent;
            PriorityTextBox.Text = rule.Priority.ToString();
            
            // 设置类型
            foreach (ComboBoxItem item in TypeComboBox.Items)
            {
                if (item.Tag.ToString() == ((int)rule.Type).ToString())
                {
                    TypeComboBox.SelectedItem = item;
                    break;
                }
            }
            
            IsEnabledCheckBox.IsChecked = rule.IsEnabled;
            
            // 加载条件
            var condition = rule.Condition;
            HealthThresholdTextBox.Text = condition.HealthThreshold?.ToString() ?? "";
            
            if (condition.Phase.HasValue)
            {
                string phaseStr = condition.Phase.Value.ToString();
                foreach (ComboBoxItem item in PhaseComboBox.Items)
                {
                    if (item.Tag.ToString() == phaseStr)
                    {
                        PhaseComboBox.SelectedItem = item;
                        break;
                    }
                }
            }
            else
            {
                PhaseComboBox.SelectedIndex = 0;
            }
            
            MapRegionTextBox.Text = condition.MapRegion ?? "";
            HeroCombinationTextBox.Text = condition.HeroCombination != null 
                ? string.Join(", ", condition.HeroCombination) 
                : "";
            EquipmentCombinationTextBox.Text = condition.EquipmentCombination != null 
                ? string.Join(", ", condition.EquipmentCombination) 
                : "";
        }

        private void ClearEditor()
        {
            AdviceContentTextBox.Text = "";
            PriorityTextBox.Text = "0";
            TypeComboBox.SelectedIndex = 0;
            IsEnabledCheckBox.IsChecked = true;
            HealthThresholdTextBox.Text = "";
            PhaseComboBox.SelectedIndex = 0;
            MapRegionTextBox.Text = "";
            HeroCombinationTextBox.Text = "";
            EquipmentCombinationTextBox.Text = "";
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            _currentRule = null;
            ClearEditor();
            RulesListBox.SelectedItem = null;
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            // 编辑功能已在选择时自动加载
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentRule == null) return;

            var result = MessageBox.Show(
                $"确定要删除规则 \"{_currentRule.AdviceContent}\" 吗？",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _database.DeleteRuleAsync(_currentRule.Id);
                    await LoadRules();
                    ClearEditor();
                    StatusText.Text = "规则已删除";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"删除失败: {ex.Message}", "错误", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var rule = _currentRule ?? new AdviceRule();
                
                // 验证输入
                if (string.IsNullOrWhiteSpace(AdviceContentTextBox.Text))
                {
                    MessageBox.Show("请输入建议内容", "验证错误", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(PriorityTextBox.Text, out int priority))
                {
                    MessageBox.Show("优先级必须是数字", "验证错误", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 更新规则信息
                rule.AdviceContent = AdviceContentTextBox.Text;
                rule.Priority = priority;
                
                if (TypeComboBox.SelectedItem is ComboBoxItem typeItem)
                {
                    rule.Type = (AdviceType)int.Parse(typeItem.Tag.ToString() ?? "0");
                }
                
                rule.IsEnabled = IsEnabledCheckBox.IsChecked ?? true;

                // 更新条件
                rule.Condition = new AdviceCondition();
                
                if (double.TryParse(HealthThresholdTextBox.Text, out double healthThreshold))
                {
                    rule.Condition.HealthThreshold = healthThreshold;
                }

                if (PhaseComboBox.SelectedItem is ComboBoxItem phaseItem && 
                    !string.IsNullOrEmpty(phaseItem.Tag.ToString()))
                {
                    string phaseStr = phaseItem.Tag.ToString() ?? "";
                    if (Enum.TryParse<GamePhase>(phaseStr, out var phase))
                    {
                        rule.Condition.Phase = phase;
                    }
                }

                rule.Condition.MapRegion = string.IsNullOrWhiteSpace(MapRegionTextBox.Text) 
                    ? null 
                    : MapRegionTextBox.Text;

                if (!string.IsNullOrWhiteSpace(HeroCombinationTextBox.Text))
                {
                    rule.Condition.HeroCombination = HeroCombinationTextBox.Text
                        .Split(',')
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                }

                if (!string.IsNullOrWhiteSpace(EquipmentCombinationTextBox.Text))
                {
                    rule.Condition.EquipmentCombination = EquipmentCombinationTextBox.Text
                        .Split(',')
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                }

                // 保存规则
                if (_currentRule == null)
                {
                    await _database.AddRuleAsync(rule);
                    StatusText.Text = "规则已添加";
                }
                else
                {
                    await _database.AddRuleAsync(rule); // 更新也是通过AddRuleAsync
                    StatusText.Text = "规则已更新";
                }

                await LoadRules();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentRule != null)
            {
                LoadRuleToEditor(_currentRule);
            }
            else
            {
                ClearEditor();
            }
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON文件|*.json|所有文件|*.*",
                Title = "导入规则文件"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string json = File.ReadAllText(dialog.FileName);
                    var list = JsonConvert.DeserializeObject<List<AdviceRule>>(json);
                    if (list == null || list.Count == 0)
                    {
                        MessageBox.Show("文件中没有有效规则", "提示",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    await _database.ImportRulesAsync(list);
                    await LoadRules();
                    MessageBox.Show($"已导入 {list.Count} 条规则", "成功",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导入失败: {ex.Message}", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON文件|*.json|所有文件|*.*",
                Title = "导出规则文件",
                FileName = "advice_rules_backup.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _rules = await _database.GetAllRulesAsync();
                    string json = JsonConvert.SerializeObject(_rules, Formatting.Indented);
                    File.WriteAllText(dialog.FileName, json);
                    MessageBox.Show($"已导出 {_rules.Count} 条规则", "成功",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导出失败: {ex.Message}", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
