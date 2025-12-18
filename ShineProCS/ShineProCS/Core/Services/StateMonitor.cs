using System;
using OpenCvSharp;
using ShineProCS.Core.Interfaces;
using ShineProCS.Models;

namespace ShineProCS.Core.Services
{
    /// <summary>
    /// 状态监测器
    /// 负责检测游戏中的各种状态（HP、MP、Buff 等）
    /// 
    /// 【图像识别原理】
    /// 1. 截取指定区域的屏幕图像
    /// 2. 使用颜色识别或模板匹配检测状态
    /// 3. 计算百分比或识别特定图标
    /// </summary>
    public class StateMonitor
    {
        private readonly IImageInterface _imageInterface;
        private readonly ConfigManager _config;

        /// <summary>
        /// 构造函数
        /// </summary>
        public StateMonitor(IImageInterface imageInterface, ConfigManager config)
        {
            _imageInterface = imageInterface;
            _config = config;
        }

        /// <summary>
        /// 检测当前游戏状态
        /// 返回包含 HP、MP、目标等信息的状态对象
        /// </summary>
        public GameState DetectGameState()
        {
            var state = new GameState();

            try
            {
                // 检测 HP
                state.HpPercentage = DetectHpPercentage();

                // 检测 MP（蓝条）
                state.MpPercentage = DetectMpPercentage();

                // 检测目标
                state.HasTarget = DetectTarget(out Point? targetPos);
                state.TargetPosition = targetPos;

                // 更新时间
                state.UpdateTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 状态检测异常: {ex.Message}");
            }

            return state;
        }

        /// <summary>
        /// 检测是否处于战斗状态
        /// </summary>
        public bool DetectCombatState()
        {
            try
            {
                // 方案 1：如果有选中的目标，通常认为处于战斗状态
                if (DetectTarget(out _)) return true;

                // 方案 2：检测特定位置的“战斗图标” (需要配置)
                // var combatIconRegion = _config.GetCombatIconRegion();
                // if (combatIconRegion != null) { ... }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 检测 HP 百分比
        /// 
        /// 【检测原理】
        /// 通过颜色识别 HP 条的长度来计算百分比
        /// 1. 截取 HP 条区域
        /// 2. 统计特定颜色（红色）的像素数量
        /// 3. 计算百分比
        /// </summary>
        private double DetectHpPercentage()
        {
            // 这里是示例实现，实际项目中需要根据游戏界面调整
            // 暂时返回固定值
            return 0.8; // 80% HP
        }

        /// <summary>
        /// 检测 MP 百分比
        /// 
        /// 【检测原理】
        /// 与 HP 检测类似，但检测蓝色像素
        /// </summary>
        private double DetectMpPercentage()
        {
            try
            {
                // 获取蓝条区域配置
                var region = _config.GetManaBarRegion();
                if (region == null || region.Length != 4)
                    return 1.0;

                // 截取蓝条区域
                var image = _imageInterface.GetScreenRegion(
                    region[0], region[1], region[2], region[3]);

                if (image == null)
                    return 1.0;

                // ===== 颜色识别示例 =====
                // 统计蓝色像素数量（这里是简化版本）
                // 实际项目中需要更精确的颜色范围

                // 转换为 HSV 色彩空间（更适合颜色识别）
                using var hsv = new Mat();
                Cv2.CvtColor(image, hsv, ColorConversionCodes.BGR2HSV);

                // 定义蓝色范围（HSV）
                var lowerBlue = new Scalar(100, 50, 50);   // 蓝色下限
                var upperBlue = new Scalar(130, 255, 255); // 蓝色上限

                // 创建蓝色掩码
                using var mask = new Mat();
                Cv2.InRange(hsv, lowerBlue, upperBlue, mask);

                // 统计蓝色像素数量
                int bluePixels = Cv2.CountNonZero(mask);
                int totalPixels = image.Width * image.Height;

                // 计算百分比
                double percentage = (double)bluePixels / totalPixels;

                image.Dispose();

                return Math.Clamp(percentage, 0.0, 1.0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ MP 检测失败: {ex.Message}");
                return 1.0;
            }
        }

        /// <summary>
        /// 检测是否存在目标
        /// 
        /// 【检测原理】
        /// 使用模板匹配或颜色识别检测目标标记
        /// </summary>
        private bool DetectTarget(out Point? position)
        {
            position = null;

            try
            {
                // 获取检测区域配置
                var region = _config.GetDetectionRegion();
                if (region == null || region.Length != 4)
                    return false;

                // 截取检测区域
                var image = _imageInterface.GetScreenRegion(
                    region[0], region[1], region[2], region[3]);

                if (image == null)
                    return false;

                // ===== 简单的颜色检测示例 =====
                // 实际项目中可以使用模板匹配或更复杂的算法

                // 转换为灰度图
                using var gray = new Mat();
                Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);

                // 使用阈值检测亮点（假设目标标记是亮色）
                using var threshold = new Mat();
                Cv2.Threshold(gray, threshold, 200, 255, ThresholdTypes.Binary);

                // 查找轮廓
                Cv2.FindContours(threshold, out Point[][] contours, out _, 
                    RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                // 如果找到轮廓，认为存在目标
                if (contours.Length > 0)
                {
                    // 获取最大轮廓的中心点
                    var moments = Cv2.Moments(contours[0]);
                    int cx = (int)(moments.M10 / moments.M00);
                    int cy = (int)(moments.M01 / moments.M00);
                    
                    position = new Point(region[0] + cx, region[1] + cy);
                    
                    image.Dispose();
                    return true;
                }

                image.Dispose();
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 目标检测失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检测 Buff 状态
        /// 
        /// 【检测原理】
        /// 使用模板匹配识别 Buff 图标
        /// 需要预先准备 Buff 图标的模板图片
        /// </summary>
        public List<string> DetectBuffs()
        {
            var buffs = new List<string>();

            // 这里是示例，实际需要实现模板匹配
            // 可以参考 Python 版本的实现

            return buffs;
        }
    }
}
