using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkinHunterWPF.Models;
using SkinHunterWPF.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.ComponentModel;
using System; // Required for StringComparison
using System.Windows; // Required for standard MessageBox

namespace SkinHunterWPF.ViewModels
{
    public partial class ChampionGridViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly ObservableCollection<ChampionSummary> _allChampions = new();

        [ObservableProperty]
        private string? _searchText;

        public ICollectionView ChampionsView { get; }

        public ChampionGridViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            ChampionsView = CollectionViewSource.GetDefaultView(_allChampions);
            ChampionsView.Filter = FilterChampions;
        }

        partial void OnSearchTextChanged(string? value)
        {
            ChampionsView.Refresh();
        }

        private bool FilterChampions(object item)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                return true;

            if (item is ChampionSummary champ)
            {
                return champ.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        [RelayCommand]
        public async Task LoadChampionsAsync()
        {
            if (_allChampions.Any()) return;

            IsLoading = true;
            var champs = await CdragonDataService.GetChampionSummariesAsync();
            if (champs != null)
            {
                _allChampions.Clear();
                foreach (var champ in champs)
                {
                    _allChampions.Add(champ);
                }
                ChampionsView.Refresh();
            }
            else
            {
                // Use standard MessageBox
                MessageBox.Show("Failed to load champions.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            IsLoading = false;
        }


        [RelayCommand]
        private void SelectChampion(ChampionSummary? champion)
        {
            if (champion != null)
            {
                _navigationService.NavigateTo<ChampionDetailViewModel>(champion.Id);
            }
        }
    }
}