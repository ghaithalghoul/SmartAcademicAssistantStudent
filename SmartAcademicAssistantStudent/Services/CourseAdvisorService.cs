using SmartAcademicAssistantStudent.Data;
using SmartAcademicAssistantStudent.Models;
using Microsoft.EntityFrameworkCore;

namespace SmartAcademicAssistantStudent.Services
{
    public class CourseAdvisorService(AppDbContext context) : ICourseAdvisorService
    {
        public async Task<StudentAcademicContext> BuildStudentContextAsync(int userId)
        {
            // جلب بيانات الطالب
            var student = await context.Students
                .Include(s => s.User)
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.CourseSection)
                        .ThenInclude(cs => cs.Course)
                .FirstOrDefaultAsync(s => s.UserId == userId)
                ?? throw new Exception("الطالب غير موجود");

            // المواد اللي خلصها
            var completedCourseIds = student.Enrollments
                .Select(e => e.CourseSection.CourseId)
                .Distinct()
                .ToList();

            var completedCourses = await context.Courses
                .Where(c => completedCourseIds.Contains(c.Id))
                .ToListAsync();

            // الساعات المنجزة
            var completedHours = completedCourses.Sum(c => c.CreditHours);

            // كل المواد المتاحة مع الـ Prerequisites والشعب المفتوحة
            var availableCourses = await context.Courses
                .Include(c => c.Prerequisites)
                    .ThenInclude(p => p.RequiredCourse)
                .Include(c => c.Sections)
                    .ThenInclude(s => s.Instructor)
                .Include(c => c.Reviews)
                .Where(c => !completedCourseIds.Contains(c.Id)) // مواد ما خلصها
                .ToListAsync();

            // المواد اللي يقدر ينزلها (Prerequisites مكتملة)
            var eligibleCourses = availableCourses
                .Where(c => c.Prerequisites.All(p =>
                    completedCourseIds.Contains(p.RequiredCourseId)))
                .ToList();

            // ساعات التخرج المطلوبة (من أول مادة في نفس التخصص)
            var totalRequired = await context.Courses
                .Where(c => c.Department == student.Major)
                .SumAsync(c => c.CreditHours);

            return new StudentAcademicContext
            {
                StudentName = student.User.Name,
                Major = student.Major,
                GPA = student.GPA,
                CompletedHours = completedHours,
                TotalRequiredHours = totalRequired,
                RemainingHours = totalRequired - completedHours,
                CompletedCourses = completedCourses,
                EligibleCourses = eligibleCourses,
                MaxHoursAllowed = student.GPA >= 3.0 ? 18 : student.GPA >= 2.0 ? 15 : 12
            };
        }
    }

    // ─── Data class لتجميع البيانات ───────────────────────────
    public class StudentAcademicContext
    {
        public string StudentName { get; set; } = string.Empty;
        public string Major { get; set; } = string.Empty;
        public double GPA { get; set; }
        public int CompletedHours { get; set; }
        public int TotalRequiredHours { get; set; }
        public int RemainingHours { get; set; }
        public int MaxHoursAllowed { get; set; }
        public List<Course> CompletedCourses { get; set; } = [];
        public List<Course> EligibleCourses { get; set; } = [];
    }
}
    