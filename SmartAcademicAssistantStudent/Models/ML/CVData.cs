using Microsoft.ML.Data;

namespace SmartAcademicAssistantStudent.Models.ML
{
    // بيانات التدريب
    public class CVData
    {
        [LoadColumn(0)] public float GPA { get; set; }
        [LoadColumn(1)] public float ExperienceYears { get; set; }
        [LoadColumn(2)] public float SkillsCount { get; set; }
        [LoadColumn(3)] public float HasGitHub { get; set; }
        [LoadColumn(4)] public float CertificationsCount { get; set; }
        [LoadColumn(5)] public float ProjectsCount { get; set; }
        [LoadColumn(6)] public float HasInternship { get; set; }
        [LoadColumn(7)] public float EnglishLevel { get; set; }

        [LoadColumn(8), ColumnName("Label")]
        public bool IsAccepted { get; set; }
    }

    // نتيجة التنبؤ
    public class CVPrediction
    {
        [ColumnName("PredictedLabel")]
        public bool IsAccepted { get; set; }

        [ColumnName("Probability")]
        public float Probability { get; set; }

        [ColumnName("Score")]
        public float Score { get; set; }
    }
}