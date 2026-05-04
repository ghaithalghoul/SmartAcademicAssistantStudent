using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartAcademicAssistantStudent.Data;
using SmartAcademicAssistantStudent.Entities;
using System.Security.Claims;

namespace SmartAcademicAssistantStudent.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CourseController(AppDbContext context) : ControllerBase
    {
        // ─── Helper ───────────────────────────────────────────────
        private int GetUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // ─── 1. كل المواد مع فلترة ────────────────────────────────
        // GET /api/course?department=CS&search=math
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? department,
            [FromQuery] string? search)
        {
            var query = context.Courses
                .Include(c => c.Reviews)
                .Include(c => c.Sections)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(department))
                query = query.Where(c => c.Department == department);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(c =>
                    c.Name.Contains(search) ||
                    c.Code.Contains(search));

            var courses = await query
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Code,
                    c.CreditHours,
                    c.Department,
                    c.Description,
                    AvgRating = c.Reviews.Any()
                        ? Math.Round(c.Reviews.Average(r => r.Rating), 1)
                        : 0,
                    AvgDifficulty = c.Reviews.Any()
                        ? Math.Round(c.Reviews.Average(r => r.Difficulty), 1)
                        : 0,
                    ReviewsCount = c.Reviews.Count,
                    SectionsCount = c.Sections.Count
                })
                .ToListAsync();

            return Ok(courses);
        }

        // ─── 2. تفاصيل مادة واحدة ────────────────────────────────
        // GET /api/course/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var course = await context.Courses
                .Include(c => c.Prerequisites)
                    .ThenInclude(p => p.RequiredCourse)
                .Include(c => c.Sections)
                    .ThenInclude(s => s.Instructor)
                .Include(c => c.Reviews)
                    .ThenInclude(r => r.Student)
                        .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course is null)
                return NotFound("المادة غير موجودة");

            return Ok(new
            {
                course.Id,
                course.Name,
                course.Code,
                course.CreditHours,
                course.Department,
                course.Description,

                Prerequisites = course.Prerequisites.Select(p => new
                {
                    p.RequiredCourse.Id,
                    p.RequiredCourse.Name,
                    p.RequiredCourse.Code
                }),

                Sections = course.Sections.Select(s => new
                {
                    s.Id,
                    s.SectionNumber,
                    s.Time,
                    s.Location,
                    Instructor = s.Instructor.Name
                }),

                Stats = new
                {
                    AvgRating = course.Reviews.Any()
                        ? Math.Round(course.Reviews.Average(r => r.Rating), 1) : 0,
                    AvgDifficulty = course.Reviews.Any()
                        ? Math.Round(course.Reviews.Average(r => r.Difficulty), 1) : 0,
                    AvgWorkload = course.Reviews.Any()
                        ? Math.Round(course.Reviews.Average(r => r.Workload), 1) : 0,
                    ReviewsCount = course.Reviews.Count
                },

                Reviews = course.Reviews
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(10)
                    .Select(r => new
                    {
                        r.Id,
                        r.Rating,
                        r.Difficulty,
                        r.Workload,
                        r.Comment,
                        r.CreatedAt,
                        StudentName = r.Student.User.Name
                    })
            });
        }

        // ─── 3. إضافة تقييم لمادة ─────────────────────────────────
        // POST /api/course/{id}/review
        // Body: { "rating": 4, "difficulty": 3, "workload": 3, "comment": "مادة ممتازة" }
        [HttpPost("{id}/review")]
        public async Task<IActionResult> AddReview(int id, [FromBody] AddReviewDto request)
        {
            var userId = GetUserId();

            var student = await context.Students
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student is null)
                return NotFound("الطالب غير موجود");

            var course = await context.Courses.FindAsync(id);
            if (course is null)
                return NotFound("المادة غير موجودة");

            // تحقق إن الطالب ما قيّم المادة قبل
            var exists = await context.CourseReviews
                .AnyAsync(r => r.StudentId == student.Id && r.CourseId == id);

            if (exists)
                return BadRequest("لقد قيّمت هذه المادة مسبقاً");

            // تحقق إن الطالب مسجّل في المادة
            var enrolled = await context.Enrollments
                .Include(e => e.CourseSection)
                .AnyAsync(e => e.StudentId == student.Id &&
                               e.CourseSection.CourseId == id);

            if (!enrolled)
                return BadRequest("لا يمكنك تقييم مادة لم تسجّل فيها");

            var review = new SmartAcademicAssistantStudent.Models.CourseReview
            {
                StudentId = student.Id,
                CourseId = id,
                Rating = Math.Clamp(request.Rating, 1, 5),
                Difficulty = Math.Clamp(request.Difficulty, 1, 5),
                Workload = Math.Clamp(request.Workload, 1, 5),
                Comment = request.Comment ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            context.CourseReviews.Add(review);
            await context.SaveChangesAsync();

            return Ok(new
            {
                review.Id,
                review.Rating,
                review.Difficulty,
                review.Workload,
                review.Comment,
                review.CreatedAt
            });
        }

        // ─── 4. الأقسام المتاحة ───────────────────────────────────
        // GET /api/course/departments
        [HttpGet("departments")]
        public async Task<IActionResult> GetDepartments()
        {
            var departments = await context.Courses
                .Select(c => c.Department)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();

            return Ok(departments);
        }

        // ─── 5. المواد المتاحة للطالب الفصل الجاي ────────────────
        // GET /api/course/eligible
        [HttpGet("eligible")]
        public async Task<IActionResult> GetEligibleCourses()
        {
            var userId = GetUserId();

            var student = await context.Students
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.CourseSection)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student is null)
                return NotFound("الطالب غير موجود");

            var completedIds = student.Enrollments
                .Select(e => e.CourseSection.CourseId)
                .Distinct()
                .ToList();

            var eligible = await context.Courses
                .Include(c => c.Prerequisites)
                .Include(c => c.Sections)
                    .ThenInclude(s => s.Instructor)
                .Include(c => c.Reviews)
                .Where(c => !completedIds.Contains(c.Id))
                .ToListAsync();

            var result = eligible
                .Where(c => c.Prerequisites.All(p =>
                    completedIds.Contains(p.RequiredCourseId)))
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Code,
                    c.CreditHours,
                    c.Department,
                    AvgRating = c.Reviews.Any()
                        ? Math.Round(c.Reviews.Average(r => r.Rating), 1) : 0,
                    AvgDifficulty = c.Reviews.Any()
                        ? Math.Round(c.Reviews.Average(r => r.Difficulty), 1) : 0,
                    Sections = c.Sections.Select(s => new
                    {
                        s.SectionNumber,
                        s.Time,
                        s.Location,
                        Instructor = s.Instructor.Name
                    })
                });

            return Ok(result);
        }
    }
}