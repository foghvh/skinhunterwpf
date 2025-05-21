using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkinHunterWPF.Services;
using System.Threading.Tasks;
using System.Linq;
using System.Windows; // Se mantiene para MessageBoxButton, MessageBoxImage
using Microsoft.Extensions.DependencyInjection;
using System;


namespace SkinHunterWPF.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private BaseViewModel? _currentViewModel;

        [ObservableProperty]
        private BaseViewModel? _dialogViewModel;

        public MainViewModel(INavigationService navigationService, ChampionGridViewModel championGridVM, IServiceProvider serviceProvider)
        {
            _navigationService = navigationService;
            _serviceProvider = serviceProvider;
            _currentViewModel = championGridVM;
        }

        public async Task EnsureInitialDataLoadedAsync()
        {
            if (CurrentViewModel is ChampionGridViewModel cgvm)
            {
                if (cgvm.ChampionsView == null || !cgvm.ChampionsView.Cast<object>().Any())
                {
                    await cgvm.LoadChampionsCommand.ExecuteAsync(null);
                }
            }
        }


        [RelayCommand]
        private void CloseDialog()
        {
            DialogViewModel = null;
        }

        [RelayCommand]
        private void NavigateToChampions()
        {
            if (DialogViewModel == null)
            {
                _navigationService.NavigateTo<ChampionGridViewModel>();
            }
        }

        [RelayCommand]
        private void NavigateToInstalled()
        {
            if (DialogViewModel == null)
                System.Windows.MessageBox.Show("Installed View Not Implemented Yet."); // Calificado aquí
        }

        [RelayCommand]
        private void OpenSearch()
        {
            if (DialogViewModel == null)
                System.Windows.MessageBox.Show("Search Not Implemented Yet."); // Calificado aquí
        }

        [RelayCommand]
        private void OpenProfile()
        {
            if (DialogViewModel == null)
                System.Windows.MessageBox.Show("Profile View Not Implemented Yet."); // Calificado aquí
        }
    }
}