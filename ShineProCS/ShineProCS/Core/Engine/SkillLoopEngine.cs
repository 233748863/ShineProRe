using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ShineProCS.Core.Interfaces;
using ShineProCS.Core.Services;
using ShineProCS.Models;

namespace ShineProCS.Core.Engine
{
    /// <summary>
    /// 技能循环引擎
    /// 核心业务逻辑类，负责技能循环的执行（类似 Python 版本的 技能循环引擎）
    /// </summary>
    public class SkillLoopEngine
    {
        // ===== 依赖注入的接口 =====
        // 这些接口在构造函数中注入，方便测试和替换实现
        private readonly IKeyboardInterface _keyboard;  // 按键接口
        private readonly IImageInterface _image;        // 图像接口
        private readonly ConfigManager _config;         // 配置管理器

        // ===== 运行状态控制 =====
        private bool _isRunning;                        // 是否正在运行
        private bool _isPaused;                         // 是否已暂停
        private CancellationTokenSource? _cts;          // 用于取消任务的令牌（类似 Python 的 threading.Event）
        private Task? _loopTask;                        // 后台循环任务

        // ===== 技能状态管理 =====
        // 使用 List 存储所有技能的运行时状态
        private List<SkillRuntimeState> _skillStates;

        // ===== 统计信息 =====
        private int _executionCount;                    // 执行次数
        private double _totalResponseTime;              // 总响应时间

        /// <summary>
        /// 构造函数 - 依赖注入
        /// 在 C# 中，构造函数用于初始化对象
        /// 参数通过依赖注入容器自动传入（后面会配置）
        /// </summary>
        /// <param name="keyboard">按键接口实现</param>
        /// <param name="image">图像接口实现</param>
        /// <param name="config">配置管理器</param>
        public SkillLoopEngine(IKeyboardInterface keyboard, IImageInterface image, ConfigManager config)
        {
            _keyboard = keyboard;
            _image = image;
            _config = config;
            
            // 加载配置
            _config.LoadConfigs();
            
            // 初始化技能状态列表
            // 为每个技能配置创建一个运行时状态对象
            _skillStates = new List<SkillRuntimeState>();
            foreach (var skillConfig in _config.Skills)
            {
                _skillStates.Add(new SkillRuntimeState(skillConfig));
            }
            
            // 初始化状态
            _isRunning = false;
            _isPaused = false;
            _executionCount = 0;
            _totalResponseTime = 0;
            
            Console.WriteLine($"✅ 引擎初始化完成，加载了 {_config.Skills.Count} 个技能");
        }

        /// <summary>
        /// 启动引擎
        /// 在后台线程中运行技能循环（类似 Python 版本的 start()）
        /// </summary>
        public void Start()
        {
            // 如果已经在运行，直接返回
            if (_isRunning)
            {
                Console.WriteLine("引擎已在运行中");
                return;
            }

            // 创建取消令牌（用于停止循环）
            _cts = new CancellationTokenSource();
            
            // 设置运行状态
            _isRunning = true;
            _isPaused = false;

            // 启动后台任务
            // Task.Run 会在后台线程执行（类似 Python 的 threading.Thread）
            _loopTask = Task.Run(() => MainLoop(_cts.Token), _cts.Token);

            Console.WriteLine("✅ 引擎已启动");
        }

        /// <summary>
        /// 停止引擎
        /// 停止后台循环并清理资源（类似 Python 版本的 stop()）
        /// </summary>
        public void Stop()
        {
            if (!_isRunning)
            {
                Console.WriteLine("引擎未运行");
                return;
            }

            // 发送取消信号
            _cts?.Cancel();
            
            // 等待循环任务结束（最多等待 5 秒）
            _loopTask?.Wait(TimeSpan.FromSeconds(5));

            // 清理资源
            _cts?.Dispose();
            _cts = null;
            _loopTask = null;

            // 重置状态
            _isRunning = false;
            _isPaused = false;

            Console.WriteLine("⏹️ 引擎已停止");
        }

        /// <summary>
        /// 暂停/恢复引擎
        /// 切换暂停状态（类似 Python 版本的 pause()）
        /// </summary>
        public void Pause()
        {
            if (!_isRunning)
            {
                Console.WriteLine("引擎未运行，无法暂停");
                return;
            }

            // 切换暂停状态
            _isPaused = !_isPaused;
            
            Console.WriteLine(_isPaused ? "⏸️ 引擎已暂停" : "▶️ 引擎已恢复");
        }

