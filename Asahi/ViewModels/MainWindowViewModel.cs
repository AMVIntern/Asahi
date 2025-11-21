using Asahi.Base;
using Asahi.DataServices;
using Asahi.Models;
using Asahi.Navigation.Stores;
using Asahi.Stores;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asahi.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase, IDisposable
    {
        private readonly NavigationStore? _navigationStore;
        public ViewModelBase? CurrentViewModel => _navigationStore?.CurrentViewModel;
        private readonly HomeViewModel _homeViewModel;
        public NavigationBarViewModel NavigationBarViewModel { get; }
        private readonly RecipeManagerViewModel _recipeManagerViewModel;
        private readonly ModalStore _modalStore;
        public ViewModelBase? ModalViewModel => _modalStore.ModalViewModel;
        public bool IsModalOpen => _modalStore.IsModalOpen;

        public MainWindowViewModel(NavigationStore navigationStore, HomeViewModel homeViewModel, NavigationBarViewModel navigationBarViewModel,
            RecipeManagerViewModel recipeManagerViewModel,ModalStore modalStore) 
        { 
            _navigationStore = navigationStore;
            _homeViewModel = homeViewModel;
            _modalStore = modalStore;

            _navigationStore.CurrentViewModelChanged += NavigationStore_CurrentViewModelChanged;
            _modalStore.PropertyChanged += ModalStore_PropertyChanged;
            _recipeManagerViewModel = recipeManagerViewModel;
            _navigationStore.RetainViewModel(_recipeManagerViewModel);
            _navigationStore.RetainViewModel(_homeViewModel);
            NavigationBarViewModel = navigationBarViewModel;
            _recipeManagerViewModel.NavigationBarViewModel = NavigationBarViewModel;
        }
        [RelayCommand]
        private async Task TriggerInspection()
        {
            await _homeViewModel.TriggerInspectionAsync();
        }
        private void NavigationStore_CurrentViewModelChanged()
        {
            OnPropertyChanged(nameof(CurrentViewModel));
        }
        private void ModalStore_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ModalStore.ModalViewModel) || e.PropertyName == nameof(ModalStore.IsModalOpen))
            {
                OnPropertyChanged(nameof(ModalViewModel));
                OnPropertyChanged(nameof(IsModalOpen));
            }
        }
        public void Dispose()
        {
            _modalStore.PropertyChanged -= ModalStore_PropertyChanged;
        }
    }
}
