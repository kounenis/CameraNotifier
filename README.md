# CameraNotifier
 This application uses ML.Net to analyze the feed from the camera outside of my house to find if there is parking available notifies me via Slack 

# How it works
This application is fed some training data, photos where parking is available and photos where parking is unavailable. It trains a machine learning model and then uses that to classify images taken from the outdoor camera.
When it notices a status change it sends a notification (with photo) to a custom Slack channel. It persists the model so it doesn't have to train it again in the next run.

# Architecture
This is an ASP.NET Core Razor pages project. 

Camera feed is fetched via a HTTP Get request (use of credentials is supported). For the slack notification I have a custom app that I have added to a free Slack project.
This configuration is required. It can be added in the appsettings.json file. For deployment I put these settings in a separate location outside my code repo and supply the location of the file via an optional --customAppSettings argument.

I start it like this:
dotnet /mnt/path/to/my/buildserver/CameraNotifier/prod/CameraNotifier.dll -- --customAppSettings=/mnt/path/to/my/configs/cameraNotifierConfig.json

# Required config 
The Slack app needs to be added to your channel and have permission to upload files

{
 "CameraFeed": {
    "Url": "http://192.168.0.99:80/ISAPI/Streaming/Channels/101/picture",
    "Username": "cameraUser",
    "Password": "cameraPass",
    "CropStartX": 968,
    "CropStartY": 17,
    "Width": 1700,
    "Height": 307
  },
  "ImageClassifier": {
    "ModelPath": "/mnt/path/to/save/model/model.zip",
    "TrainingPath": "/mnt/path/to/trainingImages/requires/two/subfolders/car/nocar/etc"
  },
  "SlackNotifier": {
    "APIKey": "my_secret_slack_app_key",
    "Channel": "parking"
  },
  "WatchService": {
    "IntervalSeconds": 10,
    "StatusFilePath": "/mnt/path/to/persist/status/status.txt"
  },
  "Logging": {
	"LogFile" : "/mnt/path/to/log/file/log.txt"
  }  
}
