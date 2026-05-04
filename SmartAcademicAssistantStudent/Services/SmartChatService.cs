using Microsoft.EntityFrameworkCore;
using SmartAcademicAssistantStudent.Data;
using SmartAcademicAssistantStudent.Models;

namespace SmartAcademicAssistantStudent.Services
{
    public class SmartChatService(AppDbContext context) : ISmartChatService
    {
        public async Task<string> GetResponseAsync(string message, int userId)
        {
            var msg = message.ToLower().Trim();

            // جلب بيانات الطالب
            var student = await context.Students
                .Include(s => s.User)
                .Include(s => s.Enrollments)
                    .ThenInclude(e => e.CourseSection)
                        .ThenInclude(cs => cs.Course)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student is null)
                return "عذراً، لم يتم العثور على بيانات الطالب.";

            // ─── تحية ─────────────────────────────────────────
            if (ContainsAny(msg, "مرحبا", "اهلا", "السلام", "هلا", "hello", "hi"))
                return $"أهلاً {student.User.Name}! 👋 كيف أقدر أساعدك؟\n" +
                       "اكتب 'مساعدة' لعرض كل الخيارات المتاحة.";

            // ─── مساعدة ───────────────────────────────────────
            if (ContainsAny(msg, "مساعدة", "ساعدني", "help", "خيارات", "ايش تقدر"))
                return """
                    أقدر أساعدك في التالي 📋:
                    1️⃣  معدلي  — لمعرفة معدلك الحالي
                    2️⃣  ساعاتي — لمعرفة ساعاتك المنجزة والمتبقية
                    3️⃣  موادي  — لعرض المواد المسجّل فيها
                    4️⃣  متاح   — لعرض المواد المتاحة الفصل الجاي
                    5️⃣  جدولي  — لعرض جدولك الدراسي
                    6️⃣  تخرج   — لمعرفة متى تتخرج
                    7️⃣  فاق    — للأسئلة الشائعة
                    """;

            // ─── المعدل ───────────────────────────────────────
            if (ContainsAny(msg, "معدل", "gpa", "درجاتي"))
                return GetGPAResponse(student);

            // ─── الساعات ──────────────────────────────────────
            if (ContainsAny(msg, "ساعات", "ساعاتي", "منجز", "انجزت"))
                return await GetHoursResponse(student);

            // ─── المواد المسجّل فيها ──────────────────────────
            if (ContainsAny(msg, "موادي", "مواد مسجل", "مسجل فيها", "دروسي"))
                return GetEnrolledCoursesResponse(student);

            // ─── المواد المتاحة الفصل الجاي ──────────────────
            if (ContainsAny(msg, "متاح", "فصل جاي", "اسجل", "تسجيل", "انزل"))
                return await GetEligibleCoursesResponse(student);

            // ─── الجدول الدراسي ───────────────────────────────
            if (ContainsAny(msg, "جدول", "جدولي", "وقت", "schedule"))
                return GetScheduleResponse(student);

            // ─── التخرج ───────────────────────────────────────
            if (ContainsAny(msg, "تخرج", "اتخرج", "متى تخرج", "باقي"))
                return await GetGraduationResponse(student);

            // ─── FAQ ──────────────────────────────────────────
            if (ContainsAny(msg, "فاق", "faq", "اسئلة", "شائعة"))
                return await GetFAQResponse(msg);

            // ─── البحث في FAQ تلقائياً ────────────────────────
            var faqResponse = await SearchFAQ(msg);
            if (faqResponse is not null)
                return faqResponse;

            // ─── ما عرف ───────────────────────────────────────
            return "عذراً، لم أفهم سؤالك 🤔\n" +
                   "اكتب 'مساعدة' لعرض كل الخيارات المتاحة.";
        }

        // ════════════════════════════════════════════════════════
        //  Responses
        // ════════════════════════════════════════════════════════

        private static string GetGPAResponse(Student student)
        {
            var status = student.GPA switch
            {
                >= 3.7 => "ممتاز 🌟",
                >= 3.0 => "جيد جداً ✅",
                >= 2.0 => "جيد 👍",
                _ => "بحاجة لتحسين ⚠️"
            };

            var maxHours = student.GPA >= 3.0 ? 18 : student.GPA >= 2.0 ? 15 : 12;

            return $"""
                📊 معدلك الحالي: {student.GPA} — {status}
                📚 الحد الأقصى للساعات هذا الفصل: {maxHours} ساعة
                """;
        }

