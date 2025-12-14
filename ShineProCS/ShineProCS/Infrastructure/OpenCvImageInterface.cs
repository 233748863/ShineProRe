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

                // ===== 第二步：将 Bitmap 转换为 OpenCV 的 Mat 格式 =====
                // 手动转换 Bitmap 到 Mat
                var mat = new Mat(height, width, MatType.CV_8UC3);
                
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
    }
}
