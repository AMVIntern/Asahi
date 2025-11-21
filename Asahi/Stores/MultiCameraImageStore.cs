using HalconDotNet;
using Asahi.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asahi.Stores
{
    public class MultiCameraImageStore
    {
        private class CameraImageEntry
        {
            public string Title { get; }
            public HImage? Image { get; set; }
            public long TimeStampUnixMs { get; set; }
            public event Action? ImageUpdated;

            public CameraImageEntry(string title)
            {
                Title = title;
            }
            public void RaiseImageUpdated()
            {
                ImageUpdated?.Invoke();
                AppLogger.Info($"[{Title}] UI update");
            }
            public void Subscribe(Action callback)
            {
                ImageUpdated += callback;
            }
            public void Unsubscribe(Action callback)
            {
                ImageUpdated -= callback;
            }
        }

        private readonly Dictionary<string, CameraImageEntry> _cameraEntries = new();

        public void RegisterCamera(string cameraId, string title)
        {
            if (_cameraEntries.ContainsKey(cameraId))
                throw new InvalidOperationException($"Camera with ID {cameraId} is already registered.");

            _cameraEntries[cameraId] = new CameraImageEntry(title);
        }

        public void UpdateImage(string cameraId, HImage image)
        {
            if (!_cameraEntries.TryGetValue(cameraId, out var entry))
                throw new KeyNotFoundException($"Camera with ID {cameraId} is not registered.");

            entry.Image = image;
            entry.TimeStampUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            entry.RaiseImageUpdated();
        }

        public void Subscribe(string cameraId, Action callback)
        {
            if (!_cameraEntries.TryGetValue(cameraId, out var entry))
                throw new KeyNotFoundException($"Camera with ID '{cameraId}' is not registered.");

            entry.Subscribe(callback);
        }

        public void Unsubscribe(string cameraId, Action callback)
        {
            if (!_cameraEntries.TryGetValue(cameraId, out var entry))
                throw new KeyNotFoundException($"Camera with ID '{cameraId}' is not registered.");

            entry.Unsubscribe(callback);
        }

        public HImage? GetImage(string cameraId)
        {
            if (!_cameraEntries.TryGetValue(cameraId, out var entry))
                throw new KeyNotFoundException($"Camera with ID '{cameraId}' is not registered.");

            return entry.Image;
        }

        public long GetTimestamp(string cameraId)
        {
            if (!_cameraEntries.TryGetValue(cameraId, out var entry))
                throw new KeyNotFoundException($"Camera with ID '{cameraId}' is not registered.");

            return entry.TimeStampUnixMs;
        }

        public string GetTitle(string cameraId)
        {
            if (!_cameraEntries.TryGetValue(cameraId, out var entry))
                throw new KeyNotFoundException($"Camera with ID '{cameraId}' is not registered.");

            return entry.Title;
        }

        public IEnumerable<string> GetAllCameraIds()
        {
            return _cameraEntries.Keys;
        }

        public void ClearImages()
        {
            foreach (var entry in _cameraEntries.Values)
            {
                if (entry.Image != null && entry.Image.IsInitialized())
                {
                    entry.Image.Dispose();
                    entry.Image = null;
                }
                entry.TimeStampUnixMs = 0;
            }
            AppLogger.Info("Cleared all images from store");
        }
    }
}
