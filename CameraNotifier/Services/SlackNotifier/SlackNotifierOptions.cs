namespace CameraNotifier.Services.SlackNotifier
{
    public class SlackNotifierOptions
    {
        public const string SettingsGroupName = "SlackNotifier";

        public string APIKey { get; set; }
        public string Channel { get; set; }
    }
}
