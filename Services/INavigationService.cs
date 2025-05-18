namespace SkinHunterWPF.Services
{
    public interface INavigationService
    {
        void NavigateTo<TViewModel>() where TViewModel : ViewModels.BaseViewModel;
        void NavigateTo<TViewModel>(object parameter) where TViewModel : ViewModels.BaseViewModel;
        void ShowDialog<TViewModel>(object parameter) where TViewModel : ViewModels.BaseViewModel;
        void GoBack();
    }
}