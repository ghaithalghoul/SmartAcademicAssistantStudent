namespace SmartAcademicAssistantStudent.Models
{
    public class Course
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public int CreditHours { get; set; }
        public string Department { get; set; }
        public string Description { get; set; }

        public ICollection<CourseSection> Sections { get; set; }
        public ICollection<CourseReview> Reviews { get; set; }


    }
}
