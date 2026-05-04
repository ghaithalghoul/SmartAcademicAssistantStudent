namespace SmartAcademicAssistantStudent.Services
{
    public interface ICourseAdvisorService
    {
        Task<StudentAcademicContext> BuildStudentContextAsync(int userId);
    }
}
