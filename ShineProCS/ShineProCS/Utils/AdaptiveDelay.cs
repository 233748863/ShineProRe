using System;

namespace ShineProCS.Utils
{
    /// <summary>
    /// 自适应延迟计算器
    /// 根据系统性能和响应时间动态调整循环延迟
    /// 
    /// 【自适应逻辑说明】
    /// 1. 如果平均响应时间过长，适当增加延迟以减轻系统负担
    /// 2. 如果响应时间非常快且稳定，可以尝试减小延迟以提升效率
    /// 3. 设定延迟的上下限，确保循环不会过快或过慢
    /// </summary>
    public class AdaptiveDelay
    {
        private readonly int _minDelay;
        private readonly int _maxDelay;
        private int _currentDelay;

        /// <summary>
        /// 是否处于战斗模式（战斗模式下响应更激进）
        /// </summary>
        public bool IsCombatMode { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="baseDelay">基础延迟（毫秒）</param>
        /// <param name="minDelay">最小延迟限制</param>
        /// <param name="maxDelay">最大延迟限制</param>
        public AdaptiveDelay(int baseDelay, int minDelay = 100, int maxDelay = 2000)
        {
            _currentDelay = baseDelay;
            _minDelay = minDelay;
            _maxDelay = maxDelay;
        }

        /// <summary>
        /// 获取当前应使用的延迟时间
        /// </summary>
        public int CurrentDelay => _currentDelay;

        /// <summary>
        /// 根据最近的响应时间调整延迟
        /// </summary>
        /// <param name="avgResponseTime">平均响应时间（秒）</param>
        /// <param name="targetResponseTime">目标响应时间（秒，默认 0.1s）</param>
        public void Adjust(double avgResponseTime, double targetResponseTime = 0.1)
        {
            // 战斗模式下，目标响应时间减半，且最小延迟允许更低
            double effectiveTarget = IsCombatMode ? targetResponseTime * 0.5 : targetResponseTime;
            int effectiveMin = IsCombatMode ? Math.Max(20, _minDelay / 2) : _minDelay;

            // 简单的自适应逻辑：
            // 如果响应时间超过目标的 1.5 倍，增加 10% 的延迟
            if (avgResponseTime > effectiveTarget * 1.5)
            {
                _currentDelay = (int)Math.Min(_maxDelay, _currentDelay * 1.1);
            }
            // 如果响应时间低于目标的 0.5 倍，减小 5% 的延迟
            else if (avgResponseTime < effectiveTarget * 0.5)
            {
                _currentDelay = (int)Math.Max(effectiveMin, _currentDelay * 0.95);
            }
        }

        /// <summary>
        /// 重置为基础延迟
        /// </summary>
        public void Reset(int baseDelay)
        {
            _currentDelay = Math.Clamp(baseDelay, _minDelay, _maxDelay);
        }
    }
}
