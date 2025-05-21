using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Forms;
using System.Drawing;
using SkinHunterWPF.ViewModels;
using System.IO;
using System.Diagnostics;

namespace SkinHunterWPF
{
    public partial class MainWindow : Window
    {
        private NotifyIcon? _notifyIcon;
        private bool _isExplicitClose = false;

        public MainWindow()
        {
            InitializeComponent();
            SetupNotifyIcon();
            MaximizeButton.IsEnabled = false;
            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel mvm)
            {
                await mvm.EnsureInitialDataLoadedAsync();
            }
        }

        private void SetupNotifyIcon()
        {
            try
            {
                string iconFileName = "icon.ico"; // Nombre del archivo de icono
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", iconFileName);

                if (File.Exists(iconPath))
                {
                    _notifyIcon = new NotifyIcon
                    {
                        Icon = new Icon(iconPath),
                        Visible = false,
                        Text = "Skin Hunter"
                    };
                    _notifyIcon.DoubleClick += NotifyIcon_DoubleClick;

                    var contextMenu = new ContextMenuStrip();
                    contextMenu.Items.Add("Abrir Skin Hunter", null, (s, args) => RestoreWindow());
                    contextMenu.Items.Add("Salir", null, (s, args) => ExitApplication());
                    _notifyIcon.ContextMenuStrip = contextMenu;
                }
                else
                {
                    Debug.WriteLine($"Error: NotifyIcon - El archivo '{iconPath}' no fue encontrado. El icono de la bandeja del sistema no estará disponible.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al configurar NotifyIcon: {ex.Message}");
            }
        }

        private void RestoreWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
            if (_notifyIcon != null) _notifyIcon.Visible = false;
        }

        private void ExitApplication()
        {
            _isExplicitClose = true;
            _notifyIcon?.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        private void NotifyIcon_DoubleClick(object? sender, EventArgs e)
        {
            RestoreWindow();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!_isExplicitClose && _notifyIcon != null && _notifyIcon.Icon != null)
            {
                e.Cancel = true;
                Hide();
                _notifyIcon.Visible = true;
            }
            else
            {
                _notifyIcon?.Dispose();
            }
            base.OnClosing(e);
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