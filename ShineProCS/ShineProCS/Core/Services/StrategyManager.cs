using System;
using System.Collections.Generic;
using System.Linq;

namespace ShineProCS.Core.Services
{
    using ShineProCS.Core.Strategies;
    using ShineProCS.Models;

    /// <summary>
    /// 策略管理器
    /// 负责管理和调度不同的技能释放策略
    /// </summary>
    public class StrategyManager
    {
        private readonly List<ISkillStrategy> _strategies = new List<ISkillStrategy>();
        private ISkillStrategy _defaultStrategy;

        public StrategyManager()
        {
            // 注册默认策略
            _defaultStrategy = new DefaultLoopStrategy();
            _strategies.Add(_defaultStrategy);
            _strategies.Add(new DispelLoopStrategy());
        }

        /// <summary>
        /// 根据上下文选择最佳策略并执行
        /// </summary>
        public SkillRuntimeState? GetNextSkill(StrategyContext context)
        {
            // 1. 查找第一个满足执行条件的非默认策略
            var strategy = _strategies
                .Where(s => s != _defaultStrategy)
                .FirstOrDefault(s => s.CanExecute(context));

            // 2. 如果没有特殊策略满足条件，使用默认策略
            strategy ??= _defaultStrategy;

            Console.WriteLine($"[StrategyManager] 使用策略: {strategy.Name}");
            return strategy.SelectSkill(context);
        }

        /// <summary>
        /// 动态添加新策略
        /// </summary>
        public void AddStrategy(ISkillStrategy strategy)
        {
            if (!_strategies.Any(s => s.Name == strategy.Name))
            {
                _strategies.Add(strategy);
            }
        }
    }
}
