using System.Windows.Controls;
using System.Windows.Navigation;
using System.Diagnostics;
using System;
using System.Windows;

namespace SkinHunterWPF.Views
{
    public partial class SkinDetailDialog : UserControl
    {
        public SkinDetailDialog()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to open hyperlink: {ex.Message}");
                MessageBox.Show($"Could not open link: {e.Uri.AbsoluteUri}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            e.Handled = true;
        }
    }
}