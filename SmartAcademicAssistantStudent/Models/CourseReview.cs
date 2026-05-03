using System.ComponentModel.DataAnnotations;

namespace SmartAcademicAssistantStudent.Models
{
    public class CourseReview
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int CourseId { get; set; }
        [Range(1, 5)]
        public int Rating { get; set; }
        [Range(1, 5)]
        public int Difficulty { get; set; }
        [Range(1, 5)]
        public int Workload { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }

        public Student Student { get; set; }
        public Course Course { get; set; }

    }
}
