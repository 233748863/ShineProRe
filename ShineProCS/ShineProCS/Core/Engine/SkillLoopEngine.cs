using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Collections;
using OpenCvSharp;
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
        private readonly StateMonitor _stateMonitor;
        private readonly AdaptiveDelay _adaptiveDelay;
        private readonly ConfigWatcher _configWatcher;

        // ===== 运行状态控制 =====
        private bool _isRunning;
        private bool _isPaused;
        private CancellationTokenSource? _cts;
        private Task? _loopTask;
        private Task? _captureTask;

        // ===== 生产者-消费者模型 =====
        private readonly BlockingCollection<Mat> _imageQueue = new(new ConcurrentQueue<Mat>(), 2);
        private byte[]? _lastFrameHash;

        // ===== UI 遮罩 =====
        private Views.OverlayWindow? _overlay;

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
            SkillStateDetector skillDetector,
            StateMonitor stateMonitor)
        {
            _keyboard = keyboard;
            _image = image;
            _config = config;
            _perfMonitor = perfMonitor;
            _memMonitor = memMonitor;
            _strategyManager = strategyManager;
            _skillDetector = skillDetector;
            _stateMonitor = stateMonitor;

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

            // 启动截屏任务 (生产者)
            _captureTask = Task.Run(() => CaptureLoop(_cts.Token), _cts.Token);
            // 启动处理任务 (消费者)
            _loopTask = Task.Run(() => MainLoop(_cts.Token), _cts.Token);

            // 启动 Overlay
            App.Current.Dispatcher.Invoke(() =>
            {
                _overlay = new Views.OverlayWindow();
                _overlay.Show();
            });
            
            Console.WriteLine("✅ 引擎已启动（高级模式：异步截屏 + 并行检测 + UI遮罩）");
        }

        public void Stop()
        {
            if (!_isRunning) return;

            _cts?.Cancel();
            Task.WaitAll(new[] { _loopTask, _captureTask }.Where(t => t != null).ToArray()!, TimeSpan.FromSeconds(5));
            
            // 关闭 Overlay
            App.Current.Dispatcher.Invoke(() =>
            {
                _overlay?.Close();
                _overlay = null;
            });

            // 清理队列
            while (_imageQueue.TryTake(out var mat)) _image.ReturnMat(mat);

            _cts?.Dispose();
            _cts = null;
            _loopTask = null;
            _captureTask = null;
            _isRunning = false;
            _isPaused = false;

            Console.WriteLine("⏹️ 引擎已停止");
        }

        private void CaptureLoop(CancellationToken token)
        {
            var region = _config.GetDetectionRegion();
            while (!token.IsCancellationRequested)
            {
                if (_isPaused)
                {
                    Thread.Sleep(100);
                    continue;
                }

                var mat = _image.GetScreenRegion(region[0], region[1], region[2], region[3]);
                if (mat != null)
                {
                    if (!_imageQueue.TryAdd(mat, 100, token))
                    {
                        _image.ReturnMat(mat); // 队列满则丢弃并归还
                    }
                }
                Thread.Sleep(10); // 限制截屏频率
            }
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

                    // 从队列获取最新图像
                    if (!_imageQueue.TryTake(out var currentFrame, 500, cancellationToken))
                        continue;

                    using (currentFrame)
                    {
                        // ===== 脏矩形检测：对比哈希 =====
                        if (IsFrameUnchanged(currentFrame))
                        {
                            _image.ReturnMat(currentFrame);
                            Thread.Sleep(_adaptiveDelay.CurrentDelay * 2); // 画面无变化，拉长间隔
                            continue;
                        }

                        // ===== 性能监控：开始操作 =====
                        _perfMonitor.StartOperation();

                        // ===== 核心逻辑：执行技能循环 =====
                        bool success = ExecuteSkillCycle(currentFrame);

                        // 每 10 次循环检测一次战斗状态 (平衡性能)
                        if (_perfMonitor.GetMetrics().TotalExecutions % 10 == 0)
                        {
                            _adaptiveDelay.IsCombatMode = _stateMonitor.DetectCombatState();
                        }

                        // 更新 Overlay (每 5 次循环更新一次，平衡性能)
                        if (_perfMonitor.GetMetrics().TotalExecutions % 5 == 0)
                        {
                            UpdateOverlay();
                        }

                        // ===== 性能监控：结束操作 =====
                        _perfMonitor.EndOperation(success);

                        // ===== 自适应延迟：动态调整 =====
                        _adaptiveDelay.Adjust(_perfMonitor.GetMetrics().AverageResponseTime);

                        _image.ReturnMat(currentFrame);
                    }

                    Thread.Sleep(_adaptiveDelay.CurrentDelay);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ 循环异常: {ex.Message}");
                    Thread.Sleep(1000);
                }
            }
        }

        private bool IsFrameUnchanged(Mat frame)
        {
            // 简单哈希对比（取缩略图哈希以提高速度）
            using var small = new Mat();
            Cv2.Resize(frame, small, new OpenCvSharp.Size(8, 8));
            var currentHash = small.ToBytes();
            
            if (_lastFrameHash != null && StructuralComparisons.StructuralEqualityComparer.Equals(_lastFrameHash, currentHash))
            {
                return true;
            }
            _lastFrameHash = currentHash;
            return false;
        }

        private void UpdateOverlay()
        {
            if (_overlay == null) return;
            var regions = _skillStates
                .Where(s => s.Config.Enabled && s.Config.IconRegion != null && s.Config.IconRegion.Length == 4)
                .Select(s => (s.Config.IconRegion, s.Config.Name, s.IsVisuallyReady))
                .ToList();
            _overlay.UpdateRegions(regions);
        }

        private bool ExecuteSkillCycle(Mat currentFrame)
        {
            // 1. 并行检测所有技能状态 (充分利用多核)
            Parallel.ForEach(_skillStates, state => 
            {
                _skillDetector.UpdateSkillStateVisually(state, currentFrame);
            });

            // 2. 构造策略上下文
            var context = new StrategyContext
            {
                SkillStates = _skillStates,
                ExecutionCount = _perfMonitor.GetMetrics().TotalExecutions,
                LoopMode = _config.AppSettings.EnableSmartMode ? "Smart" : "Default"
            };

            // 3. 使用策略管理器获取下一个技能
            var selectedSkill = _strategyManager.GetNextSkill(context);

            if (selectedSkill == null) return true;

            // 4. 防卡死逻辑：如果技能视觉上一直就绪但无法释放成功
            if (selectedSkill.IsVisuallyReady)
            {
                selectedSkill.ConsecutiveFailures++;
                if (selectedSkill.ConsecutiveFailures >= 5)
                {
                    Console.WriteLine($"[Engine] 技能 {selectedSkill.Config.Name} 连续 5 次释放失败，触发防卡死重置 (ESC)");
                    _keyboard.PressAndRelease(27); // VK_ESCAPE
                    selectedSkill.ConsecutiveFailures = 0;
                    return false;
                }
            }

            // 5. 执行按键模拟
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
