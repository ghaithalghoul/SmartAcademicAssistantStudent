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
    public class FAQController(AppDbContext context) : ControllerBase
    {
        // ─── 1. عرض كل الأسئلة — للجميع ─────────────────────────
        // GET /api/faq
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var faqs = await context.FAQs
                .OrderBy(f => f.Id)
                .Select(f => new
                {
                    f.Id,
                    f.Question,
                    f.Answer
                })
                .ToListAsync();

            return Ok(faqs);
        }

        // ─── 2. إضافة سؤال — Admin فقط ──────────────────────────
        // POST /api/faq
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Add([FromBody] AddFaqDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Question) ||
                string.IsNullOrWhiteSpace(request.Answer))
                return BadRequest("السؤال والجواب مطلوبان");

            var faq = new FAQ
            {
                Question = request.Question,
                Answer = request.Answer
            };

            context.FAQs.Add(faq);
            await context.SaveChangesAsync();

            return Ok(new { faq.Id, faq.Question, faq.Answer });
        }

        // ─── 3. تعديل سؤال — Admin فقط ──────────────────────────
        // PUT /api/faq/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] AddFaqDto request)
        {
            var faq = await context.FAQs.FindAsync(id);
            if (faq is null)
                return NotFound("السؤال غير موجود");

            if (!string.IsNullOrWhiteSpace(request.Question))
                faq.Question = request.Question;

            if (!string.IsNullOrWhiteSpace(request.Answer))
                faq.Answer = request.Answer;

            await context.SaveChangesAsync();
            return Ok(new { faq.Id, faq.Question, faq.Answer });
        }

        // ─── 4. حذف سؤال — Admin فقط ────────────────────────────
        // DELETE /api/faq/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var faq = await context.FAQs.FindAsync(id);
            if (faq is null)
                return NotFound("السؤال غير موجود");

            context.FAQs.Remove(faq);
            await context.SaveChangesAsync();
            return Ok("تم حذف السؤال");
        }
    }
}