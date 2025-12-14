using OpenCvSharp;

namespace ShineProCS.Models
{
    /// <summary>
    /// 游戏状态信息
    /// 用于存储从图像识别中提取的游戏状态数据
    /// 
    /// 【状态监测说明】
    /// 通过图像识别技术检测游戏中的各种状态
    /// 例如：HP、MP、Buff 状态等
    /// </summary>
    public class GameState
    {
        /// <summary>
        /// 当前 HP 百分比（0.0 - 1.0）
        /// </summary>
        public double HpPercentage { get; set; }

        /// <summary>
        /// 当前 MP 百分比（0.0 - 1.0）
        /// </summary>
        public double MpPercentage { get; set; }

        /// <summary>
        /// 是否存在目标
        /// </summary>
        public bool HasTarget { get; set; }

        /// <summary>
        /// 目标位置（屏幕坐标）
        /// </summary>
        public Point? TargetPosition { get; set; }

        /// <summary>
        /// 检测到的 Buff 列表
        /// </summary>
        public List<string> ActiveBuffs { get; set; } = new List<string>();

        /// <summary>
        /// 状态更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public GameState()
        {
            HpPercentage = 1.0;
            MpPercentage = 1.0;
            HasTarget = false;
            UpdateTime = DateTime.Now;
        }
    }
}
