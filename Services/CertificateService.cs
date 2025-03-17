using EducationPlatformN.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;

namespace EducationPlatformN.Services
{
    public class CertificateService : ICertificateService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public CertificateService(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _env = env ?? throw new ArgumentNullException(nameof(env));
        }

        public async Task<Certificate> IssueCertificateAsync(int userId, int courseId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
                throw new ArgumentException("الدورة غير موجودة.");

            var isEligible = await IsEligibleForCertificateAsync(userId, courseId);
            if (!isEligible)
                throw new InvalidOperationException("المستخدم غير مؤهل للحصول على الشهادة.");

            // إنشاء ملف PDF والحصول على اسم الملف
            string fileName = GenerateCertificatePdf(userId, courseId);

            var certificate = new Certificate
            {
                StudentId = userId,
                CourseId = courseId,
                CertificateUrl = fileName, // تخزين اسم الملف فقط (مثل certificate_4_11_...)
                IssuedDate = DateTime.Now
            };

            _context.Certificates.Add(certificate);
            await _context.SaveChangesAsync();
            return certificate;
        }

        public async Task<List<Certificate>> GetUserCertificatesAsync(int userId)
        {
            return await _context.Certificates
                .Where(c => c.StudentId == userId)
                .Include(c => c.Course)
                .OrderByDescending(c => c.IssuedDate)
                .ToListAsync();
        }

        public async Task<bool> IsEligibleForCertificateAsync(int userId, int courseId)
        {
            var conditions = await _context.CertificateConditions.Where(cc => cc.CourseId == courseId).ToListAsync();
            if (!conditions.Any()) return false;

            foreach (var condition in conditions)
            {
                switch (condition.ConditionType)
                {
                    case "PassPercentage":
                        var finalExam = await _context.FinalExams.FirstOrDefaultAsync(fe => fe.CourseId == courseId);
                        if (finalExam != null)
                        {
                            var result = await _context.FinalExamResults
                                .FirstOrDefaultAsync(fr => fr.UserId == userId && fr.FinalExamId == finalExam.FinalExamId);
                            if (result == null || result.Score < (double)condition.Value) return false;
                        }
                        else
                        {
                            var quizResults = await _context.QuizResults
                                .Where(qr => qr.UserId == userId && qr.Lesson.CourseId == courseId)
                                .ToListAsync();
                            var avgScore = quizResults.Any() ? quizResults.Average(qr => qr.Score) : 0;
                            if (avgScore < (double)condition.Value) return false;
                        }
                        break;

                    case "MaxAttempts":
                        var totalAttempts = await _context.QuizResults
                            .CountAsync(qr => qr.UserId == userId && qr.Lesson.CourseId == courseId);
                        if (totalAttempts > (int)condition.Value) return false;
                        break;

                    case "CompletionPercentage":
                        var lessonCount = await _context.Lessons.CountAsync(l => l.CourseId == courseId);
                        var completedLessons = await _context.QuizResults
                            .CountAsync(qr => qr.UserId == userId && qr.Lesson.CourseId == courseId);
                        var completionPercentage = lessonCount > 0 ? (double)completedLessons / lessonCount * 100 : 0;
                        if (completionPercentage < (double)condition.Value) return false;
                        break;

                    default:
                        return false;
                }
            }
            return true;
        }

        private string GenerateCertificatePdf(int userId, int courseId)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var user = _context.Users.Find(userId);
            var course = _context.Courses.Find(courseId);
            var fileName = $"certificate_{userId}_{courseId}_{DateTime.Now.Ticks}.pdf";
            var path = Path.Combine(_env.WebRootPath, "certificates", fileName);

            if (!Directory.Exists(Path.Combine(_env.WebRootPath, "certificates")))
            {
                Directory.CreateDirectory(Path.Combine(_env.WebRootPath, "certificates"));
            }

            var document = Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text("منصة عمر")
                        .SemiBold().FontSize(24).FontColor(Colors.Blue.Medium)
                        .AlignCenter();

                    page.Content()
                        .PaddingVertical(2, Unit.Centimetre)
                        .Column(x =>
                        {
                            x.Item().Text("شهادة إتمام دورة")
                                .FontSize(20).Bold().FontColor(Colors.Black)
                                .AlignCenter();

                            x.Item().PaddingTop(1, Unit.Centimetre)
                                .Text($"تم منح هذه الشهادة لـ: {user.FullName}")
                                .FontSize(16).FontColor(Colors.Grey.Darken2)
                                .AlignCenter();

                            x.Item().PaddingTop(1, Unit.Centimetre)
                                .Text($"لإكمال دورة: {course.Title} بنجاح ")
                                .FontSize(16).FontColor(Colors.Grey.Darken2)
                                .AlignCenter();

                            x.Item().PaddingTop(1, Unit.Centimetre)
                                .Text($"بتاريخ: {DateTime.Now:dd/MM/yyyy}")
                                .FontSize(14).FontColor(Colors.Black)
                                .AlignCenter();

                            x.Item().PaddingTop(2, Unit.Centimetre)
                                .Text("توقيع المشرف: ____________________")
                                .FontSize(12).AlignCenter();
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text("جميع الحقوق محفوظة © 2025 منصة عمر")
                        .FontSize(10).FontColor(Colors.Grey.Lighten1);
                });
            });

            document.GeneratePdf(path);
            return fileName; // إرجاع اسم الملف فقط
        }
    }

    
}