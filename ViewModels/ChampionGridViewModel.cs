using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkinHunterWPF.Models;
using SkinHunterWPF.Services; // Asegúrate que este using está presente
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.ComponentModel;
using System;
using System.Windows;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows.Media.Imaging;

namespace SkinHunterWPF.ViewModels
{
    public partial class ChampionGridViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly ObservableCollection<ChampionSummary> _allChampions = new();

        [ObservableProperty]
        private string? _searchText;

        [ObservableProperty]
        private ObservableCollection<string> _allRoles = new();

        [ObservableProperty]
        private string? _selectedRole = "All";

        public ICollectionView ChampionsView { get; }

        public ChampionGridViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            ChampionsView = CollectionViewSource.GetDefaultView(_allChampions);
            ChampionsView.Filter = FilterChampions;
            AllRoles.Add("All");
        }

        public void ReleaseResourcesForTray()
        {
            Debug.WriteLine("[ChampionGridViewModel] Liberando recursos para la bandeja...");
            IsLoading = true;
            if (_allChampions.Any())
            {
                var championsToRelease = _allChampions.ToList();
                System.Windows.Application.Current?.Dispatcher.Invoke(() => _allChampions.Clear());

                foreach (var champ in championsToRelease)
                {
                    champ.ReleaseImage();
                }

                System.Windows.Application.Current?.Dispatcher.Invoke(() => ChampionsView?.Refresh());
                Debug.WriteLine($"[ChampionGridViewModel] Colección _allChampions limpiada. Count: {_allChampions.Count}");
            }
            else
            {
                Debug.WriteLine("[ChampionGridViewModel] _allChampions ya estaba vacía.");
            }
            IsLoading = false;
            Debug.WriteLine("[ChampionGridViewModel] Recursos liberados.");
        }


        partial void OnSearchTextChanged(string? value)
        {
            System.Windows.Application.Current?.Dispatcher.InvokeAsync(() => ChampionsView.Refresh(), DispatcherPriority.Background);
        }

        partial void OnSelectedRoleChanged(string? value)
        {
            Debug.WriteLine($"[ChampionGridViewModel] Selected Role Changed: {value}");
            System.Windows.Application.Current?.Dispatcher.InvokeAsync(() => ChampionsView.Refresh(), DispatcherPriority.Background);
        }

        private bool FilterChampions(object item)
        {
            if (!(item is ChampionSummary champ)) return false;

            bool textMatch = string.IsNullOrWhiteSpace(SearchText) ||
                             champ.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase);

            bool roleMatch = string.IsNullOrEmpty(SelectedRole) ||
                             SelectedRole.Equals("All", StringComparison.OrdinalIgnoreCase) ||
                             (champ.Roles != null && champ.Roles.Any(r => r.Equals(SelectedRole, StringComparison.OrdinalIgnoreCase)));

            return textMatch && roleMatch;
        }

        private void PopulateRoles()
        {
            var uniqueRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (_allChampions.Any())
            {
                foreach (var champ in _allChampions)
                {
                    if (champ.Roles != null)
                    {
                        foreach (var role in champ.Roles)
                        {
                            if (!string.IsNullOrWhiteSpace(role))
                            {
                                uniqueRoles.Add(role);
                            }
                        }
                    }
                }
            }

            var sortedRoles = uniqueRoles.OrderBy(r => r).ToList();
            string? currentSelection = SelectedRole;

            System.Windows.Application.Current?.Dispatcher.Invoke(() => {
                string? actualCurrentSelection = SelectedRole;
                AllRoles.Clear();
                AllRoles.Add("All");
                foreach (var role in sortedRoles)
                {
                    string displayRole = role.Length > 0 ? char.ToUpper(role[0]) + role.Substring(1) : role;
                    AllRoles.Add(displayRole);
                }

                if (!string.IsNullOrEmpty(actualCurrentSelection) && AllRoles.Contains(actualCurrentSelection))
                {
                    if (SelectedRole != actualCurrentSelection) SelectedRole = actualCurrentSelection;
                }
                else
                {
                    if (SelectedRole != "All") SelectedRole = "All";
                }
            });
        }

        [RelayCommand]
        public async Task LoadChampionsAsync()
        {
            if (IsLoading && _allChampions.Any())
            {
                Debug.WriteLine("[ChampionGridViewModel] LoadChampionsAsync llamado pero ya está cargando con datos, retornando.");
                return;
            }

            IsLoading = true;
            Debug.WriteLine("[ChampionGridViewModel] LoadChampionsAsync INICIADO.");

            if (_allChampions.Any()) // Limpiar si ya hay datos, para asegurar que una recarga sea fresca
            {
                var championsToRelease = _allChampions.ToList();
                System.Windows.Application.Current?.Dispatcher.Invoke(() => _allChampions.Clear());
                foreach (var champ in championsToRelease)
                {
                    champ.ReleaseImage();
                }
                Debug.WriteLine("[ChampionGridViewModel] _allChampions limpiada antes de cargar nuevos datos.");
            }


            var champs = await CdragonDataService.GetChampionSummariesAsync();
            if (champs != null)
            {
                foreach (var champ in champs.OrderBy(c => c.Name))
                {
                    if (champ.Roles == null) champ.Roles = new List<string>();
                    System.Windows.Application.Current?.Dispatcher.Invoke(() => _allChampions.Add(champ));
                }
                Debug.WriteLine($"[ChampionGridViewModel] Added {_allChampions.Count} champions to collection.");

                PopulateRoles();
                System.Windows.Application.Current?.Dispatcher.Invoke(() => ChampionsView.Refresh());
                Debug.WriteLine("[ChampionGridViewModel] Roles populated and ChampionsView refreshed.");
            }
            else
            {
                Debug.WriteLine("[ChampionGridViewModel] Failed to load champions from service.");
                System.Windows.Application.Current?.Dispatcher.Invoke(() => {
                    System.Windows.MessageBox.Show("Failed to load champions.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }

            IsLoading = false;
            Debug.WriteLine("[ChampionGridViewModel] LoadChampionsAsync FINALIZADO.");
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