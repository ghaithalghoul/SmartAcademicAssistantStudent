using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartAcademicAssistantStudent.Data;

namespace SmartAcademicAssistantStudent.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InstructorController(AppDbContext context) : ControllerBase
    {
        // ─── 1. كل الأساتذة ───────────────────────────────────────
        // GET /api/instructor
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? department)
        {
            var query = context.Instructors
                .Include(i => i.Sections)
                    .ThenInclude(s => s.Course)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(department))
                query = query.Where(i => i.Department == department);

            var instructors = await query
                .Select(i => new
                {
                    i.Id,
                    i.Name,
                    i.Department,
                    CoursesCount = i.Sections
                        .Select(s => s.CourseId)
                        .Distinct()
                        .Count()
                })
                .ToListAsync();

            return Ok(instructors);
        }

        // ─── 2. تفاصيل أستاذ ──────────────────────────────────────
        // GET /api/instructor/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var instructor = await context.Instructors
                .Include(i => i.Sections)
                    .ThenInclude(s => s.Course)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (instructor is null)
                return NotFound("الأستاذ غير موجود");

            return Ok(new
            {
                instructor.Id,
                instructor.Name,
                instructor.Department,
                Sections = instructor.Sections.Select(s => new
                {
                    s.Id,
                    s.SectionNumber,
                    s.Time,
                    s.Location,
                    Course = new
                    {
                        s.Course.Id,
                        s.Course.Name,
                        s.Course.Code,
                        s.Course.CreditHours
                    }
                })
            });
        }
    }
}