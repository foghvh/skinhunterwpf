using SkinHunterWPF.ViewModels;
using SkinHunterWPF.Models;
using System;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks; // Necesario para Task

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
            if (viewModel is ChampionGridViewModel cgvm && (cgvm.ChampionsView == null || !cgvm.ChampionsView.Cast<object>().Any()))
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
                MainVM.DialogViewModel = viewModel;
                _ = viewModel.LoadSkinAsync(skinToShow);
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
                    // Determinar cuál era el _previousPreviousViewModel o ir a la vista por defecto.
                    // Por ahora, si el anterior no es ChampionGrid, y el actual es ChampionGrid, _previousViewModel se pone a null.
                    // Si el anterior era ChampionGrid, al volver a él, _previousViewModel se pone a null.
                    _previousViewModel = null; // O una lógica más compleja para múltiples niveles de "atrás"
                }
                else // Si estamos en la misma vista (ej. detalles y volvemos a detalles, aunque no debería pasar con esta lógica)
                {
                    NavigateTo<ChampionGridViewModel>(); // Ir a la vista por defecto
                }

            }
            else
            {
                NavigateTo<ChampionGridViewModel>(); // Vista por defecto si no hay historial simple
            }
        }
    }
}