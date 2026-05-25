using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartAcademicAssistantStudent.Data;
using SmartAcademicAssistantStudent.Entities;
using SmartAcademicAssistantStudent.Models;

namespace SmartAcademicAssistantStudent.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InstructorController(AppDbContext context) : ControllerBase
    {
        // ─── 1. كل الأساتذة ───────────────────────────────────────
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

        // ─── 3. إضافة أستاذ — Admin فقط ──────────────────────────
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Add([FromBody] AddInstructorDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Name) ||
                string.IsNullOrWhiteSpace(request.Department))
                return BadRequest("الاسم والقسم مطلوبان");

            var instructor = new Instructor
            {
                Name = request.Name,
                Department = request.Department
            };

            context.Instructors.Add(instructor);
            await context.SaveChangesAsync();

            return Ok(new
            {
                instructor.Id,
                instructor.Name,
                instructor.Department
            });
        }

        // ─── 4. تعديل أستاذ — Admin فقط ──────────────────────────
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(
            int id, [FromBody] AddInstructorDto request)
        {
            var instructor = await context.Instructors.FindAsync(id);
            if (instructor is null)
                return NotFound("الأستاذ غير موجود");

            if (!string.IsNullOrWhiteSpace(request.Name))
                instructor.Name = request.Name;

            if (!string.IsNullOrWhiteSpace(request.Department))
                instructor.Department = request.Department;

            await context.SaveChangesAsync();

            return Ok(new
            {
                instructor.Id,
                instructor.Name,
                instructor.Department
            });
        }

        // ─── 5. حذف أستاذ — Admin فقط ────────────────────────────
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var instructor = await context.Instructors.FindAsync(id);
            if (instructor is null)
                return NotFound("الأستاذ غير موجود");

            var hasSections = await context.CourseSections
                .AnyAsync(s => s.InstructorId == id);

            if (hasSections)
                return BadRequest(
                    "لا يمكن حذف الأستاذ لأن لديه شعب مرتبطة به");

            context.Instructors.Remove(instructor);
            await context.SaveChangesAsync();
            return Ok("تم حذف الأستاذ بنجاح");
        }
    }
}