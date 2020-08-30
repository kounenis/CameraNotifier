namespace CameraNotifier.Services.WatchService
{
    public interface IWatchService
    {
        void Start();

        (int successful, int failed) GetStats();
    }
}