        /// <summary>
        /// 获取引擎当前状态
        /// 返回状态对象供 UI 显示（类似 Python 版本的 get_running_status()）
        /// </summary>
        public EngineStatus GetStatus()
        {
            return new EngineStatus
            {
                IsRunning = _isRunning,
                IsPaused = _isPaused,
                Mode = _isRunning ? (_isPaused ? "已暂停" : "运行中") : "已停止",
                ExecutionCount = _executionCount,
                AvgResponseTime = _executionCount > 0 ? _totalResponseTime / _executionCount : 0,
                SuccessRate = 100.0  // 暂时固定为 100%
            };
        }

        /// <summary>
        /// 主循环 - 在后台线程中运行
        /// 这是引擎的核心逻辑（类似 Python 版本的 _run_loop()）
        /// </summary>
        /// <param name="cancellationToken">取消令牌，用于停止循环</param>
        private void MainLoop(CancellationToken cancellationToken)
        {
            Console.WriteLine("🔄 主循环已启动");

            // 循环直到收到取消信号
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // 如果暂停，则等待
                    if (_isPaused)
                    {
                        Thread.Sleep(100);  // 暂停时每 100ms 检查一次
                        continue;
                    }

                    // ===== 执行一次技能循环 =====
                    var startTime = DateTime.Now;
                    
                    // 这里是核心逻辑（暂时简化为按 Q 键）
                    ExecuteSkillCycle();
                    
                    // 统计响应时间
                    var responseTime = (DateTime.Now - startTime).TotalSeconds;
                    _totalResponseTime += responseTime;
                    _executionCount++;

                    // 循环间隔（避免 CPU 占用过高）
                    // 使用配置文件中的间隔时间
                    Thread.Sleep(_config.AppSettings.LoopInterval);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ 循环异常: {ex.Message}");
                    Thread.Sleep(1000);  // 出错后等待 1 秒再继续
                }
            }

            Console.WriteLine("🛑 主循环已退出");
        }

        /// <summary>
        /// 执行一次技能循环
        /// 智能选择可用技能并释放
        /// 
        /// 【技能选择算法】
        /// 1. 筛选出所有可用的技能（冷却结束且已启用）
        /// 2. 按优先级排序（优先级高的优先）
        /// 3. 选择优先级最高的技能释放
        /// 4. 更新技能的冷却状态
        /// </summary>
        private void ExecuteSkillCycle()
        {
            // ===== 第一步：筛选可用技能 =====
            // 使用 LINQ 查询可用技能
            // Where 相当于 Python 的 filter
            var availableSkills = _skillStates
                .Where(s => s.IsAvailable)  // 只选择可用的技能
                .ToList();

            // 如果没有可用技能，跳过本次循环
            if (availableSkills.Count == 0)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 暂无可用技能，等待冷却...");
                return;
            }

            // ===== 第二步：按优先级排序 =====
            // OrderByDescending 相当于 Python 的 sorted(reverse=True)
            // 按优先级从高到低排序
            var sortedSkills = availableSkills
                .OrderByDescending(s => s.Config.Priority)
                .ToList();

            // ===== 第三步：选择优先级最高的技能 =====
            var selectedSkill = sortedSkills.First();  // 获取第一个（优先级最高）

            // ===== 第四步：释放技能 =====
            try
            {
                // 按下技能对应的按键
                bool success = _keyboard.PressAndRelease(selectedSkill.Config.KeyCode);

                if (success)
                {
                    // 标记技能已使用（开始冷却）
                    selectedSkill.MarkAsUsed();

                    // 打印日志
                    Console.WriteLine(
                        $"[{DateTime.Now:HH:mm:ss}] ✅ 释放技能: {selectedSkill.Config.Name} " +
                        $"(优先级: {selectedSkill.Config.Priority}, " +
                        $"冷却: {selectedSkill.Config.Cooldown}秒)"
                    );
                }
                else
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ❌ 技能释放失败: {selectedSkill.Config.Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ❌ 技能释放异常: {ex.Message}");
            }

            // ===== 第五步：显示其他技能的冷却状态 =====
            // 这个是可选的，用于调试
            if (_executionCount % 5 == 0)  // 每 5 次循环显示一次
            {
                Console.WriteLine("\n--- 技能冷却状态 ---");
                foreach (var skill in _skillStates)
                {
                    if (skill.Config.Enabled)
                    {
                        var status = skill.IsAvailable ? "✅ 可用" : $"⏳ 冷却中 ({skill.RemainingCooldown:F1}秒)";
                        Console.WriteLine($"  {skill.Config.Name}: {status}");
                    }
                }
                Console.WriteLine("-------------------\n");
            }
        }
    }
}
