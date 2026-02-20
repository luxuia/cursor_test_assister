using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using GameAssistant.Services.ScreenCapture;

namespace GameAssistant.Views
{
    public partial class TemplateCaptureWindow : Window
    {
        private Bitmap? _sourceBitmap;
        private System.Windows.Point _selectionStart;
        private bool _isSelecting = false;
        private System.Drawing.Rectangle _selectedRegion;

        public TemplateCaptureWindow()
        {
            InitializeComponent();
        }

        private void CaptureScreenButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 获取游戏窗口句柄（这里需要从配置或主窗口获取）
                // 暂时使用全屏截图
                var screenBounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
                _sourceBitmap = new Bitmap(screenBounds.Width, screenBounds.Height, PixelFormat.Format32bppArgb);
                
                using (Graphics graphics = Graphics.FromImage(_sourceBitmap))
                {
                    graphics.CopyFromScreen(0, 0, 0, 0, screenBounds.Size);
                }

                DisplayImage(_sourceBitmap);
                StatusText.Text = "画面已捕获，请在图片上拖拽选择模板区域";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"捕获失败: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadImageButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp|所有文件|*.*",
                Title = "选择图片文件"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _sourceBitmap = new Bitmap(dialog.FileName);
                    DisplayImage(_sourceBitmap);
                    StatusText.Text = $"图片已加载: {Path.GetFileName(dialog.FileName)}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"加载图片失败: {ex.Message}", "错误", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DisplayImage(Bitmap bitmap)
        {
            var bitmapSource = ConvertBitmap(bitmap);
            SourceImage.Source = bitmapSource;
            
            // 设置Canvas大小
            ImageCanvas.Width = bitmap.Width;
            ImageCanvas.Height = bitmap.Height;
            
            // 清除选择
            ClearSelection();
        }

        private BitmapSource ConvertBitmap(Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                bitmap.PixelFormat);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width,
                bitmapData.Height,
                bitmap.HorizontalResolution,
                bitmap.VerticalResolution,
                System.Windows.Media.PixelFormats.Bgr24,
                null,
                bitmapData.Scan0,
                bitmapData.Stride * bitmapData.Height,
                bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);
            return bitmapSource;
        }

        private void ImageCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_sourceBitmap == null) return;

            _isSelecting = true;
            _selectionStart = e.GetPosition(ImageCanvas);
            SelectionRectangle.Visibility = Visibility.Visible;
            Canvas.SetLeft(SelectionRectangle, _selectionStart.X);
            Canvas.SetTop(SelectionRectangle, _selectionStart.Y);
            SelectionRectangle.Width = 0;
            SelectionRectangle.Height = 0;
        }

        private void ImageCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isSelecting || _sourceBitmap == null) return;

            var currentPoint = e.GetPosition(ImageCanvas);
            var left = Math.Min(_selectionStart.X, currentPoint.X);
            var top = Math.Min(_selectionStart.Y, currentPoint.Y);
            var width = Math.Abs(currentPoint.X - _selectionStart.X);
            var height = Math.Abs(currentPoint.Y - _selectionStart.Y);

            Canvas.SetLeft(SelectionRectangle, left);
            Canvas.SetTop(SelectionRectangle, top);
            SelectionRectangle.Width = width;
            SelectionRectangle.Height = height;
        }

        private void ImageCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isSelecting || _sourceBitmap == null) return;

            _isSelecting = false;
            
            var currentPoint = e.GetPosition(ImageCanvas);
            var left = Math.Min(_selectionStart.X, currentPoint.X);
            var top = Math.Min(_selectionStart.Y, currentPoint.Y);
            var width = Math.Abs(currentPoint.X - _selectionStart.X);
            var height = Math.Abs(currentPoint.Y - _selectionStart.Y);

            if (width > 10 && height > 10) // 最小尺寸检查
            {
                // 转换为图片坐标（考虑缩放）
                double scaleX = _sourceBitmap.Width / ImageCanvas.ActualWidth;
                double scaleY = _sourceBitmap.Height / ImageCanvas.ActualHeight;
                
                _selectedRegion = new System.Drawing.Rectangle(
                    (int)(left * scaleX),
                    (int)(top * scaleY),
                    (int)(width * scaleX),
                    (int)(height * scaleY)
                );

                UpdateTemplatePreview();
                SaveTemplateButton.IsEnabled = true;
                StatusText.Text = $"已选择区域: {_selectedRegion.Width}x{_selectedRegion.Height}";
            }
            else
            {
                ClearSelection();
            }
        }

        private void UpdateTemplatePreview()
        {
            if (_sourceBitmap == null || _selectedRegion.Width == 0 || _selectedRegion.Height == 0)
                return;

            try
            {
                using var cropped = _sourceBitmap.Clone(_selectedRegion, _sourceBitmap.PixelFormat);
                var preview = ConvertBitmap(cropped);
                TemplatePreview.Source = preview;
                
                SizeText.Text = $"尺寸: {_selectedRegion.Width} x {_selectedRegion.Height}";
            }
            catch
            {
                // 忽略错误
            }
        }

        private void ClearSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            ClearSelection();
        }

        private void ClearSelection()
        {
            SelectionRectangle.Visibility = Visibility.Collapsed;
            TemplatePreview.Source = null;
            SaveTemplateButton.IsEnabled = false;
            SizeText.Text = "尺寸: --";
            _selectedRegion = System.Drawing.Rectangle.Empty;
        }

        private void SaveTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sourceBitmap == null || _selectedRegion.Width == 0 || _selectedRegion.Height == 0)
                return;

            try
            {
                string templateName = PreviewNameTextBox.Text;
                if (string.IsNullOrWhiteSpace(templateName))
                {
                    templateName = TemplateNameTextBox.Text;
                }

                if (string.IsNullOrWhiteSpace(templateName))
                {
                    MessageBox.Show("请输入模板名称", "提示", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string category = "Heroes";
                if (PreviewCategoryComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem categoryItem)
                {
                    category = categoryItem.Content.ToString() ?? "Heroes";
                }

                // 裁剪模板
                using var cropped = _sourceBitmap.Clone(_selectedRegion, _sourceBitmap.PixelFormat);
                
                // 创建目录
                string categoryDir = Path.Combine("Templates", category);
                if (!Directory.Exists(categoryDir))
                {
                    Directory.CreateDirectory(categoryDir);
                }

                // 保存模板
                string filePath = Path.Combine(categoryDir, $"{templateName}.png");
                cropped.Save(filePath, ImageFormat.Png);

                MessageBox.Show($"模板已保存到:\n{filePath}", "成功", 
                    MessageBoxButton.OK, MessageBoxImage.Information);

                StatusText.Text = $"模板已保存: {templateName}";
                
                // 清除选择，准备下一个
                ClearSelection();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AdjustBrightnessButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sourceBitmap == null || _selectedRegion.Width == 0 || _selectedRegion.Height == 0)
                return;

            // TODO: 实现亮度调整功能
            MessageBox.Show("亮度调整功能待实现", "提示", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BatchCaptureButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: 实现批量采集功能
            MessageBox.Show("批量采集功能待实现\n\n功能说明:\n1. 捕获游戏画面\n2. 自动识别UI元素\n3. 批量提取模板", 
                "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
