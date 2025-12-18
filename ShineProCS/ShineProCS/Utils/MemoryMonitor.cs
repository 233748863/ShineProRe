using System;
using System.Diagnostics;
using System.Runtime;

namespace ShineProCS.Utils
{
    /// <summary>
    /// 内存监控器
    /// 负责监控应用程序的内存使用情况，并在必要时触发清理
    /// 
    /// 【内存管理说明】
    /// 1. 监控工作集（Working Set）和私有内存（Private Bytes）
    /// 2. 提供内存统计信息
    /// 3. 支持手动和自动触发垃圾回收（GC）
    /// </summary>
    public class MemoryMonitor
    {
        private readonly Process _currentProcess;
        private readonly object _lock = new object();

        /// <summary>
        /// 构造函数
        /// </summary>
        public MemoryMonitor()
        {
            _currentProcess = Process.GetCurrentProcess();
        }

        /// <summary>
        /// 获取当前内存统计信息（MB）
        /// </summary>
        public (double WorkingSet, double PrivateMemory, double ManagedMemory) GetMemoryStats()
        {
            _currentProcess.Refresh();
            
            double workingSet = _currentProcess.WorkingSet64 / 1024.0 / 1024.0;
            double privateMemory = _currentProcess.PrivateMemorySize64 / 1024.0 / 1024.0;
            double managedMemory = GC.GetTotalMemory(false) / 1024.0 / 1024.0;

            return (workingSet, privateMemory, managedMemory);
        }

        /// <summary>
        /// 强制执行内存清理
        /// 
        /// 【注意】
        /// 频繁调用 GC 会影响性能，应仅在必要时（如内存占用过高）调用
        /// </summary>
        public void ForceCleanup()
        {
            lock (_lock)
            {
                // 强制进行完全垃圾回收
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect(); // 再次收集以清理终结器释放的对象

                // 尝试将内存归还给操作系统
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            }
        }

        /// <summary>
        /// 检查内存是否超过安全阈值
        /// </summary>
        /// <param name="thresholdMb">阈值（MB）</param>
        public bool IsMemoryHigh(double thresholdMb = 200)
        {
            var stats = GetMemoryStats();
            return stats.PrivateMemory > thresholdMb;
        }

        /// <summary>
        /// 生成内存报告
        /// </summary>
        public string GenerateReport()
        {
            var (ws, pm, mm) = GetMemoryStats();
            return $@"
=== 内存监控报告 ===
工作集内存: {ws:F2} MB
私有内存: {pm:F2} MB
托管内存: {mm:F2} MB
GC 模式: {GCSettings.IsServerGC} (Server GC)
LOH 压缩: {GCSettings.LargeObjectHeapCompactionMode}
==================
";
        }
    }
}
