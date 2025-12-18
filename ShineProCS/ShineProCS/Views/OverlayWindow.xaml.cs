using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ShineProCS.Views
{
    public partial class OverlayWindow : Window
    {
        public OverlayWindow()
        {
            InitializeComponent();
            this.Left = 0;
            this.Top = 0;
            this.Width = SystemParameters.PrimaryScreenWidth;
            this.Height = SystemParameters.PrimaryScreenHeight;
        }

        public void UpdateRegions(IEnumerable<(int[] Region, string Name, bool IsReady)> regions)
        {
            Dispatcher.Invoke(() =>
            {
                OverlayCanvas.Children.Clear();
                foreach (var (region, name, isReady) in regions)
                {
                    if (region == null || region.Length < 4) continue;

                    var rect = new Rectangle
                    {
                        Width = region[2],
                        Height = region[3],
                        Stroke = isReady ? Brushes.Lime : Brushes.Red,
                        StrokeThickness = 2,
                        Opacity = 0.7
                    };

                    Canvas.SetLeft(rect, region[0]);
                    Canvas.SetTop(rect, region[1]);
                    OverlayCanvas.Children.Add(rect);

                    var text = new TextBlock
                    {
                        Text = name,
                        Foreground = isReady ? Brushes.Lime : Brushes.Red,
                        FontSize = 12,
                        FontWeight = FontWeights.Bold,
                        Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0))
                    };
                    Canvas.SetLeft(text, region[0]);
                    Canvas.SetTop(text, region[1] - 15);
                    OverlayCanvas.Children.Add(text);
                }
            });
        }
    }
}
