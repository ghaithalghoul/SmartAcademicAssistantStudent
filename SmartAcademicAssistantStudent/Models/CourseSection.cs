namespace SmartAcademicAssistantStudent.Models
{
    public class CourseSection
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public int InstructorId { get; set; }
        public string SectionNumber { get; set; }
        public string Time { get; set; }
        public string Location { get; set; }

        public Course Course { get; set; }
        public Instructor Instructor { get; set; }
        public ICollection<Enrollment> Enrollments { get; set; }

    }
}
