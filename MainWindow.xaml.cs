using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Forms;
using System.Drawing;
using SkinHunterWPF.ViewModels;
using System.IO;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace SkinHunterWPF
{
    public partial class MainWindow : Window
    {
        private NotifyIcon? _notifyIcon;
        private bool _isExplicitClose = false;
        private bool _notifyIconInitializedSuccessfully = false;
        private readonly IServiceProvider _serviceProvider;
        private FrameworkElement? _mainContentCache;


        public MainWindow(IServiceProvider serviceProvider)
        {
            Debug.WriteLine("[MainWindow] Constructor INICIADO.");
            InitializeComponent();
            _serviceProvider = serviceProvider;
            Debug.WriteLine("[MainWindow] InitializeComponent COMPLETADO.");
            SetupNotifyIcon();
            Debug.WriteLine("[MainWindow] SetupNotifyIcon LLAMADO desde el constructor.");
            MaximizeButton.IsEnabled = false;
            this.Loaded += MainWindow_Loaded;
            Debug.WriteLine("[MainWindow] Constructor FINALIZADO.");
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[MainWindow] MainWindow_Loaded INICIADO.");
            if (DataContext is MainViewModel mvm)
            {
                if (MainContentControl.Content is FrameworkElement fe)
                {
                    _mainContentCache = fe;
                }
                await mvm.EnsureInitialDataLoadedAsync();
            }
            Debug.WriteLine("[MainWindow] MainWindow_Loaded FINALIZADO.");
        }

        private void MinimizeToTray()
        {
            Debug.WriteLine("[MainWindow] Minimizando a la bandeja...");
            this.Hide();
            if (_notifyIcon != null) _notifyIcon.Visible = true;

            if (DataContext is MainViewModel mainVM)
            {
                if (mainVM.CurrentViewModel is ChampionGridViewModel cgvm)
                {
                    cgvm.ReleaseResourcesForTray();
                }
                else if (mainVM.CurrentViewModel is ChampionDetailViewModel cdvm)
                {
                    cdvm.ReleaseResourcesForTray();
                }
            }

            if (MainContentControl != null && MainContentControl.Content != null)
            {
                if (_mainContentCache == null) // Guardar solo si no se hizo en Loaded o si cambió
                {
                    _mainContentCache = MainContentControl.Content as FrameworkElement;
                }
                MainContentControl.Content = null; // Desconectar para liberar
                Debug.WriteLine("[MainWindow] MainContentControl.Content puesto a null.");
            }


            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
            Debug.WriteLine("[MainWindow] Recursos liberados (intento) y GC invocado.");
        }

        private async Task RestoreFromTrayAsync()
        {
            Debug.WriteLine("[MainWindow] Restaurando desde la bandeja...");
            if (_notifyIcon != null) _notifyIcon.Visible = false;

            if (MainContentControl != null && MainContentControl.Content == null && _mainContentCache != null)
            {
                MainContentControl.Content = _mainContentCache;
                Debug.WriteLine("[MainWindow] MainContentControl.Content restaurado desde caché.");
            }


            if (DataContext is MainViewModel mainVM)
            {
                if (mainVM.CurrentViewModel is ChampionGridViewModel cgvm)
                {
                    Debug.WriteLine("[MainWindow] Recargando ChampionGridViewModel...");
                    if (cgvm.LoadChampionsCommand.CanExecute(null))
                        await cgvm.LoadChampionsCommand.ExecuteAsync(null);
                }
                else if (mainVM.CurrentViewModel is ChampionDetailViewModel cdvm && cdvm.Champion != null)
                {
                    Debug.WriteLine($"[MainWindow] Recargando ChampionDetailViewModel para {cdvm.Champion.Name}...");
                    int champId = cdvm.Champion.Id;
                    cdvm.PrepareForReload();
                    if (cdvm.LoadChampionCommand.CanExecute(champId))
                        await cdvm.LoadChampionCommand.ExecuteAsync(champId);
                }
                else if (mainVM.CurrentViewModel == null && _mainContentCache != null) // Si el VM fue completamente destruido
                {
                    Debug.WriteLine("[MainWindow] CurrentViewModel era null, intentando restaurar vista por defecto.");
                    var championGridViewModel = _serviceProvider.GetRequiredService<ChampionGridViewModel>();
                    mainVM.CurrentViewModel = championGridViewModel; // Asignar directamente
                    if (MainContentControl.Content == null) MainContentControl.Content = _mainContentCache; // Asegurar que el control tiene una vista
                    if (championGridViewModel.LoadChampionsCommand.CanExecute(null))
                        await championGridViewModel.LoadChampionsCommand.ExecuteAsync(null);
                }
                else if (mainVM.CurrentViewModel == null) // Si _mainContentCache también es null (caso extremo)
                {
                    Debug.WriteLine("[MainWindow] CurrentViewModel y _mainContentCache eran null, navegando a campeones.");
                    mainVM.NavigateToChampionsCommand.Execute(null);
                }
            }

            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
            Debug.WriteLine("[MainWindow] Ventana restaurada y ViewModel recargado (intento).");
        }


        private void SetupNotifyIcon()
        {
            Debug.WriteLine("[NotifyIcon] SetupNotifyIcon INICIADO.");
            string iconFileName = "icon.ico";
            string iconPath = "";
            try
            {
                iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", iconFileName);
                Debug.WriteLine($"[NotifyIcon] Intentando cargar icono desde: {iconPath}");

                if (File.Exists(iconPath))
                {
                    Debug.WriteLine($"[NotifyIcon] Archivo '{iconPath}' EXISTE.");
                    _notifyIcon = new NotifyIcon
                    {
                        Text = "Skin Hunter",
                        Visible = false
                    };

                    try
                    {
                        _notifyIcon.Icon = new Icon(iconPath);
                        Debug.WriteLine("[NotifyIcon] Icono cargado y asignado exitosamente.");
                    }
                    catch (ArgumentException argEx)
                    {
                        Debug.WriteLine($"[NotifyIcon] ArgumentException al cargar Icon: {argEx.Message}. Asegúrate que '{iconFileName}' es un archivo .ico válido.");
                        _notifyIcon.Dispose();
                        _notifyIcon = null;
                        _notifyIconInitializedSuccessfully = false;
                        return;
                    }
                    catch (Exception exIcon)
                    {
                        Debug.WriteLine($"[NotifyIcon] Excepción general al cargar Icon: {exIcon.ToString()}");
                        _notifyIcon.Dispose();
                        _notifyIcon = null;
                        _notifyIconInitializedSuccessfully = false;
                        return;
                    }

                    _notifyIcon.DoubleClick += async (s, args) => await RestoreFromTrayAsync();

                    var contextMenu = new ContextMenuStrip();
                    contextMenu.Items.Add("Abrir Skin Hunter", null, async (s, args) => await RestoreFromTrayAsync());
                    contextMenu.Items.Add(new ToolStripSeparator());
                    contextMenu.Items.Add("Salir", null, (s, args) => ExitApplication());
                    _notifyIcon.ContextMenuStrip = contextMenu;

                    _notifyIconInitializedSuccessfully = true;
                    Debug.WriteLine("[NotifyIcon] NotifyIcon configurado completamente y marcado como exitoso.");
                }
                else
                {
                    Debug.WriteLine($"[NotifyIcon] ERROR CRÍTICO: El archivo '{iconPath}' NO FUE ENCONTRADO.");
                    _notifyIconInitializedSuccessfully = false;
                }
            }
            catch (Exception exSetup)
            {
                Debug.WriteLine($"[NotifyIcon] Excepción general en SetupNotifyIcon (fuera de la carga del Icon): {exSetup.ToString()}");
                _notifyIconInitializedSuccessfully = false;
                if (_notifyIcon != null)
                {
                    _notifyIcon.Dispose();
                    _notifyIcon = null;
                }
            }
            Debug.WriteLine($"[NotifyIcon] SetupNotifyIcon FINALIZADO. _notifyIconInitializedSuccessfully = {_notifyIconInitializedSuccessfully}");
        }

        private void ExitApplication()
        {
            Debug.WriteLine("[NotifyIcon] Salida explícita solicitada.");
            _isExplicitClose = true;
            _notifyIcon?.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            Debug.WriteLine($"[NotifyIcon] OnClosing: _isExplicitClose={_isExplicitClose}, _notifyIconInitializedSuccessfully={_notifyIconInitializedSuccessfully}");

            if (!_isExplicitClose && _notifyIconInitializedSuccessfully && _notifyIcon != null)
            {
                Debug.WriteLine("[NotifyIcon] OnClosing: Cancelando cierre, llamando a MinimizeToTray.");
                e.Cancel = true;
                MinimizeToTray();
            }
            else
            {
                Debug.WriteLine("[NotifyIcon] OnClosing: Cierre normal o NotifyIcon no listo/inicializado. Realizando dispose.");
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
            Debug.WriteLine("[MainWindow] Botón X presionado, llamando a this.Close().");
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