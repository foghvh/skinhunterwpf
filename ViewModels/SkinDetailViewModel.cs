using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SkinHunterWPF.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.DependencyInjection;
using System.Windows; // Required for standard MessageBox

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
        private Chroma? _selectedChroma;

        [ObservableProperty]
        private int _userCredits = 5;

        public bool IsDefaultSelected => SelectedChroma == null;

        public SkinDetailViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void LoadSkin(Skin skin)
        {
            SelectedSkin = skin;
            AvailableChromas.Clear();
            if (skin.Chromas != null && skin.Chromas.Any())
            {
                foreach (var chroma in skin.Chromas)
                {
                    AvailableChromas.Add(chroma);
                }
            }
            SelectedChroma = null;
            OnPropertyChanged(nameof(IsDefaultSelected));
            DownloadSkinCommand.NotifyCanExecuteChanged();
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
            // Use standard MessageBox
            MessageBox.Show($"'{skinOrChromaName}' (ID: {idToDownload}) download initiated!", "Download", MessageBoxButton.OK, MessageBoxImage.Information);

            CloseDialog();
        }

        private bool CanDownload()
        {
            return UserCredits > 0;
        }

        [RelayCommand]
        private void CloseDialog()
        {
            var mainViewModel = _serviceProvider.GetService<MainViewModel>();
            if (mainViewModel?.CloseDialogCommand.CanExecute(null) ?? false)
            {
                mainViewModel.CloseDialogCommand.Execute(null);
            }
        }

        [RelayCommand]
        private void SelectDefault()
        {
            SelectedChroma = null;
        }

        [RelayCommand]
        private void SelectChroma(Chroma? chroma)
        {
            SelectedChroma = chroma;
        }
    }
}