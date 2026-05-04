using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartAcademicAssistantStudent.Data;
using SmartAcademicAssistantStudent.Entities;
using SmartAcademicAssistantStudent.Models;
using SmartAcademicAssistantStudent.Services;
using System.Security.Claims;

namespace SmartAcademicAssistantStudent.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController(
        AppDbContext context,
        ISmartChatService chatService) : ControllerBase
    {
        private int GetUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // ─── 1. إرسال رسالة ───────────────────────────────────
        // POST /api/chat/send
        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] SendMessageDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest("الرسالة فارغة");

            var userId = GetUserId();

            // الرد من الـ ChatBot
            var response = await chatService.GetResponseAsync(
                request.Message, userId);

            // حفظ في قاعدة البيانات
            context.ChatMessages.Add(new ChatMessage
            {
                UserId = userId,
                Message = request.Message,
                Response = response,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            return Ok(new
            {
                message = request.Message,
                response
            });
        }

        // ─── 2. تاريخ المحادثة ────────────────────────────────
        // GET /api/chat/history
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var userId = GetUserId();

            var history = await context.ChatMessages
                .Where(x => x.UserId == userId)
                .OrderBy(x => x.CreatedAt)
                .Select(x => new
                {
                    x.Id,
                    x.Message,
                    x.Response,
                    x.CreatedAt
                })
                .ToListAsync();

            return Ok(history);
        }

        // ─── 3. حذف تاريخ المحادثة ───────────────────────────
        // DELETE /api/chat/history
        [HttpDelete("history")]
        public async Task<IActionResult> ClearHistory()
        {
            var userId = GetUserId();
            var messages = context.ChatMessages
                .Where(x => x.UserId == userId);
            context.ChatMessages.RemoveRange(messages);
            await context.SaveChangesAsync();
            return Ok("تم مسح تاريخ المحادثة");
        }
    }
}