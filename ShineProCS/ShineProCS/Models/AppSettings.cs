namespace ShineProCS.Models
{
    /// <summary>
    /// 应用程序配置模型
    /// 对应 appsettings.json 文件的结构
    /// 
    /// 【配置模型说明】
    /// 在 C# 中，我们使用类来映射 JSON 配置文件
    /// 这样可以获得类型安全和智能提示
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// 检测区域配置（屏幕坐标）
        /// 格式：[x, y, width, height]
        /// </summary>
        public int[] DetectionRegion { get; set; } = new int[4];

        /// <summary>
        /// 蓝条监控区域配置
        /// </summary>
        public int[] ManaBarRegion { get; set; } = new int[4];

        /// <summary>
        /// 是否启用智能模式
        /// true: 使用图像识别
        /// false: 使用简单循环
        /// </summary>
        public bool EnableSmartMode { get; set; } = true;

        /// <summary>
        /// 循环间隔时间（毫秒）
        /// </summary>
        public int LoopInterval { get; set; } = 1000;

        /// <summary>
        /// 日志级别
        /// 0=DEBUG, 1=INFO, 2=WARN, 3=ERROR
        /// </summary>
        public int LogLevel { get; set; } = 1;
    }

    /// <summary>
    /// 技能配置模型
    /// 对应 skills.json 文件的结构
    /// </summary>
    public class SkillConfig
    {
        /// <summary>
        /// 技能名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 技能按键（键值）
        /// 例如：81 代表 Q 键
        /// </summary>
        public int KeyCode { get; set; }

        /// <summary>
        /// 技能冷却时间（秒）
        /// </summary>
        public double Cooldown { get; set; }

        /// <summary>
        /// 优先级（数字越大优先级越高）
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enabled { get; set; } = true;
    }
}
