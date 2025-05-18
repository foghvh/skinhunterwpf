using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkinHunterWPF.Models;
using SkinHunterWPF.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System.Windows;
using System;

namespace SkinHunterWPF.ViewModels
{
    public partial class ChampionDetailViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;

        [ObservableProperty]
        private ChampionDetail? _champion;

        [ObservableProperty]
        private ObservableCollection<Skin> _skins = [];

        public ChampionDetailViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }

        [RelayCommand]
        public async Task LoadChampionAsync(int championId)
        {
            if (Champion?.Id == championId && Skins.Count > 0)
            {
                Console.WriteLine($"DEBUG VM: Champion {championId} already loaded with {Skins.Count} skins. Skipping reload.");
                IsLoading = false;
                return;
            }

            IsLoading = true;
            Skins.Clear();
            Champion = null;


            var details = await CdragonDataService.GetChampionDetailsAsync(championId);

            if (details != null)
            {
                Champion = details;

                Skins.Clear();
                if (details.Skins != null && details.Skins.Count > 0)
                {
                    int skinsAddedCount = 0;
                    foreach (var skin in details.Skins)
                    {
                        if (!skin.Name.Equals($"{details.Name}", StringComparison.OrdinalIgnoreCase) &&
                            !skin.Name.Equals($"Base {details.Name}", StringComparison.OrdinalIgnoreCase) &&
                            !skin.Name.Contains("Original", StringComparison.OrdinalIgnoreCase))
                        {
                            Skins.Add(skin);
                            skinsAddedCount++;
                        }
                    }
                    Console.WriteLine($"DEBUG VM: Loaded {skinsAddedCount} non-base/original skins for {Champion.Name}. Total in details.Skins: {details.Skins.Count}");
                }
                else
                {
                    Console.WriteLine($"DEBUG VM: details.Skins was null or empty for {Champion.Name}.");
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