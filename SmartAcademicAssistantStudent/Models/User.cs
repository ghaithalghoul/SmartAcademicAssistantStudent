namespace SmartAcademicAssistantStudent.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "Student";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpireTime { get; set; }

        public Student? Student { get; set; }

        // ✅ غيّر من ICollection إلى List
        public List<ChatMessage> ChatMessages { get; set; } = [];
    }
}
