using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ShineProCS.Models;

namespace ShineProCS.Core.Services
{
    /// <summary>
    /// 配置管理器
    /// 负责加载和管理应用程序配置（类似 Python 版本的 配置管理器）
    /// 
    /// 【配置管理说明】
    /// 使用 System.Text.Json 读取 JSON 配置文件
    /// 配置文件放在 config 目录下
    /// </summary>
    public class ConfigManager
    {
        // ===== 配置文件路径 =====
        private readonly string _configPath;
        private readonly string _appSettingsPath;
        private readonly string _skillsPath;

        // ===== 配置对象 =====
        private AppSettings? _appSettings;
        private List<SkillConfig>? _skills;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="configPath">配置文件目录路径，默认为 "./config"</param>
        public ConfigManager(string configPath = "./config")
        {
            _configPath = configPath;
            _appSettingsPath = Path.Combine(configPath, "appsettings.json");
            _skillsPath = Path.Combine(configPath, "skills.json");

            // 确保配置目录存在
            if (!Directory.Exists(configPath))
            {
                Directory.CreateDirectory(configPath);
                Console.WriteLine($"已创建配置目录: {configPath}");
            }
        }

        /// <summary>
        /// 加载所有配置
        /// 从 JSON 文件中读取配置并反序列化为对象
        /// </summary>
        public void LoadConfigs()
        {
            try
            {
                // ===== 加载应用程序配置 =====
                if (File.Exists(_appSettingsPath))
                {
                    // 读取 JSON 文件内容
                    var json = File.ReadAllText(_appSettingsPath);
                    
                    // 反序列化为 AppSettings 对象
                    // JsonSerializer.Deserialize 会自动将 JSON 转换为 C# 对象
                    _appSettings = JsonSerializer.Deserialize<AppSettings>(json);
                    
                    Console.WriteLine("✅ 应用配置加载成功");
                }
                else
                {
                    // 如果文件不存在，创建默认配置
                    _appSettings = new AppSettings();
                    SaveAppSettings();  // 保存默认配置到文件
                    Console.WriteLine("⚠️ 未找到配置文件，已创建默认配置");
                }

                // ===== 加载技能配置 =====
                if (File.Exists(_skillsPath))
                {
                    var json = File.ReadAllText(_skillsPath);
                    _skills = JsonSerializer.Deserialize<List<SkillConfig>>(json);
                    
                    // 确保所有技能的集合都已初始化（防止旧配置反序列化出 null）
                    if (_skills != null)
                    {
                        foreach (var skill in _skills)
                        {
                            if (skill.BuffRequirements == null)
                                skill.BuffRequirements = new System.Collections.ObjectModel.ObservableCollection<BuffConfig>();
                        }
                    }
                    
                    Console.WriteLine($"✅ 技能配置加载成功，共 {_skills?.Count ?? 0} 个技能");
                }
                else
                {
                    // 创建默认技能配置
                    _skills = CreateDefaultSkills();
                    SaveSkills();
                    Console.WriteLine("⚠️ 未找到技能配置，已创建默认配置");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 配置加载失败: {ex.Message}");
                
                // 加载失败时使用默认配置
                _appSettings = new AppSettings();
                _skills = CreateDefaultSkills();
            }
        }

        /// <summary>
        /// 保存应用程序配置到文件
        /// </summary>
        public void SaveAppSettings()
        {
            try
            {
                if (_appSettings == null) return;

                // 序列化为 JSON 字符串
                // JsonSerializerOptions 用于设置格式化选项
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,  // 格式化输出（美化 JSON）
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping  // 支持中文
                };

                var json = JsonSerializer.Serialize(_appSettings, options);
                
                // 写入文件
                File.WriteAllText(_appSettingsPath, json);
                
                Console.WriteLine("✅ 应用配置已保存");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 配置保存失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存技能配置到文件
        /// </summary>
        public void SaveSkills()
        {
            try
            {
                if (_skills == null) return;

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var json = JsonSerializer.Serialize(_skills, options);
                File.WriteAllText(_skillsPath, json);
                
                Console.WriteLine("✅ 技能配置已保存");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 技能配置保存失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建默认技能配置
        /// </summary>
        private List<SkillConfig> CreateDefaultSkills()
        {
            var skills = new List<SkillConfig>
            {
                new SkillConfig { Name = "青川濯莲", KeyCode = 49, Enabled = true },
                new SkillConfig { Name = "七情和合", KeyCode = 50, Enabled = true },
                new SkillConfig 
                { 
                    Name = "千枝绽蕊", 
                    KeyCode = 87, 
                    Enabled = true,
                    BuffRequirements = new System.Collections.ObjectModel.ObservableCollection<BuffConfig>
                    {
                        new BuffConfig { Name = "千枝态", IsRequired = true, IsDebuff = false }
                    }
                },
                new SkillConfig { Name = "逐云寒蕊", KeyCode = 51, Enabled = true },
                new SkillConfig { Name = "当归四逆", KeyCode = 69, Enabled = true, MinHp = 50 },
                new SkillConfig { Name = "银光照雪", KeyCode = 52, Enabled = true },
                new SkillConfig 
                { 
                    Name = "赤芍寒香", 
                    KeyCode = 81, 
                    Enabled = true, 
                    MinHp = 80,
                    BuffRequirements = new System.Collections.ObjectModel.ObservableCollection<BuffConfig>
                    {
                        new BuffConfig { Name = "赤芍Buff", IsRequired = false, IsDebuff = false }
                    }
                },
                new SkillConfig { Name = "绿野蔓生", KeyCode = 53, Enabled = true },
                new SkillConfig { Name = "白芷含芳", KeyCode = 54, Enabled = true }
            };
            return skills;
        }

        // ===== 公共属性（用于访问配置）=====
        
        /// <summary>
        /// 获取应用程序配置
        /// </summary>
        public AppSettings AppSettings => _appSettings ?? new AppSettings();

        /// <summary>
        /// 获取技能配置列表
        /// </summary>
        public List<SkillConfig> Skills => _skills ?? new List<SkillConfig>();

        /// <summary>
        /// 获取检测区域
        /// </summary>
        public int[] GetDetectionRegion() => AppSettings.DetectionRegion;

        /// <summary>
        /// 获取蓝条区域
        /// </summary>
        public int[] GetManaBarRegion() => AppSettings.ManaBarRegion;

        /// <summary>
        /// 保存所有配置到文件
        /// </summary>
        public void SaveConfigs()
        {
            SaveAppSettings();
            SaveSkills();
        }
    }
}
