using EducationPlatformN.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace EducationPlatformN.Controllers
{
    public class QuizController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public QuizController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _env = env ?? throw new ArgumentNullException(nameof(env));
        }

        [HttpGet]
        public IActionResult TakeQuiz(int lessonId)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var questions = _context.Questions
                .Where(q => q.LessonId == lessonId)
                .ToList();

            if (!questions.Any())
            {
                return NotFound("لا توجد أسئلة لهذا الدرس.");
            }

            ViewBag.LessonId = lessonId;
            return View(questions);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitQuiz(int lessonId, Dictionary<int, string> answers)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = HttpContext.Session.GetInt32("UserId").Value;
            var questions = _context.Questions.Where(q => q.LessonId == lessonId).ToList();

            // تسجيل الإجابات
            foreach (var answer in answers)
            {
                var question = questions.FirstOrDefault(q => q.QuestionId == answer.Key);
                if (question != null)
                {
                    _context.UserAnswers.Add(new UserAnswer
                    {
                        UserId = userId,
                        QuestionId = answer.Key,
                        SelectedAnswer = answer.Value,
                        AnsweredAt = DateTime.Now
                    });
                }
            }
            _context.SaveChanges();

            // حساب الدرجة بناءً على نوع السؤال
            var userAnswers = _context.UserAnswers
                .Include(ua => ua.Question)
                .Where(ua => ua.UserId == userId && questions.Select(q => q.QuestionId).Contains(ua.QuestionId))
                .ToList();

            int correctAnswers = 0;
            foreach (var ua in userAnswers)
            {
                switch (ua.Question.QuestionType)
                {
                    case "MultipleChoice":
                    case "TrueFalse":
                        if (ua.SelectedAnswer == ua.Question.CorrectAnswer)
                            correctAnswers++;
                        break;
                    case "Text":
                        if (ua.SelectedAnswer.Trim().ToLower() == ua.Question.CorrectAnswer.Trim().ToLower())
                            correctAnswers++;
                        break;
                }
            }

            var score = (double)correctAnswers / questions.Count * 100;

            // تسجيل النتيجة
            var quizResult = new QuizResult
            {
                UserId = userId,
                LessonId = lessonId,
                Score = score,
                CompletionDate = DateTime.Now,
                AnswersJson = JsonConvert.SerializeObject(answers)
            };
            _context.QuizResults.Add(quizResult);
            _context.SaveChanges();



            return RedirectToAction("QuizResults", new { lessonId });
        }

        private bool CheckQuizSuccess(int userId, int lessonId)
        {
            var questions = _context.Questions.Where(q => q.LessonId == lessonId).ToList();
            var userAnswers = _context.UserAnswers
                .Where(ua => ua.UserId == userId && questions.Select(q => q.QuestionId).Contains(ua.QuestionId))
                .ToList();

            return userAnswers.Count(ua => ua.SelectedAnswer == ua.Question.CorrectAnswer) >= questions.Count * 0.7;
        }
        [HttpGet]
        public IActionResult QuizResults(int lessonId)
        {
            if (HttpContext.Session.GetInt32("UserId") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = HttpContext.Session.GetInt32("UserId").Value;
            var quizResult = _context.QuizResults
                .Include(qr => qr.Lesson)
                .ThenInclude(l => l.Questions)
                .FirstOrDefault(qr => qr.UserId == userId && qr.LessonId == lessonId);

            if (quizResult == null)
            {
                return NotFound("لم يتم العثور على نتيجة اختبار لهذا الدرس.");
            }

            var userAnswers = _context.UserAnswers
                .Include(ua => ua.Question)
                .Where(ua => ua.UserId == userId && ua.Question.LessonId == lessonId)
                .ToList();

            var viewModel = new QuizResultViewModel
            {
                LessonTitle = quizResult.Lesson.Title,
                Score = quizResult.Score,
                CompletionDate = quizResult.CompletionDate,
                Answers = userAnswers.Select(ua => new AnswerDetail
                {
                    LessonId = ua.Question.LessonId, // إضافة LessonId إلى AnswerDetail
                    QuestionId = ua.QuestionId.ToString(), // تحويل QuestionId إلى سلسلة نصية
                    QuestionText = ua.Question.QuestionText,
                    SelectedAnswer = ua.SelectedAnswer,
                    CorrectAnswer = ua.Question.CorrectAnswer,
                    IsCorrect = ua.Question.QuestionType == "Text"
                        ? ua.SelectedAnswer.Trim().ToLower() == ua.Question.CorrectAnswer.Trim().ToLower()
                        : ua.SelectedAnswer == ua.Question.CorrectAnswer
                }).ToList()
            };

            // تمرير lessonId واستخراج courseId
            ViewBag.LessonId = lessonId;
            var courseId = _context.Lessons.FirstOrDefault(l => l.LessonId == lessonId)?.CourseId ?? 0;
            ViewBag.CourseId = courseId;

            ViewBag.IsLoggedIn = true;
            ViewBag.IsAdmin = HttpContext.Session.GetString("Role") == "Admin";
            ViewBag.UnreadNotifications = _context.Notifications.Count(n => n.UserId == userId && !n.IsRead);
            ViewBag.UserId = userId;

            return View(viewModel);
        }

        public class QuizResultViewModel
        {
            public string LessonTitle { get; set; }
            public double Score { get; set; }
            public DateTime CompletionDate { get; set; }
            public List<AnswerDetail> Answers { get; set; }
        }

        public class AnswerDetail
        {
            public int LessonId { get; set; }
            public string QuestionId { get; set; }
            public string QuestionText { get; set; }
            public string SelectedAnswer { get; set; }
            public string CorrectAnswer { get; set; }
            public bool IsCorrect { get; set; }
        }
        // ... (باقي الدوال مثل IssueCertificate و GenerateCertificatePdf بدون تغيير بعد)
        private bool CheckCourseCompletion(int userId, int courseId)
        {
            var lessons = _context.Lessons.Where(l => l.CourseId == courseId).ToList();
            var quizResults = _context.QuizResults
                .Where(qr => qr.UserId == userId && lessons.Select(l => l.LessonId).Contains(qr.LessonId))
                .ToList();

            // الحصول على الشروط من قاعدة البيانات
            var passPercentage = _context.CertificateConditions
                .FirstOrDefault(c => c.ConditionType == "PassPercentage")?.Value ?? 70m; // استخدام 70m كـ decimal
            var maxAttempts = _context.CertificateConditions
                .FirstOrDefault(c => c.ConditionType == "MaxAttempts")?.Value ?? 3m; // استخدام 3m كـ decimal

            // تحويل passPercentage إلى double للمقارنة مع Score
            double passPercentageDouble = (double)passPercentage;

            // التحقق من إكمال جميع الدروس بنسبة النجاح
            var completedLessons = lessons.Count(l => quizResults.Any(qr => qr.LessonId == l.LessonId && qr.Score >= passPercentageDouble));

            // التحقق من عدد المحاولات (افتراضي: لا يزيد عن maxAttempts لكل درس)
            var attemptCount = _context.QuizResults
                .Where(qr => qr.UserId == userId && lessons.Select(l => l.LessonId).Contains(qr.LessonId))
                .GroupBy(qr => qr.LessonId)
                .Select(g => new { LessonId = g.Key, Count = g.Count() })
                .All(g => g.Count <= (int)maxAttempts); // تحويل maxAttempts إلى int

            return lessons.Count == completedLessons && attemptCount;
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
            return $"/certificates/{fileName}";
        }

    }   
}