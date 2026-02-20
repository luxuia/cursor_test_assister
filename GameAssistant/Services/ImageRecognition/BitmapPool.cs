using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;

namespace GameAssistant.Services.ImageRecognition
{
    /// <summary>
    /// Bitmap对象池，用于减少GC压力
    /// </summary>
    public class BitmapPool : IDisposable
    {
        private readonly ConcurrentQueue<Bitmap> _pool = new ConcurrentQueue<Bitmap>();
        private readonly int _maxPoolSize;
        private readonly PixelFormat _pixelFormat;
        private readonly int _width;
        private readonly int _height;

        public BitmapPool(int width, int height, PixelFormat pixelFormat = PixelFormat.Format32bppArgb, int maxPoolSize = 10)
        {
            _width = width;
            _height = height;
            _pixelFormat = pixelFormat;
            _maxPoolSize = maxPoolSize;
        }

        /// <summary>
        /// 从池中获取Bitmap
        /// </summary>
        public Bitmap Rent()
        {
            if (_pool.TryDequeue(out var bitmap))
            {
                return bitmap;
            }

            return new Bitmap(_width, _height, _pixelFormat);
        }

        /// <summary>
        /// 归还Bitmap到池中
        /// </summary>
        public void Return(Bitmap bitmap)
        {
            if (bitmap == null)
                return;

            // 检查尺寸是否匹配
            if (bitmap.Width != _width || bitmap.Height != _height || bitmap.PixelFormat != _pixelFormat)
            {
                bitmap.Dispose();
                return;
            }

            if (_pool.Count < _maxPoolSize)
            {
                _pool.Enqueue(bitmap);
            }
            else
            {
                bitmap.Dispose();
            }
        }

        public void Dispose()
        {
            while (_pool.TryDequeue(out var bitmap))
            {
                bitmap.Dispose();
            }
        }
    }
}
