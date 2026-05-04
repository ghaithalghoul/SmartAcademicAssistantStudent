using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartAcademicAssistantStudent.Data;
using SmartAcademicAssistantStudent.Entities;
using SmartAcademicAssistantStudent.Models;
using System.Security.Claims;

namespace SmartAcademicAssistantStudent.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StudentController(AppDbContext context) : ControllerBase
    {
        // ─── Helper ───────────────────────────────────────────────
        private int GetUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // ─── 1. بيانات الطالب ─────────────────────────────────────
        // GET /api/student/profile
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetUserId();

            var student = await context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student is null)
                return NotFound("الطالب غير موجود");

            return Ok(new
            {
                student.Id,
                student.UniversityId,
                student.Major,
                student.GPA,
                Name = student.User.Name,
                Email = student.User.Email,
                student.User.CreatedAt
            });
        }

        // ─── 2. تحديث بيانات الطالب ──────────────────────────────
        // PUT /api/student/profile
        // Body: { "major": "CS", "universityId": "12345" }
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateStudentDto request)
        {
            var userId = GetUserId();

            var student = await context.Students
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student is null)
                return NotFound("الطالب غير موجود");

            if (!string.IsNullOrWhiteSpace(request.Major))
                student.Major = request.Major;

            if (!string.IsNullOrWhiteSpace(request.UniversityId))
                student.UniversityId = request.UniversityId;

            await context.SaveChangesAsync();
            return Ok(new { student.Major, student.UniversityId, student.GPA });
        }

        // ─── 3. المواد المسجّل فيها ───────────────────────────────
        // GET /api/student/enrollments
        [HttpGet("enrollments")]
        public async Task<IActionResult> GetEnrollments()
        {
            var userId = GetUserId();

            var student = await context.Students
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student is null)
                return NotFound("الطالب غير موجود");

            var enrollments = await context.Enrollments
                .Include(e => e.CourseSection)
                    .ThenInclude(cs => cs.Course)
                .Include(e => e.CourseSection)
                    .ThenInclude(cs => cs.Instructor)
                .Where(e => e.StudentId == student.Id)
                .Select(e => new
                {
                    e.Id,
                    e.Semester,
                    Course = new
                    {
                        e.CourseSection.Course.Id,
                        e.CourseSection.Course.Name,
                        e.CourseSection.Course.Code,
                        e.CourseSection.Course.CreditHours,
                        e.CourseSection.Course.Department
                    },
                    Section = new
                    {
                        e.CourseSection.SectionNumber,
                        e.CourseSection.Time,
                        e.CourseSection.Location,
                        Instructor = e.CourseSection.Instructor.Name
                    }
                })
                .ToListAsync();

            return Ok(enrollments);
        }

        // ─── 4. إحصائيات الطالب ───────────────────────────────────
        // GET /api/student/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var userId = GetUserId();

            var student = await context.Students
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.CourseSection)
                        .ThenInclude(cs => cs.Course)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student is null)
                return NotFound("الطالب غير موجود");

            var completedHours = student.Enrollments
                .Sum(e => e.CourseSection.Course.CreditHours);

            var totalRequired = await context.Courses
                .Where(c => c.Department == student.Major)
                .SumAsync(c => c.CreditHours);

            var remainingHours = totalRequired - completedHours;
            var maxHoursAllowed = student.GPA >= 3.0 ? 18 : student.GPA >= 2.0 ? 15 : 12;

            return Ok(new
            {
                student.GPA,
                completedHours,
                totalRequired,
                remainingHours,
                maxHoursAllowed,
                enrolledCoursesCount = student.Enrollments.Count
            });
        }

        // ─── 5. تقييمات الطالب ────────────────────────────────────
        // GET /api/student/reviews
        [HttpGet("reviews")]
        public async Task<IActionResult> GetMyReviews()
        {
            var userId = GetUserId();

            var student = await context.Students
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student is null)
                return NotFound("الطالب غير موجود");

            var reviews = await context.CourseReviews
                .Include(r => r.Course)
                .Where(r => r.StudentId == student.Id)
                .Select(r => new
                {
                    r.Id,
                    r.Rating,
                    r.Difficulty,
                    r.Workload,
                    r.Comment,
                    r.CreatedAt,
                    Course = new
                    {
                        r.Course.Name,
                        r.Course.Code
                    }
                })
                .ToListAsync();

            return Ok(reviews);
        }
    }
}