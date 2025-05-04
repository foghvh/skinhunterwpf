using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkinHunterWPF.Services;
using System.Threading.Tasks;
using System.Linq;
using System.Windows;

namespace SkinHunterWPF.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;

        [ObservableProperty]
        private BaseViewModel? _currentViewModel;

        // Property to hold the ViewModel for the currently displayed dialog
        [ObservableProperty]
        private BaseViewModel? _dialogViewModel;

        public MainViewModel(INavigationService navigationService, ChampionGridViewModel championGridVM)
        {
            _navigationService = navigationService;
            _currentViewModel = championGridVM;
            _currentViewModel.IsLoading = true;
            _ = LoadInitialDataAsync();
        }

        private async Task LoadInitialDataAsync()
        {
            // Check underlying collection via view if ChampionsView is used
            if (CurrentViewModel is ChampionGridViewModel cgvm && !cgvm.ChampionsView.Cast<object>().Any())
            {
                await cgvm.LoadChampionsCommand.ExecuteAsync(null);
            }
            IsLoading = false;
        }

        // Command bound to the Dialog's close button (or triggered by dialog VM)
        [RelayCommand]
        private void CloseDialog()
        {
            DialogViewModel = null;
        }

        // --- Navigation Commands ---
        [RelayCommand]
        private void NavigateToChampions()
        {
            if (DialogViewModel == null) // Prevent navigation while dialog is open? Optional.
                _navigationService.NavigateTo<ChampionGridViewModel>();
        }

        [RelayCommand]
        private void NavigateToInstalled()
        {
            if (DialogViewModel == null)
                MessageBox.Show("Installed View Not Implemented Yet."); // Use standard MessageBox
        }

        [RelayCommand]
        private void OpenSearch()
        {
            if (DialogViewModel == null)
                MessageBox.Show("Search Not Implemented Yet.");
        }

        [RelayCommand]
        private void OpenProfile()
        {
            if (DialogViewModel == null)
                MessageBox.Show("Profile View Not Implemented Yet.");
        }
    }
}