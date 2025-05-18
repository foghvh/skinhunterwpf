using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkinHunterWPF.Models;
using SkinHunterWPF.Services;
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

        partial void OnSearchTextChanged(string? value)
        {
            Application.Current?.Dispatcher.InvokeAsync(() => ChampionsView.Refresh(), DispatcherPriority.Background);
        }

        partial void OnSelectedRoleChanged(string? value)
        {
            Debug.WriteLine($"[ChampionGridViewModel] Selected Role Changed: {value}");
            Application.Current?.Dispatcher.InvokeAsync(() => ChampionsView.Refresh(), DispatcherPriority.Background);
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

            AllRoles.Clear();
            AllRoles.Add("All");
            foreach (var role in sortedRoles)
            {
                string displayRole = role.Length > 0 ? char.ToUpper(role[0]) + role.Substring(1) : role;
                AllRoles.Add(displayRole);
            }

            if (!string.IsNullOrEmpty(currentSelection) && AllRoles.Contains(currentSelection))
            {
                if (SelectedRole != currentSelection) SelectedRole = currentSelection;
            }
            else
            {
                if (SelectedRole != "All") SelectedRole = "All";
            }
        }

        [RelayCommand]
        public async Task LoadChampionsAsync()
        {
            if (IsLoading) return;

            if (_allChampions.Any() && AllRoles.Count > 1)
            {
                Debug.WriteLine("[ChampionGridViewModel] Champions and Roles likely loaded. Forcing UI Refresh.");
                IsLoading = true;
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    PopulateRoles();
                    ChampionsView.Refresh();
                }, DispatcherPriority.DataBind);
                IsLoading = false;
                return;
            }

            IsLoading = true;
            Debug.WriteLine("[ChampionGridViewModel] Initial Loading Champions...");

            foreach (var champ in _allChampions)
            {
                champ.CancelCurrentLoad();
            }
            _allChampions.Clear();

            AllRoles.Clear();
            AllRoles.Add("All");
            SelectedRole = "All";

            var champs = await CdragonDataService.GetChampionSummariesAsync();

            if (champs != null)
            {
                foreach (var champ in champs.OrderBy(c => c.Name))
                {
                    if (champ.Roles == null) champ.Roles = new List<string>();
                    _allChampions.Add(champ);
                }
                Debug.WriteLine($"[ChampionGridViewModel] Added {_allChampions.Count} champions to collection.");

                await Application.Current.Dispatcher.InvokeAsync(() => {
                    PopulateRoles();
                    ChampionsView.Refresh();
                    Debug.WriteLine("[ChampionGridViewModel] Roles populated and ChampionsView refreshed.");
                }, DispatcherPriority.DataBind);
            }
            else
            {
                Debug.WriteLine("[ChampionGridViewModel] Failed to load champions from service.");
                Application.Current?.Dispatcher.Invoke(() => {
                    MessageBox.Show("Failed to load champions.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
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