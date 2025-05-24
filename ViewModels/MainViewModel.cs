using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkinHunterWPF.Services;
using System.Threading.Tasks;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;


namespace SkinHunterWPF.ViewModels
{
    public partial class MainViewModel(INavigationService navigationService, ChampionGridViewModel championGridVM, IServiceProvider serviceProvider) : BaseViewModel
    {
        private readonly INavigationService _navigationService = navigationService;
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        [ObservableProperty]
        private BaseViewModel? _currentViewModel = championGridVM;

        [ObservableProperty]
        private BaseViewModel? _dialogViewModel;

        [ObservableProperty]
        private OmnisearchViewModel? _omnisearchDialogViewModel;


        public async Task EnsureInitialDataLoadedAsync()
        {
            if (CurrentViewModel is ChampionGridViewModel cgvm)
            {
                if (cgvm.ChampionsView == null || !cgvm.ChampionsView.Cast<object>().Any())
                {
                    if (cgvm.LoadChampionsCommand.CanExecute(null))
                        await cgvm.LoadChampionsCommand.ExecuteAsync(null);
                }
            }
        }


        [RelayCommand]
        private void CloseDialog()
        {
            DialogViewModel = null;
        }

        // Usar [RelayCommand], el generador crea la versión Async si el método devuelve Task
        [RelayCommand]
        private async Task OpenOmnisearchDialogAsync()
        {
            if (OmnisearchDialogViewModel == null)
            {
                Debug.WriteLine("[MainViewModel] Creando instancia de OmnisearchViewModel.");
                OmnisearchDialogViewModel = _serviceProvider.GetRequiredService<OmnisearchViewModel>();
            }
            Debug.WriteLine("[MainViewModel] Asegurando datos para OmnisearchViewModel.");
            if (OmnisearchDialogViewModel != null)
            {
                await OmnisearchDialogViewModel.EnsureDataLoadedAsync();
            }
            Debug.WriteLine("[MainViewModel] OmnisearchDialogViewModel listo para mostrarse.");
        }


        [RelayCommand]
        private void NavigateToChampions()
        {
            if (DialogViewModel == null && OmnisearchDialogViewModel == null)
            {
                _navigationService.NavigateTo<ChampionGridViewModel>();
            }
        }

        [RelayCommand]
        private void NavigateToInstalled()
        {
            if (DialogViewModel == null && OmnisearchDialogViewModel == null)
                System.Windows.MessageBox.Show("Installed View Not Implemented Yet.");
        }

        // El comando generado para este método será OpenSearchCommand
        [RelayCommand]
        private async Task OpenSearchAsync()
        {
            if (DialogViewModel == null)
            {
                // El comando generado para OpenOmnisearchDialogAsync será OpenOmnisearchDialogCommand
                if (OpenOmnisearchDialogCommand != null && OpenOmnisearchDialogCommand.CanExecute(null))
                {
                    await OpenOmnisearchDialogCommand.ExecuteAsync(null);
                }
            }
        }

        [RelayCommand]
        private void OpenProfile()
        {
            if (DialogViewModel == null && OmnisearchDialogViewModel == null)
                System.Windows.MessageBox.Show("Profile View Not Implemented Yet.");
        }
    }
}