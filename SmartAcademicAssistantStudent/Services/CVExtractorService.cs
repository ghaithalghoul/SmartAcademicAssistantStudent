using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.Text;
using System.Text.RegularExpressions;

namespace SmartAcademicAssistantStudent.Services
{
    public interface ICVExtractorService
    {
        Task<string> ExtractTextAsync(IFormFile file);
        CVFeatures ExtractFeatures(string text);
    }

    public class CVFeatures
    {
        public double GPA { get; set; }
        public int ExperienceYears { get; set; }
        public int SkillsCount { get; set; }
        public bool HasGitHub { get; set; }
        public int CertificationsCount { get; set; }
        public int ProjectsCount { get; set; }
        public bool HasInternship { get; set; }
        public int EnglishLevel { get; set; }
    }

    public class CVExtractorService : ICVExtractorService
    {
        // ─── Tech Skills ──────────────────────────────────────
        private static readonly string[] TechSkills =
        [
            "python", "java", "c#", "c++", "javascript", "typescript",
            "react", "angular", "vue", "node", "asp.net", "django",
            "sql", "mysql", "postgresql", "mongodb", "redis",
            "docker", "kubernetes", "aws", "azure", "git",
            "machine learning", "deep learning", "flutter",
            "swift", "kotlin", "php", "laravel", "spring",
            "html", "css", "bootstrap", "tailwind", "graphql",
            "rest api", "microservices", "linux", "tensorflow",
            "pytorch", "scikit", "pandas", "numpy", "express",
            "next.js", "nuxt", "firebase", "supabase", ".net",
        ];

        // ─── Section Headers ──────────────────────────────────
        private static readonly string[] ExperienceHeaders =
        [
            "experience", "work experience", "employment",
            "professional experience", "work history",
            "الخبرة", "خبرة العمل", "التجربة المهنية"
        ];

        private static readonly string[] ProjectHeaders =
        [
            "projects", "personal projects", "academic projects",
            "side projects", "portfolio", "key projects",
            "selected projects", "notable projects",
            "المشاريع", "مشاريع", "أعمال"
        ];

        private static readonly string[] CertHeaders =
        [
            "certifications", "certificates", "licenses",
            "courses", "training", "professional development",
            "شهادات", "دورات", "تدريب"
        ];

        private static readonly string[] InternshipKeywords =
        [
            "intern", "internship", "trainee", "training",
            "summer training", "industrial training",
            "تدريب", "متدرب", "تدريب صيفي"
        ];

        private static readonly string[] CertProviders =
        [
            "aws", "microsoft", "google", "cisco", "oracle",
            "comptia", "pmp", "coursera", "udemy", "edx",
            "linkedin", "ibm", "meta", "apple", "scrum",
        ];

        private static readonly string[] CertKeywords =
        [
            "certified", "certification", "certificate",
            "fundamentals", "associate", "professional",
            "practitioner", "expert", "developer",
            "شهادة", "اعتماد"
        ];

        // ════════════════════════════════════════════════════════
        //  Extract Text — PDF line-by-line
        // ════════════════════════════════════════════════════════

        public async Task<string> ExtractTextAsync(IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName).ToLower();

            if (ext != ".pdf" && ext != ".docx")
                throw new InvalidOperationException(
                    "يرجى رفع PDF أو Word فقط");

            if (file.Length > 10 * 1024 * 1024)
                throw new InvalidOperationException("الملف أكبر من 10MB");

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            return ext == ".pdf"
                ? ExtractFromPdf(stream)
                : ExtractFromDocx(stream);
        }

