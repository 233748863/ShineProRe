using System;
using System.Collections.Generic;

namespace ShineProCS.Models
{
    /// <summary>
    /// 性能指标
    /// 用于存储引擎的性能统计数据
    /// 
    /// 【性能监控说明】
    /// 通过收集和分析性能数据，可以：
    /// 1. 发现性能瓶颈
    /// 2. 优化循环延迟
    /// 3. 监控系统稳定性
    /// </summary>
    public class PerformanceMetrics
    {
        /// <summary>
        /// 总执行次数
        /// </summary>
        public int TotalExecutions { get; set; }

        /// <summary>
        /// 成功执行次数
        /// </summary>
        public int SuccessfulExecutions { get; set; }

        /// <summary>
        /// 失败执行次数
        /// </summary>
        public int FailedExecutions { get; set; }

        /// <summary>
        /// 总执行时间（秒）
        /// </summary>
        public double TotalExecutionTime { get; set; }

        /// <summary>
        /// 平均响应时间（秒）
        /// </summary>
        public double AverageResponseTime => 
            TotalExecutions > 0 ? TotalExecutionTime / TotalExecutions : 0;

        /// <summary>
        /// 最小响应时间（秒）
        /// </summary>
        public double MinResponseTime { get; set; } = double.MaxValue;

        /// <summary>
        /// 最大响应时间（秒）
        /// </summary>
        public double MaxResponseTime { get; set; }

        /// <summary>
        /// 成功率（百分比）
        /// </summary>
        public double SuccessRate =>
            TotalExecutions > 0 ? (double)SuccessfulExecutions / TotalExecutions * 100 : 0;

        /// <summary>
        /// 最近的响应时间列表（用于分析趋势）
        /// 保留最近 100 次
        /// </summary>
        public Queue<double> RecentResponseTimes { get; set; } = new Queue<double>(100);

        /// <summary>
        /// 开始监控时间
        /// </summary>
        public DateTime StartTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 运行时长（秒）
        /// </summary>
        public double RunningDuration => (DateTime.Now - StartTime).TotalSeconds;

        /// <summary>
        /// 每秒执行次数（TPS）
        /// </summary>
        public double ExecutionsPerSecond =>
            RunningDuration > 0 ? TotalExecutions / RunningDuration : 0;
    }
}
