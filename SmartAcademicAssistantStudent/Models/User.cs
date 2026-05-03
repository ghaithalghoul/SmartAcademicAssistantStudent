namespace SmartAcademicAssistantStudent.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpireTime { get; set; }

        public Student Student { get; set; }
        public ICollection<ChatMessage> ChatMessages { get; set; }
    }
}
