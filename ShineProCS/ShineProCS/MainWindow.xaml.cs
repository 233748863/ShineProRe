using Hardcodet.Wpf.TaskbarNotification;
using ShineProCS.ViewModels;
using System.Windows;

namespace ShineProCS
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑（Code-Behind）
    /// 
    /// 【HandyControl Window 说明】
    /// 继承自 HandyControl.Controls.Window
    /// 这样可以使用 HandyControl 提供的增强窗口功能
    /// 
    /// 注意：使用完全限定名避免命名空间冲突
    /// </summary>
    public partial class MainWindow : HandyControl.Controls.Window
    {
        // 托盘图标对象
        private TaskbarIcon? _trayIcon;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="viewModel">通过依赖注入传入的 ViewModel</param>
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            
            // 初始化托盘图标
            InitializeTrayIcon();
            
            // 窗口关闭事件（最小化到托盘而不是真正关闭）
            this.Closing += MainWindow_Closing;
        }

        /// <summary>
        /// 初始化系统托盘图标
        /// 在 Code-Behind 中创建托盘图标，避免 XAML 结构问题
        /// </summary>
        private void InitializeTrayIcon()
        {
            _trayIcon = new TaskbarIcon
            {
                // 设置图标（暂时使用应用程序图标）
                // 实际项目中应该使用自定义的 .ico 文件
                Icon = System.Drawing.SystemIcons.Application,
                
                // 设置提示文本
                ToolTipText = "ShinePro - 技能循环引擎",
                
                // 设置右键菜单（从 XAML 资源中获取）
                ContextMenu = (System.Windows.Controls.ContextMenu)this.FindResource("TrayMenu")
            };

            // 双击事件
            _trayIcon.TrayMouseDoubleClick += TrayIcon_DoubleClick;
        }

        // ===== 托盘图标事件处理 =====

        /// <summary>
        /// 窗口关闭事件 - 最小化到托盘
        /// 当用户点击关闭按钮时，不真正关闭窗口，而是隐藏到托盘
        /// </summary>
        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // 取消关闭操作
            e.Cancel = true;
            
            // 隐藏窗口到托盘
            this.Hide();
            
            // 显示托盘提示（可选）
            _trayIcon?.ShowBalloonTip("ShinePro", "程序已最小化到系统托盘", BalloonIcon.Info);
        }

        /// <summary>
        /// 托盘图标双击 - 显示/隐藏窗口
        /// </summary>
        private void TrayIcon_DoubleClick(object sender, RoutedEventArgs e)
        {
            if (this.IsVisible)
            {
                this.Hide();
            }
            else
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.Activate();  // 激活窗口（获得焦点）
            }
        }

        /// <summary>
        /// 显示主窗口
        /// </summary>
        private void ShowWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        /// <summary>
        /// 隐藏主窗口
        /// </summary>
        private void HideWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        /// <summary>
        /// 启动引擎（通过 ViewModel）
        /// </summary>
        private void StartEngine_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as MainViewModel;
            viewModel?.StartCommand.Execute(null);
        }

        /// <summary>
        /// 停止引擎（通过 ViewModel）
        /// </summary>
        private void StopEngine_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as MainViewModel;
            viewModel?.StopCommand.Execute(null);
        }

        /// <summary>
        /// 退出程序
        /// </summary>
        private void ExitApp_Click(object sender, RoutedEventArgs e)
        {
            // 真正退出程序
            _trayIcon?.Dispose();  // 释放托盘图标
            Application.Current.Shutdown();
        }
    }
}