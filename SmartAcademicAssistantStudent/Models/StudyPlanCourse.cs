namespace SmartAcademicAssistantStudent.Models
{
    public class StudyPlanCourse
    {
        public int Id { get; set; }

        public int StudyPlanId { get; set; }
        public StudyPlan StudyPlan { get; set; } = null!;

        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;

        public int SemesterNumber { get; set; }   // 1, 2, 3 ... 8
        public string Status { get; set; } = "Planned"; // Planned | Completed | InProgress
    }
}