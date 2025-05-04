using CommunityToolkit.Mvvm.ComponentModel;

namespace SkinHunterWPF.ViewModels
{
    public abstract partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotLoading))]
        private bool _isLoading;

        public bool IsNotLoading => !IsLoading;
    }
}