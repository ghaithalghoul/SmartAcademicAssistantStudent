using Microsoft.ML;
using Microsoft.ML.Data;
using SmartAcademicAssistantStudent.Models.ML;

namespace SmartAcademicAssistantStudent.Services
{
    public interface ICVMLService
    {
        CVPrediction Predict(CVFeatures features);
    }

    public class CVMLService : ICVMLService
    {
        private readonly MLContext _mlContext;
        private readonly PredictionEngine<CVData, CVPrediction> _engine;
        private readonly string _modelPath = "MLModel/cv_model.zip";
        private readonly string _dataPath = "MLModel/cv_training_data.csv";

        public CVMLService()
        {
            _mlContext = new MLContext(seed: 42);
            var model = LoadOrTrain();
            _engine = _mlContext.Model
                .CreatePredictionEngine<CVData, CVPrediction>(model);
        }

        public CVPrediction Predict(CVFeatures features)
        {
            var data = new CVData
            {
                GPA = (float)features.GPA,
                ExperienceYears = features.ExperienceYears,
                SkillsCount = features.SkillsCount,
                HasGitHub = features.HasGitHub ? 1 : 0,
                CertificationsCount = features.CertificationsCount,
                ProjectsCount = features.ProjectsCount,
                HasInternship = features.HasInternship ? 1 : 0,
                EnglishLevel = features.EnglishLevel
            };

            return _engine.Predict(data);
        }

        private ITransformer LoadOrTrain()
        {
            if (File.Exists(_modelPath))
            {
                using var stream = File.OpenRead(_modelPath);
                return _mlContext.Model.Load(stream, out _);
            }
            return TrainAndSave();
        }

        private ITransformer TrainAndSave()
        {
            var data = _mlContext.Data.LoadFromTextFile<CVData>(
                _dataPath, separatorChar: ',', hasHeader: true);

            var pipeline = _mlContext.Transforms
                .Concatenate("Features",
                    nameof(CVData.GPA),
                    nameof(CVData.ExperienceYears),
                    nameof(CVData.SkillsCount),
                    nameof(CVData.HasGitHub),
                    nameof(CVData.CertificationsCount),
                    nameof(CVData.ProjectsCount),
                    nameof(CVData.HasInternship),
                    nameof(CVData.EnglishLevel))
                .Append(_mlContext.BinaryClassification.Trainers
                    .FastTree(labelColumnName: "Label",
                              featureColumnName: "Features"));

            var model = pipeline.Fit(data);

            Directory.CreateDirectory("MLModel");
            using var fs = File.Create(_modelPath);
            _mlContext.Model.Save(model, data.Schema, fs);

            return model;
        }
    }
}