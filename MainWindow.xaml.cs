using System.Windows;
using System.Windows.Input;
using System; // Required for EventArgs
using System.Windows.Shapes; // Required for Path

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
            // Find the Path element by its Name within the visual tree or template if necessary
            // For simplicity, assuming MaximizeIcon is directly accessible (might need adjustment)
            var maximizeButton = FindName("MaximizeIcon") as Path; // Use FindName or VisualTreeHelper
            if (maximizeButton != null)
            {
                if (this.WindowState == WindowState.Maximized)
                {
                    maximizeButton.Data = System.Windows.Media.Geometry.Parse("M2,0 H10 V8 H8 V10 H0 V2 H2 Z M3,3 V7 H7 V3 Z");
                }
                else
                {
                    maximizeButton.Data = System.Windows.Media.Geometry.Parse("M0,0 H10 V10 H0 Z M2,2 V8 H8 V2 Z");
                }
            }
            base.OnStateChanged(e);
        }
    }
}