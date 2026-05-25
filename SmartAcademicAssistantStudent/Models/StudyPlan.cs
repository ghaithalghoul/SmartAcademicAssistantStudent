namespace SmartAcademicAssistantStudent.Models
{
    public class StudyPlan
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public int TotalSemesters { get; set; }
        public int TotalCreditHours { get; set; }

        public List<StudyPlanCourse> Courses { get; set; } = [];
    }
}