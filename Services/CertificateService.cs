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
        private readonly ILogger<CertificateService> _logger;

        public CertificateService(AppDbContext context, IWebHostEnvironment env, ILogger<CertificateService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _env = env ?? throw new ArgumentNullException(nameof(env));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Certificate> IssueCertificateAsync(int userId, int courseId)
        {
            _logger.LogInformation("Checking eligibility for certificate for UserId {UserId}, CourseId {CourseId}", userId, courseId);

            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
            {
                _logger.LogWarning("Course with ID {CourseId} not found.", courseId);
                throw new ArgumentException("الدورة غير موجودة.");
            }

            var existingCertificate = await _context.Certificates
                .FirstOrDefaultAsync(c => c.StudentId == userId && c.CourseId == courseId);
            if (existingCertificate != null)
            {
                _logger.LogWarning("Certificate already issued for UserId {UserId}, CourseId {CourseId}", userId, courseId);
                throw new InvalidOperationException("الشهادة تم إصدارها مسبقًا.");
            }

            var isEligible = await IsEligibleForCertificateAsync(userId, courseId);
            if (!isEligible)
            {
                _logger.LogInformation("UserId {UserId} not eligible for certificate in CourseId {CourseId}", userId, courseId);
                throw new InvalidOperationException("المستخدم غير مؤهل للحصول على الشهادة.");
            }

            string fileName = GenerateCertificatePdf(userId, courseId);

            var certificate = new Certificate
            {
                StudentId = userId,
                CourseId = courseId,
                CertificateUrl = $"/certificates/{fileName}", // توحيد المسار
                IssuedDate = DateTime.Now
            };

            _context.Certificates.Add(certificate);

            // إضافة إشعار
            var notification = new Notification
            {
                UserId = userId,
                Message = $"تم إصدار شهادتك لدورة {course.Title} بنجاح!",
                CreatedAt = DateTime.Now,
                IsRead = false,
                InvoiceUrl = certificate.CertificateUrl
            };
            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();
            _logger.LogInformation("Certificate issued successfully for UserId {UserId}, CourseId {CourseId}", userId, courseId);
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
            var conditions = await _context.CertificateConditions
                .Where(cc => cc.CourseId == courseId)
                .AsNoTracking()
                .ToListAsync();
            if (!conditions.Any()) return false;

            var finalExam = await _context.FinalExams
                .Where(fe => fe.CourseId == courseId)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            var progress = await _context.StudentProgress
                .Where(sp => sp.StudentId == userId && sp.Lesson.CourseId == courseId)
                .AsNoTracking()
                .ToListAsync();

            var quizResults = await _context.QuizResults
                .Where(qr => qr.UserId == userId && qr.Lesson.CourseId == courseId)
                .AsNoTracking()
                .ToListAsync();

            var lessonCount = await _context.Lessons
                .CountAsync(l => l.CourseId == courseId);

            var completedLessons = progress.Count(sp => sp.ProgressPercentage >= 100); // استخدام StudentProgress بدلاً من QuizResults
            var completionPercentage = lessonCount > 0 ? (double)completedLessons / lessonCount * 100 : 0;
            var avgScore = quizResults.Any() ? quizResults.Average(qr => qr.Score) : 0;

            if (finalExam != null)
            {
                var result = await _context.FinalExamResults
                    .Where(fr => fr.UserId == userId && fr.FinalExamId == finalExam.FinalExamId)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();
                if (result == null || result.Score < 70) // افتراض نسبة نجاح 70%
                    return false;
            }

            foreach (var condition in conditions)
            {
                switch (condition.ConditionType)
                {
                    case "PassPercentage":
                        if (finalExam != null)
                        {
                            var result = await _context.FinalExamResults
                                .Where(fr => fr.UserId == userId && fr.FinalExamId == finalExam.FinalExamId)
                                .AsNoTracking()
                                .FirstOrDefaultAsync();
                            if (result == null || result.Score < (double)condition.Value)
                                return false;
                        }
                        else if (avgScore < (double)condition.Value)
                            return false;
                        break;

                    case "MaxAttempts":
                        var totalAttempts = quizResults.Sum(qr => qr.Attempts ?? 0); // افتراض أن Attempts قد تكون nullable
                        if (totalAttempts > (int)condition.Value)
                            return false;
                        break;

                    case "CompletionPercentage":
                        if (completionPercentage < (double)condition.Value)
                            return false;
                        break;
                }
            }
            return true;
        }

        private string GenerateCertificatePdf(int userId, int courseId)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var user = _context.Users.Find(userId);
            var course = _context.Courses.Find(userId); // تصحيح: يجب أن يكون courseId
            if (user == null || course == null)
            {
                _logger.LogWarning("UserId {UserId} or CourseId {CourseId} not found for certificate generation.", userId, courseId);
                throw new ArgumentException("المستخدم أو الدورة غير موجودين.");
            }

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
                        .Text("منصة عمر التعليمية")
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
                                .Text($"لإكمال دورة: {course.Title}")
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
            return fileName; // إرجاع اسم الملف فقط، المسار يُضاف في CertificateUrl
        }
    }
}