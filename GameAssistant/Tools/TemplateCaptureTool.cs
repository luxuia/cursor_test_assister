using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using System.Windows.Media;

namespace GameAssistant.Tools
{
    /// <summary>
    /// 模板采集工具
    /// </summary>
    public class TemplateCaptureTool
    {
        /// <summary>
        /// 从屏幕截图采集模板
        /// </summary>
        public static void CaptureTemplateFromScreen(string templateName, string category, Rectangle region)
        {
            try
            {
                // 创建目录
                string categoryDir = Path.Combine("Templates", category);
                if (!Directory.Exists(categoryDir))
                {
                    Directory.CreateDirectory(categoryDir);
                }

                // 截图
                using var bitmap = CaptureScreenRegion(region);
                
                // 保存模板
                string filePath = Path.Combine(categoryDir, $"{templateName}.png");
                bitmap.Save(filePath, ImageFormat.Png);
                
                MessageBox.Show($"模板已保存到: {filePath}", "成功", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存模板失败: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 从Bitmap采集模板
        /// </summary>
        public static void CaptureTemplateFromBitmap(string templateName, string category, Bitmap source, Rectangle region)
        {
            try
            {
                // 创建目录
                string categoryDir = Path.Combine("Templates", category);
                if (!Directory.Exists(categoryDir))
                {
                    Directory.CreateDirectory(categoryDir);
                }

                // 裁剪区域
                using var cropped = source.Clone(region, source.PixelFormat);
                
                // 保存模板
                string filePath = Path.Combine(categoryDir, $"{templateName}.png");
                cropped.Save(filePath, ImageFormat.Png);
            }
            catch (Exception ex)
            {
                throw new Exception($"保存模板失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 截图指定区域
        /// </summary>
        private static Bitmap CaptureScreenRegion(Rectangle region)
        {
            Bitmap bitmap = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(region.X, region.Y, 0, 0, region.Size);
            }
            return bitmap;
        }

        /// <summary>
        /// 交互式选择区域并采集模板
        /// </summary>
        public static void CaptureTemplateInteractive(string templateName, string category)
        {
            var form = new Form
            {
                WindowState = FormWindowState.Maximized,
                FormBorderStyle = FormBorderStyle.None,
                TopMost = true,
                BackColor = Color.Black,
                Opacity = 0.3,
                Cursor = Cursors.Cross
            };

            Rectangle? selectedRegion = null;
            Point startPoint = Point.Empty;
            bool isSelecting = false;

            form.Paint += (s, e) =>
            {
                if (isSelecting && selectedRegion.HasValue)
                {
                    e.Graphics.DrawRectangle(Pens.Red, selectedRegion.Value);
                    e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(50, Color.Red)), selectedRegion.Value);
                }
            };

            form.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    isSelecting = true;
                    startPoint = e.Location;
                }
            };

            form.MouseMove += (s, e) =>
            {
                if (isSelecting)
                {
                    int x = Math.Min(startPoint.X, e.X);
                    int y = Math.Min(startPoint.Y, e.Y);
                    int width = Math.Abs(e.X - startPoint.X);
                    int height = Math.Abs(e.Y - startPoint.Y);
                    selectedRegion = new Rectangle(x, y, width, height);
                    form.Invalidate();
                }
            };

            form.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Left && isSelecting && selectedRegion.HasValue)
                {
                    isSelecting = false;
                    
                    // 转换为屏幕坐标
                    var screenRegion = new Rectangle(
                        form.PointToScreen(selectedRegion.Value.Location),
                        selectedRegion.Value.Size
                    );
                    
                    CaptureTemplateFromScreen(templateName, category, screenRegion);
                    form.Close();
                }
            };

            form.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    form.Close();
                }
            };

            form.ShowDialog();
        }
    }
}
