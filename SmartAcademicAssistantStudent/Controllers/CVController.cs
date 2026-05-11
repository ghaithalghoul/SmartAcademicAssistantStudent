using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartAcademicAssistantStudent.Data;
using SmartAcademicAssistantStudent.Models;
using SmartAcademicAssistantStudent.Services;
using System.Security.Claims;

namespace SmartAcademicAssistantStudent.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CVController(
        AppDbContext context,
        ICVEvaluationService evaluationService) : ControllerBase
    {
        private int GetUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // ─── رفع وتقييم CV ────────────────────────────────────
        // POST /api/cv/evaluate
        [HttpPost("evaluate")]
        public async Task<IActionResult> Evaluate(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("لم يتم رفع أي ملف");

            var userId = GetUserId();

            CVEvaluationResult result;
            try
            {
                result = await evaluationService.EvaluateAsync(file, userId);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }

            // حفظ في قاعدة البيانات
            var evaluation = new CVEvaluation
            {
                UserId = userId,
                FileName = file.FileName,
                GPA = result.Features.GPA,
                ExperienceYears = result.Features.ExperienceYears,
                SkillsCount = result.Features.SkillsCount,
                HasGitHub = result.Features.HasGitHub,
                CertificationsCount = result.Features.CertificationsCount,
                ProjectsCount = result.Features.ProjectsCount,
                HasInternship = result.Features.HasInternship,
                EnglishLevel = result.Features.EnglishLevel,
                Result = result.Result,
                AcceptanceProbability = result.Probability,
                Strengths = string.Join("|", result.Strengths),
                Weaknesses = string.Join("|", result.Weaknesses),
                CreatedAt = DateTime.UtcNow
            };

            context.CVEvaluations.Add(evaluation);
            await context.SaveChangesAsync();

            return Ok(new
            {
                fileName = file.FileName,
                result = result.Result,
                score = result.Score,
                acceptanceProbability = $"{result.Probability:F1}%",
                strengths = result.Strengths,
                weaknesses = result.Weaknesses,
                suggestions = result.Suggestions,
                improvementPriority = result.ImprovementPriority,
                features = new
                {
                    result.Features.GPA,
                    result.Features.ExperienceYears,
                    result.Features.SkillsCount,
                    result.Features.HasGitHub,
                    result.Features.CertificationsCount,
                    result.Features.ProjectsCount,
                    result.Features.HasInternship,
                    result.Features.EnglishLevel
                }
            });
        }

        // ─── سجل التقييمات ────────────────────────────────────
        // GET /api/cv/history
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var userId = GetUserId();

            var history = await context.CVEvaluations
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.Id,
                    c.FileName,
                    c.Result,
                    c.AcceptanceProbability,
                    c.CreatedAt
                })
                .ToListAsync();

            return Ok(history);
        }
    }
}