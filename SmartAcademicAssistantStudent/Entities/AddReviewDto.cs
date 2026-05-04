namespace SmartAcademicAssistantStudent.Entities
{
    public class AddReviewDto
    {
        public int Rating { get; set; }      // 1-5
        public int Difficulty { get; set; }  // 1-5
        public int Workload { get; set; }    // 1-5
        public string? Comment { get; set; }
    }
}
