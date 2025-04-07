using System;
using System.IO;
using System.Net;
using Microsoft.Extensions.Options;

namespace CameraNotifier.Services.CameraFeed
{
    public class CameraFeedService : ICameraFeedService
    {
        private readonly CameraFeedOptions _options;

        public CameraFeedService(IOptions<CameraFeedOptions> options)
        {
            _options = options.Value;

            Serilog.Log.Logger.Information("Initializing CameraFeedService");
        }

        public string GetPhoto()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.jpg");
            using (var webClient = new WebClient())
            {
                webClient.Credentials = new NetworkCredential(_options.Username, _options.Password);
                webClient.DownloadFile(_options.Url, tempFile);
                return tempFile;
            }
        }
    }
}
