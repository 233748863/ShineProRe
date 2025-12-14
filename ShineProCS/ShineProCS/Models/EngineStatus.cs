namespace ShineProCS.Models
{
    /// <summary>
    /// 引擎运行状态模型
    /// 用于在 UI 和引擎之间传递状态信息（类似 Python 版本的 get_running_status 返回值）
    /// </summary>
    public class EngineStatus
    {
        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning { get; set; }

        /// <summary>
        /// 是否已暂停
        /// </summary>
        public bool IsPaused { get; set; }

        /// <summary>
        /// 当前运行模式（如"智能识别"、"简单循环"等）
        /// </summary>
        public string Mode { get; set; } = "等待启动";

        /// <summary>
        /// 执行次数（已执行了多少次技能循环）
        /// </summary>
        public int ExecutionCount { get; set; }

        /// <summary>
        /// 平均响应时间（秒）
        /// </summary>
        public double AvgResponseTime { get; set; }

        /// <summary>
        /// 成功率（百分比）
        /// </summary>
        public double SuccessRate { get; set; }
    }
}
