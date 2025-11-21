using Asahi.AppCycleManager;
using Asahi.DataServices;
using Asahi.ImageSources;
using Asahi.Models;
using Asahi.Navigation.Stores;
using Asahi.Stores;
using Asahi.ViewModels;
using Asahi.Views;
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
                    .AddSingleton<ImageLogger>()
                    .AddSingleton<ImageLoggingService>();
                }).Build();
        }
        protected override async void OnStartup(StartupEventArgs e)
        {
            var navigationStore = _host.Services.GetRequiredService<NavigationStore>();
            var homeViewModel = _host.Services.GetRequiredService<HomeViewModel>();
            navigationStore.CurrentViewModel = homeViewModel;

            var mainWindow =  _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }
    }
}
