namespace CameraNotifier.Services.ImageClassifier
{
    public interface IImageClassifier
    {
        string ClassifyImage(string imagePath);
    }
}