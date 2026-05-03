namespace SmartAcademicAssistantStudent.Models
{
    public class Instructor
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Department { get; set; }

        public ICollection<CourseSection> Sections { get; set; }

    }
}
