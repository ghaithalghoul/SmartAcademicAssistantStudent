namespace SmartAcademicAssistantStudent.Models
{
    public class CoursePrerequisite
    {
        public int Id { get; set; }

        // المادة الحالية
        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;

        // المادة المطلوبة قبلها
        public int RequiredCourseId { get; set; }
        public Course RequiredCourse { get; set; } = null!;
    }
}
