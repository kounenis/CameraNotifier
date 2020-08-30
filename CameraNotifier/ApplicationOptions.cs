using CommandLine;

namespace CameraNotifier
{
    public class ApplicationOptions
    {
        [Option("customAppSettings", Required = false, HelpText = "Specify custom appsettings.json file")]
        public string CustomAppSettings { get; set; }
    }
}
