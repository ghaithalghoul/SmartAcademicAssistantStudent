namespace SmartAcademicAssistantStudent.Models
{
    public class Enrollment
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int CourseSectionId { get; set; }
        public string Semester { get; set; }

        public Student Student { get; set; }
        public CourseSection CourseSection { get; set; }

    }
}
