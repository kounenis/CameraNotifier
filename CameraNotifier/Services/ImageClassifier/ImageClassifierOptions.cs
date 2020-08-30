namespace CameraNotifier.Services.ImageClassifier
{
    public class ImageClassifierOptions
    {
        public const string SettingsGroupName = "ImageClassifier";

        public string ModelPath { get; set; }
        public string TrainingPath { get; set; }
    }
}
