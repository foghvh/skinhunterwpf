using SkinHunterWPF.ViewModels;
using SkinHunterWPF.Models;
using System;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace SkinHunterWPF.Services
{
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private BaseViewModel? _previousViewModel;
        private MainViewModel? _mainViewModelCache;

        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        private MainViewModel MainVM => _mainViewModelCache ??= _serviceProvider.GetRequiredService<MainViewModel>();

        private BaseViewModel GetViewModel<TViewModel>() where TViewModel : BaseViewModel
        {
            return _serviceProvider.GetRequiredService<TViewModel>();
        }

        private void SetCurrentViewModel(BaseViewModel viewModel)
        {
            _previousViewModel = MainVM.CurrentViewModel;
            MainVM.CurrentViewModel = viewModel;
        }

        public void NavigateTo<TViewModel>() where TViewModel : BaseViewModel
        {
            var viewModel = GetViewModel<TViewModel>();
            SetCurrentViewModel(viewModel);
            if (viewModel is ChampionGridViewModel cgvm && !cgvm.ChampionsView.Cast<object>().Any())
            {
                _ = cgvm.LoadChampionsCommand.ExecuteAsync(null);
            }
        }

        public void NavigateTo<TViewModel>(object parameter) where TViewModel : BaseViewModel
        {
            var viewModel = GetViewModel<TViewModel>();

            if (viewModel is ChampionDetailViewModel cdvm && parameter is int champId)
            {
                _ = cdvm.LoadChampionCommand.ExecuteAsync(champId);
            }

            SetCurrentViewModel(viewModel);
        }

        public void ShowDialog<TViewModel>(object parameter) where TViewModel : BaseViewModel
        {
            if (typeof(TViewModel) == typeof(SkinDetailViewModel) && parameter is Skin skinToShow)
            {
                var viewModel = _serviceProvider.GetRequiredService<SkinDetailViewModel>();
                viewModel.LoadSkin(skinToShow);
                MainVM.DialogViewModel = viewModel;
            }
            else
            {
                Console.WriteLine($"Cannot show dialog for VM {typeof(TViewModel)} with parameter {parameter?.GetType()}");
                MainVM.DialogViewModel = null;
            }
        }

        public void GoBack()
        {
            if (MainVM.DialogViewModel != null)
            {
                MainVM.DialogViewModel = null;
                return;
            }

            if (_previousViewModel != null)
            {
                var currentType = MainVM.CurrentViewModel?.GetType();
                var previousType = _previousViewModel.GetType();

                if (currentType != previousType)
                {
                    MainVM.CurrentViewModel = _previousViewModel;
                    _previousViewModel = _serviceProvider.GetService<ChampionGridViewModel>();
                }
                else
                {
                    NavigateTo<ChampionGridViewModel>();
                }

            }
            else
            {
                NavigateTo<ChampionGridViewModel>();
            }
        }
    }
}