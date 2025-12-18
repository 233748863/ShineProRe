using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShineProCS.Core.Engine;
using ShineProCS.Core.Services;
using ShineProCS.Models;
using ShineProCS.Utils;

namespace ShineProCS.ViewModels
{
    /// <summary>
    /// 主窗口 ViewModel
    /// 负责连接 UI 和引擎，处理用户交互
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        private readonly SkillLoopEngine _engine;
        private readonly MemoryMonitor _memMonitor;
        private readonly ConfigManager _config;
        private readonly DispatcherTimer _statusTimer;
        private readonly DateTime _startTime;

        // ===== 运行状态属性 (UI 绑定) =====
        
        [ObservableProperty]
        private string _statusText = "就绪";

        [ObservableProperty]
        private bool _isRunning;

        [ObservableProperty]
        private bool _isPaused;

        [ObservableProperty]
        private int _executionCount;

        [ObservableProperty]
        private double _avgResponseTime;

        [ObservableProperty]
        private double _successRate;

        [ObservableProperty]
        private string _memoryStats = "0 MB / 0 MB";

        [ObservableProperty]
        private string _performanceReport = "暂无数据";

        // ===== 配置管理属性 (UI 绑定) =====

        [ObservableProperty]
        private ObservableCollection<SkillConfig> _skills;

        [ObservableProperty]
        private SkillConfig? _selectedSkill;

        [ObservableProperty]
        private AppSettings _appSettings;

        [ObservableProperty]
        private ObservableCollection<string> _availableProfiles;

        [ObservableProperty]
        private string _selectedProfile;

        /// <summary>
        /// 构造函数 - 依赖注入
        /// </summary>
        public MainViewModel(SkillLoopEngine engine, MemoryMonitor memMonitor, ConfigManager config)
        {
            _engine = engine;
            _memMonitor = memMonitor;
            _config = config;
            _startTime = DateTime.Now;

            // 初始化配置数据
            _skills = new ObservableCollection<SkillConfig>(_config.Skills);
            _selectedSkill = _skills.FirstOrDefault();
            _appSettings = _config.AppSettings;

            // 初始化方案列表
            _availableProfiles = new ObservableCollection<string>(_config.GetAvailableProfiles());
            _selectedProfile = "skills";

            // 初始化状态定时器（每 500ms 更新一次 UI）
            _statusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _statusTimer.Tick += OnStatusTimerTick;
            _statusTimer.Start();
        }

        // ===== 引擎控制命令 =====

        [RelayCommand]
        private void StartEngine()
        {
            _engine.Start();
            UpdateStatus();
        }

        [RelayCommand]
        private void StopEngine()
        {
            _engine.Stop();
            UpdateStatus();
        }

        [RelayCommand]
        private void PauseEngine()
        {
            _engine.Pause();
            UpdateStatus();
        }

        [RelayCommand]
        private void ForceCleanup()
        {
            _memMonitor.ForceCleanup();
            StatusText = "已手动触发内存清理";
        }

        // ===== 配置管理命令 =====

        [RelayCommand]
        private void AddSkill()
        {
            var newSkill = new SkillConfig { Name = "新技能", KeyCode = 81, Priority = 1 };
            Skills.Add(newSkill);
            SelectedSkill = newSkill;
        }

        [RelayCommand]
        private void DeleteSkill()
        {
            if (SelectedSkill != null)
            {
                Skills.Remove(SelectedSkill);
                SelectedSkill = Skills.FirstOrDefault();
            }
        }

        [RelayCommand]
        private void MoveSkillUp()
        {
            if (SelectedSkill == null) return;
            int index = Skills.IndexOf(SelectedSkill);
            if (index > 0)
            {
                Skills.Move(index, index - 1);
            }
        }

        [RelayCommand]
        private void MoveSkillDown()
        {
            if (SelectedSkill == null) return;
            int index = Skills.IndexOf(SelectedSkill);
            if (index < Skills.Count - 1)
            {
                Skills.Move(index, index + 1);
            }
        }

        [RelayCommand]
        private void SaveConfig()
        {
            // 同步 ObservableCollection 到 ConfigManager 的 List
            _config.Skills.Clear();
            foreach (var skill in Skills)
            {
                _config.Skills.Add(skill);
            }

            // 保存到文件
            _config.SaveConfigs();
            StatusText = "配置已保存并生效";
        }