        // ─── PDF — مع ترتيب الأسطر حسب Y position ───────────
        private static string ExtractFromPdf(Stream stream)
        {
            var sb = new StringBuilder();

            using var pdf = PdfDocument.Open(stream);
            foreach (var page in pdf.GetPages())
            {
                // ترتيب الكلمات حسب الموضع (Y تنازلي، X تصاعدي)
                var words = page.GetWords()
                    .OrderByDescending(w => w.BoundingBox.Bottom)
                    .ThenBy(w => w.BoundingBox.Left)
                    .ToList();

                if (!words.Any())
                {
                    sb.AppendLine(page.Text);
                    continue;
                }

                double? lastY = null;
                double? lastX = null;
                var lineBuilder = new StringBuilder();

                foreach (var word in words)
                {
                    var y = Math.Round(word.BoundingBox.Bottom, 0);
                    var x = word.BoundingBox.Left;

                    if (lastY.HasValue && Math.Abs(y - lastY.Value) > 3)
                    {
                        // سطر جديد
                        var line = lineBuilder.ToString().Trim();
                        if (!string.IsNullOrWhiteSpace(line))
                            sb.AppendLine(line);
                        lineBuilder.Clear();
                    }
                    else if (lastX.HasValue && x - lastX.Value > 15)
                    {
                        lineBuilder.Append("  ");
                    }

                    lineBuilder.Append(word.Text + " ");
                    lastY = y;
                    lastX = word.BoundingBox.Right;
                }

                var lastLine = lineBuilder.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(lastLine))
                    sb.AppendLine(lastLine);
            }

