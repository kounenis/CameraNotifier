using System;
using System.IO;
using System.Threading;
using CameraNotifier.Services.CameraFeed;
using CameraNotifier.Services.ImageClassifier;
using CameraNotifier.Services.SlackNotifier;
using Microsoft.Extensions.Options;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace CameraNotifier.Services.WatchService
{
    internal class WatchService : IWatchService
    {
        private readonly IImageClassifier _imageClassifier;
        private readonly ICameraFeedService _cameraFeedService;
        private readonly ISlackNotifier _slackNotifier;

        private readonly CameraFeedOptions _cameraFeedOptions;
        private readonly WatchServiceOptions _options;

        private Timer _timer;

        private int failed = 0;
        private int successful = 0;

        public WatchService(IImageClassifier imageClassifier, ICameraFeedService cameraFeedService,
            ISlackNotifier slackNotifier, IOptions<WatchServiceOptions> watchServiceOptions,
            IOptions<CameraFeedOptions> cameraFeedOptions)
        {
            _imageClassifier = imageClassifier;
            _cameraFeedService = cameraFeedService;
            _slackNotifier = slackNotifier;

            _cameraFeedOptions = cameraFeedOptions.Value;
            _options = watchServiceOptions.Value;
        }

        public void Start()
        {
            _timer = new Timer(OnTimerTick, null, TimeSpan.Zero, Timeout.InfiniteTimeSpan);
        }

        private void OnTimerTick(object obj)
        {
            var start = DateTime.UtcNow;

            string fullImagePath = null;
            string croppedImagePath = null;

            try
            {
                fullImagePath = _cameraFeedService.GetPhoto();

                string currentStatus = null;
                if (File.Exists(_options.StatusFilePath))
                {
                    currentStatus = File.ReadAllText(_options.StatusFilePath);
                }

                croppedImagePath = GetCroppedImagePath(fullImagePath);

                var newStatus = _imageClassifier.ClassifyImage(croppedImagePath);

                if (currentStatus != newStatus)
                {
                    _slackNotifier.SendNotification($"Status changed from {currentStatus} to {newStatus}",
                        fullImagePath);

                    File.WriteAllText(_options.StatusFilePath, newStatus);
                }
                successful++;

                if (successful == 1)
                {
                    Log.Logger.Information("Successfully processed the first image");
                }
            }
            catch (Exception e)
            {
                failed++;
                Serilog.Log.Error($"Error getting image and classifying it. {e.Message}\n{e.StackTrace}", e);
            }
            finally
            {
                var now = DateTime.UtcNow;
                var nextRunTime = start.Add(TimeSpan.FromSeconds(_options.IntervalSeconds));
                var delayTimespan = nextRunTime > now ? nextRunTime.Subtract(now) : TimeSpan.Zero;
                _timer.Change(delayTimespan, Timeout.InfiniteTimeSpan);
            }

            SafeDeleteFile(fullImagePath);
            SafeDeleteFile(croppedImagePath);

        }

        public (int successful, int failed) GetStats()
        {
            return (successful, failed);
        }

        private static void SafeDeleteFile(string filePath)
        {
            try
            {
                if (filePath != null && File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception e)
            {
                Serilog.Log.Error($"Failed to delete temp file {filePath}: {e.Message}", e);
            }
        }

        private string GetCroppedImagePath(string imagePath)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".jpg");
            Image sourceImage = Image.Load(File.ReadAllBytes(imagePath));
            sourceImage.Mutate(ctx => ctx.Crop(new Rectangle(
                _cameraFeedOptions.CropStartX,
                _cameraFeedOptions.CropStartY,
                _cameraFeedOptions.Width,
                _cameraFeedOptions.Height)));
            sourceImage.Save(tempPath);
            return tempPath;
        }
    }
}
