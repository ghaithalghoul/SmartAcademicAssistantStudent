using Microsoft.EntityFrameworkCore;
using SmartAcademicAssistantStudent.Data;
using SmartAcademicAssistantStudent.Models;

namespace SmartAcademicAssistantStudent.Services
{
    public class StudyPlanService(AppDbContext context)
    {
        private const int MaxHoursPerSemester = 18;
        private const int MaxSemesters = 8;

        public async Task<StudyPlan> GeneratePlanAsync(int studentId)
        {
            // 1. حذف الخطة القديمة
            var existing = await context.StudyPlans
                .FirstOrDefaultAsync(sp => sp.StudentId == studentId);
            if (existing != null)
            {
                context.StudyPlans.Remove(existing);
                await context.SaveChangesAsync();
            }

            // 2. جلب بيانات الطالب
            var student = await context.Students
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.CourseSection)
                .FirstOrDefaultAsync(s => s.Id == studentId)
                ?? throw new Exception("الطالب غير موجود");

            // المواد المكتملة
            var completedCourseIds = student.Enrollments
                .Select(e => e.CourseSection.CourseId)
                .Distinct()
                .ToHashSet();

            // 3. جلب كل المواد مع المتطلبات
            var allCourses = await context.Courses
                .Include(c => c.Prerequisites)
                .ToListAsync();

            // المواد الباقية فقط
            var remaining = allCourses
                .Where(c => !completedCourseIds.Contains(c.Id))
                .ToList();

            // 4. توزيع المواد على الفصول
            var semesterPlan = new Dictionary<int, List<Course>>();
            for (int i = 1; i <= MaxSemesters; i++)
                semesterPlan[i] = [];

            var placed = new HashSet<int>(completedCourseIds);
            var semesterIndex = 1;

            while (remaining.Count > 0 && semesterIndex <= MaxSemesters)
            {
                int hoursThisSemester = 0;
                var toPlace = new List<Course>();

                foreach (var course in remaining.OrderBy(c => c.Prerequisites.Count)
                                                .ThenBy(c => c.CreditHours))
                {
                    bool prereqsMet = course.Prerequisites
                        .All(p => placed.Contains(p.RequiredCourseId));

                    bool fitsInSemester =
                        hoursThisSemester + course.CreditHours <= MaxHoursPerSemester;

                    if (prereqsMet && fitsInSemester)
                    {
                        toPlace.Add(course);
                        hoursThisSemester += course.CreditHours;
                    }
                }

                foreach (var course in toPlace)
                {
                    semesterPlan[semesterIndex].Add(course);
                    placed.Add(course.Id);
                    remaining.Remove(course);
                }

                if (toPlace.Count == 0)
                {
                    semesterIndex++;
                    continue;
                }

                semesterIndex++;
            }

            // 5. بناء كائن الخطة
            var plan = new StudyPlan
            {
                StudentId = studentId,
                GeneratedAt = DateTime.UtcNow,
                TotalSemesters = semesterPlan.Values.Count(s => s.Count > 0),
                TotalCreditHours = allCourses.Sum(c => c.CreditHours), // ✅ كل الساعات
                Courses = []
            };

            // ✅ أضف المواد المكتملة كفصل 0
            var completedCourses = allCourses
                .Where(c => completedCourseIds.Contains(c.Id))
                .ToList();

            foreach (var course in completedCourses)
            {
                plan.Courses.Add(new StudyPlanCourse
                {
                    CourseId = course.Id,
                    SemesterNumber = 0,
                    Status = "Completed"
                });
            }

            // ✅ أضف المواد الباقية حسب الفصول
            foreach (var (semNum, courses) in semesterPlan)
            {
                foreach (var course in courses)
                {
                    plan.Courses.Add(new StudyPlanCourse
                    {
                        CourseId = course.Id,
                        SemesterNumber = semNum,
                        Status = "Planned"
                    });
                }
            }

            context.StudyPlans.Add(plan);
            await context.SaveChangesAsync();

            return plan;
        }

        public async Task SyncStatusAsync(int studentId)
        {
            var plan = await context.StudyPlans
                .Include(sp => sp.Courses)
                .FirstOrDefaultAsync(sp => sp.StudentId == studentId);

            if (plan is null) return;

            var student = await context.Students
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.CourseSection)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student is null) return;

            var completedIds = student.Enrollments
                .Select(e => e.CourseSection.CourseId)
                .ToHashSet();

            foreach (var spc in plan.Courses)
            {
                spc.Status = completedIds.Contains(spc.CourseId)
                    ? "Completed"
                    : "Planned";

                // ✅ المواد المكتملة تبقى في فصل 0
                if (spc.Status == "Completed")
                    spc.SemesterNumber = 0;
            }

            await context.SaveChangesAsync();
        }
    }
}