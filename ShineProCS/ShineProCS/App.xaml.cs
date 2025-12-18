using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ShineProCS.Core.Engine;
using ShineProCS.Core.Interfaces;
using ShineProCS.Core.Services;
using ShineProCS.Core.Strategies;
using ShineProCS.Infrastructure;
using ShineProCS.Utils;
using ShineProCS.ViewModels;

namespace ShineProCS
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// 应用程序的入口点，负责配置依赖注入和启动主窗口
    /// </summary>
    public partial class App : Application
    {
        // ===== 依赖注入容器 =====
        // ServiceProvider 是 .NET 的依赖注入容器（类似 Python 的依赖容器）
        private ServiceProvider? _serviceProvider;

        public App()
        {
            // 捕获 UI 线程上的未处理异常
            this.DispatcherUnhandledException += (s, e) =>
            {
                System.IO.File.AppendAllText("startup_error.txt", $"\n[Dispatcher Error] {DateTime.Now}: {e.Exception}");
                e.Handled = true;
                MessageBox.Show($"UI 异常: {e.Exception.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            // 捕获非 UI 线程上的未处理异常
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                System.IO.File.AppendAllText("startup_error.txt", $"\n[AppDomain Error] {DateTime.Now}: {e.ExceptionObject}");
            };
        }

        /// <summary>
        /// 应用程序启动时调用
        /// 这是整个程序的入口点
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            try 
            {
                base.OnStartup(e);

                // ===== 配置依赖注入 =====
                var services = new ServiceCollection();
                ConfigureServices(services);
                _serviceProvider = services.BuildServiceProvider();

                // ===== 启动主窗口 =====
                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                // 将错误写入文件以便诊断
                System.IO.File.WriteAllText("startup_error.txt", ex.ToString());
                MessageBox.Show($"启动失败: {ex.Message}\n详情请查看 startup_error.txt", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        /// <summary>
        /// 配置依赖注入服务
        /// 
        /// 【依赖注入说明】
        /// 依赖注入（DI）是一种设计模式，用于管理对象的创建和依赖关系
        /// 
        /// 生命周期：
        /// - Singleton: 单例，整个应用只创建一次（类似 Python 的全局变量）
        /// - Transient: 瞬时，每次请求都创建新实例
        /// - Scoped: 作用域，在同一作用域内共享实例
        /// </summary>
        private void ConfigureServices(IServiceCollection services)
        {
            // ===== 注册接口实现 =====
            services.AddSingleton<IKeyboardInterface, Win32KeyboardInterface>();
            services.AddSingleton<IImageInterface, OpenCvImageInterface>();

            // ===== 注册工具类 =====
            services.AddSingleton<PerformanceMonitor>();
            services.AddSingleton<MemoryMonitor>();
            services.AddSingleton<CacheManager>(CacheManager.Instance);

            // ===== 注册配置管理器 =====
            services.AddSingleton<ConfigManager>();

            // ===== 注册高级服务 =====
            services.AddSingleton<StateMonitor>();
            services.AddSingleton<TargetSelector>();
            services.AddSingleton<StrategyManager>();
            services.AddSingleton<SkillStateDetector>();

            // ===== 注册核心服务 =====
            services.AddSingleton<SkillLoopEngine>();

            // ===== 注册 ViewModel =====
            services.AddSingleton<MainViewModel>();

            // ===== 注册窗口 =====
            services.AddTransient<MainWindow>();
        }

        /// <summary>
        /// 应用程序退出时调用
        /// 清理资源
        /// </summary>
        protected override void OnExit(ExitEventArgs e)
        {
            // 释放依赖注入容器
            _serviceProvider?.Dispose();
            
            base.OnExit(e);
        }
    }
}