            return sb.ToString().Trim();
        }

        // ─── Word ─────────────────────────────────────────────
        private static string ExtractFromDocx(Stream stream)
        {
            var sb = new StringBuilder();
            using var doc = WordprocessingDocument.Open(stream, false);
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body == null) return string.Empty;

            foreach (var para in body
                .Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
            {
                var text = para.InnerText.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                    sb.AppendLine(text);
            }
            return sb.ToString().Trim();
        }

        // ════════════════════════════════════════════════════════
        //  Extract Features
        // ════════════════════════════════════════════════════════

        public CVFeatures ExtractFeatures(string text)
        {
            var lower = text.ToLower();

            // نقسم النص لأسطر ونزيل الفارغة
            var lines = text
                .Split(new[] { '\n', '\r' },
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => l.Length > 1)
                .ToList();

            return new CVFeatures
            {
                GPA = ExtractGPA(lower, lines),
                ExperienceYears = ExtractExperience(lines, lower),
                SkillsCount = ExtractSkills(lower),
                HasGitHub = lower.Contains("github"),
                CertificationsCount = ExtractCertifications(lines, lower),
                ProjectsCount = ExtractProjects(lines, lower),
                HasInternship = DetectInternship(lines, lower),
                EnglishLevel = ExtractEnglish(lower),
            };
        }

        // ════════════════════════════════════════════════════════
        //  GPA
        // ════════════════════════════════════════════════════════

        private static double ExtractGPA(string lower, List<string> lines)
        {
            var patterns = new[]
            {
                @"gpa[\s:]*(\d+\.?\d*)",
                @"cgpa[\s:]*(\d+\.?\d*)",
                @"معدل[\s:]*(\d+\.?\d*)",
                @"(\d+\.\d+)\s*/\s*4(?:\.0)?",
                @"(\d+\.\d+)\s*out\s*of\s*4",
                @"grade\s*point[\s:]*(\d+\.?\d*)",
                @"cumulative[\s:]*(\d+\.?\d*)",
            };

            foreach (var p in patterns)
            {
                var m = Regex.Match(lower, p, RegexOptions.IgnoreCase);
                if (m.Success && double.TryParse(m.Groups[1].Value, out double g))
                {
                    if (g > 4.0 && g <= 100) g = Math.Round(g / 100 * 4, 2);
                    if (g > 0 && g <= 4.0) return g;
                }
            }
            return 0.0;
        }

        // ════════════════════════════════════════════════════════
        //  Experience — أذكى طريقة
        // ════════════════════════════════════════════════════════

        private static int ExtractExperience(List<string> lines, string lower)
        {
            // ─── طريقة 1: نص صريح "X years of experience" ────
            var explicitPatterns = new[]
            {
                @"(\d+)\+?\s*years?\s*of\s*(?:professional\s*)?experience",
                @"(\d+)\+?\s*سنوات?\s*(?:من\s*)?خبرة",
                @"experience\s*(?:of\s*)?(\d+)\+?\s*years?",
            };

            foreach (var p in explicitPatterns)
            {
                var m = Regex.Match(lower, p, RegexOptions.IgnoreCase);
                if (m.Success && int.TryParse(m.Groups[1].Value, out int y))
                    return Math.Min(y, 30);
            }

            // ─── طريقة 2: تواريخ داخل Experience section ─────
            bool inExp = false;
            var dateRanges = new List<(int start, int end)>();

            foreach (var line in lines)
            {
                var l = line.ToLower().Trim();

                if (IsHeader(l, ExperienceHeaders))
                {
                    inExp = true; continue;
                }
                if (inExp && IsNewMajorSection(l, ExperienceHeaders))
                {
                    inExp = false; continue;
                }

                if (inExp || lower.Contains("experience"))
                {
                    // "June 2023 - September 2023" أو "2022 - 2024" أو "2023 - Present"
                    var rangeMatch = Regex.Match(line,
                        @"(\b20\d{2}\b)[\s\-–—]+(\b20\d{2}\b|present|now|current|حالياً|الآن)",
                        RegexOptions.IgnoreCase);

                    if (rangeMatch.Success)
                    {
                        int.TryParse(rangeMatch.Groups[1].Value, out int start);
                        var endStr = rangeMatch.Groups[2].Value.ToLower();
                        int end = Regex.IsMatch(endStr, @"present|now|current|حالياً|الآن")
                            ? DateTime.Now.Year
                            : int.Parse(endStr);

                        if (start >= 2000 && end >= start)
                            dateRanges.Add((start, end));
                    }
                }
            }

            if (dateRanges.Any())
            {
                // جمع مدد الخبرة بدون تكرار
                int totalMonths = 0;
                foreach (var (s, e) in dateRanges)
                    totalMonths += (e - s) * 12;
                return Math.Min(totalMonths / 12, 20);
            }

            // ─── طريقة 3: أي تواريخ في النص ─────────────────
            var allYears = Regex.Matches(lower, @"\b(20\d{2})\b")
                .Select(m => int.Parse(m.Value))
                .Where(y => y >= 2000 && y <= DateTime.Now.Year)
                .Distinct().OrderBy(y => y).ToList();

            if (allYears.Count >= 2)
                return Math.Min(DateTime.Now.Year - allYears.First(), 15);

            return 0;
        }

        // ════════════════════════════════════════════════════════
        //  Projects — متعدد الطرق
        // ════════════════════════════════════════════════════════

        private static int ExtractProjects(List<string> lines, string lower)
        {
            var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // ─── طريقة 1: Section المشاريع ────────────────────
            bool inSection = false;

            foreach (var line in lines)
            {
                var l = line.ToLower().Trim();

                if (IsHeader(l, ProjectHeaders))
                {
                    inSection = true; continue;
                }

                if (inSection && IsNewMajorSection(l, ProjectHeaders))
                {
                    inSection = false; continue;
                }

                if (inSection)
                {
                    // أي سطر غير فارغ وغير تاريخ وغير وصف قصير = اسم مشروع
                    var clean = Regex.Replace(line, @"^[•\-\*\d\.\)]\s*", "").Trim();

                    if (clean.Length >= 4 &&
                        !Regex.IsMatch(clean, @"^\d{4}") &&
                        !IsDateLine(clean) &&
                        !IsBulletDescription(clean))
                    {
                        found.Add(clean[..Math.Min(clean.Length, 60)]);
                    }
                }
            }

            // ─── طريقة 2: Regex أنماط أسماء المشاريع ─────────
            var projectPatterns = new[]
            {
                // "Project Name | Tech" أو "Project Name - Tech"
                @"([A-Z][a-zA-Z\s]{4,50})\s*[|\-–]\s*(?:React|Node|Python|C#|Java|Django|Laravel|ASP|Angular|Vue|Flutter|Spring|Express)",
                // Bullet point + Capital letter
                @"(?:^|\n)[•\-\*]\s+([A-Z][a-zA-Z\s]{4,50}(?:System|App|Platform|API|Website|Portal|Tool|Bot|Assistant|Manager|Tracker))",
                // "Developed/Built/Created X"
                @"(?:developed|built|created|designed|implemented|engineered)\s+(?:a\s+|an\s+)?([A-Z][^\n,\.]{4,60})",
            };

            foreach (var p in projectPatterns)
            {
                var matches = Regex.Matches(lower.Length > 0 ?
                    string.Join("\n", lines) : "", p,
                    RegexOptions.Multiline | RegexOptions.IgnoreCase);

                foreach (Match m in matches)
                {
                    var name = m.Groups[1].Value.Trim();
                    if (name.Length >= 4 && name.Length < 80)
                        found.Add(name[..Math.Min(name.Length, 60)]);
                }
            }

            // ─── طريقة 3: عدّ Bullet Points في Section ────────
            if (found.Count == 0)
            {
                int bullets = 0;
                inSection = false;

                foreach (var line in lines)
                {
                    var l = line.ToLower();
                    if (IsHeader(l, ProjectHeaders)) { inSection = true; continue; }
                    if (inSection && IsNewMajorSection(l, ProjectHeaders)) break;

                    if (inSection && IsBulletLine(line))
                        bullets++;
                }
                return Math.Min(bullets, 15);
            }

            return Math.Min(found.Count, 15);
        }

        // ════════════════════════════════════════════════════════
        //  Certifications
        // ════════════════════════════════════════════════════════

        private static int ExtractCertifications(List<string> lines, string lower)
        {
            var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool inSection = false;

            // ─── طريقة 1: Section الشهادات ────────────────────
            foreach (var line in lines)
            {
                var l = line.ToLower().Trim();

                if (IsHeader(l, CertHeaders))
                {
                    inSection = true; continue;
                }

                if (inSection && IsNewMajorSection(l, CertHeaders))
                {
                    inSection = false; continue;
                }

                if (inSection)
                {
                    var clean = Regex.Replace(line, @"^[•\-\*\d\.\)]\s*", "").Trim();
                    if (clean.Length >= 4 && !IsDateLine(clean))
                        found.Add(clean[..Math.Min(clean.Length, 100)]);
                }
            }

            // ─── طريقة 2: أسماء شهادات مشهورة في كل النص ─────
            var certPatterns = new[]
            {
                // "AWS Certified Solutions Architect"
                $@"({string.Join("|", CertProviders.Select(Regex.Escape))})\s+[^\n]{{5,60}}",
                // "X Certificate" أو "Certified in X"
                @"([A-Z][a-zA-Z\s]{3,50}(?:Certificate|Certification|Certified))",
                @"(Certified\s+[A-Z][a-zA-Z\s]{3,50})",
                // شهادة بالعربي
                @"(شهادة\s+[^\n]{3,50})",
            };

            foreach (var p in certPatterns)
            {
                var matches = Regex.Matches(
                    string.Join("\n", lines), p,
                    RegexOptions.IgnoreCase | RegexOptions.Multiline);

                foreach (Match m in matches)
                {
                    var name = m.Groups[1].Value.Trim();
                    if (name.Length >= 5 && name.Length < 100)
                        found.Add(name[..Math.Min(name.Length, 100)]);
                }
            }

            // ─── طريقة 3: Bullet Points في Section ───────────
            if (found.Count == 0)
            {
                int bullets = 0;
                inSection = false;

                foreach (var line in lines)
                {
                    var l = line.ToLower();
                    if (IsHeader(l, CertHeaders)) { inSection = true; continue; }
                    if (inSection && IsNewMajorSection(l, CertHeaders)) break;
                    if (inSection && IsBulletLine(line)) bullets++;
                }
                return Math.Min(bullets, 10);
            }

            return Math.Min(found.Count, 10);
        }

        // ════════════════════════════════════════════════════════
        //  Internship — يشمل كل أنواع التدريب
        // ════════════════════════════════════════════════════════

        private static bool DetectInternship(List<string> lines, string lower)
        {
            // كلمات مباشرة
            if (InternshipKeywords.Any(k => lower.Contains(k)))
                return true;

            // "Junior Developer" أو "Part-time" تحتها تاريخ
            var juniorPatterns = new[]
            {
                @"junior\s+\w+",
                @"part.?time",
                @"volunteer",
                @"freelance",
                @"student\s+worker",
            };

            return juniorPatterns.Any(p =>
                Regex.IsMatch(lower, p, RegexOptions.IgnoreCase));
        }

        // ════════════════════════════════════════════════════════
        //  Skills
        // ════════════════════════════════════════════════════════

        private static int ExtractSkills(string lower)
            => TechSkills.Count(skill => lower.Contains(skill));

        // ════════════════════════════════════════════════════════
        //  English Level
        // ════════════════════════════════════════════════════════

        private static int ExtractEnglish(string lower)
        {
            if (Regex.IsMatch(lower,
                @"native|fluent|c[12]|proficient|bilingual|full\s*professional",
                RegexOptions.IgnoreCase))
                return 3;

            if (Regex.IsMatch(lower,
                @"advanced|upper.intermediate|b2|ielts|toefl|toeic|professional\s*working",
                RegexOptions.IgnoreCase))
                return 2;

            if (Regex.IsMatch(lower,
                @"english|intermediate|b1|good\s*command|conversational",
                RegexOptions.IgnoreCase))
                return 1;

            return 1;
        }

        // ════════════════════════════════════════════════════════
        //  Helpers
        // ════════════════════════════════════════════════════════

        private static bool IsHeader(string line, string[] headers)
        {
            var clean = line.Trim().ToLower()
                .TrimEnd(':', ' ', '-', '_');

            return headers.Any(h =>
                clean == h ||
                clean.StartsWith(h + " ") ||
                clean.StartsWith(h + ":") ||
                Regex.IsMatch(clean, $@"^{Regex.Escape(h)}\s*$"));
        }

        private static readonly string[] AllSectionHeaders =
        [
            "experience", "work experience", "education", "skills",
            "projects", "certifications", "certificates", "languages",
            "summary", "objective", "references", "contact",
            "awards", "publications", "volunteer", "interests",
            "courses", "training", "achievements", "activities",
            "خبرة", "تعليم", "مهارات", "مشاريع", "شهادات", "لغات"
        ];

        private static bool IsNewMajorSection(string line, string[] currentHeaders)
        {
            var clean = line.Trim().ToLower().TrimEnd(':', ' ');
            return AllSectionHeaders
                .Except(currentHeaders)
                .Any(h => clean == h ||
                          clean.StartsWith(h + ":") ||
                          Regex.IsMatch(clean, $@"^{Regex.Escape(h)}\s*$"));
        }

        private static bool IsDateLine(string line) =>
            Regex.IsMatch(line,
                @"\b(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec|20\d{2}|19\d{2})\b",
                RegexOptions.IgnoreCase);

        private static bool IsBulletLine(string line) =>
            Regex.IsMatch(line.Trim(), @"^[•\-\*\>\►\d\.\)▪▸◦]");

        private static bool IsBulletDescription(string line)
        {
            var lower = line.ToLower();
            var descWords = new[]
            {
                "responsible", "developed", "worked", "collaborated",
                "managed", "created", "designed", "implemented",
                "using", "with", "and", "the", "for", "in"
            };
            var words = lower.Split(' ');
            return words.Length > 8 ||
                   descWords.Any(w => lower.StartsWith(w));
        }
    }
}