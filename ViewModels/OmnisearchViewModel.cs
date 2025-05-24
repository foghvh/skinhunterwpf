using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkinHunterWPF.Models;
using SkinHunterWPF.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;


namespace SkinHunterWPF.ViewModels
{
    public partial class OmnisearchViewModel : BaseViewModel
    {
        private readonly INavigationService? _navigationService;
        private readonly IServiceProvider? _serviceProvider;
        private List<ChampionSummary> _allChampionsMasterList = [];
        private List<Skin> _allSkinsMasterList = [];
        private Dictionary<int, ChampionSummary> _championMap = [];

        [ObservableProperty]
        private string? _query;

        [ObservableProperty]
        private bool _showChampionsFilter = true;

        [ObservableProperty]
        private bool _showSkinsFilter = true;

        [ObservableProperty]
        private bool _isFilterPopupOpen;

        [ObservableProperty]
        private bool _isLoadingSearchResults;

        public ObservableCollection<SearchResultItem> SearchResults { get; } = [];
        public ICollectionView SearchResultsView { get; }

        public OmnisearchViewModel()
        {
            if (DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
            {
                Query = "Search...";
                SearchResults.Add(new SearchResultItem(new ChampionSummary { Id = 1, Name = "Design Champion", SquarePortraitPath = "" }));
                SearchResults.Add(new SearchResultItem(new Skin { Id = 1001, Name = "Design Skin", TilePath = "", ChampionId = 1 }, new ChampionSummary { Id = 1, Name = "Parent" }));
            }
            _navigationService = null;
            _serviceProvider = null;
            SearchResultsView = CollectionViewSource.GetDefaultView(SearchResults);
            if (SearchResultsView.GroupDescriptions != null)
            {
                SearchResultsView.GroupDescriptions.Add(new PropertyGroupDescription("DisplayType"));
            }
        }

        public OmnisearchViewModel(INavigationService navigationService, IServiceProvider serviceProvider)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            SearchResultsView = CollectionViewSource.GetDefaultView(SearchResults);
            if (SearchResultsView.GroupDescriptions != null)
            {
                SearchResultsView.GroupDescriptions.Add(new PropertyGroupDescription("DisplayType"));
            }
        }

        private bool _isDataLoaded = false;

        public async Task EnsureDataLoadedAsync()
        {
            if (_isDataLoaded) return;
            if (_serviceProvider == null) return;

            IsLoading = true;
            try
            {
                var champsTask = CdragonDataService.GetChampionSummariesAsync();
                var skinsTask = CdragonDataService.GetAllSkinsAsync();
                await Task.WhenAll(champsTask, skinsTask);

                var champs = await champsTask;
                if (champs != null)
                {
                    _allChampionsMasterList = champs;
                    _championMap = champs.ToDictionary(c => c.Id);
                }

                var skinsDict = await skinsTask;
                if (skinsDict != null)
                {
                    _allSkinsMasterList = skinsDict.Values
                        .Where(s => {
                            bool isBaseSkinName = false;
                            if (_championMap.TryGetValue(s.ChampionId, out var parentChamp))
                            {
                                isBaseSkinName = s.Name.Equals(parentChamp.Name, StringComparison.OrdinalIgnoreCase) ||
                                                 s.Name.Equals($"Base {parentChamp.Name}", StringComparison.OrdinalIgnoreCase);
                            }
                            return !isBaseSkinName && !s.Name.Contains("Original", StringComparison.OrdinalIgnoreCase);
                        })
                        .OrderBy(s => s.Name)
                        .ToList();
                }
                _isDataLoaded = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OmnisearchViewModel] Error cargando datos maestros: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        partial void OnQueryChanged(string? value)
        {
            PerformSearch();
        }

        partial void OnShowChampionsFilterChanged(bool value)
        {
            PerformSearch();
        }

        partial void OnShowSkinsFilterChanged(bool value)
        {
            PerformSearch();
        }

        private CancellationTokenSource _searchCts = new();

        private async void PerformSearch()
        {
            if (!_isDataLoaded && _serviceProvider != null)
            {
                await EnsureDataLoadedAsync();
                if (!_isDataLoaded)
                {
                    SearchResults.Clear();
                    return;
                }
            }
            else if (!_isDataLoaded && _serviceProvider == null)
            {
                return;
            }

            _searchCts.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;
            var currentQuery = Query;

            SearchResults.Clear();

            if (string.IsNullOrWhiteSpace(currentQuery) || currentQuery.Length < 1)
            {
                IsLoadingSearchResults = false;
                return;
            }

            IsLoadingSearchResults = true;

            try
            {
                await Task.Run(async () => {
                    if (token.IsCancellationRequested) return;
                    List<SearchResultItem> newResults = [];
                    if (ShowChampionsFilter)
                    {
                        newResults.AddRange(_allChampionsMasterList
                            .Where(c => c.Name.Contains(currentQuery, StringComparison.OrdinalIgnoreCase))
                            .Select(c => new SearchResultItem(c)));
                    }
                    if (ShowSkinsFilter)
                    {
                        newResults.AddRange(_allSkinsMasterList
                            .Where(s => s.Name.Contains(currentQuery, StringComparison.OrdinalIgnoreCase))
                            .Select(s => new SearchResultItem(s, _championMap.TryGetValue(s.ChampionId, out var champ) ? champ : null)));
                    }
                    if (token.IsCancellationRequested) return;
                    var orderedResults = newResults.OrderBy(r => r.Type).ThenBy(r => r.Name).Take(25).ToList();
                    if (token.IsCancellationRequested) return;

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (!token.IsCancellationRequested)
                        {
                            foreach (var item in orderedResults)
                            {
                                SearchResults.Add(item);
                                _ = item.LoadImageAsync();
                            }
                        }
                    });
                }, token);
            }
            catch (TaskCanceledException) { Debug.WriteLine("[OmnisearchViewModel] Búsqueda (Task.Run) cancelada."); }
            catch (Exception ex) { Debug.WriteLine($"[OmnisearchViewModel] Error durante la búsqueda: {ex.Message}"); }
            finally
            {
                if (!token.IsCancellationRequested)
                {
                    IsLoadingSearchResults = false;
                }
            }
        }

        [RelayCommand]
        private void SelectResult(SearchResultItem? selectedItem)
        {
            if (selectedItem == null || _navigationService == null) return;
            CloseOmnisearchDialog();
            if (selectedItem.Type == SearchResultType.Champion)
            {
                _navigationService.NavigateTo<ChampionDetailViewModel>(selectedItem.ChampionId);
            }
            else if (selectedItem.Type == SearchResultType.Skin && selectedItem.OriginalSkinObject != null)
            {
                _navigationService.ShowDialog<SkinDetailViewModel>(selectedItem.OriginalSkinObject);
            }
        }

        [RelayCommand]
        private void CloseOmnisearchDialog()
        {
            IsFilterPopupOpen = false;
            if (_serviceProvider == null) return;
            var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
            if (mainViewModel?.OmnisearchDialogViewModel == this)
            {
                mainViewModel.OmnisearchDialogViewModel = null;
            }
        }

        [RelayCommand]
        private void ToggleFilterPopup()
        {
            IsFilterPopupOpen = !IsFilterPopupOpen;
        }
    }
}