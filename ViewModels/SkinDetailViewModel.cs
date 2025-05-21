using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkinHunterWPF.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.DependencyInjection;
using System.Windows; // Se mantiene para MessageBoxButton, MessageBoxImage
using System.Diagnostics;

namespace SkinHunterWPF.ViewModels
{
    public partial class SkinDetailViewModel : BaseViewModel
    {
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private Skin? _selectedSkin;

        [ObservableProperty]
        private ObservableCollection<Chroma> _availableChromas = new();

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
            Debug.WriteLine("[SkinDetailViewModel] Constructor called.");
        }

        public void LoadSkin(Skin skin)
        {
            Debug.WriteLine($"[SkinDetailViewModel] LoadSkin called for Skin ID: {skin.Id} ('{skin.Name}')");
            SelectedSkin = skin;
            AvailableChromas.Clear();
            Debug.WriteLine($"[SkinDetailViewModel] AvailableChromas cleared.");

            if (skin.Chromas != null && skin.Chromas.Any())
            {
                Debug.WriteLine($"[SkinDetailViewModel] Skin has {skin.Chromas.Count} chromas to process.");
                foreach (var chroma in skin.Chromas)
                {
                    if (chroma != null)
                    {
                        chroma.IsSelected = false;
                        AvailableChromas.Add(chroma);
                        Debug.WriteLine($"[SkinDetailViewModel] Added Chroma ID: {chroma.Id}, Name: '{chroma.Name}' to AvailableChromas.");
                    }
                }
                Debug.WriteLine($"[SkinDetailViewModel] Finished processing chromas. Final AvailableChromas count: {AvailableChromas.Count}");
            }
            else
            {
                Debug.WriteLine($"[SkinDetailViewModel] Skin ID: {skin.Id} has no chromas.");
            }

            SelectedChroma = null;
            DownloadSkinCommand.NotifyCanExecuteChanged();
            RefreshChromaSelections(null);
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
            Debug.WriteLine($"[SkinDetailViewModel] DownloadSkinAsync: Downloading '{skinOrChromaName}' (ID: {idToDownload})");

            await Task.Delay(1500);

            UserCredits--;
            DownloadSkinCommand.NotifyCanExecuteChanged();

            IsLoading = false;
            System.Windows.MessageBox.Show($"'{skinOrChromaName}' (ID: {idToDownload}) download initiated!", "Download", MessageBoxButton.OK, MessageBoxImage.Information); // Calificado aquí

            CloseDialog();
        }


        [RelayCommand]
        private void CloseDialog()
        {
            Debug.WriteLine("[SkinDetailViewModel] CloseDialog called.");
            var mainViewModel = _serviceProvider.GetService<MainViewModel>();
            mainViewModel?.CloseDialogCommand.Execute(null);
        }

        private void SetDefaultSelection()
        {
            if (SelectedChroma != null)
            {
                SelectedChroma.IsSelected = false;
            }
            SelectedChroma = null;
            RefreshChromaSelections(null);
        }

        [RelayCommand]
        private void ToggleChromaSelection(Chroma? clickedChroma)
        {
            if (clickedChroma == null) return;

            Debug.WriteLine($"[SkinDetailViewModel] ToggleChromaSelection called for Chroma ID: {clickedChroma.Id}");

            if (SelectedChroma == clickedChroma)
            {
                Debug.WriteLine($"[SkinDetailViewModel] Deselecting Chroma ID: {clickedChroma.Id}");
                SetDefaultSelection();
            }
            else
            {
                Debug.WriteLine($"[SkinDetailViewModel] Selecting Chroma ID: {clickedChroma.Id}");
                if (SelectedChroma != null)
                {
                    SelectedChroma.IsSelected = false;
                }
                SelectedChroma = clickedChroma;
                SelectedChroma.IsSelected = true;
                RefreshChromaSelections(SelectedChroma);
            }
        }

        private void RefreshChromaSelections(Chroma? selected)
        {
            foreach (var ch in AvailableChromas)
            {
                ch.IsSelected = (ch == selected);
            }
            var tempList = AvailableChromas.ToList();
            AvailableChromas.Clear();
            foreach (var item in tempList)
            {
                AvailableChromas.Add(item);
            }
        }
    }
}