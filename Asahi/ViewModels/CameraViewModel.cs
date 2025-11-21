using Asahi.AppCycleManager;
using Asahi.Base;
using Asahi.Enums;
using Asahi.Helpers;
using Asahi.Navigation.Stores;
using Asahi.Stores;
using CommunityToolkit.Mvvm.ComponentModel;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Asahi.ViewModels
{
    public partial class CameraViewModel: ViewModelBase,IDisposable
    {
        [ObservableProperty]
        private string title;

        [ObservableProperty]
        private HImage? image;
        [ObservableProperty]
        private Brush borderBrush = Brushes.Gray;

        [ObservableProperty]
        private CameraStatus status = CameraStatus.Disconnected;
        public event Action? StatusChanged;
        private readonly string _cameraId;
        public string CameraId => _cameraId;

        private readonly HomeViewModel _homeViewModel;
        private readonly MultiCameraImageStore _imageStore;
        private readonly NavigationStore _navigationStore;
        public bool IsDisconnected => Status == CameraStatus.Disconnected;
        private readonly TriggerSessionManager _triggerSessionManager;
        public static readonly object TriggerRegistrationLock = new();
        public event Action? InspectionVisualsUpdated;

        public CameraViewModel(string cameraId,MultiCameraImageStore imageStore,NavigationStore navigationStore, HomeViewModel homeViewModel,
            TriggerSessionManager triggerSessionManager)
        {
            _cameraId = cameraId;
            _imageStore = imageStore;
            _navigationStore = navigationStore;
            _homeViewModel = homeViewModel;
            _triggerSessionManager = triggerSessionManager;
            Title = _imageStore.GetTitle(_cameraId);
            Image = _imageStore.GetImage(_cameraId);

            _imageStore.Subscribe(_cameraId, OnImageCaptured);
        }
        private void OnImageCaptured()
        {
            AppLogger.Info($"[{_cameraId}] inside on Image Captured");

            Image = _imageStore.GetImage(_cameraId);

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            AppLogger.Info($"[{Title}] Timestamp: {timestamp}");

            var triggerId = _triggerSessionManager.GetOrCreateTriggerId(timestamp);
            Debug.WriteLine($"```````````````````````````````````````````````````````` Cam ID: {_cameraId}, Trigger ID: {triggerId}, Current Trigger ID: {_triggerSessionManager.CurrentTriggerId}");

            lock (TriggerRegistrationLock)
            {
                if (_triggerSessionManager.AssignedTriggerId != _triggerSessionManager.CurrentTriggerId && _triggerSessionManager.AssignedTriggerBool == false)
                {
                    //_collectorInspectionModel.InspectionFail = false;
                    //_plcCommsService.SendInspectionComplete(0);
                    //_inspectionCycleManager.RegisterTrigger(triggerId, new Dictionary<string, int>
                    //{
                    //    ["Cam1"] = 1,
                    //    ["Cam2"] = 1,
                    //});
                    //_triggerSessionManager.AssignedTriggerId = _triggerSessionManager.CurrentTriggerId;
                    //_triggerSessionManager.AssignedTriggerBool = true;
                }

            }
        }
        partial void OnStatusChanged(CameraStatus oldValue, CameraStatus newValue)
        {
            OnPropertyChanged(nameof(IsDisconnected));
            StatusChanged?.Invoke();
        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
