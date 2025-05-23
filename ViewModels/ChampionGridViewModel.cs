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
            if (_allChampions.Any())
            {
                foreach (var champ in _allChampions.ToList()) // ToList para poder modificar la colección base
                {
                    champ.ReleaseImage();
                }
                _allChampions.Clear(); // Ahora seguro limpiar

                // No limpiar AllRoles ni SelectedRole para que los filtros persistan al restaurar,
                // a menos que la recarga de LoadChampionsAsync los repopule de todas formas.
                // Si LoadChampionsAsync siempre repopula roles, se podrían limpiar aquí.

                System.Windows.Application.Current?.Dispatcher.Invoke(() => ChampionsView?.Refresh());
                Debug.WriteLine($"[ChampionGridViewModel] Colección _allChampions limpiada. Count: {_allChampions.Count}");
            }
            else
            {
                Debug.WriteLine("[ChampionGridViewModel] _allChampions ya estaba vacía.");
            }
            IsLoading = false; // Asegurar que no se quede en estado de carga
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
            string? currentSelection = SelectedRole; // Guardar la selección actual

            // Usar Dispatcher para modificar AllRoles si es necesario (aunque generalmente se llama desde el hilo de UI)
            System.Windows.Application.Current?.Dispatcher.Invoke(() => {
                AllRoles.Clear();
                AllRoles.Add("All");
                foreach (var role in sortedRoles)
                {
                    string displayRole = role.Length > 0 ? char.ToUpper(role[0]) + role.Substring(1) : role;
                    AllRoles.Add(displayRole);
                }

                // Restaurar la selección si aún es válida, sino default a "All"
                if (!string.IsNullOrEmpty(currentSelection) && AllRoles.Contains(currentSelection))
                {
                    if (SelectedRole != currentSelection) SelectedRole = currentSelection; // Evitar re-trigger innecesario
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
            if (IsLoading && _allChampions.Any()) // Si ya está cargando y tiene datos, no hacer nada extra
            {
                Debug.WriteLine("[ChampionGridViewModel] LoadChampionsAsync llamado pero ya está cargando con datos, retornando.");
                return;
            }

            IsLoading = true; // Poner IsLoading a true al inicio de la carga
            Debug.WriteLine("[ChampionGridViewModel] LoadChampionsAsync INICIADO.");

            // Si _allChampions está vacía O si queremos forzar una recarga completa
            if (!_allChampions.Any())
            {
                Debug.WriteLine("[ChampionGridViewModel] Colección _allChampions está vacía, procediendo a cargar desde servicio.");
                var champs = await CdragonDataService.GetChampionSummariesAsync();
                if (champs != null)
                {
                    // Limpiar antes de añadir para evitar duplicados si esto se llama múltiples veces
                    System.Windows.Application.Current?.Dispatcher.Invoke(() => _allChampions.Clear());

                    foreach (var champ in champs.OrderBy(c => c.Name))
                    {
                        if (champ.Roles == null) champ.Roles = new List<string>();
                        System.Windows.Application.Current?.Dispatcher.Invoke(() => _allChampions.Add(champ));
                    }
                    Debug.WriteLine($"[ChampionGridViewModel] Added {_allChampions.Count} champions to collection.");
                }
                else
                {
                    Debug.WriteLine("[ChampionGridViewModel] Failed to load champions from service.");
                    System.Windows.Application.Current?.Dispatcher.Invoke(() => {
                        System.Windows.MessageBox.Show("Failed to load champions.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                    IsLoading = false; // Asegurar que IsLoading se ponga a false si hay error
                    return;
                }
            }
            else
            {
                Debug.WriteLine("[ChampionGridViewModel] _allChampions ya tiene datos, refrescando roles y vista.");
            }


            PopulateRoles(); // Esto debería estar en el hilo de UI o usar Dispatcher si se llama desde otro hilo
            System.Windows.Application.Current?.Dispatcher.Invoke(() => ChampionsView.Refresh());
            Debug.WriteLine("[ChampionGridViewModel] Roles populated and ChampionsView refreshed.");

            IsLoading = false; // Poner IsLoading a false al final
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