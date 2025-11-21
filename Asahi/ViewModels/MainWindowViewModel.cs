using Asahi.Base;
using Asahi.DataServices;
using Asahi.Navigation.Stores;
using Asahi.Stores;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
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
        public MainWindowViewModel(NavigationStore navigationStore, HomeViewModel homeViewModel) 
        { 
            _navigationStore = navigationStore;
            _homeViewModel = homeViewModel;
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
        public void Dispose()
        {
            //_modalStore.PropertyChanged -= ModalStore_PropertyChanged;
        }
    }
}
