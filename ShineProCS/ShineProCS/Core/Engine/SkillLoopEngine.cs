using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ShineProCS.Core.Interfaces;
using ShineProCS.Core.Services;
using ShineProCS.Core.Strategies;
using ShineProCS.Models;
using ShineProCS.Utils;

namespace ShineProCS.Core.Engine
{
    /// <summary>
    /// 技能循环引擎（高级集成版）
    /// 核心业务逻辑类，集成了策略模式、性能监控、内存管理和自适应延迟
    /// </summary>
    public class SkillLoopEngine
    {
        // ===== 基础依赖 =====
        private readonly IKeyboardInterface _keyboard;
        private readonly IImageInterface _image;
        private readonly ConfigManager _config;

        // ===== 高级功能组件 =====
        private readonly PerformanceMonitor _perfMonitor;
        private readonly MemoryMonitor _memMonitor;
        private readonly StrategyManager _strategyManager;
        private readonly SkillStateDetector _skillDetector;
        private readonly AdaptiveDelay _adaptiveDelay;
        private readonly ConfigWatcher _configWatcher;

        // ===== 运行状态控制 =====
        private bool _isRunning;
        private bool _isPaused;
        private CancellationTokenSource? _cts;
        private Task? _loopTask;

        // ===== 技能状态管理 =====
        private List<SkillRuntimeState> _skillStates;

        /// <summary>
        /// 构造函数 - 依赖注入
        /// </summary>
        public SkillLoopEngine(
            IKeyboardInterface keyboard, 
            IImageInterface image, 
            ConfigManager config,
            PerformanceMonitor perfMonitor,
            MemoryMonitor memMonitor,
            StrategyManager strategyManager,
            SkillStateDetector skillDetector)
        {
            _keyboard = keyboard;
            _image = image;
            _config = config;
            _perfMonitor = perfMonitor;
            _memMonitor = memMonitor;
            _strategyManager = strategyManager;
            _skillDetector = skillDetector;

            // 初始化技能状态列表
            _skillStates = new List<SkillRuntimeState>();

            // 初始化自适应延迟
            _adaptiveDelay = new AdaptiveDelay(_config.AppSettings.LoopInterval);

            // 初始化配置监听器
            _configWatcher = new ConfigWatcher(AppDomain.CurrentDomain.BaseDirectory);
            _configWatcher.ConfigChanged += OnConfigChanged;
            
            // 加载配置并初始化技能
            LoadAndInitializeSkills();
            
            _isRunning = false;
            _isPaused = false;
        }

        private void LoadAndInitializeSkills()
        {
            _config.LoadConfigs();
            _skillStates = _config.Skills.Select(s => new SkillRuntimeState(s)).ToList();
            Console.WriteLine($"✅ 引擎初始化完成，加载了 {_skillStates.Count} 个技能");
        }

        private void OnConfigChanged(string filePath)
        {
            if (filePath.Contains("skills.json") || filePath.Contains("appsettings.json"))
            {
                Console.WriteLine("[Engine] 检测到配置更新，正在热重载...");
                LoadAndInitializeSkills();
                _adaptiveDelay.Reset(_config.AppSettings.LoopInterval);
            }
        }

        public void Start()
        {
            if (_isRunning) return;

            _cts = new CancellationTokenSource();
            _isRunning = true;
            _isPaused = false;
            _perfMonitor.Reset();

            _loopTask = Task.Run(() => MainLoop(_cts.Token), _cts.Token);
            Console.WriteLine("✅ 引擎已启动（高级模式）");
        }

        public void Stop()
        {
            if (!_isRunning) return;

            _cts?.Cancel();
            _loopTask?.Wait(TimeSpan.FromSeconds(5));
            _cts?.Dispose();
            _cts = null;
            _loopTask = null;
            _isRunning = false;
            _isPaused = false;

            Console.WriteLine("⏹️ 引擎已停止");
        }

        public void Pause()
        {
            if (!_isRunning) return;
            _isPaused = !_isPaused;
            Console.WriteLine(_isPaused ? "⏸️ 引擎已暂停" : "▶️ 引擎已恢复");
        }

        public EngineStatus GetStatus()
        {
            var perf = _perfMonitor.GetMetrics();
            return new EngineStatus
            {
                IsRunning = _isRunning,
                IsPaused = _isPaused,
                Mode = _isRunning ? (_isPaused ? "已暂停" : "运行中") : "已停止",
                ExecutionCount = perf.TotalExecutions,
                AvgResponseTime = perf.AverageResponseTime,
                SuccessRate = perf.SuccessRate
            };
        }

        private void MainLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_isPaused)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    // ===== 性能监控：开始操作 =====
                    _perfMonitor.StartOperation();

                    // ===== 内存管理：定期检查 =====
                    if (_perfMonitor.GetMetrics().TotalExecutions % 100 == 0)
                    {
                        if (_memMonitor.IsMemoryHigh(300))
                        {
                            Console.WriteLine("[Engine] 内存占用过高，触发清理...");
                            _memMonitor.ForceCleanup();
                        }
                    }

                    // ===== 核心逻辑：执行技能循环 =====
                    bool success = ExecuteSkillCycle();

                    // ===== 性能监控：结束操作 =====
                    _perfMonitor.EndOperation(success);

                    // ===== 自适应延迟：动态调整 =====
                    _adaptiveDelay.Adjust(_perfMonitor.GetMetrics().AverageResponseTime);

                    // 循环间隔
                    Thread.Sleep(_adaptiveDelay.CurrentDelay);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ 循环异常: {ex.Message}");
                    Thread.Sleep(1000);
                }
            }
        }

        private bool ExecuteSkillCycle()
        {
            // 1. 构造策略上下文
            var context = new StrategyContext
            {
                SkillStates = _skillStates,
                ExecutionCount = _perfMonitor.GetMetrics().TotalExecutions,
                LoopMode = _config.AppSettings.EnableSmartMode ? "Smart" : "Default"
            };

            // 2. 使用策略管理器获取下一个技能
            var selectedSkill = _strategyManager.GetNextSkill(context);

            if (selectedSkill == null) return true;

            // 3. 使用技能状态检测器进行二次确认（高级图像识别）
            if (!_skillDetector.IsSkillReady(selectedSkill))
            {
                return false;
            }

            // 4. 执行按键模拟
            try
            {
                bool success = _keyboard.PressAndRelease(selectedSkill.Config.KeyCode);
                if (success)
                {
                    selectedSkill.MarkAsUsed();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Engine] 技能释放异常: {ex.Message}");
            }

            return false;
        }
    }
}
