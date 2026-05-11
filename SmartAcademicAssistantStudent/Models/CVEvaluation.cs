namespace SmartAcademicAssistantStudent.Models
{
    public class CVEvaluation
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ExtractedText { get; set; } = string.Empty;

        // Features المستخرجة
        public double GPA { get; set; }
        public int ExperienceYears { get; set; }
        public int SkillsCount { get; set; }
        public bool HasGitHub { get; set; }
        public int CertificationsCount { get; set; }
        public int ProjectsCount { get; set; }
        public bool HasInternship { get; set; }
        public int EnglishLevel { get; set; } // 1-3

        // النتيجة
        public string Result { get; set; } = string.Empty; // Accepted/Rejected
        public float AcceptanceProbability { get; set; }
        public string Strengths { get; set; } = string.Empty;
        public string Weaknesses { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public User? User { get; set; }
    }
}