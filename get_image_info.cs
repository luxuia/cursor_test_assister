using System;
using System.Drawing;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("请提供图片文件路径");
            return;
        }

        string imagePath = args[0];

        try
        {
            using (Bitmap bitmap = new Bitmap(imagePath))
            {
                Console.WriteLine($"图片文件: {imagePath}");
                Console.WriteLine($"尺寸: {bitmap.Width} x {bitmap.Height} 像素");
                Console.WriteLine($"水平分辨率: {bitmap.HorizontalResolution} dpi");
                Console.WriteLine($"垂直分辨率: {bitmap.VerticalResolution} dpi");
                Console.WriteLine($"物理尺寸: {Math.Round(bitmap.Width / bitmap.HorizontalResolution, 2)} x {Math.Round(bitmap.Height / bitmap.VerticalResolution, 2)} 英寸");
                Console.WriteLine($"像素格式: {bitmap.PixelFormat}");
                Console.WriteLine($"原始图片大小: {new System.IO.FileInfo(imagePath).Length / 1024} KB");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"错误: {ex.Message}");
        }
    }
}
