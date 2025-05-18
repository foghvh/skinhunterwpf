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
            if (CurrentViewModel is ChampionGridViewModel cgvm && !cgvm.ChampionsView.Cast<object>().Any())
            {
                await cgvm.LoadChampionsCommand.ExecuteAsync(null);
            }
            IsLoading = false;
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
                _navigationService.NavigateTo<ChampionGridViewModel>();
        }

        [RelayCommand]
        private void NavigateToInstalled()
        {
            if (DialogViewModel == null)
                MessageBox.Show("Installed View Not Implemented Yet.");
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