namespace SmartAcademicAssistantStudent.Services
{
    public interface ISmartChatService
    {
        Task<string> GetResponseAsync(string message, int userId);
    }
}