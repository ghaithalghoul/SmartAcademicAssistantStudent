using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartAcademicAssistantStudent.Data;
using SmartAcademicAssistantStudent.Services;
using System.Security.Claims;

namespace SmartAcademicAssistantStudent.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StudyPlanController(AppDbContext context, StudyPlanService planService)
        : ControllerBase
    {
        private int GetUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // ─── GET /api/studyplan ───────────────────────────────────
        // جلب خطة الطالب الحالي
        [HttpGet]
        public async Task<IActionResult> GetMyPlan()
        {
            var userId = GetUserId();

            var student = await context.Students
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.CourseSection)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student is null)
                return NotFound("الطالب غير موجود");

            // ✅ اجلب الساعات المنجزة من الـ Enrollments الفعلية
            var completedCourseIds = student.Enrollments
                .Select(e => e.CourseSection.CourseId)
                .Distinct()
                .ToHashSet();

            await planService.SyncStatusAsync(student.Id);

            var plan = await context.StudyPlans
                .Include(sp => sp.Courses)
                    .ThenInclude(spc => spc.Course)
                .FirstOrDefaultAsync(sp => sp.StudentId == student.Id);

            if (plan is null)
                return NotFound("لا توجد خطة دراسية");

            // ✅ احسب الـ completed من الـ enrollments مش من الـ plan status
            var completedHours = plan.Courses
                .Where(c => completedCourseIds.Contains(c.CourseId))
                .Sum(c => c.Course.CreditHours);

            var totalHours = plan.Courses.Sum(c => c.Course.CreditHours);

            var semesters = plan.Courses
                .GroupBy(c => c.SemesterNumber)
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    SemesterNumber = g.Key,
                    SemesterLabel = $"Semester {g.Key}",
                    TotalHours = g.Sum(c => c.Course.CreditHours),
                    Courses = g.Select(c => new
                    {
                        c.Id,
                        c.CourseId,
                        c.Course.Name,
                        c.Course.Code,
                        c.Course.CreditHours,
                        c.Course.Department,
                        // ✅ الحالة من الـ enrollments مباشرة
                        Status = completedCourseIds.Contains(c.CourseId)
                            ? "Completed" : "Planned"
                    }).OrderBy(c => c.Name).ToList()
                }).ToList();

            return Ok(new
            {
                plan.Id,
                plan.GeneratedAt,
                plan.TotalSemesters,
                TotalCreditHours = totalHours,
                CompletedHours = completedHours,  // ✅ صح هلق
                Semesters = semesters
            });
        }
        // ─── POST /api/studyplan/generate ────────────────────────
        // توليد خطة جديدة (يحذف القديمة)
        [HttpPost("generate")]
        public async Task<IActionResult> Generate()
        {
            var userId = GetUserId();

            var student = await context.Students
                .FirstOrDefaultAsync(s => s.UserId == userId);
            if (student is null)
                return NotFound("الطالب غير موجود");

            try
            {
                var plan = await planService.GeneratePlanAsync(student.Id);

                return Ok(new
                {
                    Message = "تم توليد الخطة الدراسية بنجاح",
                    plan.Id,
                    plan.TotalSemesters,
                    plan.TotalCreditHours
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ─── PUT /api/studyplan/course/{id}/status ────────────────
        // تحديث حالة مادة واحدة يدوياً
        [HttpPut("course/{spcId}/status")]
        public async Task<IActionResult> UpdateStatus(int spcId, [FromBody] string status)
        {
            var allowed = new[] { "Planned", "InProgress", "Completed" };
            if (!allowed.Contains(status))
                return BadRequest("حالة غير صحيحة. القيم المسموحة: Planned, InProgress, Completed");

            var userId = GetUserId();
            var student = await context.Students
                .FirstOrDefaultAsync(s => s.UserId == userId);
            if (student is null) return NotFound();

            var spc = await context.StudyPlanCourses
                .Include(x => x.StudyPlan)
                .FirstOrDefaultAsync(x => x.Id == spcId &&
                                          x.StudyPlan.StudentId == student.Id);

            if (spc is null) return NotFound("المادة غير موجودة في خطتك");

            spc.Status = status;
            await context.SaveChangesAsync();

            return Ok(new { spc.Id, spc.Status });
        }
    }
}