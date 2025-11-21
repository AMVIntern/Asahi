using Asahi.Base;
using Asahi.Helpers;
using Asahi.Navigation.Stores;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Asahi.Stores;
using Asahi.ViewModels;

namespace Asahi.ViewModels
{
    public partial class NavigationBarViewModel : ViewModelBase, IDisposable
    {
        private readonly NavigationStore _navigationStore;
        private readonly Func<HomeViewModel> _getHomeViewModel;
        private readonly Func<RecipeManagerViewModel> _getRecipeManagerViewModel;
        private readonly ModalStore _modalStore;
        //private readonly UserStore _userStore;
       // private readonly UserManagerViewModel _userManagerViewModel;
        //public string LoginButtonLabel => _userStore.IsLoggedIn ? "Logout" : "Login";
        [ObservableProperty]
        private bool _isCollapsed = true;

        public Visibility ExpandImageVisbility => IsCollapsed ? Visibility.Visible : Visibility.Collapsed;
        public Visibility CollapseImageVisibility => IsCollapsed ? Visibility.Collapsed : Visibility.Visible;

        [RelayCommand]
        public void ToggleNavigationBar()
        {
            IsCollapsed = !IsCollapsed;
        }
        partial void OnIsCollapsedChanged(bool value)
        {
            OnPropertyChanged(nameof(ExpandImageVisbility));
            OnPropertyChanged(nameof(CollapseImageVisibility));
        }

        [RelayCommand]
        public void HomeButton()
        {
            Debug.WriteLine("Home Button Clicked!");
            _navigationStore.CurrentViewModel = _getHomeViewModel();
        }

        [RelayCommand]
        public void RecipeManagerButton()
        {
            Debug.WriteLine("Recipe Manager Button Clicked!");
            _navigationStore.CurrentViewModel = _getRecipeManagerViewModel();
        }

        [RelayCommand]
        public async Task ExitApplicationButton()
        {
            bool confirm = await _modalStore.ShowConfirmationAsync("Exit Application!", "Are you sure you want to exit?");
            if (confirm == true)
            {
                Application.Current.Shutdown();
            }
            else
            {
                return;
            }
        }

        public NavigationBarViewModel(NavigationStore navigationStore, Func<HomeViewModel> getHomeViewModel,
                                      Func<RecipeManagerViewModel> getRecipeManagerViewModel, ModalStore modalStore
                                     // UserStore userStore, UserManagerViewModel userManagerViewModel
            )
        {
            AppLogger.Info("Navigation Bar Initializing...");
            _navigationStore = navigationStore;
            _getHomeViewModel = getHomeViewModel;
            _getRecipeManagerViewModel = getRecipeManagerViewModel;
            _modalStore = modalStore;
            //_userStore = userStore;
            //_userManagerViewModel = userManagerViewModel;
            _navigationStore.CurrentViewModelChanged += NavigationStore_CurrentViewModelChanged;
           // _userStore.PropertyChanged += UserStore_PropertyChanged;
            AppLogger.Info("Navigation Bar Initialized!");
        }
        //private void UserStore_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        //{
        //    if (e.PropertyName == nameof(UserStore.CurrentUser))
        //    {
        //        OnPropertyChanged(nameof(LoginButtonLabel));
        //    }
        //}
        private void NavigationStore_CurrentViewModelChanged()
        {
            IsCollapsed = true;
        }
        //[RelayCommand]
        //public void LoginOrLogout()
        //{
        //    if (_userStore.IsLoggedIn)
        //    {
        //        var currentUser = _userStore.CurrentUser.Username;
        //        _modalStore.ShowModal(new MessageModalViewModel("User Logged Out!", $"User {currentUser} logged out.", _modalStore));
        //        AppLogger.Info($"{currentUser} logged out.");
        //        _userStore.Logout();
        //        _userManagerViewModel.Refresh();
        //        OnPropertyChanged(nameof(LoginButtonLabel));
        //    }
        //    else
        //    {
        //        _modalStore.ShowModal(new LoginModalViewModel(_userStore, _modalStore));
        //        _userManagerViewModel.Refresh();
        //    }
        //}

        public void Dispose()
        {
            _navigationStore.CurrentViewModelChanged -= NavigationStore_CurrentViewModelChanged;
        }
    }
}
