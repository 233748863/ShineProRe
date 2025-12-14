using OpenCvSharp;

namespace ShineProCS.Core.Interfaces
{
    /// <summary>
    /// 图像获取接口
    /// 定义了屏幕截图的基本操作（类似 Python 版本的 图像获取接口）
    /// </summary>
    public interface IImageInterface
    {
        /// <summary>
        /// 获取指定屏幕区域的图像
        /// </summary>
        /// <param name="x">起始 X 坐标</param>
        /// <param name="y">起始 Y 坐标</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <returns>OpenCV 格式的图像（Mat 对象），失败返回 null</returns>
        Mat? GetScreenRegion(int x, int y, int width, int height);
    }
}
