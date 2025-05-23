using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkinHunterWPF.Models;
using SkinHunterWPF.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System.Windows;
using System;
using System.Diagnostics;

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

        public void ReleaseResourcesForTray()
        {
            Debug.WriteLine($"[ChampionDetailViewModel] Liberando recursos para la bandeja (Campeón: {Champion?.Name}).");
            IsLoading = true; // Indicar que estamos procesando
            Champion?.ReleaseImage(); // Si ChampionSummary tiene el método
            Champion = null;
            Skins.Clear();
            IsLoading = false;
            Debug.WriteLine("[ChampionDetailViewModel] Recursos liberados.");
        }

        public void PrepareForReload()
        {
            Debug.WriteLine($"[ChampionDetailViewModel] Preparando para recarga (Campeón anterior: {Champion?.Name}).");
            IsLoading = true;
            Champion?.ReleaseImage();
            Champion = null;
            Skins.Clear();
            // No ponemos IsLoading = false aquí, porque LoadChampionAsync lo hará.
        }

        [RelayCommand]
        public async Task LoadChampionAsync(int championId)
        {
            // Evitar recarga si ya está cargado y es el mismo campeón, a menos que las skins estén vacías (podría ser después de ReleaseResources)
            if (Champion?.Id == championId && Skins.Any())
            {
                Debug.WriteLine($"[ChampionDetailViewModel] Campeón {championId} ya cargado con {Skins.Count} skins. Omitiendo recarga.");
                IsLoading = false;
                return;
            }

            Debug.WriteLine($"[ChampionDetailViewModel] LoadChampionAsync para ID: {championId}");
            IsLoading = true;
            // Si es un campeón diferente o las skins están vacías, limpiar.
            if (Champion?.Id != championId || !Skins.Any())
            {
                Skins.Clear();
                Champion?.ReleaseImage(); // Liberar imagen del campeón anterior si existe
                Champion = null;
            }

            var details = await CdragonDataService.GetChampionDetailsAsync(championId);

            if (details != null)
            {
                Champion = details; // Esto cargará la imagen del nuevo campeón

                // Skins.Clear(); // Ya se hizo arriba si era necesario
                if (details.Skins != null && details.Skins.Any())
                {
                    foreach (var skin in details.Skins.Where(s =>
                                !s.Name.Equals(details.Name, StringComparison.OrdinalIgnoreCase) &&
                                !s.Name.Equals($"Base {details.Name}", StringComparison.OrdinalIgnoreCase) &&
                                !s.Name.Contains("Original", StringComparison.OrdinalIgnoreCase)))
                    {
                        Skins.Add(skin);
                    }
                    Debug.WriteLine($"[ChampionDetailViewModel] Cargadas {Skins.Count} skins para {Champion.Name}.");
                }
                else
                {
                    Debug.WriteLine($"[ChampionDetailViewModel] No se encontraron skins (o lista vacía) para {Champion?.Name}.");
                }
            }
            else
            {
                System.Windows.MessageBox.Show($"Failed to load details for Champion ID: {championId}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            IsLoading = false;
            Debug.WriteLine($"[ChampionDetailViewModel] LoadChampionAsync para ID: {championId} FINALIZADO. IsLoading={IsLoading}");
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