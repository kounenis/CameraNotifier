using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Serilog;

namespace CameraNotifier.Services.SlackNotifier
{
    internal class SlackNotifier : ISlackNotifier
    {
        private const string SlackUploadUri = "https://slack.com/api/files.upload";

        private readonly SlackNotifierOptions _options;

        public SlackNotifier(IOptions<SlackNotifierOptions> options)
        {
            _options = options.Value;
            Log.Logger.Information("Initializing SlackNotifier");
        }

        public void SendNotification(string text, string imageFilePath)
        {
            UploadFilesToChannel(_options.Channel, Path.GetFileName(imageFilePath),
                imageFilePath, text).Wait();
        }

        public async Task UploadFilesToChannel(string channel, string fileName, string filePath, string comment)
        {
            var nameValueCollection = new NameValueCollection()
            {
                { "token", _options.APIKey },
                { "channels", channel },
                { "filename", fileName },
                { "initial_comment", comment}
            };

            var client = new WebClient() {QueryString = nameValueCollection};
            await client.UploadFileTaskAsync(new Uri(SlackUploadUri), filePath);
        }
    }
}
