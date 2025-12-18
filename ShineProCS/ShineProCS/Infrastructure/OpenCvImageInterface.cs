using System;
using System.Drawing;
using System.Drawing.Imaging;
using OpenCvSharp;
using ShineProCS.Core.Interfaces;

namespace ShineProCS.Infrastructure
{
    /// <summary>
    /// OpenCV 图像接口实现
    /// 使用 Windows GDI+ 截图，然后转换为 OpenCV 格式
    /// （类似 Python 版本的 Windows图像接口）
    /// </summary>
    public class OpenCvImageInterface : IImageInterface
    {
        private readonly MatPool _pool = new MatPool(5); // 预分配 5 个缓冲区

        /// <summary>
        /// 获取指定屏幕区域的图像
        /// </summary>
        public Mat? GetScreenRegion(int x, int y, int width, int height)
        {
            try
            {
                // ===== 第一步：使用 GDI+ 截取屏幕 =====
                using var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                using var graphics = Graphics.FromImage(bitmap);
                graphics.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(width, height));

                // ===== 第二步：从池中获取或创建 Mat =====
                var mat = _pool.Rent(width, height);
                
                // 锁定 Bitmap 数据
                var bitmapData = bitmap.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format24bppRgb);

                try
                {
                    // 复制数据到 Mat
                    unsafe
                    {
                        byte* srcPtr = (byte*)bitmapData.Scan0;
                        byte* dstPtr = (byte*)mat.Data;
                        int stride = bitmapData.Stride;
                        
                        for (int row = 0; row < height; row++)
                        {
                            Buffer.MemoryCopy(
                                srcPtr + row * stride,
                                dstPtr + row * width * 3,
                                width * 3,
                                width * 3);
                        }
                    }
                }
                finally
                {
                    bitmap.UnlockBits(bitmapData);
                }
                
                return mat;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"截图失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 归还 Mat 到池中
        /// </summary>
        public void ReturnMat(Mat mat)
        {
            _pool.Return(mat);
        }

        /// <summary>
        /// 简单的 Mat 对象池实现
        /// </summary>
        private class MatPool
        {
            private readonly System.Collections.Concurrent.ConcurrentQueue<Mat> _pool = new();
            private readonly int _maxSize;

            public MatPool(int maxSize)
            {
                _maxSize = maxSize;
            }

            public Mat Rent(int width, int height)
            {
                if (_pool.TryDequeue(out var mat))
                {
                    // 如果大小不匹配，释放并重新创建
                    if (mat.Width != width || mat.Height != height)
                    {
                        mat.Dispose();
                        return new Mat(height, width, MatType.CV_8UC3);
                    }
                    return mat;
                }
                return new Mat(height, width, MatType.CV_8UC3);
            }

            public void Return(Mat mat)
            {
                if (_pool.Count < _maxSize)
                {
                    _pool.Enqueue(mat);
                }
                else
                {
                    mat.Dispose();
                }
            }
        }
    }
}
