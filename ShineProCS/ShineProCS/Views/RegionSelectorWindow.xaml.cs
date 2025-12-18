using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ShineProCS.Views
{
    /// <summary>
    /// 区域选择器窗口
    /// 允许用户在屏幕上拖拽出一个矩形区域
    /// </summary>
    public partial class RegionSelectorWindow : Window
    {
        private Point _startPoint;
        private bool _isSelecting;

        /// <summary>
        /// 选择结果（X, Y, Width, Height）
        /// </summary>
        public Int32Rect SelectedRegion { get; private set; }

        public RegionSelectorWindow()
        {
            InitializeComponent();
            
            // 全屏显示
            this.Left = 0;
            this.Top = 0;
            this.Width = SystemParameters.PrimaryScreenWidth;
            this.Height = SystemParameters.PrimaryScreenHeight;
        }

        private void SelectionCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _isSelecting = true;
                _startPoint = e.GetPosition(SelectionCanvas);
                
                Canvas.SetLeft(SelectionBorder, _startPoint.X);
                Canvas.SetTop(SelectionBorder, _startPoint.Y);
                SelectionBorder.Width = 0;
                SelectionBorder.Height = 0;
                SelectionBorder.Visibility = Visibility.Visible;
            }
        }

        private void SelectionCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isSelecting)
            {
                var currentPoint = e.GetPosition(SelectionCanvas);
                
                var x = Math.Min(_startPoint.X, currentPoint.X);
                var y = Math.Min(_startPoint.Y, currentPoint.Y);
                var w = Math.Abs(_startPoint.X - currentPoint.X);
                var h = Math.Abs(_startPoint.Y - currentPoint.Y);

                Canvas.SetLeft(SelectionBorder, x);
                Canvas.SetTop(SelectionBorder, y);
                SelectionBorder.Width = w;
                SelectionBorder.Height = h;
            }
        }

        private void SelectionCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isSelecting)
            {
                _isSelecting = false;
                
                // 记录结果
                SelectedRegion = new Int32Rect(
                    (int)Canvas.GetLeft(SelectionBorder),
                    (int)Canvas.GetTop(SelectionBorder),
                    (int)SelectionBorder.Width,
                    (int)SelectionBorder.Height
                );

                this.DialogResult = true;
                this.Close();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
                this.Close();
            }
            base.OnKeyDown(e);
        }
    }
}
