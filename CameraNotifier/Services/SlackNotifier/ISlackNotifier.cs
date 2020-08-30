namespace CameraNotifier.Services.SlackNotifier
{
    internal interface ISlackNotifier
    {
        void SendNotification(string text, string imageFilePath);
    }
}