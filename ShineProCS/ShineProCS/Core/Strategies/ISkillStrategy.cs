using System;
using System.Collections.Generic;
using ShineProCS.Models;

namespace ShineProCS.Core.Strategies
{
    /// <summary>
    /// 策略上下文
    /// 包含策略执行所需的各种环境信息
    /// </summary>
    public class StrategyContext
    {
        /// <summary>
        /// 当前技能状态列表
        /// </summary>
        public List<SkillRuntimeState> SkillStates { get; set; } = new List<SkillRuntimeState>();

        /// <summary>
        /// 当前游戏状态
        /// </summary>
        public GameState GameState { get; set; } = new GameState();

        /// <summary>
        /// 循环模式
        /// </summary>
        public string LoopMode { get; set; } = "Default";

        /// <summary>
        /// 上次执行的技能
        /// </summary>
        public SkillRuntimeState? LastSkill { get; set; }

        /// <summary>
        /// 执行次数
        /// </summary>
        public int ExecutionCount { get; set; }
    }

    /// <summary>
    /// 技能策略接口
    /// 定义了所有技能循环策略必须实现的方法
    /// </summary>
    public interface ISkillStrategy
    {
        /// <summary>
        /// 策略名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 选择下一个要释放的技能
        /// </summary>
        /// <param name="context">策略上下文</param>
        /// <returns>选中的技能，如果没有合适的则返回 null</returns>
        SkillRuntimeState? SelectSkill(StrategyContext context);

        /// <summary>
        /// 是否适用于当前上下文
        /// </summary>
        bool CanExecute(StrategyContext context);
    }
}
