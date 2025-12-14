using System;
using System.Diagnostics;
using ShineProCS.Models;

namespace ShineProCS.Utils
{
    /// <summary>
    /// 性能监控器
    /// 负责收集和分析引擎的性能数据
    /// 
    /// 【使用方法】
    /// 1. 在操作开始前调用 StartOperation()
    /// 2. 在操作结束后调用 EndOperation()
    /// 3. 定期调用 GetMetrics() 获取性能报告
    /// </summary>
    public class PerformanceMonitor
    {
        private readonly PerformanceMetrics _metrics;
        private readonly Stopwatch _currentOperation;
        private readonly object _lock = new object();

        /// <summary>
        /// 构造函数
        /// </summary>
        public PerformanceMonitor()
        {
            _metrics = new PerformanceMetrics();
            _currentOperation = new Stopwatch();
        }

        /// <summary>
        /// 开始一个操作的计时
        /// 
        /// 【使用示例】
        /// monitor.StartOperation();
        /// // 执行操作
        /// monitor.EndOperation(success: true);
        /// </summary>
        public void StartOperation()
        {
            _currentOperation.Restart();
        }

        /// <summary>
        /// 结束一个操作的计时并记录结果
        /// </summary>
        /// <param name="success">操作是否成功</param>
        public void EndOperation(bool success = true)
        {
            _currentOperation.Stop();
            double elapsedSeconds = _currentOperation.Elapsed.TotalSeconds;

            lock (_lock)
            {
                // 更新总次数
                _metrics.TotalExecutions++;

                // 更新成功/失败次数
                if (success)
                    _metrics.SuccessfulExecutions++;
                else
                    _metrics.FailedExecutions++;

                // 更新总时间
                _metrics.TotalExecutionTime += elapsedSeconds;

                // 更新最小/最大响应时间
                if (elapsedSeconds < _metrics.MinResponseTime)
                    _metrics.MinResponseTime = elapsedSeconds;

                if (elapsedSeconds > _metrics.MaxResponseTime)
                    _metrics.MaxResponseTime = elapsedSeconds;

                // 记录最近的响应时间
                _metrics.RecentResponseTimes.Enqueue(elapsedSeconds);
                
                // 保持队列大小在 100 以内
                if (_metrics.RecentResponseTimes.Count > 100)
                    _metrics.RecentResponseTimes.Dequeue();
            }
        }

        /// <summary>
        /// 获取当前性能指标
        /// </summary>
        public PerformanceMetrics GetMetrics()
        {
            lock (_lock)
            {
                // 返回副本，避免外部修改
                return new PerformanceMetrics
                {
                    TotalExecutions = _metrics.TotalExecutions,
                    SuccessfulExecutions = _metrics.SuccessfulExecutions,
                    FailedExecutions = _metrics.FailedExecutions,
                    TotalExecutionTime = _metrics.TotalExecutionTime,
                    MinResponseTime = _metrics.MinResponseTime,
                    MaxResponseTime = _metrics.MaxResponseTime,
                    StartTime = _metrics.StartTime,
                    RecentResponseTimes = new Queue<double>(_metrics.RecentResponseTimes)
                };
            }
        }

        /// <summary>
        /// 重置所有性能指标
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                _metrics.TotalExecutions = 0;
                _metrics.SuccessfulExecutions = 0;
                _metrics.FailedExecutions = 0;
                _metrics.TotalExecutionTime = 0;
                _metrics.MinResponseTime = double.MaxValue;
                _metrics.MaxResponseTime = 0;
                _metrics.StartTime = DateTime.Now;
                _metrics.RecentResponseTimes.Clear();
            }
        }

        /// <summary>
        /// 生成性能报告（文本格式）
        /// </summary>
        public string GenerateReport()
        {
            var metrics = GetMetrics();
            
            return $@"
=== 性能监控报告 ===
运行时长: {metrics.RunningDuration:F2} 秒
总执行次数: {metrics.TotalExecutions}
成功次数: {metrics.SuccessfulExecutions}
失败次数: {metrics.FailedExecutions}
成功率: {metrics.SuccessRate:F2}%
---
平均响应时间: {metrics.AverageResponseTime:F4} 秒
最小响应时间: {(metrics.MinResponseTime == double.MaxValue ? 0 : metrics.MinResponseTime):F4} 秒
最大响应时间: {metrics.MaxResponseTime:F4} 秒
每秒执行次数: {metrics.ExecutionsPerSecond:F2} TPS
==================
";
        }

        /// <summary>
        /// 检查性能是否异常
        /// 
        /// 【异常判断标准】
        /// 1. 平均响应时间超过阈值
        /// 2. 成功率低于阈值
        /// </summary>
        /// <param name="maxAvgResponseTime">最大平均响应时间（秒）</param>
        /// <param name="minSuccessRate">最小成功率（百分比）</param>
        /// <returns>是否存在性能异常</returns>
        public bool HasPerformanceIssue(double maxAvgResponseTime = 0.5, double minSuccessRate = 90.0)
        {
            var metrics = GetMetrics();

            // 检查平均响应时间
            if (metrics.AverageResponseTime > maxAvgResponseTime)
                return true;

            // 检查成功率
            if (metrics.SuccessRate < minSuccessRate && metrics.TotalExecutions > 10)
                return true;

            return false;
        }
    }
}