        private async Task<string> GetHoursResponse(Student student)
        {
            var completedHours = student.Enrollments
                .Sum(e => e.CourseSection.Course.CreditHours);

            var totalRequired = await context.Courses
                .Where(c => c.Department == student.Major)
                .SumAsync(c => c.CreditHours);

            var remaining = totalRequired - completedHours;
            var percentage = totalRequired > 0
                ? (completedHours * 100 / totalRequired) : 0;

            return $"""
                📈 إحصائيات ساعاتك:
                ✅ منجزة  : {completedHours} ساعة
                📚 مطلوبة : {totalRequired} ساعة
                🎯 متبقية : {remaining} ساعة
                📊 النسبة : {percentage}%
                """;
        }

        private static string GetEnrolledCoursesResponse(Student student)
        {
            var courses = student.Enrollments
                .Select(e => e.CourseSection.Course)
                .Distinct()
                .ToList();

            if (!courses.Any())
                return "لا توجد مواد مسجّل فيها حالياً.";

            var list = string.Join("\n", courses
                .Select(c => $"• {c.Name} ({c.Code}) - {c.CreditHours} ساعة"));

            return $"📚 موادك المسجّلة ({courses.Count} مادة):\n{list}";
        }

        private async Task<string> GetEligibleCoursesResponse(Student student)
        {
            var completedIds = student.Enrollments
                .Select(e => e.CourseSection.CourseId)
                .Distinct().ToList();

            var allCourses = await context.Courses
                .Include(c => c.Prerequisites)
                .Where(c => !completedIds.Contains(c.Id))
                .ToListAsync();

            var eligible = allCourses
                .Where(c => c.Prerequisites
                    .All(p => completedIds.Contains(p.RequiredCourseId)))
                .ToList();

            if (!eligible.Any())
                return "⚠️ ما في مواد متاحة لك الفصل الجاي حالياً.";

            var maxHours = student.GPA >= 3.0 ? 18 :
                           student.GPA >= 2.0 ? 15 : 12;

            var list = string.Join("\n", eligible
                .Take(8)
                .Select(c => $"• {c.Name} ({c.Code}) - {c.CreditHours} ساعة"));

            return $"""
                🎓 المواد المتاحة لك الفصل الجاي:
                {list}

                ⏱️ تقدر تأخذ حد أقصى {maxHours} ساعة بناءً على معدلك.
                """;
        }

        private static string GetScheduleResponse(Student student)
        {
            var sections = student.Enrollments
                .Select(e => e.CourseSection)
                .ToList();

            if (!sections.Any())
                return "لا يوجد جدول دراسي حالياً.";

            var list = string.Join("\n", sections
                .Select(s => $"• {s.Course.Name} | {s.Time} | {s.Location}"));

            return $"🗓️ جدولك الدراسي:\n{list}";
        }

        private async Task<string> GetGraduationResponse(Student student)
        {
            var completedHours = student.Enrollments
                .Sum(e => e.CourseSection.Course.CreditHours);

            var totalRequired = await context.Courses
                .Where(c => c.Department == student.Major)
                .SumAsync(c => c.CreditHours);

            var remaining = totalRequired - completedHours;
            var maxHours = student.GPA >= 3.0 ? 18 :
                           student.GPA >= 2.0 ? 15 : 12;

            var semestersLeft = remaining > 0
                ? Math.Ceiling((double)remaining / maxHours) : 0;

            if (remaining <= 0)
                return "🎉 مبروك! أنهيت كل متطلبات التخرج!";

            return $"""
                🎓 معلومات التخرج:
                📚 ساعات متبقية  : {remaining} ساعة
                ⏱️ فصول متبقية   : {semestersLeft} فصل تقريباً
                📊 بناءً على معدلك تأخذ {maxHours} ساعة/فصل
                """;
        }

        private async Task<string> GetFAQResponse(string msg)
        {
            var faqs = await context.FAQs.ToListAsync();
            if (!faqs.Any())
                return "لا توجد أسئلة شائعة متاحة حالياً.";

            var list = string.Join("\n", faqs
                .Take(5)
                .Select((f, i) => $"{i + 1}. {f.Question}"));

            return $"❓ الأسئلة الشائعة:\n{list}\n\nاكتب السؤال كاملاً للحصول على الجواب.";
        }

        private async Task<string?> SearchFAQ(string message)
        {
            var faqs = await context.FAQs.ToListAsync();
            var words = message.Split(' ',
                StringSplitOptions.RemoveEmptyEntries);

            var match = faqs.FirstOrDefault(f =>
                words.Any(w => w.Length > 2 &&
                    f.Question.ToLower().Contains(w)));

            return match?.Answer;
        }

        // ════════════════════════════════════════════════════════
        //  Helper
        // ════════════════════════════════════════════════════════

        private static bool ContainsAny(string message, params string[] keywords)
            => keywords.Any(message.Contains);
    }
}