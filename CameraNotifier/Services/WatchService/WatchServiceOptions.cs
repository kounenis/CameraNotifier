namespace CameraNotifier.Services.WatchService
{
    public class WatchServiceOptions
    {
        public const string SettingsGroupName = "WatchService";

        public int IntervalSeconds { get; set; }
        public string StatusFilePath { get; set; }
    }
}
