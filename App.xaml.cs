// App.xaml.cs (Modificado)
using Microsoft.Extensions.DependencyInjection;
using SkinHunterWPF.Services;
using SkinHunterWPF.ViewModels;
using System.Windows;
using System;

namespace SkinHunterWPF
{
    public partial class App : Application
    {
        public static IServiceProvider? ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            try // <--- Añadir try
            {
                base.OnStartup(e);

                // No theme setup needed here as HandyControl is removed

                var serviceCollection = new ServiceCollection();
                ConfigureServices(serviceCollection);
                ServiceProvider = serviceCollection.BuildServiceProvider();

                var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                mainWindow.DataContext = ServiceProvider.GetRequiredService<MainViewModel>();
                mainWindow.Show(); // Esta línea debería mostrar la ventana
            }
            catch (Exception ex) // <--- Añadir catch
            {
                // Mostrar el error en un MessageBox simple
                MessageBox.Show($"Error fatal durante el inicio:\n\n{ex.ToString()}",
                                "Error de Inicio",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                // Opcional: Escribir en la consola de depuración
                System.Diagnostics.Debug.WriteLine($"STARTUP ERROR: {ex}");
                // Cerrar la aplicación después de mostrar el error
                // Environment.Exit(1); // Puedes descomentar esto si prefieres que cierre inmediatamente
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // --- Registro de Servicios y ViewModels ---
            services.AddSingleton<MainViewModel>();

            // Registrar NavigationService usando una factory para manejar la dependencia de MainViewModel
            // Esto evita resolver MainViewModel *durante* el registro de NavigationService si causa problemas
            services.AddSingleton<INavigationService>(sp => new NavigationService(sp));

            services.AddTransient<ChampionGridViewModel>();
            services.AddTransient<ChampionDetailViewModel>();
            services.AddTransient<SkinDetailViewModel>();

            // Registrar la ventana principal
            services.AddSingleton<MainWindow>(); // O AddTransient si prefieres nueva instancia siempre
        }

        // No UpdateSkin method needed anymore
    }
}