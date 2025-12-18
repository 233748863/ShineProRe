using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace ShineProCS.Models
{
    /// <summary>
    /// 应用程序配置模型
    /// </summary>
    public partial class AppSettings : ObservableObject
    {
        [ObservableProperty]
        private int[] _detectionRegion = new int[4];

        [ObservableProperty]
        private int[] _manaBarRegion = new int[4];

        [ObservableProperty]
        private bool _enableSmartMode = true;

        [ObservableProperty]
        private int _loopInterval = 1000;

        [ObservableProperty]
        private int _logLevel = 1;
    }

    /// <summary>
    /// Buff/Debuff 配置模型
    /// </summary>
    public partial class BuffConfig : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private int[] _iconRegion = new int[4];

        [ObservableProperty]
        private string _templatePath = string.Empty;

        [ObservableProperty]
        private double _similarityThreshold = 0.8;

        [ObservableProperty]
        private bool _isDebuff = false;

        /// <summary>
        /// 是否必须拥有（对于 Buff）或不能拥有（对于 Debuff）
        /// </summary>
        [ObservableProperty]
        private bool _isRequired = true;
    }

    /// <summary>
    /// 技能配置模型
    /// </summary>
    public partial class SkillConfig : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private int _keyCode;

        [ObservableProperty]
        private int _priority;

        [ObservableProperty]
        private bool _enabled = true;

        // ===== 视觉检测配置 =====

        [ObservableProperty]
        private int[] _iconRegion = new int[4];

        [ObservableProperty]
        private string _templatePath = string.Empty;

        [ObservableProperty]
        private double _similarityThreshold = 0.8;

        // ===== 释放前置条件 =====

        [ObservableProperty]
        private double _minHp = 0;

        [ObservableProperty]
        private double _minMp = 0;

        [ObservableProperty]
        private bool _requireTarget = false;

        [ObservableProperty]
        private double _cooldown;

        // ===== Buff/Debuff 前置条件 (可视化版) =====

        [ObservableProperty]
        private ObservableCollection<BuffConfig> _buffRequirements = new ObservableCollection<BuffConfig>();

        // 保留旧字段用于兼容性（可选，建议逐步迁移）
        [ObservableProperty]
        private string _requiredBuffs = string.Empty;

        [ObservableProperty]
        private string _excludedBuffs = string.Empty;
    }
}
