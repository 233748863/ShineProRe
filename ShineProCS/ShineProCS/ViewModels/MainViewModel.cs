using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShineProCS.Core.Engine;
using ShineProCS.Models;

namespace ShineProCS.ViewModels
{
    /// <summary>
    /// 主窗口的视图模型（ViewModel）
    /// 在 MVVM 模式中，ViewModel 负责处理 UI 逻辑和数据绑定
    /// 
    /// 【MVVM 模式简介】
    /// - Model: 数据模型（如 EngineStatus）
    /// - View: 界面（XAML 文件）
    /// - ViewModel: 连接 Model 和 View 的桥梁（本类）
    /// 
    /// 【ObservableObject 说明】
    /// 继承自 ObservableObject，可以自动通知 UI 更新
    /// 当属性值改变时，UI 会自动刷新（类似 Vue.js 的响应式）
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        // ===== 依赖注入 =====
        private readonly SkillLoopEngine _engine;

        /// <summary>
        /// 构造函数 - 注入引擎实例
        /// </summary>
        public MainViewModel(SkillLoopEngine engine)
        {
            _engine = engine;
            
            // 初始化状态文本
            StatusText = "就绪";
            IsRunning = false;
        }

        // ===== 可观察属性 =====
        // [ObservableProperty] 是一个特性（Attribute），会自动生成属性代码
        // 当这些属性改变时，UI 会自动更新
        
        /// <summary>
        /// 状态文本（显示在 UI 上）
        /// 注意：字段名用小写开头，生成的属性名会自动转为大写开头
        /// 例如：statusText -> StatusText
        /// </summary>
        [ObservableProperty]
        private string statusText;

        /// <summary>
        /// 是否正在运行（用于控制按钮状态）
        /// </summary>
        [ObservableProperty]
        private bool isRunning;

        /// <summary>
        /// 执行次数（显示统计信息）
        /// </summary>
        [ObservableProperty]
        private int executionCount;

        /// <summary>
        /// 平均响应时间（秒）
        /// </summary>
        [ObservableProperty]
        private double avgResponseTime;

        // ===== 命令（Commands）=====
        // [RelayCommand] 会自动生成命令代码
        // 命令用于绑定按钮点击等事件（类似 Vue.js 的 methods）
        
        /// <summary>
        /// 启动命令
        /// 在 XAML 中绑定到"启动"按钮
        /// 方法名 Start -> 生成的命令名 StartCommand
        /// </summary>
        [RelayCommand]
        private void Start()
        {
            try
            {
                _engine.Start();
                IsRunning = true;
                StatusText = "运行中...";
                
                // 启动定时器更新状态（每秒更新一次）
                StartStatusUpdateTimer();
            }
            catch (Exception ex)
            {
                StatusText = $"启动失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 停止命令
        /// </summary>
        [RelayCommand]
        private void Stop()
        {
            try
            {
                _engine.Stop();
                IsRunning = false;
                StatusText = "已停止";
                
                // 更新最终状态
                UpdateStatus();
            }
            catch (Exception ex)
            {
                StatusText = $"停止失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 暂停/恢复命令
        /// </summary>
        [RelayCommand]
        private void Pause()
        {
            try
            {
                _engine.Pause();
                var status = _engine.GetStatus();
                StatusText = status.IsPaused ? "已暂停" : "运行中...";
            }
            catch (Exception ex)
            {
                StatusText = $"暂停失败: {ex.Message}";
            }
        }

        // ===== 私有方法 =====
        
        /// <summary>
        /// 启动状态更新定时器
        /// 使用 System.Timers.Timer 每秒更新一次状态
        /// </summary>
        private void StartStatusUpdateTimer()
        {
            var timer = new System.Timers.Timer(1000);  // 1000 毫秒 = 1 秒
            timer.Elapsed += (s, e) =>
            {
                if (!IsRunning)
                {
                    timer.Stop();  // 停止运行时停止定时器
                    return;
                }
                
                UpdateStatus();
            };
            timer.Start();
        }

        /// <summary>
        /// 更新状态信息
        /// 从引擎获取最新状态并更新 UI
        /// </summary>
        private void UpdateStatus()
        {
            var status = _engine.GetStatus();
            
            // 更新属性（UI 会自动刷新）
            ExecutionCount = status.ExecutionCount;
            AvgResponseTime = status.AvgResponseTime;
            
            // 更新状态文本
            if (!IsRunning)
            {
                StatusText = "已停止";
            }
            else if (status.IsPaused)
            {
                StatusText = "已暂停";
            }
            else
            {
                StatusText = $"运行中 - 已执行 {status.ExecutionCount} 次";
            }
        }
    }
}
