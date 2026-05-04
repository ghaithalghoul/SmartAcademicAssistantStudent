using System.ComponentModel.DataAnnotations;

namespace SmartAcademicAssistantStudent.Models
{
    public class Student
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UniversityId { get; set; } = string.Empty;
        public string Major { get; set; } = string.Empty;
        public double GPA { get; set; }

        public User User { get; set; } = null!;

        // ✅ غيّر من ICollection إلى List
        public List<Enrollment> Enrollments { get; set; } = [];
        public List<CourseReview> Reviews { get; set; } = [];

    }
}
