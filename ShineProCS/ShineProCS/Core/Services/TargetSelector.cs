using System;
using System.Collections.Generic;
using System.Linq;
using OpenCvSharp;
using ShineProCS.Core.Interfaces;

namespace ShineProCS.Core.Services
{
    /// <summary>
    /// 目标选择器
    /// 负责在屏幕上查找和选择目标
    /// 
    /// 【目标选择策略】
    /// 1. 最近目标 - 选择距离最近的目标
    /// 2. 优先级目标 - 根据目标类型选择
    /// 3. 血量最低 - 选择血量最低的目标
    /// </summary>
    public class TargetSelector
    {
        private readonly IImageInterface _imageInterface;
        private readonly ConfigManager _config;

        /// <summary>
        /// 目标信息
        /// </summary>
        public class TargetInfo
        {
            public Point Position { get; set; }      // 目标位置
            public double Distance { get; set; }     // 距离
            public double Confidence { get; set; }   // 置信度
            public string Type { get; set; } = "";   // 目标类型
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public TargetSelector(IImageInterface imageInterface, ConfigManager config)
        {
            _imageInterface = imageInterface;
            _config = config;
        }

        /// <summary>
        /// 查找所有目标
        /// 
        /// 【检测原理】
        /// 1. 截取检测区域
        /// 2. 使用颜色识别或模板匹配查找目标
        /// 3. 返回所有找到的目标列表
        /// </summary>
        public List<TargetInfo> FindAllTargets()
        {
            var targets = new List<TargetInfo>();

            try
            {
                // 获取检测区域配置
                var region = _config.GetDetectionRegion();
                if (region == null || region.Length != 4)
                    return targets;

                // 截取检测区域
                var image = _imageInterface.GetScreenRegion(
                    region[0], region[1], region[2], region[3]);

                if (image == null)
                    return targets;

                // ===== 使用颜色检测查找目标 =====
                // 这里是示例实现，实际项目中可能需要更复杂的算法

                // 转换为 HSV 色彩空间
                using var hsv = new Mat();
                Cv2.CvtColor(image, hsv, ColorConversionCodes.BGR2HSV);

                // 定义目标颜色范围（例如红色敌人）
                var lowerRed1 = new Scalar(0, 100, 100);
                var upperRed1 = new Scalar(10, 255, 255);
                var lowerRed2 = new Scalar(160, 100, 100);
                var upperRed2 = new Scalar(180, 255, 255);

                // 创建红色掩码
                using var mask1 = new Mat();
                using var mask2 = new Mat();
                using var mask = new Mat();
                Cv2.InRange(hsv, lowerRed1, upperRed1, mask1);
                Cv2.InRange(hsv, lowerRed2, upperRed2, mask2);
                Cv2.BitwiseOr(mask1, mask2, mask);

                // 形态学操作（去噪）
                using var kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(5, 5));
                Cv2.MorphologyEx(mask, mask, MorphTypes.Open, kernel);
                Cv2.MorphologyEx(mask, mask, MorphTypes.Close, kernel);

                // 查找轮廓
                Cv2.FindContours(mask, out Point[][] contours, out _, 
                    RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                // 屏幕中心点（用于计算距离）
                Point screenCenter = new Point(region[2] / 2, region[3] / 2);

                // 处理每个轮廓
                foreach (var contour in contours)
                {
                    // 计算轮廓面积（过滤太小的噪点）
                    double area = Cv2.ContourArea(contour);
                    if (area < 100) // 最小面积阈值
                        continue;

                    // 计算轮廓中心点
                    var moments = Cv2.Moments(contour);
                    if (moments.M00 == 0)
                        continue;

                    int cx = (int)(moments.M10 / moments.M00);
                    int cy = (int)(moments.M01 / moments.M00);

                    // 计算距离屏幕中心的距离
                    double distance = Math.Sqrt(
                        Math.Pow(cx - screenCenter.X, 2) + 
                        Math.Pow(cy - screenCenter.Y, 2));

                    // 添加到目标列表
                    targets.Add(new TargetInfo
                    {
                        Position = new Point(region[0] + cx, region[1] + cy),
                        Distance = distance,
                        Confidence = Math.Min(area / 1000.0, 1.0), // 简单的置信度计算
                        Type = "Enemy"
                    });
                }

                image.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 目标查找失败: {ex.Message}");
            }

            return targets;
        }

        /// <summary>
        /// 选择最佳目标
        /// 
        /// 【选择策略】
        /// 默认选择距离最近的目标
        /// 可以根据需要实现其他策略
        /// </summary>
        public TargetInfo? SelectBestTarget()
        {
            var targets = FindAllTargets();

            if (targets.Count == 0)
                return null;

            // ===== 策略 1: 选择距离最近的目标 =====
            // 使用 LINQ 排序并选择第一个
            var nearestTarget = targets
                .OrderBy(t => t.Distance)
                .FirstOrDefault();

            return nearestTarget;
        }

        /// <summary>
        /// 选择优先级最高的目标
        /// 
        /// 【优先级规则】
        /// 可以根据目标类型、血量等因素定义优先级
        /// </summary>
        public TargetInfo? SelectPriorityTarget()
        {
            var targets = FindAllTargets();

            if (targets.Count == 0)
                return null;

            // ===== 策略 2: 根据置信度和距离综合评分 =====
            var bestTarget = targets
                .OrderByDescending(t => t.Confidence - t.Distance / 1000.0) // 综合评分
                .FirstOrDefault();

            return bestTarget;
        }

        /// <summary>
        /// 使用模板匹配查找特定目标
        /// 
        /// 【模板匹配】
        /// 需要预先准备目标的模板图片
        /// 使用 OpenCV 的 MatchTemplate 函数
        /// </summary>
        public Point? FindTargetByTemplate(Mat template, double threshold = 0.8)
        {
            try
            {
                var region = _config.GetDetectionRegion();
                if (region == null || region.Length != 4)
                    return null;

                var image = _imageInterface.GetScreenRegion(
                    region[0], region[1], region[2], region[3]);

                if (image == null)
                    return null;

                // 执行模板匹配
                using var result = new Mat();
                Cv2.MatchTemplate(image, template, result, TemplateMatchModes.CCoeffNormed);

                // 查找最佳匹配位置
                Cv2.MinMaxLoc(result, out _, out double maxVal, out _, out Point maxLoc);

                image.Dispose();

                // 如果匹配度超过阈值，返回位置
                if (maxVal >= threshold)
                {
                    // 计算模板中心点
                    int centerX = maxLoc.X + template.Width / 2;
                    int centerY = maxLoc.Y + template.Height / 2;

                    return new Point(region[0] + centerX, region[1] + centerY);
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 模板匹配失败: {ex.Message}");
                return null;
            }
        }
    }
}
