using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Forms;
using System.Drawing;
using SkinHunterWPF.ViewModels;
using SkinHunterWPF.Services; // AÑADIR ESTE USING
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
        private int? _lastChampionIdFromDetailView = null;


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
                    Debug.WriteLine("[MainWindow] Llamando a ReleaseResourcesForTray en ChampionGridViewModel.");
                    cgvm.ReleaseResourcesForTray();
                }
                else if (mainVM.CurrentViewModel is ChampionDetailViewModel cdvm)
                {
                    Debug.WriteLine("[MainWindow] Llamando a ReleaseResourcesForTray en ChampionDetailViewModel.");
                    _lastChampionIdFromDetailView = cdvm.Champion?.Id;
                    cdvm.ReleaseResourcesForTray();
                }
            }

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
            GC.WaitForPendingFinalizers();
            Debug.WriteLine("[MainWindow] Intento de liberación de recursos y GC invocado para bandeja.");
        }

        private async Task RestoreFromTrayAsync()
        {
            Debug.WriteLine("[MainWindow] Restaurando desde la bandeja...");
            if (_notifyIcon != null) _notifyIcon.Visible = false;

            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();

            if (DataContext is MainViewModel mainVM)
            {
                Debug.WriteLine($"[MainWindow] ViewModel actual al restaurar: {mainVM.CurrentViewModel?.GetType().Name}");
                if (mainVM.CurrentViewModel is ChampionGridViewModel cgvm)
                {
                    Debug.WriteLine("[MainWindow] Recargando ChampionGridViewModel...");
                    if (cgvm.LoadChampionsCommand.CanExecute(null))
                        await cgvm.LoadChampionsCommand.ExecuteAsync(null);
                }
                else if (mainVM.CurrentViewModel is ChampionDetailViewModel cdvm)
                {
                    if (_lastChampionIdFromDetailView.HasValue)
                    {
                        Debug.WriteLine($"[MainWindow] Preparando para recargar ChampionDetailViewModel (ID guardado: {_lastChampionIdFromDetailView.Value})...");
                        cdvm.PrepareForReload();
                        if (cdvm.LoadChampionCommand.CanExecute(_lastChampionIdFromDetailView.Value))
                        {
                            Debug.WriteLine($"[MainWindow] Recargando ChampionDetailViewModel para ID: {_lastChampionIdFromDetailView.Value}.");
                            await cdvm.LoadChampionCommand.ExecuteAsync(_lastChampionIdFromDetailView.Value);
                        }
                        else
                        {
                            Debug.WriteLine($"[MainWindow] No se pudo ejecutar LoadChampionCommand para ChampionDetailViewModel ID: {_lastChampionIdFromDetailView.Value}. Volviendo a la cuadrícula.");
                            _serviceProvider.GetRequiredService<INavigationService>().NavigateTo<ChampionGridViewModel>();
                        }
                        _lastChampionIdFromDetailView = null;
                    }
                    else
                    {
                        Debug.WriteLine("[MainWindow] ChampionDetailViewModel no tenía un ID de campeón previo para recargar, volviendo a la cuadrícula.");
                        _serviceProvider.GetRequiredService<INavigationService>().NavigateTo<ChampionGridViewModel>();
                    }
                }
                else
                {
                    Debug.WriteLine("[MainWindow] No se reconoce el ViewModel actual o es null, navegando a la vista de campeones por defecto.");
                    _serviceProvider.GetRequiredService<INavigationService>().NavigateTo<ChampionGridViewModel>();
                }
            }
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
                    catch (Exception exIcon)
                    {
                        Debug.WriteLine($"[NotifyIcon] EXCEPCIÓN al cargar Icon (archivo: {iconPath}): {exIcon.ToString()}. Asegúrate que '{iconFileName}' es un archivo .ico válido y accesible.");
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