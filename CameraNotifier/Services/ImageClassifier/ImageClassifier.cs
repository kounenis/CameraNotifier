using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Options;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Vision;

namespace CameraNotifier.Services.ImageClassifier
{
    public class ImageClassifier : IImageClassifier
    {
        private MLContext _mlContext;
        private ITransformer _model;

        private readonly ImageClassifierOptions _options;

        public ImageClassifier(IOptions<ImageClassifierOptions> options)
        {
            _options = options.Value;
        }

        public string ClassifyImage(string imagePath)
        {
            if (_mlContext == null || _model == null)
            {
                BuildModel(false);
            }

            var data = new [] { new ImageData() {ImagePath = imagePath} };

            IDataView testSet = _mlContext.Data.LoadFromEnumerable(data);
            var transformedData = GetPreprocessingPipeline(_mlContext)
                .Fit(testSet).Transform(testSet);

            PredictionEngine<ModelInput, ModelOutput> predictionEngine = _mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(_model);

            ModelInput image = _mlContext.Data.CreateEnumerable<ModelInput>(transformedData, reuseRowObject: true).First();

            ModelOutput prediction = predictionEngine.Predict(image);

            return prediction.PredictedLabel;
        }

        private void BuildModel(bool clearPrevious)
        {
            if (clearPrevious && File.Exists(_options.ModelPath))
            {
                File.Delete(_options.ModelPath);
            }

            _mlContext = new MLContext();

            if (File.Exists(_options.ModelPath))
            {
                _model = _mlContext.Model.Load(_options.ModelPath, out _);
            }
            else
            {
                _model = CreateModel(_mlContext);
            }
        }

        private ITransformer CreateModel(MLContext mlContext)
        {
            var workspacePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(workspacePath);


            IEnumerable<ImageData> images = LoadImagesFromDirectory(_options.TrainingPath, useFolderNameAsLabel: true);

            IDataView imageData = mlContext.Data.LoadFromEnumerable(images);
            IDataView shuffledData = mlContext.Data.ShuffleRows(imageData);

            IDataView preProcessedData = GetPreprocessingPipeline(mlContext)
                .Fit(shuffledData)
                .Transform(shuffledData);

            DataOperationsCatalog.TrainTestData trainSplit =
                mlContext.Data.TrainTestSplit(data: preProcessedData, testFraction: 0.3);
            DataOperationsCatalog.TrainTestData validationTestSplit = mlContext.Data.TrainTestSplit(trainSplit.TestSet);

            IDataView trainSet = trainSplit.TrainSet;
            IDataView validationSet = validationTestSplit.TrainSet;
            IDataView testSet = validationTestSplit.TestSet;

            var classifierOptions = new ImageClassificationTrainer.Options()
            {
                Epoch = 600,
                FeatureColumnName = "Image",
                LabelColumnName = "LabelAsKey",
                ValidationSet = validationSet,
                Arch = ImageClassificationTrainer.Architecture.ResnetV2101,
                TestOnTrainSet = true,
                ReuseTrainSetBottleneckCachedValues = true,
                ReuseValidationSetBottleneckCachedValues = true,
                WorkspacePath = workspacePath
            };

            classifierOptions.MetricsCallback = metrics => Serilog.Log.Information(metrics.ToString());

            var trainingPipeline = mlContext.MulticlassClassification.Trainers
                .ImageClassification(classifierOptions)
                .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            var trainedModel = trainingPipeline.Fit(trainSet);

            mlContext.Model.Save(trainedModel, imageData.Schema, _options.ModelPath);

            Directory.Delete(workspacePath);

            return trainedModel;
        }

        private static IEnumerable<ImageData> LoadImagesFromDirectory(string folder, bool useFolderNameAsLabel = true)
        {
            var files = Directory.GetFiles(folder, "*",
                searchOption: SearchOption.AllDirectories);

            foreach (var file in files)
            {
                if ((Path.GetExtension(file) != ".jpg") && (Path.GetExtension(file) != ".png"))
                    continue;

                var label = Path.GetFileName(file);

                if (useFolderNameAsLabel)
                    label = Directory.GetParent(file).Name;
                else
                {
                    for (int index = 0; index < label.Length; index++)
                    {
                        if (!char.IsLetter(label[index]))
                        {
                            label = label.Substring(0, index);
                            break;
                        }
                    }
                }

                yield return new ImageData()
                {
                    ImagePath = file,
                    Label = label
                };
            }
        }

        private EstimatorChain<ImageLoadingTransformer> GetPreprocessingPipeline(MLContext mlContext)
        {
            var preprocessingPipeline = mlContext.Transforms.Conversion.MapValueToKey(
                    inputColumnName: "Label",
                    outputColumnName: "LabelAsKey")
                .Append(mlContext.Transforms.LoadRawImageBytes(
                    outputColumnName: "Image",
                    imageFolder: _options.TrainingPath,
                    inputColumnName: "ImagePath"));
            return preprocessingPipeline;
        }
    }
}