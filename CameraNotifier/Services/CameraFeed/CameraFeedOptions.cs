namespace CameraNotifier.Services.CameraFeed
{
    public class CameraFeedOptions
    {
        public const string SettingsGroupName = "CameraFeed";

        public string Url { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int CropStartX { get; set; }
        public int CropStartY { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
