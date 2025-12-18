using System;
using System.Linq;
using ShineProCS.Models;

namespace ShineProCS.Core.Strategies
{
    /// <summary>
    /// 默认循环策略
    /// 按照优先级和冷却时间选择技能（Python 版本的默认逻辑）
    /// </summary>
    public class DefaultLoopStrategy : ISkillStrategy
    {
        public string Name => "DefaultLoop";

        public bool CanExecute(StrategyContext context)
        {
            // 默认策略始终可以执行
            return true;
        }

        public SkillRuntimeState? SelectSkill(StrategyContext context)
        {
            // 按照列表顺序依次检测技能
            // 这种模式下，列表前面的技能具有天然的“优先权”
            foreach (var skill in context.SkillStates)
            {
                // 1. 基础检查：技能是否启用
                if (!skill.Config.Enabled) continue;

                // 2. 逻辑冷却检查 (仅在非智能模式下严格遵守)
                if (context.LoopMode != "Smart" && !skill.IsAvailable) continue;

                // 注意：具体的视觉检测和 Buff 检测将在 SkillStateDetector 中进行
                // 这里我们返回第一个“潜在可用”的技能，由引擎后续做最终确认
                return skill;
            }

            return null;
        }
    }

    /// <summary>
    /// 驱散循环策略
    /// 优先释放具有驱散属性的技能（模拟 Python 版本的驱散策略）
    /// </summary>
    public class DispelLoopStrategy : ISkillStrategy
    {
        public string Name => "DispelLoop";

        public bool CanExecute(StrategyContext context)
        {
            // 只有在特定模式下才执行
            return context.LoopMode == "Dispel";
        }

        public SkillRuntimeState? SelectSkill(StrategyContext context)
        {
            // 1. 优先查找名称中包含“驱散”或“净化”的可用技能
            var dispelSkills = context.SkillStates
                .Where(s => s.Config.Enabled && (context.LoopMode == "Smart" || s.IsAvailable) && 
                           (s.Config.Name.Contains("驱散") || s.Config.Name.Contains("净化")))
                .ToList();

            if (dispelSkills.Any())
            {
                return dispelSkills.OrderByDescending(s => s.Config.Priority).First();
            }

            // 2. 如果没有驱散技能可用，退回到默认逻辑
            return context.SkillStates
                .Where(s => s.Config.Enabled && (context.LoopMode == "Smart" || s.IsAvailable))
                .OrderByDescending(s => s.Config.Priority)
                .FirstOrDefault();
        }
    }
}
