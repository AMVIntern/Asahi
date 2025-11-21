using Asahi.DataServices;
using Asahi.Enums;
using Asahi.Helpers;
using Asahi.Interfaces;
using Asahi.Models;
using Asahi.Stores;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asahi.ImageSources
{
    public class CameraImageLoader : IImageSource
    {
        private readonly CameraFrameGrabber _cameraFrameGrabber;
        private readonly ImageAcquisitionModel _imageAcquisitionModel;
        private readonly MultiCameraImageStore _imageStore;
        private readonly string _cameraId;
        private readonly ImageLogger _imageLogger;
        private readonly Action<CameraStatus>? _setStatus;
        private CameraStatus? _lastStatus = null;
        public string CameraId => _cameraId;

        public CameraImageLoader(
            CameraFrameGrabber cameraFrameGrabber,
            ImageAcquisitionModel imageAcquisitionModel,
            MultiCameraImageStore imageStore,
            string cameraId,
            ImageLogger imageLogger,
            Action<CameraStatus>? setStatus = null)
        {
            _cameraFrameGrabber = cameraFrameGrabber;
            _imageAcquisitionModel = imageAcquisitionModel;
            _imageStore = imageStore;
            _cameraId = cameraId;
            _imageLogger = imageLogger;
            _setStatus = setStatus;
        }

        public Task GrabNextFrameAsync()
        {
            try
            {
                var image = GrabImage(_imageAcquisitionModel.AcqHandles[_cameraId]);
                _imageStore.UpdateImage(_cameraId, image);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Live image acquisition failed for {_cameraId}", ex);
            }

            return Task.CompletedTask;
        }

        private HImage GrabImage(HTuple acqHandle)
        {
            bool timeout = true;
            HImage hImage = new HImage();

            while (timeout)
            {
                try
                {
                    HOperatorSet.GrabImage(out HObject img, acqHandle);
                    hImage = new HImage(img);
                    timeout = false;
                    AppLogger.Info($"[CAPTURE:OK] cam={_cameraId} via GrabImage");
                    Debug.WriteLine($"Image {_cameraId} Captured!");
                }
                catch (Exception ex)
                {
                    if (ex is HDevEngineException hdevEx)
                    {
                        int errorCode = hdevEx.HalconError;

                        if (errorCode == 5322)
                        {
                            AppLogger.Info($"[CAPTURE:TIMEOUT] cam={_cameraId} waiting for trigger");
                            Debug.WriteLine($"[HALCON] Timeout waiting for trigger ({_cameraId})");
                            timeout = true;
                        }
                        else
                        {
                            AppLogger.Error($"[CAPTURE:ERR] cam={_cameraId} halconError={errorCode} closing and restarting frame grabber");
                            Debug.WriteLine($"[HALCON] Error {errorCode} on {_cameraId}: closing and restarting frame grabber");
                            UpdateStatus(CameraStatus.Disconnected);
                            _cameraFrameGrabber.CloseFrameGrabber(acqHandle);
                            try
                            {
                                AppLogger.Info($"[CAPTURE:RESTARTED] cam={_cameraId}");
                                _imageAcquisitionModel.AcqHandles[_cameraId] = StartLiveCamera(_cameraId);
                                UpdateStatus(CameraStatus.Connected);
                            }
                            catch (Exception innerEx)
                            {
                                AppLogger.Error($"Failed to restart frame grabber for {_cameraId}", innerEx);
                            }
                            throw; // or swallow depending on your policy
                        }
                    }
                    else
                    {
                        AppLogger.Error($"[CAPTURE:EXC] cam={_cameraId} ex={ex.GetType().Name}");
                        Debug.WriteLine($"[HALCON] Error {ex} on {_cameraId}: closing and restarting frame grabber");
                        UpdateStatus(CameraStatus.Disconnected);
                        _cameraFrameGrabber.CloseFrameGrabber(acqHandle);
                        try
                        {
                            _imageAcquisitionModel.AcqHandles[_cameraId] = StartLiveCamera(_cameraId);
                            UpdateStatus(CameraStatus.Connected);
                            AppLogger.Info($"[CAPTURE:RESTARTED] cam={_cameraId}");
                        }
                        catch (Exception innerEx)
                        {
                            AppLogger.Error($"Failed to restart frame grabber for {_cameraId}", innerEx);
                        }
                        throw; // or swallow depending on your policy
                    }
                }
                finally
                {
                    //call.Dispose();
                    //proc.Dispose();
                }
            }

            return hImage;
        }

        private void UpdateStatus(CameraStatus newStatus)
        {
            if (_lastStatus != newStatus)
            {
                _setStatus?.Invoke(newStatus);
                _lastStatus = newStatus;
            }
        }

        public void Dispose()
        {
            // Optional: clean up if needed
        }

        public HTuple StartLiveCamera(string cameraId)
        {
            var call = new HDevProcedureCall(new HDevProcedure("StartCameraFrameGrabber"));
            call.SetInputCtrlParamTuple("CameraName", cameraId);
            call.Execute();
            return call.GetOutputCtrlParamTuple("AcqHandle");
        }
    }
}
