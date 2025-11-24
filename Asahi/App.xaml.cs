using Asahi.AppCycleManager;
using Asahi.DataServices;
using Asahi.Helpers;
using Asahi.ImageSources;
using Asahi.Models;
using Asahi.Navigation.Stores;
using Asahi.Stores;
using Asahi.ViewModels;
using Asahi.Views;
using Asahi.Vision.Handlers.Core;
using HalconDotNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Configuration;
using System.Data;
using System.Windows;

namespace Asahi
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IHost _host;
        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((_, services) =>
                {
                    services.AddSingleton<MainWindow>(provider => new MainWindow
                    {
                        DataContext = provider.GetRequiredService<MainWindowViewModel>()
                    })
                    .AddSingleton<MainWindowViewModel>()
                    .AddSingleton<NavigationStore>()
                    .AddSingleton<HomeViewModel>()
                    .AddSingleton<MultiCameraImageStore>()
                    .AddSingleton<AppConfigModel>()
                    .AddSingleton<ImageLogger>()
                    .AddSingleton<ImageLoggingService>()
                    .AddSingleton<TriggerSessionManager>()
                    .AddSingleton<ImageAcquisitionModel>()
                    .AddSingleton<CameraFrameGrabber>()
                    .AddSingleton<ImageLoggingService>()
                    .AddSingleton<ImageLoggingService>()
                    .AddSingleton<RecipeManagerViewModel>()
                    .AddSingleton<NavigationBarViewModel>(provider =>
                    {
                        var navigationStore = provider.GetRequiredService<NavigationStore>();
                        var modalStore = provider.GetRequiredService<ModalStore>();
                       
                        return new NavigationBarViewModel(
                            navigationStore,
                            () => provider.GetRequiredService<HomeViewModel>(),
                            () => provider.GetRequiredService<RecipeManagerViewModel>(),
                            modalStore);
                    })
                    .AddSingleton<JSONDataService>()
                    .AddSingleton<DefaultRecipeValuesModel>()
                    .AddSingleton<RecipeParameterStore>()
                    .AddSingleton<RecipeStore>()
                    .AddSingleton<ModalStore>()
                    .AddSingleton<InspectionContext>()
                    .AddSingleton<ImageAcquisitionModel>()
                    .AddSingleton<ImageAcquisitionViewModel>();
                }).Build();
        }
        protected override async void OnStartup(StartupEventArgs e)
        {
            _ = _host.Services.GetRequiredService<ImageAcquisitionViewModel>();

            var recipeManagerViewModel = _host.Services.GetRequiredService<RecipeManagerViewModel>();
            await recipeManagerViewModel.InitializeAsync();

            var navigationStore = _host.Services.GetRequiredService<NavigationStore>();
            var homeViewModel = _host.Services.GetRequiredService<HomeViewModel>();
            navigationStore.CurrentViewModel = homeViewModel;

            var mainWindow =  _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }
    }
}
