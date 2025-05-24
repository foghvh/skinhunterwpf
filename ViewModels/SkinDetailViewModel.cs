using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkinHunterWPF.Models;
using SkinHunterWPF.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Diagnostics;

namespace SkinHunterWPF.ViewModels
{
    public partial class SkinDetailViewModel : BaseViewModel
    {
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private Skin? _selectedSkin;

        public ObservableCollection<Chroma> AvailableChromas { get; } = [];

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsDefaultSelected))]
        [NotifyPropertyChangedFor(nameof(KhadaViewerUrl))]
        private Chroma? _selectedChroma;

        [ObservableProperty]
        private int _userCredits = 5;

        public bool IsDefaultSelected => SelectedChroma == null;

        public string? KhadaViewerUrl
        {
            get
            {
                if (SelectedSkin == null) return null;
                int skinId = SelectedSkin.Id;
                int? chromaId = SelectedChroma?.Id;
                string url = $"https://modelviewer.lol/model-viewer?id={skinId}";
                if (chromaId.HasValue && chromaId.Value != 0 && chromaId.Value / 1000 == skinId)
                {
                    url += $"&chroma={chromaId.Value}";
                }
                return url;
            }
        }

        public SkinDetailViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task LoadSkinAsync(Skin skin)
        {
            IsLoading = true;
            Debug.WriteLine($"[SkinDetailViewModel] LoadSkinAsync para Skin ID: {skin.Id} ('{skin.Name}')");

            await CdragonDataService.EnrichSkinWithSupabaseChromaDataAsync(skin);
            SelectedSkin = skin;

            AvailableChromas.Clear();
            Debug.WriteLine($"[SkinDetailViewModel] AvailableChromas borrados. Procesando {SelectedSkin.Chromas?.Count ?? 0} chromas.");

            if (SelectedSkin.Chromas != null && SelectedSkin.Chromas.Any())
            {
                foreach (var chroma in SelectedSkin.Chromas)
                {
                    if (chroma != null)
                    {
                        chroma.IsSelected = false;
                        AvailableChromas.Add(chroma);
                        Debug.WriteLine($"[SkinDetailViewModel] Added Chroma ID: {chroma.Id}, Name: '{chroma.Name}' to AvailableChromas.");
                    }
                }
                Debug.WriteLine($"[SkinDetailViewModel] Procesamiento de chromas finalizado. Count: {AvailableChromas.Count}");
            }
            else
            {
                Debug.WriteLine($"[SkinDetailViewModel] Skin ID: {SelectedSkin.Id} no tiene chromas después del enriquecimiento.");
            }

            SelectedChroma = null;
            DownloadSkinCommand.NotifyCanExecuteChanged();
            IsLoading = false;
        }

        public bool CanDownload()
        {
            return UserCredits > 0;
        }

        [RelayCommand(CanExecute = nameof(CanDownload))]
        private async Task DownloadSkinAsync()
        {
            IsLoading = true;
            var skinOrChromaName = IsDefaultSelected ? SelectedSkin?.Name : SelectedChroma?.Name;
            var idToDownload = IsDefaultSelected ? SelectedSkin?.Id : SelectedChroma?.Id;

            await Task.Delay(1500);

            UserCredits--;
            DownloadSkinCommand.NotifyCanExecuteChanged();

            IsLoading = false;
            System.Windows.MessageBox.Show($"'{skinOrChromaName}' (ID: {idToDownload}) download initiated!", "Download", MessageBoxButton.OK, MessageBoxImage.Information);

            CloseDialog();
        }

        [RelayCommand]
        private void CloseDialog()
        {
            var mainViewModel = _serviceProvider.GetService<MainViewModel>();
            mainViewModel?.CloseDialogCommand.Execute(null);
        }

        private void SetDefaultSelection()
        {
            SelectedChroma = null;
            RefreshChromaSelections(null);
        }

        [RelayCommand]
        private void ToggleChromaSelection(Chroma? clickedChroma)
        {
            if (clickedChroma == null) return;

            if (SelectedChroma == clickedChroma)
            {
                SetDefaultSelection();
            }
            else
            {
                SelectedChroma = clickedChroma;
                RefreshChromaSelections(SelectedChroma);
            }
        }

        private void RefreshChromaSelections(Chroma? selected)
        {
            foreach (var ch in AvailableChromas)
            {
                ch.IsSelected = (ch == selected);
            }
        }
    }
}