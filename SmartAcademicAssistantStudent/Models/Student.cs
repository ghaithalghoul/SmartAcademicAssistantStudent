using System.ComponentModel.DataAnnotations;

namespace SmartAcademicAssistantStudent.Models
{
    public class Student
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UniversityId { get; set; }
        public string Major { get; set; }
        [Range(0.0, 4.0)]
        public double GPA { get; set; }

        public User User { get; set; }
        public ICollection<Enrollment> Enrollments { get; set; }
        public ICollection<CourseReview> Reviews { get; set; }

    }
}
