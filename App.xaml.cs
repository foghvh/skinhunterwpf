using Microsoft.Extensions.DependencyInjection;
using SkinHunterWPF.Services;
using SkinHunterWPF.ViewModels;
using System;
using System.Windows;

namespace SkinHunterWPF
{
    public partial class App : System.Windows.Application
    {
        public static IServiceProvider? ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);

                var serviceCollection = new ServiceCollection();
                ConfigureServices(serviceCollection);
                ServiceProvider = serviceCollection.BuildServiceProvider();

                var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                mainWindow.DataContext = ServiceProvider.GetRequiredService<MainViewModel>();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error fatal durante el inicio:\n\n{ex.ToString()}",
                                "Error de Inicio",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"STARTUP ERROR: {ex}");
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<INavigationService>(sp => new NavigationService(sp));
            services.AddTransient<ChampionGridViewModel>();
            services.AddTransient<ChampionDetailViewModel>();
            services.AddTransient<SkinDetailViewModel>();
            services.AddTransient<OmnisearchViewModel>();
            services.AddSingleton<MainWindow>(sp => new MainWindow(sp));
        }
    }
}