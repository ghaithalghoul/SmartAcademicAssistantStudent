namespace SmartAcademicAssistantStudent.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        public string Message { get; set; } = string.Empty;

        public string? Response { get; set; }


        public DateTime CreatedAt { get; set; }

        public User? User { get; set; }
    }
}
