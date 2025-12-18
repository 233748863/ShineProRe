using Hardcodet.Wpf.TaskbarNotification;
using ShineProCS.ViewModels;
using System.Windows;

namespace ShineProCS
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑（Code-Behind）
    /// </summary>
    public partial class MainWindow : Window
    {
        // 托盘图标对象
        private TaskbarIcon? _trayIcon;

        /// <summary>
        /// 构造函数，支持依赖注入
        /// </summary>
        /// <param name="viewModel">主视图模型</param>
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            
            // 设置数据上下文，使 XAML 绑定生效
            DataContext = viewModel;
            
            // 初始化托盘图标
            InitializeTrayIcon();
        }

        /// <summary>
        /// 初始化托盘图标逻辑
        /// </summary>
        private void InitializeTrayIcon()
        {
            _trayIcon = new TaskbarIcon
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath),
                ToolTipText = "ShinePro - 技能循环引擎",
                ContextMenu = (System.Windows.Controls.ContextMenu)FindResource("TrayMenu")
            };
        }

        /// <summary>
        /// 窗口关闭时的处理
        /// </summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // 点击关闭按钮时隐藏窗口而不是真正关闭
            e.Cancel = true;
            this.Hide();
            base.OnClosing(e);
        }

        // --- 托盘菜单事件处理 ---

        private void ShowWindow_Click(object sender, RoutedEventArgs e)
        {
            this.Show();
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

        private void RootNavigation_SelectionChanged(ModernWpf.Controls.NavigationView sender, ModernWpf.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            // 导航逻辑处理，目前主要通过 XAML 中的 DataTrigger 处理内容切换
        }
    }
}