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
using System.Windows; // Para Application.Current

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
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                Query = "Search...";
            }
            _navigationService = null;
            _serviceProvider = null;
            SearchResultsView = CollectionViewSource.GetDefaultView(SearchResults);
            if (SearchResultsView.GroupDescriptions is not null)
            {
                SearchResultsView.GroupDescriptions.Add(new PropertyGroupDescription("DisplayType"));
            }
        }

        public OmnisearchViewModel(INavigationService navigationService, IServiceProvider serviceProvider)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            SearchResultsView = CollectionViewSource.GetDefaultView(SearchResults);
            if (SearchResultsView.GroupDescriptions is not null)
            {
                SearchResultsView.GroupDescriptions.Add(new PropertyGroupDescription("DisplayType"));
            }
        }

        private bool _isDataLoaded = false;

        public async Task EnsureDataLoadedAsync()
        {
            if (_isDataLoaded) return;
            if (_serviceProvider is null) return;

            IsLoading = true;
            try
            {
                var champsTask = CdragonDataService.GetChampionSummariesAsync();
                var skinsTask = CdragonDataService.GetAllSkinsAsync();
                await Task.WhenAll(champsTask, skinsTask);

                var champs = await champsTask;
                if (champs is not null)
                {
                    _allChampionsMasterList = champs;
                    _championMap = champs.ToDictionary(c => c.Id);
                }

                var skinsDict = await skinsTask;
                if (skinsDict is not null)
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
            _searchCts.Cancel();
            _searchCts = new();
            var token = _searchCts.Token;
            var currentQuery = Query;

            if (string.IsNullOrWhiteSpace(currentQuery) || currentQuery.Length < 1)
            {
                SearchResults.Clear();
                IsLoadingSearchResults = false;
                return;
            }

            if (!_isDataLoaded && _serviceProvider is not null)
            {
                IsLoadingSearchResults = true; // Mostrar carga mientras se esperan datos maestros
                await EnsureDataLoadedAsync();
                if (token.IsCancellationRequested || !_isDataLoaded)
                {
                    IsLoadingSearchResults = false;
                    SearchResults.Clear();
                    return;
                }
            }
            else if (!_isDataLoaded && _serviceProvider is null)
            {
                return; // Modo diseño
            }

            SearchResults.Clear();
            IsLoadingSearchResults = true;

            try
            {
                // La búsqueda real se ejecuta en un hilo de trabajo
                List<SearchResultItem> newRawResults = await Task.Run(() => {
                    if (token.IsCancellationRequested) return [];

                    List<SearchResultItem> filteredResults = [];
                    if (ShowChampionsFilter)
                    {
                        filteredResults.AddRange(_allChampionsMasterList
                            .Where(c => c.Name.Contains(currentQuery, StringComparison.OrdinalIgnoreCase))
                            .Select(c => new SearchResultItem(c)));
                    }
                    if (ShowSkinsFilter)
                    {
                        filteredResults.AddRange(_allSkinsMasterList
                            .Where(s => s.Name.Contains(currentQuery, StringComparison.OrdinalIgnoreCase))
                            .Select(s => new SearchResultItem(s, _championMap.TryGetValue(s.ChampionId, out var champ) ? champ : null)));
                    }
                    return filteredResults.OrderBy(r => r.Type).ThenBy(r => r.Name).Take(25).ToList();
                }, token);

                if (token.IsCancellationRequested)
                {
                    Debug.WriteLine("[OmnisearchViewModel] Búsqueda cancelada después de Task.Run.");
                    IsLoadingSearchResults = false;
                    return;
                }

                // Actualizar la colección observable en el hilo de UI
                // SearchResults.Clear(); // Ya se hizo antes de IsLoadingSearchResults = true
                foreach (var item in newRawResults)
                {
                    SearchResults.Add(item);
                }

                // Iniciar la carga de imágenes para los resultados recién añadidos
                // sin esperar a que todas terminen para actualizar IsLoadingSearchResults
                if (SearchResults.Any())
                {
                    _ = Task.Run(async () =>
                    {
                        var imageLoadTasks = SearchResults.Select(item => item.LoadImageAsync()).ToList();
                        try
                        {
                            await Task.WhenAll(imageLoadTasks);
                        }
                        catch (Exception imgEx)
                        {
                            Debug.WriteLine($"[OmnisearchViewModel] Error durante carga de imágenes en lote: {imgEx.Message}");
                        }
                    }, token); // Pasar el token también a esta tarea anidada si es necesario
                }
            }
            catch (TaskCanceledException) { Debug.WriteLine("[OmnisearchViewModel] Búsqueda principal (Task.Run) cancelada."); }
            catch (Exception ex) { Debug.WriteLine($"[OmnisearchViewModel] Error durante la búsqueda: {ex.Message}"); }
            finally
            {
                // IsLoadingSearchResults se establece a false solo cuando la parte síncrona de UI ha terminado.
                // Las imágenes se cargan en segundo plano.
                IsLoadingSearchResults = false;
            }
        }

        [RelayCommand]
        private void SelectResult(SearchResultItem? selectedItem)
        {
            if (selectedItem is null || _navigationService is null) return;
            CloseOmnisearchDialog();
            if (selectedItem.Type == SearchResultType.Champion)
            {
                _navigationService.NavigateTo<ChampionDetailViewModel>(selectedItem.ChampionId);
            }
            else if (selectedItem.Type == SearchResultType.Skin && selectedItem.OriginalSkinObject is not null)
            {
                _navigationService.ShowDialog<SkinDetailViewModel>(selectedItem.OriginalSkinObject);
            }
        }

        [RelayCommand]
        private void CloseOmnisearchDialog()
        {
            IsFilterPopupOpen = false;
            if (_serviceProvider is null) return;
            var mainViewModel = _serviceProvider.GetService<MainViewModel>();
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