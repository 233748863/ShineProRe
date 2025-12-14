using System;

namespace ShineProCS.Models
{
    /// <summary>
    /// 技能运行时状态
    /// 用于跟踪技能的冷却时间和可用性
    /// 
    /// 【运行时状态说明】
    /// 这个类在程序运行时动态维护技能状态
    /// 与 SkillConfig（配置）不同，这个类记录实时数据
    /// </summary>
    public class SkillRuntimeState
    {
        /// <summary>
        /// 技能配置（引用）
        /// </summary>
        public SkillConfig Config { get; set; }

        /// <summary>
        /// 上次释放时间
        /// 用于计算冷却是否结束
        /// </summary>
        public DateTime LastUsedTime { get; set; }

        /// <summary>
        /// 是否可用（冷却是否结束）
        /// </summary>
        public bool IsAvailable
        {
            get
            {
                // 如果技能未启用，直接返回 false
                if (!Config.Enabled)
                    return false;

                // 计算距离上次使用的时间（秒）
                var elapsedSeconds = (DateTime.Now - LastUsedTime).TotalSeconds;
                
                // 如果经过的时间大于等于冷却时间，则可用
                return elapsedSeconds >= Config.Cooldown;
            }
        }

        /// <summary>
        /// 剩余冷却时间（秒）
        /// </summary>
        public double RemainingCooldown
        {
            get
            {
                var elapsed = (DateTime.Now - LastUsedTime).TotalSeconds;
                var remaining = Config.Cooldown - elapsed;
                return remaining > 0 ? remaining : 0;
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public SkillRuntimeState(SkillConfig config)
        {
            Config = config;
            // 初始化为很久以前，确保第一次可以立即使用
            LastUsedTime = DateTime.MinValue;
        }

        /// <summary>
        /// 标记技能已使用
        /// 更新上次使用时间，开始冷却
        /// </summary>
        public void MarkAsUsed()
        {
            LastUsedTime = DateTime.Now;
        }
    }
}
