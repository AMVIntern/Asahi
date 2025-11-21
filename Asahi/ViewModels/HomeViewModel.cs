using Asahi.AppCycleManager;
using Asahi.Base;
using Asahi.DataServices;
using Asahi.Enums;
using Asahi.Helpers;
using Asahi.ImageSources;
using Asahi.Interfaces;
using Asahi.Models;
using Asahi.Navigation.Stores;
using Asahi.Stores;
using System.IO;

namespace Asahi.ViewModels
{
    public partial class HomeViewModel : ViewModelBase, IDisposable
    {
        private readonly List<IImageSource> _imageSources = new();
        private readonly HashSet<string> _activeCameraIds = new();
        private readonly MultiCameraImageStore _imageStore;
        private readonly NavigationStore _navigationStore;
        private readonly ImageLogger _imageLogger;
        public CameraViewModel Cam1 { get; }
        private readonly ImageAcquisitionModel _imageAcquisitionModel;
        private readonly CameraFrameGrabber _cameraFrameGrabber;
        private readonly TriggerSessionManager _triggerSessionManager;
        private readonly CancellationTokenSource _cts = new();
        public HomeViewModel(NavigationStore navigationStore, MultiCameraImageStore imageStore,ImageLogger imageLogger, 
            ImageAcquisitionModel imageAcquisitionModel, CameraFrameGrabber cameraFrameGrabber, TriggerSessionManager triggerSessionManager)
        {
            _navigationStore = navigationStore;
            _imageStore = imageStore;
            _imageLogger = imageLogger;
            _imageAcquisitionModel = imageAcquisitionModel;
            _cameraFrameGrabber = cameraFrameGrabber;
            _triggerSessionManager = triggerSessionManager;

            imageStore.RegisterCamera("Cam1", "Cam1");
            Cam1 = new CameraViewModel("Cam1", _imageStore, _navigationStore, this, _triggerSessionManager);
            if (AppEnvironment.IsOfflineMode)
            {
                _imageSources.Add(new FolderImageLoader(Path.Combine(PathConfig.LocalImagePath, "Cam1"), _imageStore, "Cam1", _imageLogger));
            }
            else
            {
                TryRegisterCamera("Cam1", -90.0, Cam1);
            }
        }
        private void TryRegisterCamera(string cameraId, double rotation, CameraViewModel viewModel)
        {
            _imageAcquisitionModel.RotateAngles[cameraId] = rotation;
            viewModel.Status = CameraStatus.Disconnected;

            var connected = TryRegisterCameraOnce(cameraId, viewModel);

            if (!connected)
                StartCameraReconnectLoop(cameraId, viewModel);
        }
        private bool TryRegisterCameraOnce(string cameraId, CameraViewModel viewModel)
        {
            try
            {
                if (_activeCameraIds.Contains(cameraId))
                    return false;

                var handle = _cameraFrameGrabber.StartFrameGrabber(cameraId, "StartCameraFrameGrabber");

                if (handle != null && handle.Length > 0)
                {
                    _imageAcquisitionModel.AcqHandles[cameraId] = handle;
                    viewModel.Status = CameraStatus.Connected;

                    var loader = new CameraImageLoader(
                        _cameraFrameGrabber,
                        _imageAcquisitionModel,
                        _imageStore,
                        cameraId,
                        _imageLogger,
                        status => viewModel.Status = status
                    );
                    _imageSources.Add(loader);
                    _activeCameraIds.Add(cameraId);
                    return true;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Initial camera connect failed for {cameraId}: {ex.Message}");
            }

            return false;
        }
        private void StartCameraReconnectLoop(string cameraId, CameraViewModel viewModel)
        {
            Task.Run(async () =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    AppLogger.Info($"Retrying camera {cameraId}...");

                    var success = TryRegisterCameraOnce(cameraId, viewModel);
                    if (success) break;

                    await Task.Delay(2000);
                }
            });
        }
        public CameraViewModel? GetCameraViewModel(string cameraId)
        {
            return cameraId switch
            {
                "Cam1" => Cam1,
                _ => null
            };
        }
        public async Task TriggerInspectionAsync()
        {
            try
            {
                AppLogger.Info("[Trigger] Manual inspection triggered");
                AppLogger.Info("[Loading] Set IsLoading = true (trigger pressed)");

                _imageStore.ClearImages();

                if (AppEnvironment.IsOfflineMode)
                {
                    foreach (var source in _imageSources)
                    {
                        if (source is FolderImageLoader folderLoader)
                        {
                            try
                            {
                                await folderLoader.GrabNextFrameAsync();
                            }
                            catch (Exception ex)
                            {
                                AppLogger.Error($"[Trigger] Failed to grab frame from FolderImageLoader ({folderLoader.CameraId}): {ex.Message}", ex);
                            }
                        }
                    }
                }
                else
                {
                    foreach (var source in _imageSources)
                    {
                        if (source is CameraImageLoader cameraLoader)
                        {
                            try
                            {
                                await cameraLoader.GrabNextFrameAsync();
                            }
                            catch (Exception ex)
                            {
                                AppLogger.Error($"[Trigger] Failed to grab frame from CameraImageLoader ({cameraLoader.CameraId}): {ex.Message}", ex);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"[Trigger] Failed to trigger inspection: {ex.Message}", ex);
            }
        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
