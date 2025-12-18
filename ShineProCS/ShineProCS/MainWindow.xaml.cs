using Hardcodet.Wpf.TaskbarNotification;
using ShineProCS.ViewModels;
using System.Windows;

namespace ShineProCS
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑（Code-Behind）
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
        /// </summary>
        private void InitializeTrayIcon()
        {
            _trayIcon = new TaskbarIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                ToolTipText = "ShinePro - 技能循环引擎",
                ContextMenu = (System.Windows.Controls.ContextMenu)this.FindResource("TrayMenu")
            };

            // 双击事件
            _trayIcon.TrayMouseDoubleClick += TrayIcon_DoubleClick;
        }

        // ===== 托盘图标事件处理 =====

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
            _trayIcon?.ShowBalloonTip("ShinePro", "程序已最小化到系统托盘", BalloonIcon.Info);
        }

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
                this.Activate();
            }
        }

        private void ShowWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void HideWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void StartEngine_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as MainViewModel;
            viewModel?.StartEngineCommand.Execute(null);
        }

        private void StopEngine_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as MainViewModel;
            viewModel?.StopEngineCommand.Execute(null);
        }

        private void ExitApp_Click(object sender, RoutedEventArgs e)
        {
            _trayIcon?.Dispose();
            // 强制退出进程，确保所有后台线程关闭
            System.Environment.Exit(0);
        }
    }
}