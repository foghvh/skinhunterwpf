using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkinHunterWPF.Models;
using SkinHunterWPF.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System.Windows;

namespace SkinHunterWPF.ViewModels
{
    public partial class ChampionDetailViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;

        [ObservableProperty]
        private ChampionDetail? _champion;

        [ObservableProperty]
        private ObservableCollection<Skin> _skins = new();

        public ChampionDetailViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        [RelayCommand]
        public async Task LoadChampionAsync(int championId)
        {
            if (Champion?.Id == championId && Skins.Any()) return;

            IsLoading = true;
            Skins.Clear();
            Champion = null;

            var details = await CdragonDataService.GetChampionDetailsAsync(championId);
            if (details != null)
            {
                Champion = details;
                if (details.Skins != null)
                {
                    foreach (var skin in details.Skins)
                    {
                        if (!skin.Name.Equals($"{details.Name}", StringComparison.OrdinalIgnoreCase) &&
                            !skin.Name.Equals($"Base {details.Name}", StringComparison.OrdinalIgnoreCase) &&
                            !skin.Name.Contains("Original", StringComparison.OrdinalIgnoreCase))
                        {
                            Skins.Add(skin);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show($"Failed to load details for Champion ID: {championId}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            IsLoading = false;
        }

        [RelayCommand]
        private void SelectSkin(Skin? skin)
        {
            if (skin != null)
            {
                _navigationService.ShowDialog<SkinDetailViewModel>(skin);
            }
        }

        [RelayCommand]
        private void GoBack()
        {
            _navigationService.GoBack();
        }
    }
}