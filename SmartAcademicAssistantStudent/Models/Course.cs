namespace SmartAcademicAssistantStudent.Models
{
    public class Course
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int CreditHours { get; set; }
        public string Department { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // ✅ غيّر من ICollection إلى List مع قيمة افتراضية
        public List<CoursePrerequisite> Prerequisites { get; set; } = [];
        public List<CourseSection> Sections { get; set; } = [];
        public List<CourseReview> Reviews { get; set; } = [];


    }
}
