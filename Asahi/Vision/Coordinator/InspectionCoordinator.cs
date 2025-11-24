using Asahi.AppCycleManager;
using Asahi.DataServices;
using Asahi.Helpers;
using Asahi.Stores;
using Asahi.ViewModels;
using HalconDotNet;
using Asahi.Vision.Handlers.Core;
using Asahi.Vision.Results;
using Asahi.Vision.Handlers.Interfaces;
using System.Windows;

namespace Asahi.Vision.Coordinator
{
    public class InspectionCoordinator
    {
        private readonly Dictionary<string, IInspectionRunner> _runners;
        private readonly MultiCameraImageStore _imageStore;
        private readonly Dictionary<string, CameraViewModel> _cameraViewModels;
        private readonly ImageLogger _imageLogger;
        private readonly TriggerSessionManager _triggerSessionManager;
        private readonly HomeViewModel? _homeViewModel;

        public InspectionCoordinator(
            Dictionary<string, IInspectionRunner> runners,
            MultiCameraImageStore imageStore,
            Dictionary<string, CameraViewModel> cameraViewModels,
            ImageLogger imageLogger,
            TriggerSessionManager triggerSessionManager,
            HomeViewModel? homeViewModel = null)
        {
            _runners = runners;
            _imageStore = imageStore;
            _cameraViewModels = cameraViewModels;
            _imageLogger = imageLogger;
            _triggerSessionManager = triggerSessionManager;
            _homeViewModel = homeViewModel;
            foreach (var (cameraId, runner) in _runners)
            {
                _imageStore.Subscribe(cameraId, async () => await HandleNewImage(cameraId, runner));
            }
        }

        private async Task HandleNewImage(string cameraId, IInspectionRunner runner)
        {
            var image = _imageStore.GetImage(cameraId);
            if (image == null || !image.IsInitialized())
            {
                AppLogger.Error($"[Inspection] Skipping frame for '{cameraId}' — image is null or uninitialized.");
                return;
            }

            var context = new InspectionContext
            {
                Image = image.Clone(),
                CameraId = cameraId,
            };

            try
            {
                var updatedContext = await runner.RunAsync(context);

                await HandleResult(cameraId, updatedContext, image);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Inspection failed for camera '{cameraId}': {ex.Message}", ex);
            }
            finally
            {
                context?.Dispose();
            }
        }

        private async Task HandleResult(string cameraId, InspectionContext context, HImage image)
        {
            var result = ConvertContextToCameraInspectionResult(cameraId, context);
            bool overallPass = result.OverallPass;

            AppLogger.Info($"[{result.CameraId}] PASS = {overallPass} ({result.Results.Count} inspections)");
            foreach (var r in result.Results)
            {
                AppLogger.Info($"  • {r.InspectionName}: {(r.Passed ? "PASS" : "FAIL")} | Confidence = {r.Confidence}");
            }

            // Extract barcode results from context and update HomeViewModel
            if (context.Parameters.TryGetValue("BarCode", out var barcodeObj) && barcodeObj is string[] barcodeArray)
            {
                if (_homeViewModel != null)
                {
                    // Ensure UI updates happen on the UI thread
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _homeViewModel.BarcodeResults = barcodeArray;
                        _homeViewModel.BarcodeDisplayText = barcodeArray.Length > 0 
                            ? string.Join("", barcodeArray) 
                            : "No barcode detected";
                    });
                    AppLogger.Info($"[Barcode] Updated HomeViewModel with barcode: {string.Join("", barcodeArray)}");
                }
            }

            if (_cameraViewModels.TryGetValue(result.CameraId, out var vm))
            {
               // vm.UpdateInspectionResult(result);
            }

            double maxConfidence = context.InspectionResults.Values
                .OfType<InspectionResult>()
                .Select(ir => ir.Confidence)
                .DefaultIfEmpty(0.0)
                .Max();
        }

        private CameraInspectionResult ConvertContextToCameraInspectionResult(string cameraId, InspectionContext context)
        {
            var inspectionResults = new List<InspectionResult>();

            foreach (var kvp in context.InspectionResults)
            {
                if (kvp.Value is InspectionResult inspectionResult)
                {
                    inspectionResults.Add(inspectionResult);
                }
            }

            return new CameraInspectionResult()
            {
                CameraId = cameraId,
                Results = inspectionResults
            };
        }
    }
}