        [RelayCommand]
        private void SwitchProfile(string? profileName)
        {
            if (string.IsNullOrEmpty(profileName)) return;
            
            Console.WriteLine($"[ViewModel] 正在切换方案到: {profileName}");
            _config.LoadProfile(profileName);
            
            // 重新加载技能列表
            Skills.Clear();
            foreach (var skill in _config.Skills)
            {
                Skills.Add(skill);
            }
            SelectedSkill = Skills.FirstOrDefault();
            
            StatusText = $"已切换到方案: {profileName}";
        }

        [RelayCommand]
        private void RefreshProfiles()
        {
            var profiles = _config.GetAvailableProfiles();
            AvailableProfiles.Clear();
            foreach (var p in profiles)
            {
                AvailableProfiles.Add(p);
            }
            StatusText = "方案列表已刷新";
        }

        // ===== 视觉助手命令 =====

        [RelayCommand]
        private void SelectRegion(object? parameter)
        {
            if (parameter is SkillConfig skill)
            {
                var selector = new ShineProCS.Views.RegionSelectorWindow();
                if (selector.ShowDialog() == true)
                {
                    var region = selector.SelectedRegion;
                    skill.IconRegion = new int[] { region.X, region.Y, region.Width, region.Height };
                    StatusText = $"已设置技能 {skill.Name} 的图标区域";
                }
            }
            else if (parameter is BuffConfig buff)
            {
                var selector = new ShineProCS.Views.RegionSelectorWindow();
                if (selector.ShowDialog() == true)
                {
                    var region = selector.SelectedRegion;
                    buff.IconRegion = new int[] { region.X, region.Y, region.Width, region.Height };
                    StatusText = $"已设置 Buff {buff.Name} 的检测区域";
                }
            }
        }

        [RelayCommand]
        private void SelectTemplateFile(object? parameter)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "图片文件|*.png;*.jpg;*.bmp|所有文件|*.*",
                Title = "选择图标模板"
            };

            if (dialog.ShowDialog() == true)
            {
                if (parameter is SkillConfig skill)
                {
                    skill.TemplatePath = dialog.FileName;
                }
                else if (parameter is BuffConfig buff)
                {
                    buff.TemplatePath = dialog.FileName;
                }
            }
        }

        // ===== 可视化 Buff 管理命令 =====

        [RelayCommand]
        private void AddBuffRequirement(SkillConfig? skill)
        {
            // 如果没传参数，尝试使用当前选中的技能
            var targetSkill = skill ?? SelectedSkill;
            if (targetSkill != null)
            {
                // 彻底防御：如果集合为 null，则初始化它
                if (targetSkill.BuffRequirements == null)
                {
                    targetSkill.BuffRequirements = new ObservableCollection<BuffConfig>();
                }
                
                targetSkill.BuffRequirements.Add(new BuffConfig { Name = "新 Buff" });
                StatusText = $"已为技能 {targetSkill.Name} 添加 Buff 要求";
            }
            else
            {
                StatusText = "请先选择一个技能";
            }
        }

        [RelayCommand]
        private void DeleteBuffRequirement(object? parameter)
        {
            if (parameter is BuffConfig buff && SelectedSkill != null)
            {
                SelectedSkill.BuffRequirements.Remove(buff);
                StatusText = $"已删除 Buff 要求: {buff.Name}";
            }
        }

        [RelayCommand]
        private void AddGlobalBuff()
        {
            AppSettings.GlobalBuffs.Add(new BuffConfig { Name = "新全局 Buff" });
        }

        [RelayCommand]
        private void DeleteGlobalBuff(BuffConfig? buff)
        {
            if (buff != null)
            {
                AppSettings.GlobalBuffs.Remove(buff);
            }
        }

        /// <summary>
        /// 定时更新 UI 状态
        /// </summary>
        private void OnStatusTimerTick(object? sender, EventArgs e)
        {
            UpdateStatus();
        }

        /// <summary>
        /// 从引擎获取最新状态并更新属性
        /// </summary>
        private void UpdateStatus()
        {
            var status = _engine.GetStatus();
            
            IsRunning = status.IsRunning;
            IsPaused = status.IsPaused;
            StatusText = status.Mode;
            ExecutionCount = status.ExecutionCount;
            AvgResponseTime = status.AvgResponseTime;
            SuccessRate = status.SuccessRate;

            // 更新内存统计
            var mem = _memMonitor.GetMemoryStats();
            MemoryStats = $"{mem.PrivateMemory:F1} MB / {mem.WorkingSet:F1} MB";

            // 简单性能报告
            double duration = (DateTime.Now - _startTime).TotalSeconds;
            PerformanceReport = $"TPS: {ExecutionCount / Math.Max(1, duration):F2}";
        }
    }
}
