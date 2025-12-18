using System;
using System.IO;
using System.Threading;

namespace ShineProCS.Utils
{
    /// <summary>
    /// 配置监听器
    /// 负责监听配置文件的变化并触发重新加载
    /// 
    /// 【热重载说明】
    /// 1. 使用 FileSystemWatcher 监听文件系统事件
    /// 2. 引入防抖（Debounce）机制，避免频繁触发
    /// 3. 通过事件通知外部配置已更新
    /// </summary>
    public class ConfigWatcher : IDisposable
    {
        private readonly FileSystemWatcher _watcher;
        private readonly string _configPath;
        private Timer? _debounceTimer;
        private const int DebounceDelayMs = 500;

        /// <summary>
        /// 配置更新事件
        /// </summary>
        public event Action<string>? ConfigChanged;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="configDirectory">配置目录路径</param>
        public ConfigWatcher(string configDirectory)
        {
            _configPath = Path.GetFullPath(configDirectory);
            
            if (!Directory.Exists(_configPath))
            {
                Directory.CreateDirectory(_configPath);
            }

            _watcher = new FileSystemWatcher(_configPath)
            {
                Filter = "*.json",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            _watcher.Changed += OnFileChanged;
            _watcher.Created += OnFileChanged;
            _watcher.Renamed += OnFileChanged;
        }

        /// <summary>
        /// 文件变化回调
        /// </summary>
        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            // 防抖处理：在短时间内多次触发时，只执行最后一次
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(_ => 
            {
                Console.WriteLine($"[ConfigWatcher] 检测到配置变化: {e.Name}");
                ConfigChanged?.Invoke(e.FullPath);
            }, null, DebounceDelayMs, Timeout.Infinite);
        }

        /// <summary>
        /// 停止监听
        /// </summary>
        public void Stop()
        {
            _watcher.EnableRaisingEvents = false;
        }

        /// <summary>
        /// 恢复监听
        /// </summary>
        public void Start()
        {
            _watcher.EnableRaisingEvents = true;
        }

        public void Dispose()
        {
            _debounceTimer?.Dispose();
            _watcher.Dispose();
        }
    }
}
