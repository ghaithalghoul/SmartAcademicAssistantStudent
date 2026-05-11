namespace SmartAcademicAssistantStudent.Services
{
    public interface ICVEvaluationService
    {
        Task<CVEvaluationResult> EvaluateAsync(IFormFile file, int userId);
    }

    public class CVEvaluationResult
    {
        public string Result { get; set; } = string.Empty;
        public float Probability { get; set; }
        public int Score { get; set; }
        public List<string> Strengths { get; set; } = [];
        public List<string> Weaknesses { get; set; } = [];
        public List<string> Suggestions { get; set; } = [];
        public List<string> ImprovementPriority { get; set; } = [];
        public CVFeatures Features { get; set; } = new();
    }

    public class CVEvaluationService(
        ICVExtractorService extractor,
        ICVMLService mlService) : ICVEvaluationService
    {
        public async Task<CVEvaluationResult> EvaluateAsync(
            IFormFile file, int userId)
        {
            var text = await extractor.ExtractTextAsync(file);
            var features = extractor.ExtractFeatures(text);
            var score = CalculateScore(features);
            var probability = CalculateProbability(score);

            return new CVEvaluationResult
            {
                Result = score switch
                {
                    >= 75 => "مقبول بقوة ✅",
                    >= 60 => "مقبول ✅",
                    >= 45 => "مقبول مشروط ⚠️",
                    _ => "مرفوض ❌"
                },
                Probability = probability,
                Score = score,
                Strengths = GetStrengths(features),
                Weaknesses = GetWeaknesses(features),
                Suggestions = GetSuggestions(features),
                ImprovementPriority = GetImprovementPriority(features),
                Features = features
            };
        }

        // ─── Probability من Score ─────────────────────────────
        private static float CalculateProbability(int score)
        {
            float probability = score switch
            {
                >= 80 => 85 + (score - 80) * 0.5f,
                >= 60 => 60 + (score - 60) * 1.25f,
                >= 40 => 30 + (score - 40) * 1.5f,
                _ => score * 0.75f
            };
            return (float)Math.Min(Math.Round(probability, 1), 95.0);
        }

        // ─── نقاط القوة ───────────────────────────────────────
        private static List<string> GetStrengths(CVFeatures f)
        {
            var strengths = new List<string>();

            if (f.GPA >= 3.7)
                strengths.Add($"معدل ممتاز ({f.GPA}) 🌟");
            else if (f.GPA >= 3.0)
                strengths.Add($"معدل جيد جداً ({f.GPA}) ✅");
            else if (f.GPA >= 2.5)
                strengths.Add($"معدل مقبول ({f.GPA})");

            if (f.ExperienceYears >= 3)
                strengths.Add($"خبرة عملية ممتازة ({f.ExperienceYears} سنوات) 💼");
            else if (f.ExperienceYears >= 1)
                strengths.Add($"خبرة عملية ({f.ExperienceYears} سنة)");

            if (f.SkillsCount >= 10)
                strengths.Add($"مهارات تقنية متميزة ({f.SkillsCount} مهارة) 💻");
            else if (f.SkillsCount >= 6)
                strengths.Add($"مهارات تقنية جيدة ({f.SkillsCount} مهارة)");

            if (f.HasGitHub)
                strengths.Add("حساب GitHub نشط 🐙");

            if (f.CertificationsCount >= 3)
                strengths.Add($"شهادات احترافية متعددة ({f.CertificationsCount}) 🏆");
            else if (f.CertificationsCount >= 1)
                strengths.Add($"شهادة احترافية ({f.CertificationsCount})");

            if (f.ProjectsCount >= 5)
                strengths.Add($"مشاريع عملية كثيرة ({f.ProjectsCount}) 🚀");
            else if (f.ProjectsCount >= 2)
                strengths.Add($"مشاريع عملية ({f.ProjectsCount})");

            if (f.HasInternship)
                strengths.Add("تدريب صيفي ✅");

            if (f.EnglishLevel == 3)
                strengths.Add("مستوى إنجليزي متقدم 🌍");
            else if (f.EnglishLevel == 2)
                strengths.Add("مستوى إنجليزي جيد");

            return strengths;
        }

        // ─── نقاط الضعف ───────────────────────────────────────
        private static List<string> GetWeaknesses(CVFeatures f)
        {
            var weaknesses = new List<string>();

            if (f.GPA > 0 && f.GPA < 2.5)
                weaknesses.Add($"المعدل الأكاديمي منخفض ({f.GPA})");
            else if (f.GPA == 0)
                weaknesses.Add("المعدل الأكاديمي غير مذكور في الـ CV");

            if (f.ExperienceYears == 0 && !f.HasInternship)
                weaknesses.Add("لا توجد خبرة عملية أو تدريب صيفي");
            else if (f.ExperienceYears == 0 && f.HasInternship)
                weaknesses.Add("لا توجد خبرة عملية رسمية — يوجد تدريب فقط");

            if (f.SkillsCount < 3)
                weaknesses.Add("مهارات تقنية محدودة جداً");
            else if (f.SkillsCount < 6)
                weaknesses.Add("مهارات تقنية أقل من المطلوب");

            if (!f.HasGitHub)
                weaknesses.Add("لا يوجد حساب GitHub أو Portfolio");

            if (f.CertificationsCount == 0)
                weaknesses.Add("لا توجد شهادات احترافية");

            if (f.ProjectsCount == 0)
                weaknesses.Add("لا توجد مشاريع عملية مذكورة");

            if (f.EnglishLevel == 1)
                weaknesses.Add("مستوى الإنجليزي ضعيف أو غير مذكور");

            return weaknesses;
        }

        // ─── اقتراحات ─────────────────────────────────────────
        private static List<string> GetSuggestions(CVFeatures f)
        {
            var suggestions = new List<string>();

            if (f.GPA < 3.0 && f.GPA > 0)
                suggestions.Add("حاول تحسين معدلك في الفصول القادمة");

            if (!f.HasGitHub)
                suggestions.Add("أنشئ حساب GitHub وارفع مشاريعك عليه");

            if (f.SkillsCount < 6)
                suggestions.Add("أضف مهارات تقنية — تعلم Framework أو لغة جديدة");

            if (f.CertificationsCount == 0)
                suggestions.Add("احصل على شهادة احترافية مثل AWS أو Microsoft");

            if (f.ProjectsCount < 3)
                suggestions.Add("أضف مشاريع عملية لـ CV");

            if (!f.HasInternship && f.ExperienceYears == 0)
                suggestions.Add("ابحث عن فرصة تدريب صيفي");

            if (f.EnglishLevel < 2)
                suggestions.Add("حسّن مستوى الإنجليزي — خذ دورة أو شهادة IELTS");

            return suggestions;
        }

        // ─── أولويات التحسين ──────────────────────────────────
        private static List<string> GetImprovementPriority(CVFeatures f)
        {
            var priorities = new List<(int Weight, string Action)>();

            if (!f.HasGitHub)
                priorities.Add((30,
                    "إنشاء GitHub Portfolio ورفع مشاريعك عليه"));

            if (f.ExperienceYears == 0 && !f.HasInternship)
                priorities.Add((28,
                    "التقدم لتدريب صيفي في شركة تقنية"));
            else if (f.ExperienceYears == 0 && f.HasInternship)
                priorities.Add((20,
                    "البحث عن فرصة عمل جزء من وقت (Part-time)"));

            if (f.SkillsCount < 4)
                priorities.Add((25,
                    "تعلم مهارات تقنية جديدة (Framework أو لغة برمجة)"));
            else if (f.SkillsCount < 7)
                priorities.Add((15,
                    "توسيع قائمة المهارات التقنية"));

            if (f.CertificationsCount == 0)
                priorities.Add((22,
                    "الحصول على شهادة احترافية (AWS / Microsoft / Google)"));
            else if (f.CertificationsCount == 1)
                priorities.Add((12,
                    "إضافة شهادة احترافية ثانية"));

            if (f.ProjectsCount == 0)
                priorities.Add((20,
                    "بناء مشروع عملي وإضافته للـ CV"));
            else if (f.ProjectsCount < 3)
                priorities.Add((10,
                    "إضافة مشاريع عملية أكثر للـ CV"));

            if (f.GPA > 0 && f.GPA < 2.5)
                priorities.Add((18,
                    "تحسين المعدل الأكاديمي في الفصول القادمة"));
            else if (f.GPA == 0)
                priorities.Add((15,
                    "إضافة المعدل الأكاديمي بوضوح في الـ CV"));

            if (f.EnglishLevel == 1)
                priorities.Add((16,
                    "تحسين مستوى الإنجليزي والحصول على شهادة IELTS أو TOEFL"));
            else if (f.EnglishLevel == 2)
                priorities.Add((8,
                    "رفع مستوى الإنجليزي من B2 إلى C1"));

            return priorities
                .OrderByDescending(p => p.Weight)
                .Take(5)
                .Select(p => p.Action)
                .ToList();
        }

        // ─── حساب النقاط ──────────────────────────────────────
        private static int CalculateScore(CVFeatures f)
        {
            var score = 0;

            score += f.GPA switch
            {
                >= 3.7 => 25,
                >= 3.5 => 22,
                >= 3.0 => 18,
                >= 2.5 => 12,
                >= 2.0 => 7,
                > 0 => 3,
                _ => 0
            };

            score += f.ExperienceYears switch
            {
                >= 4 => 20,
                >= 2 => 15,
                >= 1 => 10,
                _ => 0
            };

            score += Math.Min(f.SkillsCount * 2, 15);

            if (f.HasGitHub) score += 10;

            score += Math.Min(f.CertificationsCount * 4, 10);

            score += Math.Min(f.ProjectsCount * 2, 10);

            if (f.HasInternship) score += 5;

            score += f.EnglishLevel switch
            {
                3 => 5,
                2 => 3,
                _ => 1
            };

            return Math.Min(score, 100);
        }
    }
}