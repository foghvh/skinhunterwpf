using System.Windows;
using System.Windows.Input;
using System;
using System.Windows.Shapes;

namespace SkinHunterWPF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            var maximizeIcon = MaximizeIconPath;
            if (maximizeIcon != null)
            {
                if (this.WindowState == WindowState.Maximized)
                {
                    maximizeIcon.Data = System.Windows.Media.Geometry.Parse("M2,0 H8 V2 H0 V10 H2 V8 H10 V2 H8 Z M3,3 V7 H7 V3Z");
                }
                else
                {
                    maximizeIcon.Data = System.Windows.Media.Geometry.Parse("M0,0 H10 V10 H0 Z M2,2 V8 H8 V2 Z");
                }
            }
            base.OnStateChanged(e);
        }
    }
}